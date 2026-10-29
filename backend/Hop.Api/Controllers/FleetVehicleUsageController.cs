using System.Globalization;
using System.Security.Claims;
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

public sealed record VehicleUsageQuery(DateOnly? StartDate, DateOnly? EndDate, string? Status, string? VehicleId, int Page = 1, int PageSize = 10);
public sealed record VehicleUsageRow(Guid RequestId, Guid? TripId, Guid? VehicleId, string VehicleCode, string RegistrationNumber,
    string RequestNo, DateTime DepartureAt, DateTime ExpectedReturnAt, string Destination, string Status, string Driver,
    decimal? StartMileage, decimal? EndMileage, decimal? DistanceKm, bool IsAborted);
public sealed record VehicleUsageGroup(Guid? VehicleId, string VehicleCode, string RegistrationNumber, int RequestCount, int TripCount, decimal? DistanceKm, int CalculatedTripCount,
    decimal? FirstRecordedMileage = null, DateTime? FirstRecordedMileageAt = null, decimal? LatestRecordedMileage = null, DateTime? LatestRecordedMileageAt = null);
public sealed record VehicleUsageResponse(IReadOnlyList<VehicleUsageGroup> Vehicles, IReadOnlyList<VehicleUsageRow> Items, int Page, int PageSize, int TotalItems, int TotalPages);

[ApiController, Authorize, Route("api/fleet/reports/usage/vehicles")]
public sealed class FleetVehicleUsageController(AppDbContext db, FleetDateRangeService ranges) : ControllerBase
{
    [HttpGet, RequirePermission(FleetPermissions.ReportView)]
    public async Task<ActionResult<ApiResponse<VehicleUsageResponse>>> Get([FromQuery] VehicleUsageQuery query, CancellationToken ct)
    {
        try
        {
            var rows = await Rows(query, ct);
            var groups = rows.GroupBy(x => x.VehicleId).Select(g => new VehicleUsageGroup(g.Key, g.First().VehicleCode,
                g.First().RegistrationNumber, g.Select(x => x.RequestId).Distinct().Count(), g.Count(x => x.TripId != null),
                g.Any(x => x.DistanceKm != null) ? g.Sum(x => x.DistanceKm ?? 0) : null, g.Count(x => x.DistanceKm != null))).ToArray();
            var vehicleIds = groups.Where(x => x.VehicleId != null).Select(x => x.VehicleId!.Value).ToArray();
            // This history intentionally ignores the report's date/status filters.
            var history = await db.FleetTripRecords.AsNoTracking()
                .Where(x => x.Assignment != null && vehicleIds.Contains(x.Assignment.VehicleId))
                .Select(x => new { x.Id, x.Assignment!.VehicleId, x.ActualStartAt, x.ActualEndAt, x.StartMileage, x.EndMileage }).ToListAsync(ct);
            var readings = history.SelectMany(x => new[] {
                new { x.Id, x.VehicleId, At = x.ActualStartAt, Mileage = x.StartMileage, IsEnd = false },
                new { x.Id, x.VehicleId, At = x.ActualEndAt, Mileage = x.EndMileage, IsEnd = true }
            }).Where(x => x.At != null && x.Mileage >= 0).ToLookup(x => x.VehicleId);
            groups = groups.Select(group => {
                if (group.VehicleId is not { } id) return group;
                var first = readings[id].Where(x => !x.IsEnd).OrderBy(x => x.At).ThenBy(x => x.Id).FirstOrDefault();
                var latest = readings[id].OrderByDescending(x => x.At).ThenByDescending(x => x.IsEnd).ThenBy(x => x.Id).FirstOrDefault();
                return group with { FirstRecordedMileage = first?.Mileage, FirstRecordedMileageAt = first?.At,
                    LatestRecordedMileage = latest?.Mileage, LatestRecordedMileageAt = latest?.At };
            }).ToArray();
            var filtered = Filter(rows, query.VehicleId).ToArray();
            var page = Math.Max(1, query.Page); var size = Math.Clamp(query.PageSize, 1, 100);
            return ApiResponse<VehicleUsageResponse>.Ok(new(groups, filtered.Skip((page - 1) * size).Take(size).ToArray(),
                page, size, filtered.Length, (int)Math.Ceiling(filtered.Length / (double)size)));
        }
        catch (ArgumentException ex) { return BadRequest(ApiResponse<object>.Fail(ex.Message)); }
    }

    [HttpGet("export"), RequirePermission(FleetPermissions.ReportExport)]
    public async Task<IActionResult> Export([FromQuery] VehicleUsageQuery query, CancellationToken ct)
    {
        try
        {
            var rows = Filter(await Rows(query, ct), query.VehicleId);
            var csv = new StringBuilder("\uFEFFรหัสรถ,ทะเบียนรถ,เลขคำขอ,วันเวลาเดินทาง (เวลาไทย),กำหนดกลับ (เวลาไทย),จุดหมาย,สถานะคำขอ,คนขับ,เลขไมล์เริ่มต้น,เลขไมล์สิ้นสุด,ระยะทางจริง (กม.),ยุติทริป\r\n");
            foreach (var x in rows)
                csv.AppendLine(string.Join(',', new[] { x.VehicleCode, x.RegistrationNumber, x.RequestNo, Thai(x.DepartureAt), Thai(x.ExpectedReturnAt),
                    x.Destination, StatusLabel(x.Status), x.Driver, Number(x.StartMileage), Number(x.EndMileage), x.DistanceKm is null ? "ไม่รวมคำนวณ" : Number(x.DistanceKm), x.IsAborted ? "ใช่" : "ไม่" }.Select(FleetReportExportService.Escape)));
            var actor = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            db.AuditLogs.Add(new() { UserId = actor, EffectiveActorUserId = actor, Action = "Fleet.ReportExported", EntityName = "FleetVehicleUsageReport",
                EntityId = "vehicles", Detail = System.Text.Json.JsonSerializer.Serialize(query), CorrelationId = HttpContext.TraceIdentifier });
            await db.SaveChangesAsync(ct);
            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", "fleet-vehicle-usage.csv");
        }
        catch (ArgumentException ex) { return BadRequest(ApiResponse<object>.Fail(ex.Message)); }
    }

    private async Task<List<VehicleUsageRow>> Rows(VehicleUsageQuery query, CancellationToken ct)
    {
        var range = ranges.Resolve("custom", query.StartDate, query.EndDate);
        if (!string.IsNullOrEmpty(query.VehicleId) && query.VehicleId != "unassigned" && !Guid.TryParse(query.VehicleId, out _))
            throw new ArgumentException("รถที่เลือกไม่ถูกต้อง");
        var requests = db.FleetRequests.AsNoTracking().Where(x => x.DepartureAt >= range.Start && x.DepartureAt < range.End);
        if (!string.IsNullOrWhiteSpace(query.Status)) requests = requests.Where(x => x.Status == query.Status);
        var data = await requests.Include(x => x.Assignments).ThenInclude(x => x.Vehicle)
            .Include(x => x.Assignments).ThenInclude(x => x.DriverUser).AsSplitQuery().ToListAsync(ct);
        var ids = data.Select(x => x.Id).ToArray();
        var trips = await db.FleetTripRecords.AsNoTracking().Where(x => ids.Contains(x.FleetRequestId) && x.ActualStartAt != null)
            .Include(x => x.Assignment).ThenInclude(x => x!.Vehicle).Include(x => x.DriverUser).ToListAsync(ct);
        var byRequest = trips.ToLookup(x => x.FleetRequestId);
        var result = new List<VehicleUsageRow>();
        foreach (var request in data)
        {
            var actual = byRequest[request.Id].OrderBy(x => x.ActualStartAt).ThenBy(x => x.Id).ToArray();
            if (actual.Length == 0) Add(request, null, request.Assignments.Where(x => x.IsActive).OrderByDescending(x => x.AssignedAt).ThenBy(x => x.Id).FirstOrDefault());
            else foreach (var trip in actual) Add(request, trip, trip.Assignment);
        }
        return result.OrderByDescending(x => x.DepartureAt).ThenBy(x => x.RequestNo).ThenBy(x => x.TripId).ToList();

        void Add(FleetRequest request, FleetTripRecord? trip, FleetAssignment? assignment)
        {
            decimal? distance = trip is { ActualEndAt: not null, IsAborted: false, StartMileage: >= 0, EndMileage: not null }
                && trip.EndMileage >= trip.StartMileage && request.Status is not (FleetRequestStatuses.Cancelled or FleetRequestStatuses.Draft)
                ? trip.EndMileage - trip.StartMileage : null;
            result.Add(new(request.Id, trip?.Id, assignment?.VehicleId, assignment?.Vehicle?.VehicleCode ?? "ยังไม่จัดรถ",
                assignment?.Vehicle?.RegistrationNumber ?? "–", request.RequestNo, request.DepartureAt, request.ExpectedReturnAt, request.Destination,
                request.Status, (trip != null ? trip.DriverUser?.FullName : assignment?.DriverUser?.FullName) ?? "ยังไม่ระบุคนขับ",
                trip?.StartMileage, trip?.EndMileage, distance, trip?.IsAborted ?? false));
        }
    }
    private static IEnumerable<VehicleUsageRow> Filter(IEnumerable<VehicleUsageRow> rows, string? vehicle) => string.IsNullOrEmpty(vehicle) ? rows
        : vehicle == "unassigned" ? rows.Where(x => x.VehicleId == null) : rows.Where(x => x.VehicleId == Guid.Parse(vehicle));
    private static string Number(decimal? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "ยังไม่บันทึก";
    private static string Thai(DateTime value) => value.AddHours(7).ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("th-TH"));
    private static string StatusLabel(string value) => value switch {
        "DRAFT" => "แบบร่าง", "PENDING_DISPATCH" => "รอจัดรถและคนขับ", "PENDING_ADMIN_REVIEW" => "รอหัวหน้าฝ่ายบริหารตรวจสอบ",
        "PENDING_DIRECTOR" => "รอผู้อำนวยการอนุมัติ", "APPROVED" => "อนุมัติแล้ว", "PENDING_DRIVER_ACK" => "รอคนขับรับทราบ",
        "READY" => "พร้อมเดินทาง", "IN_PROGRESS" => "กำลังปฏิบัติงาน", "COMPLETED" => "เสร็จสิ้น", "CANCELLATION_PENDING" => "รอพิจารณายกเลิก",
        "RETURNED" => "ส่งกลับแก้ไข", "REJECTED" => "ไม่รับคำขอ", "CANCELLED" => "ยกเลิก", "ABORTED" => "ยุติการเดินทาง", _ => value };
}
