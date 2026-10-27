using System.Security.Claims;
using System.Globalization;
using System.Text;
using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

public sealed record FleetUsageExportQuery(DateOnly? StartDate, DateOnly? EndDate, string? Status, string? Search);
public sealed record FleetDistanceSummaryQuery(DateOnly? StartDate, DateOnly? EndDate, string? Status);
public sealed record FleetDistanceSummaryResponse(decimal? TotalDistanceKm, int CalculatedTripCount, int IncompleteTripCount);

[ApiController, Authorize, Route("api/fleet/reports/usage")]
public sealed class FleetUsageReportsController(AppDbContext db, FleetDateRangeService ranges) : ControllerBase
{
    [HttpGet("distance-summary"), RequirePermission(FleetPermissions.ReportView)]
    public async Task<ActionResult<ApiResponse<FleetDistanceSummaryResponse>>> DistanceSummary([FromQuery] FleetDistanceSummaryQuery query, CancellationToken ct)
    {
        try
        {
            var range = ranges.Resolve("custom", query.StartDate, query.EndDate);
            // Query trip records directly so assignment history cannot multiply a trip.
            var trips = db.FleetTripRecords.AsNoTracking().Where(x => x.ActualEndAt != null && !x.IsAborted &&
                x.FleetRequest != null && x.FleetRequest.Status != FleetRequestStatuses.Cancelled &&
                x.FleetRequest.Status != FleetRequestStatuses.Draft && x.FleetRequest.DepartureAt >= range.Start && x.FleetRequest.DepartureAt < range.End);
            if (!string.IsNullOrWhiteSpace(query.Status)) trips = trips.Where(x => x.FleetRequest!.Status == query.Status);
            var values = await trips.Select(x => new { x.StartMileage, x.EndMileage }).ToListAsync(ct);
            var valid = values.Where(x => x.StartMileage >= 0 && x.EndMileage >= x.StartMileage).ToArray();
            return ApiResponse<FleetDistanceSummaryResponse>.Ok(new(valid.Length == 0 ? null : valid.Sum(x => x.EndMileage!.Value - x.StartMileage!.Value),
                valid.Length, values.Count - valid.Length));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<FleetDistanceSummaryResponse>.Fail(ex.Message));
        }
    }

    [HttpGet("export"), RequirePermission(FleetPermissions.ReportExport)]
    public async Task<IActionResult> Export([FromQuery] FleetUsageExportQuery query, CancellationToken ct)
    {
        try
        {
            var range = ranges.Resolve("custom", query.StartDate, query.EndDate);
            var start = range.Start;
            var end = range.End;
            var requests = db.FleetRequests.AsNoTracking().Where(x => x.SubmittedAt >= start && x.SubmittedAt < end
                && x.Status != FleetRequestStatuses.Cancelled && x.Status != FleetRequestStatuses.Draft);
            if (!string.IsNullOrWhiteSpace(query.Status)) requests = requests.Where(x => x.Status == query.Status);
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim().ToLower();
                requests = requests.Where(x => x.RequestNo.ToLower().Contains(search) || x.Destination.ToLower().Contains(search)
                    || (x.RequesterDepartment != null && x.RequesterDepartment.Name.ToLower().Contains(search)));
            }

            var rows = await requests
                .Include(x => x.RequesterUser)
                .Include(x => x.RequesterDepartment)
                .Include(x => x.RequestedVehicleType)
                .Include(x => x.Passengers)
                .Include(x => x.Assignments).ThenInclude(x => x.Vehicle)
                .Include(x => x.Assignments).ThenInclude(x => x.DriverUser)
                .Include(x => x.StatusHistories)
                .AsSplitQuery()
                .OrderBy(x => x.SubmittedAt).ThenBy(x => x.RequestNo)
                .ToListAsync(ct);
            var requestIds = rows.Select(x => x.Id).ToArray();
            var trips = await db.FleetTripRecords.AsNoTracking()
                .Where(x => requestIds.Contains(x.FleetRequestId))
                .OrderByDescending(x => x.ActualStartAt)
                .ToListAsync(ct);
            var latestTrips = trips.GroupBy(x => x.FleetRequestId).ToDictionary(x => x.Key, x => x.First());

            var csv = new StringBuilder("\uFEFF");
            csv.AppendLine("รายการคำขอใช้รถ");
            csv.AppendLine($"ช่วงวันที่ยื่น,{Csv(query.StartDate?.ToString("yyyy-MM-dd") ?? "")},{Csv(query.EndDate?.ToString("yyyy-MM-dd") ?? "")}");
            csv.AppendLine($"จำนวนคำขอทั้งหมด,{rows.Count}");
            csv.AppendLine();
            csv.AppendLine("สรุปตามสถานะ,จำนวนคำขอ");
            foreach (var group in rows.GroupBy(x => x.Status).OrderByDescending(x => x.Count()))
                csv.AppendLine($"{Csv(StatusLabel(group.Key))},{group.Count()}");
            csv.AppendLine();
            csv.AppendLine("สรุปตามจุดหมาย,จำนวนคำขอ");
            foreach (var group in rows.GroupBy(x => x.Destination.Trim()).OrderByDescending(x => x.Count()))
                csv.AppendLine($"{Csv(group.Key)},{group.Count()}");
            csv.AppendLine();
            csv.AppendLine("เลขคำขอ,วันที่ยื่น (เวลาไทย),ผู้ขอ,หน่วยงาน,ประเภทภารกิจ,วัตถุประสงค์,จุดหมาย,วันที่เดินทาง (เวลาไทย),กำหนดกลับ (เวลาไทย),ระดับความเร่งด่วน,เหตุผลเร่งด่วน,จำนวนผู้ร่วมเดินทาง,รายชื่อผู้ร่วมเดินทาง,ผู้ติดต่อ,เบอร์โทรผู้ติดต่อ,ประเภทรถที่ขอ,รถที่จัด,ทะเบียนรถ,คนขับ,วันที่เริ่มเดินทางจริง (เวลาไทย),วันที่สิ้นสุดจริง (เวลาไทย),เลขไมล์เริ่มต้น,เลขไมล์สิ้นสุด,ระยะทางจริง (กม.),สถานะปัจจุบัน,การดำเนินการล่าสุด,เวลาการดำเนินการล่าสุด (เวลาไทย),หมายเหตุการดำเนินการล่าสุด,ข้อกำหนดพิเศษ,ชื่อผู้ป่วย Refer (ปกปิด)");
            foreach (var row in rows)
            {
                var assignment = row.Assignments.Where(x => x.IsActive).OrderByDescending(x => x.AssignedAt).FirstOrDefault();
                latestTrips.TryGetValue(row.Id, out var trip);
                var lastHistory = row.StatusHistories.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
                csv.AppendLine(string.Join(',', new[] {
                    row.RequestNo, ThaiDate(row.SubmittedAt!.Value), row.RequesterUser?.FullName ?? "-",
                    row.RequesterDepartment?.Name ?? "-", row.MissionType, row.Purpose, row.Destination,
                    ThaiDate(row.DepartureAt), ThaiDate(row.ExpectedReturnAt), row.Priority, row.UrgentReason ?? row.EmergencyReason ?? "",
                    row.PassengerCount.ToString(CultureInfo.InvariantCulture),
                    string.Join("; ", row.Passengers.OrderBy(x => x.SortOrder).Select(x => x.FullName)),
                    row.ContactPersonName, row.ContactPhone, row.RequestedVehicleType?.Name ?? "",
                    assignment?.Vehicle?.VehicleCode ?? "", assignment?.Vehicle?.RegistrationNumber ?? "",
                    assignment?.DriverUser?.FullName ?? "", ThaiDateOrEmpty(trip?.ActualStartAt), ThaiDateOrEmpty(trip?.ActualEndAt),
                    Number(trip?.StartMileage), Number(trip?.EndMileage),
                    trip?.StartMileage is decimal startMileage && trip.EndMileage is decimal endMileage ? Number(endMileage - startMileage) : "",
                    StatusLabel(row.Status), lastHistory?.Action ?? "", ThaiDateOrEmpty(lastHistory?.CreatedAt), lastHistory?.Reason ?? "",
                    row.SpecialRequirement ?? "",
                    FleetReferPolicy.IsRefer(row) && !string.IsNullOrWhiteSpace(row.PatientName)
                        ? FleetLineGroupMessageTemplateService.MaskPatientName(row.PatientName) : ""
                }.Select(Csv)));
            }

            var actor = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            db.AuditLogs.Add(new() { UserId = actor, EffectiveActorUserId = actor, Action = "Fleet.ReportExported",
                EntityName = "FleetUsageReport", EntityId = "usage", Detail = System.Text.Json.JsonSerializer.Serialize(query),
                CorrelationId = HttpContext.TraceIdentifier });
            await db.SaveChangesAsync(ct);
            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", $"fleet-usage-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
        }
        catch (ArgumentException ex) { return BadRequest(ApiResponse<object>.Fail(ex.Message)); }
    }

    private static string Csv(string value) => FleetReportExportService.Escape(value);
    private static string Number(decimal? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "";
    private static string ThaiDateOrEmpty(DateTime? value) => value is null ? "" : ThaiDate(value.Value);
    private static string StatusLabel(string status) => status switch
    {
        FleetRequestStatuses.PendingDispatch => "รอจัดรถและคนขับ",
        FleetRequestStatuses.PendingAdminReview => "รอหัวหน้าฝ่ายบริหารตรวจสอบ",
        FleetRequestStatuses.PendingDirector => "รอผู้อำนวยการอนุมัติ",
        FleetRequestStatuses.Approved => "อนุมัติแล้ว",
        FleetRequestStatuses.PendingDriverAck => "รอคนขับรับทราบ",
        FleetRequestStatuses.Ready => "พร้อมเดินทาง",
        FleetRequestStatuses.InProgress => "กำลังปฏิบัติงาน",
        FleetRequestStatuses.Completed => "เสร็จสิ้น",
        FleetRequestStatuses.CancellationPending => "รอพิจารณายกเลิก",
        FleetRequestStatuses.Returned => "ส่งกลับแก้ไข",
        FleetRequestStatuses.Rejected => "ไม่รับคำขอ",
        FleetRequestStatuses.Aborted => "ยุติการเดินทาง",
        _ => status
    };
    private static string ThaiDate(DateTime value)
    {
        TimeZoneInfo zone;
        try { zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok"); }
        catch { zone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(value, DateTimeKind.Utc), zone)
            .ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("th-TH"));
    }
}
