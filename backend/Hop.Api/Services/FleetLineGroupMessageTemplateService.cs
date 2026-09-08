using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Hop.Api.Configuration;
using Hop.Api.Data;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed record FleetGroupRenderedMessage(
    string Format,
    string Text,
    string CanonicalEventType,
    Guid FleetRequestId,
    string? FlexContentsJson = null,
    string? AltText = null);

public interface IFleetLineGroupMessageTemplateService
{
    Task<FleetGroupRenderedMessage?> RenderAsync(DomainEventRecord domainEvent, string canonicalEventType, CancellationToken ct);
}

public sealed partial class FleetLineGroupMessageTemplateService(
    AppDbContext db,
    LineConfigurationResolver lineConfiguration) : IFleetLineGroupMessageTemplateService
{
    private static readonly IReadOnlyDictionary<string, (string Icon, string Title, string Status)> EventLabels =
        new Dictionary<string, (string, string, string)>(StringComparer.Ordinal)
        {
            ["Fleet.RequestSubmitted"] = ("🚐", "มีคำขอใช้รถใหม่", "รอจัดรถและคนขับ"),
            ["Fleet.AssignmentCreated"] = ("🚘", "จัดรถและคนขับแล้ว", "รอตรวจสอบคำขอ"),
            ["Fleet.AdminReviewed"] = ("📋", "ตรวจคำขอเรียบร้อย", "รอผู้อำนวยการอนุมัติ"),
            ["Fleet.Returned"] = ("↩️", "คำขอถูกส่งกลับ", "ส่งกลับแก้ไข"),
            ["Fleet.DirectorApproved"] = ("✅", "คำขอใช้รถได้รับอนุมัติ", "อนุมัติแล้ว"),
            ["Fleet.Rejected"] = ("❌", "คำขอใช้รถไม่ผ่านการพิจารณา", "ไม่อนุมัติ"),
            ["Fleet.Cancelled"] = ("🚫", "คำขอใช้รถถูกยกเลิก", "ยกเลิกแล้ว"),
            ["Fleet.AssignmentChanged"] = ("🔄", "มีการเปลี่ยนรถหรือคนขับ", "เปลี่ยนการจัดรถ"),
            ["Fleet.DriverAcknowledged"] = ("👤", "คนขับรับทราบงานแล้ว", "คนขับรับทราบ"),
            ["Fleet.TripCompleted"] = ("🏁", "ภารกิจเสร็จสิ้น", "เสร็จสิ้น"),
            ["Fleet.TripOverdue"] = ("⚠️", "ภารกิจเกินกำหนด", "เกินกำหนด")
        };

    public async Task<FleetGroupRenderedMessage?> RenderAsync(DomainEventRecord domainEvent, string canonicalEventType, CancellationToken ct)
    {
        if (!EventLabels.TryGetValue(canonicalEventType, out var label) || !string.Equals(domainEvent.Scope, "FLEET", StringComparison.Ordinal)) return null;
        var request = await db.FleetRequests.AsNoTracking()
            .Include(x => x.RequesterUser).Include(x => x.RequesterDepartment)
            .Include(x => x.Assignments).ThenInclude(x => x.Vehicle)
            .Include(x => x.Assignments).ThenInclude(x => x.DriverUser)
            .SingleOrDefaultAsync(x => x.Id == domainEvent.AggregateId, ct);
        if (request is null) return null;

        var actorName = domainEvent.ActorUserId is null ? null : await db.Users.AsNoTracking()
            .Where(x => x.Id == domainEvent.ActorUserId).Select(x => x.FullName).SingleOrDefaultAsync(ct);
        var assignment = request.Assignments.SingleOrDefault(x => x.IsActive);
        if (canonicalEventType == "Fleet.TripCompleted")
        {
            var tripAssignmentId = await db.FleetTripRecords.AsNoTracking()
                .Where(x => x.FleetRequestId == request.Id)
                .OrderByDescending(x => x.ActualEndAt)
                .ThenByDescending(x => x.CreatedAt)
                .Select(x => (Guid?)x.AssignmentId)
                .FirstOrDefaultAsync(ct);
            assignment = tripAssignmentId is null
                ? assignment
                : request.Assignments.SingleOrDefault(x => x.Id == tripAssignmentId.Value) ?? assignment;
        }
        assignment ??= request.Assignments
            .OrderByDescending(x => x.AssignedAt)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefault();
        var builder = new StringBuilder()
            .AppendLine($"{label.Icon} {label.Title}")
            .AppendLine($"เลขที่: {Safe(request.RequestNo, 40)}")
            .AppendLine($"ผู้ขอ: {Safe(request.RequesterUser?.FullName, 120)}")
            .AppendLine($"หน่วยงาน: {Safe(request.RequesterDepartment?.Name, 160)}")
            .AppendLine($"เดินทาง: {FormatBangkok(request.DepartureAt)}")
            .AppendLine($"ปลายทาง: {Safe(request.Destination, 180)}")
            .AppendLine($"ผู้ร่วมเดินทาง: {request.PassengerCount} คน")
            .AppendLine($"สถานะ: {label.Status}");

        if (canonicalEventType == "Fleet.AssignmentChanged")
            AppendAssignmentChange(builder, request.Assignments, domainEvent.Payload);
        else if (canonicalEventType is "Fleet.AssignmentCreated" or "Fleet.DirectorApproved" or "Fleet.DriverAcknowledged" or "Fleet.TripCompleted" or "Fleet.TripOverdue")
            AppendAssignment(builder, assignment, "รถ/คนขับ");

        if (ShouldShowActor(canonicalEventType) && !string.IsNullOrWhiteSpace(actorName))
            builder.AppendLine($"ดำเนินการโดย: {Safe(actorName, 120)}");

        var deepLink = BuildDeepLink(request.Id);
        builder.Append($"ดูรายละเอียด: {deepLink}");
        var text = builder.ToString();
        if (text.Length > 1800) text = text[..1797] + "...";
        var assignmentText = assignment is null
            ? "ยังไม่ได้จัดรถและคนขับ"
            : $"{Safe(assignment.Vehicle?.VehicleCode, 60)} · {Safe(assignment.Vehicle?.RegistrationNumber, 40)} / {Safe(assignment.DriverUser?.FullName, 120)}";
        var flex = BuildFlexContents(label.Icon, label.Title, label.Status, request, assignmentText, actorName, canonicalEventType, deepLink);
        return new FleetGroupRenderedMessage("text", text, canonicalEventType, request.Id, flex, $"{label.Title} · {request.RequestNo}");
    }

    private static string BuildFlexContents(
        string icon,
        string title,
        string status,
        FleetRequest request,
        string assignment,
        string? actorName,
        string eventType,
        string deepLink)
    {
        var rows = new List<object>
        {
            FlexRow("เลขที่คำขอ", Safe(request.RequestNo, 40), true),
            FlexRow("ผู้ขอ", Safe(request.RequesterUser?.FullName, 120)),
            FlexRow("หน่วยงาน", Safe(request.RequesterDepartment?.Name, 160)),
            FlexRow("วันเวลาเดินทาง", FormatBangkok(request.DepartureAt)),
            FlexRow("ปลายทาง", Safe(request.Destination, 180)),
            FlexRow("ผู้ร่วมเดินทาง", $"{request.PassengerCount} คน")
        };
        if (eventType is "Fleet.AssignmentCreated" or "Fleet.DirectorApproved" or "Fleet.DriverAcknowledged" or "Fleet.TripCompleted" or "Fleet.TripOverdue" or "Fleet.AssignmentChanged")
            rows.Add(FlexRow("รถ / คนขับ", assignment));
        if (ShouldShowActor(eventType) && !string.IsNullOrWhiteSpace(actorName))
            rows.Add(FlexRow("ดำเนินการโดย", Safe(actorName, 120)));

        var statusStyle = StatusStyle(eventType);
        var bodyContents = new List<object>
        {
            new
            {
                type = "box",
                layout = "vertical",
                backgroundColor = statusStyle.Background,
                cornerRadius = "10px",
                paddingAll = "14px",
                spacing = "xs",
                contents = new object[]
                {
                    new { type = "text", text = "สถานะปัจจุบัน", size = "xs", color = statusStyle.Foreground, weight = "bold" },
                    new { type = "box", layout = "horizontal", alignItems = "center", spacing = "sm", contents = new object[]
                    {
                        new { type = "text", text = statusStyle.Symbol, size = "lg", color = statusStyle.Foreground, flex = 0 },
                        new { type = "text", text = status, size = "md", color = statusStyle.Foreground, weight = "bold", wrap = true, flex = 1 }
                    }}
                }
            },
            new { type = "separator", color = "#E7E3D8", margin = "md" }
        };
        bodyContents.AddRange(rows);

        var bubble = new
        {
            type = "bubble",
            size = "kilo",
            styles = new
            {
                header = new { backgroundColor = "#155E4B" },
                footer = new { separator = true, separatorColor = "#E4D3A2" }
            },
            header = new
            {
                type = "box",
                layout = "vertical",
                paddingAll = "18px",
                contents = new object[]
                {
                    new { type = "text", text = "HOP · ระบบจองรถ", color = "#E8D29B", size = "xs", weight = "bold" },
                    new { type = "text", text = $"{icon} {title}", color = "#FFFFFF", size = "lg", weight = "bold", margin = "sm", wrap = true }
                }
            },
            body = new { type = "box", layout = "vertical", paddingAll = "18px", spacing = "md", contents = bodyContents },
            footer = new
            {
                type = "box",
                layout = "vertical",
                paddingAll = "14px",
                contents = new object[]
                {
                    new { type = "button", style = "primary", color = "#155E4B", height = "sm", action = new { type = "uri", label = "ดูรายละเอียดคำขอ", uri = deepLink } }
                }
            }
        };
        return JsonSerializer.Serialize(bubble);
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

    private static (string Background, string Foreground, string Symbol) StatusStyle(string eventType) => eventType switch
    {
        "Fleet.DirectorApproved" or "Fleet.DriverAcknowledged" or "Fleet.TripCompleted" => ("#E5F5EE", "#0B6B4F", "●"),
        "Fleet.Rejected" or "Fleet.Cancelled" or "Fleet.TripOverdue" => ("#FDECEC", "#B42318", "!"),
        "Fleet.Returned" or "Fleet.AssignmentChanged" => ("#F0EAFE", "#6941C6", "↻"),
        _ => ("#FFF3D6", "#8A5A00", "●")
    };

    private static void AppendAssignmentChange(StringBuilder builder, IEnumerable<FleetAssignment> assignments, string payload)
    {
        Guid? previousId = null; Guid? currentId = null;
        try
        {
            using var json = JsonDocument.Parse(payload);
            if (json.RootElement.TryGetProperty("PreviousAssignmentId", out var previous) && previous.TryGetGuid(out var parsedPrevious)) previousId = parsedPrevious;
            if (json.RootElement.TryGetProperty("NewAssignmentId", out var current) && current.TryGetGuid(out var parsedCurrent)) currentId = parsedCurrent;
        }
        catch (JsonException) { }
        var rows = assignments.ToList();
        AppendAssignment(builder, previousId is null ? null : rows.SingleOrDefault(x => x.Id == previousId), "เดิม");
        AppendAssignment(builder, currentId is null ? rows.SingleOrDefault(x => x.IsActive) : rows.SingleOrDefault(x => x.Id == currentId), "ใหม่");
    }

    private static void AppendAssignment(StringBuilder builder, FleetAssignment? assignment, string prefix)
    {
        var vehicle = assignment?.Vehicle is null ? "ยังไม่ระบุ" : $"{Safe(assignment.Vehicle.VehicleCode, 60)} ({Safe(assignment.Vehicle.RegistrationNumber, 40)})";
        var driver = assignment?.DriverUser is null ? "ยังไม่ระบุ" : Safe(assignment.DriverUser.FullName, 120);
        builder.AppendLine($"{prefix}: {vehicle} · {driver}");
    }

    private string BuildDeepLink(Guid requestId)
    {
        var root = (lineConfiguration.PublicAppUrl ?? string.Empty).TrimEnd('/');
        return string.IsNullOrWhiteSpace(root) ? $"/fleet/requests/{requestId}" : $"{root}/fleet/requests/{requestId}";
    }

    private static string FormatBangkok(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        TimeZoneInfo zone;
        try { zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok"); }
        catch (TimeZoneNotFoundException) { zone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, zone);
        return $"{local:dd/MM}/{local.Year + 543} {local:HH:mm} น.";
    }

    private static bool ShouldShowActor(string eventType) => eventType is
        "Fleet.AssignmentCreated" or "Fleet.AdminReviewed" or "Fleet.Returned" or "Fleet.DirectorApproved" or
        "Fleet.Rejected" or "Fleet.Cancelled" or "Fleet.AssignmentChanged";

    private static string Safe(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return "-";
        var sanitized = ControlCharacters().Replace(value.Trim(), " ");
        sanitized = SensitiveNumber().Replace(sanitized, "[ข้อมูลถูกปกปิด]");
        return sanitized[..Math.Min(sanitized.Length, maxLength)];
    }

    [GeneratedRegex(@"[\u0000-\u001F\u007F]+")]
    private static partial Regex ControlCharacters();
    [GeneratedRegex(@"\b\d{13}\b|\b\d{9,12}\b")]
    private static partial Regex SensitiveNumber();
}
