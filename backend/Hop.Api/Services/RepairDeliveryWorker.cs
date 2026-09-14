using System.Text.Encodings.Web;
using System.Text.Json;
using Hop.Api.Configuration;
using Hop.Api.Data;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed class RepairDeliveryWorker(IServiceScopeFactory scopes, IConfiguration config, ILogger<RepairDeliveryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (config.GetValue<bool>("Repairs:NotificationsEnabled"))
                {
                    using var scope = scopes.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<RepairDeliveryService>().ProcessAsync(stoppingToken);
                }
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogError("Repair notification worker failed. ErrorType={ErrorType}", ex.GetType().Name);
                try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            }
        }
    }
}

public sealed class RepairDeliveryService(AppDbContext db, ILineGroupPushClient client,
    LineConfigurationResolver urls, IConfiguration config)
{
    public async Task ProcessAsync(CancellationToken ct)
    {
        var ids = await db.Set<RepairDispatch>().AsNoTracking().Where(x => x.Status == "Pending" && x.AvailableAt <= DateTime.UtcNow)
            .OrderBy(x => x.AvailableAt).Select(x => x.Id).Take(20).ToListAsync(ct);
        foreach (var id in ids)
        {
            var job = await db.Set<RepairDispatch>().SingleAsync(x => x.Id == id, ct);
            if (job.Status != "Pending") continue;
            var r = await db.Set<RepairRequest>().AsNoTracking().SingleAsync(x => x.Id == job.RequestId, ct);
            if (r.TeamCode != job.TeamCode)
            {
                job.Status = "Superseded"; job.ConcurrencyToken = Guid.NewGuid();
                try { await db.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); }
                continue;
            }
            var destination = await db.LineGroupDestinations.AsNoTracking().SingleOrDefaultAsync(x => x.Module == "REPAIR_" + job.TeamCode && x.Status == "Active", ct);
            var root = urls.PublicAppUrl?.TrimEnd('/');
            var allowedHosts = config.GetSection("Repairs:AllowedNotificationHosts").Get<string[]>() ?? [];
            if (destination is null || !Uri.TryCreate(destination.EndpointUrl, UriKind.Absolute, out var endpoint) ||
                endpoint.Scheme != "https" || endpoint.IsLoopback || endpoint.UserInfo != "" || !allowedHosts.Contains(endpoint.Host, StringComparer.OrdinalIgnoreCase) ||
                !Uri.TryCreate(root, UriKind.Absolute, out var app) || app.Scheme != "https" || app.IsLoopback)
            {
                job.ErrorCode = "CONFIGURATION_REQUIRED"; job.AvailableAt = DateTime.UtcNow.AddMinutes(5); job.ConcurrencyToken = Guid.NewGuid();
                try { await db.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); }
                continue;
            }
            // Claim before sending. An ambiguous/crashed send is not automatically replayed.
            job.Status = "Sending"; job.Attempts++; job.ErrorCode = null; job.ConcurrencyToken = Guid.NewGuid();
            try { await db.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); continue; }
            var link = $"{root}/repairs/{r.Id}";
            var categoryName = await db.Set<RepairCategory>().AsNoTracking()
                .Where(x => x.Id == r.CategoryId).Select(x => x.Name).SingleOrDefaultAsync(ct);
            var requesterName = await db.Users.AsNoTracking()
                .Where(x => x.Id == r.RequesterId).Select(x => x.FullName).SingleOrDefaultAsync(ct);
            var departmentName = r.DepartmentId is null ? null : await db.Departments.AsNoTracking()
                .Where(x => x.Id == r.DepartmentId).Select(x => x.Name).SingleOrDefaultAsync(ct);
            var teamLabel = job.TeamCode == "IT" ? "ทีม IT" : "ทีมช่างทั่วไป";
            var repairEvent = await db.Set<RepairEvent>().AsNoTracking()
                .Where(x => x.Id == job.EventId)
                .Select(x => new { x.Action, x.ActorId, x.SolverId, x.Priority, x.CreatedAt })
                .SingleAsync(ct);
            var presentation = EventPresentation(repairEvent.Action);
            var personId = repairEvent.Action == "solve" ? repairEvent.SolverId ?? repairEvent.ActorId : repairEvent.ActorId;
            var personName = await db.Users.AsNoTracking()
                .Where(x => x.Id == personId).Select(x => x.FullName).SingleOrDefaultAsync(ct);
            var personLabel = repairEvent.Action == "solve" ? "ผู้แก้ไขหลัก"
                : repairEvent.Action == "accept" ? "ผู้ตรวจรับ"
                : "ผู้ดำเนินการ";
            var text = $"{presentation.Icon} {presentation.Label} REP-{r.Number:D6}\n{Safe(r.Title, 120)} · {teamLabel}\n{presentation.StatusText}\n📍 {Safe(r.Location, 100)}\nดูรายละเอียด: {link}";
            var flex = JsonSerializer.Serialize(new {
                type = "bubble",
                size = "kilo",
                styles = new {
                    header = new { backgroundColor = "#155E4B" },
                    footer = new { separator = true, separatorColor = "#E4D3A2" }
                },
                header = new { type = "box", layout = "vertical", paddingAll = "18px", contents = new object[] {
                    new { type = "text", text = "HOP · ระบบแจ้งซ่อม", color = "#E8D29B", size = "xs", weight = "bold" },
                    new { type = "text", text = $"{presentation.Icon} {presentation.Label}", color = "#FFFFFF", size = "lg", weight = "bold", margin = "sm", wrap = true }
                }},
                body = new { type = "box", layout = "vertical", paddingAll = "18px", spacing = "md", contents = new object[] {
                    new { type = "box", layout = "vertical", backgroundColor = presentation.BackgroundColor, cornerRadius = "10px", paddingAll = "14px", spacing = "xs", contents = new object[] {
                        new { type = "text", text = "สถานะจากเหตุการณ์ล่าสุด", size = "xs", color = presentation.TextColor, weight = "bold" },
                        new { type = "text", text = presentation.StatusText, size = "md", color = presentation.TextColor, weight = "bold", wrap = true }
                    }},
                    new { type = "separator", color = "#E7E3D8" },
                    FlexRow("🎫 เลขที่ใบงาน", $"REP-{r.Number:D6}", true),
                    FlexRow("📝 หัวข้องาน", Safe(r.Title, 140)),
                    FlexRow("🧰 ประเภทงาน", Safe(categoryName, 100)),
                    FlexRow("👥 ทีมรับผิดชอบ", teamLabel),
                    FlexRow("📍 สถานที่", Safe(r.Location, 120)),
                    FlexRow("🏥 หน่วยงานผู้แจ้ง", Safe(departmentName, 140)),
                    FlexRow("👤 ผู้แจ้ง", Safe(requesterName, 100)),
                    FlexRow($"👷 {personLabel}", Safe(personName, 100)),
                    FlexRow("⏰ อัปเดตเมื่อ", FormatBangkok(repairEvent.CreatedAt)),
                    FlexRow("⚡ ความเร่งด่วน", PriorityLabel(repairEvent.Priority))
                }},
                footer = new { type = "box", layout = "vertical", paddingAll = "14px", contents = new[] {
                    new { type = "button", style = "primary", color = "#155E4B", height = "sm", action = new { type = "uri", label = "เปิดดูรายละเอียดใบงาน", uri = link } }
                }}
            }, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            var result = await client.PushMessageAsync(destination, new FleetGroupRenderedMessage("Flex", text, presentation.EventName, r.Id, flex, $"{presentation.Label} · REP-{r.Number:D6}"), ct);
            job.ErrorCode = result.ErrorCode;
            if (result.Success) { job.Status = "Sent"; job.SentAt = DateTime.UtcNow; }
            else if (result.IsTransient && result.ErrorCode != "CUSTOM_ENDPOINT_NETWORK_ERROR" && job.Attempts < 3)
            { job.Status = "Pending"; job.AvailableAt = DateTime.UtcNow.AddMinutes(job.Attempts); }
            else job.Status = "Attention";
            job.ConcurrencyToken = Guid.NewGuid();
            await db.SaveChangesAsync(ct);
        }
    }

    private static RepairEventPresentation EventPresentation(string action) => action switch
    {
        "start" => new("Repair.Started", "เริ่มดำเนินการ", "🛠️", "🛠️ กำลังดำเนินการ", "#E8F2FF", "#175EA8"),
        "resume" => new("Repair.Resumed", "กลับมาดำเนินการ", "▶️", "▶️ กลับมาดำเนินการแล้ว", "#E8F2FF", "#175EA8"),
        "reject-solution" => new("Repair.SolutionRejected", "ดำเนินการแก้ไขเพิ่มเติม", "🔁", "🔁 ผู้แจ้งขอให้แก้ไขเพิ่มเติม", "#FFF4DB", "#8A5A00"),
        "solve" => new("Repair.Solved", "ช่างซ่อมเสร็จ", "✅", "✅ ซ่อมเสร็จ รอตรวจรับ", "#E5F5EE", "#0B6B4F"),
        "accept" => new("Repair.Closed", "ปิดใบงาน", "🏁", "🏁 ผู้แจ้งตรวจรับและปิดใบงานแล้ว", "#E5F5EE", "#0B6B4F"),
        "reopen" => new("Repair.Reopened", "เปิดงานซ่อมอีกครั้ง", "🔄", "🔄 เปิดรอบซ่อมใหม่แล้ว", "#FFF4DB", "#8A5A00"),
        "resubmit" => new("Repair.Resubmitted", "ส่งงานซ่อมใหม่", "📨", "📨 ส่งเข้าคิวทีมใหม่แล้ว", "#E5F5EE", "#0B6B4F"),
        _ => new("Repair.Submitted", "งานแจ้งซ่อมใหม่", "🔧", "🆕 ส่งเข้าคิวแล้ว", "#E5F5EE", "#0B6B4F")
    };

    private sealed record RepairEventPresentation(string EventName, string Label, string Icon, string StatusText,
        string BackgroundColor, string TextColor);

    private static object FlexRow(string label, string value, bool highlight = false) => new
    {
        type = "box",
        layout = "vertical",
        spacing = "xs",
        contents = new object[]
        {
            new { type = "text", text = label, size = "xs", color = "#718096" },
            new { type = "text", text = value, size = "sm", color = highlight ? "#155E4B" : "#1F2937", weight = highlight ? "bold" : "regular", wrap = true }
        }
    };

    private static string PriorityLabel(string? priority) => priority switch
    {
        "Emergency" => "🔴 ฉุกเฉิน",
        "Urgent" => "🟠 เร่งด่วน",
        "Normal" => "🟢 ปกติ",
        _ => "⚪ ยังไม่ประเมิน"
    };

    private static string FormatBangkok(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        TimeZoneInfo zone;
        try { zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok"); }
        catch (TimeZoneNotFoundException) { zone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, zone);
        return $"{local:dd/MM}/{local.Year + 543} {local:HH:mm} น.";
    }

    private static string Safe(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return "-";
        var sanitized = string.Concat(value.Trim().Select(character => char.IsControl(character) ? ' ' : character));
        return sanitized[..Math.Min(sanitized.Length, maxLength)];
    }
}
