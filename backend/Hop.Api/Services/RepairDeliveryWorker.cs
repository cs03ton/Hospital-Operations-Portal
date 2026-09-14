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
            var eventAction = await db.Set<RepairEvent>().AsNoTracking()
                .Where(x => x.Id == job.EventId).Select(x => x.Action).SingleOrDefaultAsync(ct);
            var eventLabel = eventAction == "reopen" ? "เปิดงานซ่อมอีกครั้ง" : eventAction == "resubmit" ? "ส่งงานซ่อมใหม่" : "งานแจ้งซ่อมใหม่";
            var text = $"🔧 {eventLabel} REP-{r.Number:D6}\n{Safe(r.Title, 120)} · {teamLabel}\n📍 {Safe(r.Location, 100)}\nดูรายละเอียด: {link}";
            var flex = JsonSerializer.Serialize(new {
                type = "bubble",
                size = "kilo",
                styles = new {
                    header = new { backgroundColor = "#155E4B" },
                    footer = new { separator = true, separatorColor = "#E4D3A2" }
                },
                header = new { type = "box", layout = "vertical", paddingAll = "18px", contents = new object[] {
                    new { type = "text", text = "HOP · ระบบแจ้งซ่อม", color = "#E8D29B", size = "xs", weight = "bold" },
                    new { type = "text", text = $"🔧 {eventLabel}", color = "#FFFFFF", size = "lg", weight = "bold", margin = "sm", wrap = true }
                }},
                body = new { type = "box", layout = "vertical", paddingAll = "18px", spacing = "md", contents = new object[] {
                    new { type = "box", layout = "vertical", backgroundColor = "#E5F5EE", cornerRadius = "10px", paddingAll = "14px", spacing = "xs", contents = new object[] {
                        new { type = "text", text = "สถานะปัจจุบัน", size = "xs", color = "#0B6B4F", weight = "bold" },
                        new { type = "text", text = "🆕 ส่งเข้าคิวแล้ว", size = "md", color = "#0B6B4F", weight = "bold", wrap = true }
                    }},
                    new { type = "separator", color = "#E7E3D8" },
                    FlexRow("🎫 เลขที่ใบงาน", $"REP-{r.Number:D6}", true),
                    FlexRow("📝 หัวข้องาน", Safe(r.Title, 140)),
                    FlexRow("🧰 ประเภทงาน", Safe(categoryName, 100)),
                    FlexRow("👥 ทีมรับผิดชอบ", teamLabel),
                    FlexRow("📍 สถานที่", Safe(r.Location, 120)),
                    FlexRow("🏥 หน่วยงานผู้แจ้ง", Safe(departmentName, 140)),
                    FlexRow("👤 ผู้แจ้ง", Safe(requesterName, 100)),
                    FlexRow("⏰ แจ้งเมื่อ", FormatBangkok(r.CreatedAt)),
                    FlexRow("⚡ ความเร่งด่วน", PriorityLabel(r.Priority))
                }},
                footer = new { type = "box", layout = "vertical", paddingAll = "14px", contents = new[] {
                    new { type = "button", style = "primary", color = "#155E4B", height = "sm", action = new { type = "uri", label = "เปิดดูรายละเอียดใบงาน", uri = link } }
                }}
            });
            var result = await client.PushMessageAsync(destination, new FleetGroupRenderedMessage("Flex", text, "Repair.Submitted", r.Id, flex, $"{eventLabel} · REP-{r.Number:D6}"), ct);
            job.ErrorCode = result.ErrorCode;
            if (result.Success) { job.Status = "Sent"; job.SentAt = DateTime.UtcNow; }
            else if (result.IsTransient && result.ErrorCode != "CUSTOM_ENDPOINT_NETWORK_ERROR" && job.Attempts < 3)
            { job.Status = "Pending"; job.AvailableAt = DateTime.UtcNow.AddMinutes(job.Attempts); }
            else job.Status = "Attention";
            job.ConcurrencyToken = Guid.NewGuid();
            await db.SaveChangesAsync(ct);
        }
    }

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
