using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed class FleetAvailabilityService(AppDbContext db, IFleetCompatibilityService compatibility)
{
    public FleetAvailabilityService(AppDbContext db) : this(db, new FleetCompatibilityService(db)) { }
    private static readonly string[] BlockingRequestStatuses = [FleetRequestStatuses.PendingDispatch, FleetRequestStatuses.PendingAdminReview, FleetRequestStatuses.PendingDirector, FleetRequestStatuses.Approved, FleetRequestStatuses.PendingDriverAck, FleetRequestStatuses.Ready, FleetRequestStatuses.InProgress, FleetRequestStatuses.CancellationPending];

    public async Task<FleetAvailabilityResponse> GetAsync(FleetRequest request, CancellationToken cancellationToken)
    {
        var monthStart = new DateTime(request.DepartureAt.Year, request.DepartureAt.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);
        var assignments = await db.FleetAssignments.AsNoTracking().Include(x => x.FleetRequest)
            .Where(x => x.IsActive && x.FleetRequestId != request.Id && x.FleetRequest != null && BlockingRequestStatuses.Contains(x.FleetRequest.Status))
            .ToListAsync(cancellationToken);

        var vehicles = await db.FleetVehicles.AsNoTracking().Include(x => x.VehicleType).Include(x => x.UnavailabilityPeriods).OrderBy(x => x.VehicleCode).ToListAsync(cancellationToken);
        var maintenanceBlocks = await db.FleetVehicleMaintenanceSchedules.AsNoTracking().Include(x => x.MaintenanceType).Include(x => x.Vehicle).Where(x => x.IsActive && (x.Status == FleetMaintenanceStatuses.InProgress || x.MaintenanceType!.BlocksAvailabilityWhenOverdue && (x.DueDate < DateTime.UtcNow || x.DueMileage < x.Vehicle!.CurrentMileage))).Select(x => x.VehicleId).Distinct().ToListAsync(cancellationToken);
        var expiredDocuments = await db.FleetVehicleDocuments.AsNoTracking().Where(x => x.IsActive && x.IsRequired && x.ExpiresAt < DateTime.UtcNow).Select(x => x.VehicleId).Distinct().ToListAsync(cancellationToken);
        var compatibilityResults = new Dictionary<Guid,FleetCompatibilityResult>();
        foreach (var vehicle in vehicles) compatibilityResults[vehicle.Id] = await compatibility.EvaluateAsync(request.Id, vehicle.Id, request.DepartureAt, cancellationToken);
        var vehicleResults = vehicles.Select(vehicle =>
        {
            var reasons = new List<string>();
            if (!vehicle.IsActive) reasons.Add("รถถูกปิดใช้งาน");
            if (vehicle.Status is FleetVehicleStatuses.Maintenance or FleetVehicleStatuses.TemporarilyUnavailable or FleetVehicleStatuses.Decommissioned) reasons.Add("สถานะรถไม่พร้อมใช้งาน");
            if (vehicle.PassengerCapacity < request.PassengerCount) reasons.Add("ความจุผู้โดยสารไม่เพียงพอ");
            if (request.RequestedVehicleTypeId is not null && vehicle.VehicleTypeId != request.RequestedVehicleTypeId) reasons.Add("ประเภทรถไม่ตรงความต้องการ");
            if (vehicle.UnavailabilityPeriods.Any(x => Overlaps(x.StartAt, x.EndAt, request.DepartureAt, request.ExpectedReturnAt))) reasons.Add("รถมีช่วงงดใช้งานชนเวลา");
            if (maintenanceBlocks.Contains(vehicle.Id)) reasons.Add("รถอยู่ระหว่างหรือเกินกำหนดบำรุงรักษาที่บล็อกการใช้งาน");
            if (expiredDocuments.Contains(vehicle.Id)) reasons.Add("เอกสารบังคับของรถหมดอายุ");
            if (assignments.Any(x => x.VehicleId == vehicle.Id && Overlaps(x.FleetRequest!.DepartureAt, x.FleetRequest.ExpectedReturnAt, request.DepartureAt, request.ExpectedReturnAt))) reasons.Add("รถมีงานชนเวลา");
            var compatibilityResult = compatibilityResults[vehicle.Id];
            if (compatibilityResult.Status == FleetCompatibilityStatuses.NotMatch) reasons.AddRange(compatibilityResult.Capabilities.Where(x=>x.IsMandatory&&x.Status==FleetCompatibilityStatuses.NotMatch).Select(x=>$"Compatibility: {x.Code} - {x.Reason}"));
            if (compatibilityResult.Status == FleetCompatibilityStatuses.PartialMatch) reasons.Add("รถตรงความต้องการเพียงบางส่วน");
            var related = assignments.Where(x => x.VehicleId == vehicle.Id).ToList();
            return new FleetAvailabilityItem(vehicle.Id, vehicle.VehicleCode, $"{vehicle.RegistrationNumber} · {vehicle.VehicleType?.Name}", reasons.Count == 0, reasons, related.Count(x => x.AssignedAt >= monthStart && x.AssignedAt < monthEnd), related.MaxBy(x => x.AssignedAt)?.AssignedAt);
        }).ToList();

        var profiles = await db.FleetDriverProfiles.AsNoTracking().Include(x => x.User).ToListAsync(cancellationToken);
        var driverUnavailability = await db.FleetDriverUnavailability.AsNoTracking().Where(x => x.StartAt < request.ExpectedReturnAt && x.EndAt > request.DepartureAt).ToListAsync(cancellationToken);
        var bangkok = ResolveBangkokTimeZone();
        var localStart = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(request.DepartureAt, bangkok));
        var localEnd = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(request.ExpectedReturnAt, bangkok));
        var approvedLeaveUsers = await db.LeaveRequests.AsNoTracking().Where(x => x.Status == "Approved" && x.StartDate <= localEnd && x.EndDate >= localStart).Select(x => x.UserId).Distinct().ToListAsync(cancellationToken);
        var requestedTypeCode = request.RequestedVehicleTypeId is null ? null : await db.FleetVehicleTypes.Where(x => x.Id == request.RequestedVehicleTypeId).Select(x => x.Code).FirstOrDefaultAsync(cancellationToken);
        var driverResults = profiles.Select(profile =>
        {
            var reasons = new List<string>();
            if (!profile.IsActive || profile.User is null || !profile.User.IsActive) reasons.Add("คนขับถูกปิดใช้งาน");
            if (profile.DriverStatus != FleetDriverStatuses.Available) reasons.Add("สถานะคนขับไม่พร้อม");
            if (profile.LicenseExpiryDate is not null && profile.LicenseExpiryDate < localEnd) reasons.Add("ใบขับขี่หมดอายุก่อนจบภารกิจ");
            if (!CanDrive(profile, requestedTypeCode)) reasons.Add("ไม่มีสิทธิ์ขับรถประเภทที่ขอ");
            if (driverUnavailability.Any(x => x.UserId == profile.UserId)) reasons.Add("คนขับมีช่วงไม่พร้อมใช้งานชนเวลา");
            if (approvedLeaveUsers.Contains(profile.UserId)) reasons.Add("คนขับมีใบลาอนุมัติชนช่วงเดินทาง");
            if (assignments.Any(x => x.DriverUserId == profile.UserId && Overlaps(x.FleetRequest!.DepartureAt, x.FleetRequest.ExpectedReturnAt, request.DepartureAt, request.ExpectedReturnAt))) reasons.Add("คนขับมีงานชนเวลา");
            var related = assignments.Where(x => x.DriverUserId == profile.UserId).ToList();
            return new FleetAvailabilityItem(profile.UserId, profile.User?.EmployeeCode ?? "-", profile.User?.FullName ?? "-", reasons.Count == 0, reasons, related.Count(x => x.AssignedAt >= monthStart && x.AssignedAt < monthEnd), related.MaxBy(x => x.AssignedAt)?.AssignedAt);
        }).ToList();

        return new(vehicleResults, driverResults);
    }

    private static bool Overlaps(DateTime startA, DateTime endA, DateTime startB, DateTime endB) => startA < endB && endA > startB;
    private static bool CanDrive(FleetDriverProfile p, string? code) => code switch { "SEDAN" => p.CanDriveSedan, "PICKUP" => p.CanDrivePickup, "VAN" => p.CanDriveVan, "AMBULANCE" => p.CanDriveAmbulance, null => true, _ => p.CanDriveOther };
    private static TimeZoneInfo ResolveBangkokTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
    }
}
