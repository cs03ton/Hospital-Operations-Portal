using System.Security.Claims;
using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/fleet")]
[Authorize]
public sealed class FleetMasterDataController(AppDbContext db, IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet("vehicle-types")]
    [RequireAnyPermission(FleetPermissions.VehicleManage, FleetPermissions.DriverManage, FleetPermissions.RequestCreate, FleetPermissions.DispatchView, FleetPermissions.MaintenanceView, FleetPermissions.MaintenanceManage)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FleetVehicleTypeResponse>>>> GetVehicleTypes(CancellationToken cancellationToken)
    {
        var items = await db.FleetVehicleTypes.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new FleetVehicleTypeResponse(x.Id, x.Code, x.Name, x.Description, x.SortOrder, x.IsActive))
            .ToListAsync(cancellationToken);
        return ApiResponse<IReadOnlyList<FleetVehicleTypeResponse>>.Ok(items);
    }

    [HttpPost("vehicle-types")]
    [RequirePermission(FleetPermissions.VehicleManage)]
    public async Task<ActionResult<ApiResponse<FleetVehicleTypeResponse>>> CreateVehicleType(SaveFleetVehicleTypeRequest request, CancellationToken cancellationToken)
    {
        var code = NormalizeCode(request.Code);
        if (await db.FleetVehicleTypes.AnyAsync(x => x.Code == code, cancellationToken))
            return Conflict(ApiResponse<FleetVehicleTypeResponse>.Fail("Vehicle type code already exists."));

        var item = new FleetVehicleType { Code = code, Name = request.Name.Trim(), Description = Clean(request.Description), SortOrder = request.SortOrder, IsActive = request.IsActive };
        db.FleetVehicleTypes.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        await Audit("Fleet.VehicleTypeCreated", "FleetVehicleType", item.Id, code);
        return CreatedAtAction(nameof(GetVehicleTypes), ApiResponse<FleetVehicleTypeResponse>.Ok(new(item.Id, item.Code, item.Name, item.Description, item.SortOrder, item.IsActive)));
    }

    [HttpPut("vehicle-types/{id:guid}")]
    [RequirePermission(FleetPermissions.VehicleManage)]
    public async Task<ActionResult<ApiResponse<FleetVehicleTypeResponse>>> UpdateVehicleType(Guid id, SaveFleetVehicleTypeRequest request, CancellationToken cancellationToken)
    {
        var item = await db.FleetVehicleTypes.FindAsync([id], cancellationToken);
        if (item is null) return NotFound(ApiResponse<FleetVehicleTypeResponse>.Fail("Vehicle type not found."));
        var code = NormalizeCode(request.Code);
        if (await db.FleetVehicleTypes.AnyAsync(x => x.Id != id && x.Code == code, cancellationToken))
            return Conflict(ApiResponse<FleetVehicleTypeResponse>.Fail("Vehicle type code already exists."));
        item.Code = code; item.Name = request.Name.Trim(); item.Description = Clean(request.Description); item.SortOrder = request.SortOrder; item.IsActive = request.IsActive; item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await Audit("Fleet.VehicleTypeUpdated", "FleetVehicleType", item.Id, code);
        return ApiResponse<FleetVehicleTypeResponse>.Ok(new(item.Id, item.Code, item.Name, item.Description, item.SortOrder, item.IsActive));
    }

    [HttpGet("vehicles")]
    [RequireAnyPermission(FleetPermissions.VehicleManage, FleetPermissions.DispatchView, FleetPermissions.MaintenanceView, FleetPermissions.MaintenanceManage)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FleetVehicleResponse>>>> GetVehicles(CancellationToken cancellationToken)
    {
        var items = await db.FleetVehicles.AsNoTracking().OrderBy(x => x.VehicleCode).Select(x => new FleetVehicleResponse(
            x.Id, x.VehicleCode, x.RegistrationNumber, x.RegistrationProvince, x.VehicleTypeId, x.VehicleType!.Name,
            x.Brand, x.Model, x.ManufactureYear, x.SeatCapacityTotal, x.PassengerCapacity, x.FuelType, x.CurrentMileage,
            x.OwningDepartmentId, x.ResponsibleUserId, x.Status, x.IsActive, x.Note, x.ImagePath)).ToListAsync(cancellationToken);
        return ApiResponse<IReadOnlyList<FleetVehicleResponse>>.Ok(items);
    }

    [HttpPost("vehicles")]
    [RequirePermission(FleetPermissions.VehicleManage)]
    public Task<ActionResult<ApiResponse<FleetVehicleResponse>>> CreateVehicle(SaveFleetVehicleRequest request, CancellationToken cancellationToken) => SaveVehicle(null, request, cancellationToken);

    [HttpPut("vehicles/{id:guid}")]
    [RequirePermission(FleetPermissions.VehicleManage)]
    public Task<ActionResult<ApiResponse<FleetVehicleResponse>>> UpdateVehicle(Guid id, SaveFleetVehicleRequest request, CancellationToken cancellationToken) => SaveVehicle(id, request, cancellationToken);

    [HttpGet("driver-profiles")]
    [RequirePermission(FleetPermissions.DriverManage)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FleetDriverProfileResponse>>>> GetDriverProfiles(CancellationToken cancellationToken)
    {
        var items = await db.FleetDriverProfiles.AsNoTracking().OrderBy(x => x.User!.FullName).Select(x => new FleetDriverProfileResponse(
            x.Id, x.UserId, x.User!.EmployeeCode ?? string.Empty, x.User.FullName, x.LicenseNumber, x.LicenseType,
            x.LicenseIssueDate, x.LicenseExpiryDate, x.CanDriveSedan, x.CanDrivePickup, x.CanDriveVan,
            x.CanDriveAmbulance, x.CanDriveOther, x.DriverStatus, x.IsActive, x.Note)).ToListAsync(cancellationToken);
        return ApiResponse<IReadOnlyList<FleetDriverProfileResponse>>.Ok(items);
    }

    [HttpPost("driver-profiles")]
    [RequirePermission(FleetPermissions.DriverManage)]
    public Task<ActionResult<ApiResponse<FleetDriverProfileResponse>>> CreateDriverProfile(SaveFleetDriverProfileRequest request, CancellationToken cancellationToken) => SaveDriverProfile(null, request, cancellationToken);

    [HttpPut("driver-profiles/{id:guid}")]
    [RequirePermission(FleetPermissions.DriverManage)]
    public Task<ActionResult<ApiResponse<FleetDriverProfileResponse>>> UpdateDriverProfile(Guid id, SaveFleetDriverProfileRequest request, CancellationToken cancellationToken) => SaveDriverProfile(id, request, cancellationToken);

    private async Task<ActionResult<ApiResponse<FleetVehicleResponse>>> SaveVehicle(Guid? id, SaveFleetVehicleRequest request, CancellationToken cancellationToken)
    {
        if (!FleetVehicleStatuses.All.Contains(request.Status)) return BadRequest(ApiResponse<FleetVehicleResponse>.Fail("Invalid vehicle status."));
        if (!await db.FleetVehicleTypes.AnyAsync(x => x.Id == request.VehicleTypeId && x.IsActive, cancellationToken)) return BadRequest(ApiResponse<FleetVehicleResponse>.Fail("Active vehicle type not found."));
        if (request.OwningDepartmentId is not null && !await db.Departments.AnyAsync(x => x.Id == request.OwningDepartmentId && x.IsActive, cancellationToken)) return BadRequest(ApiResponse<FleetVehicleResponse>.Fail("Active owning department not found."));
        if (request.ResponsibleUserId is not null && !await db.Users.AnyAsync(x => x.Id == request.ResponsibleUserId && x.IsActive, cancellationToken)) return BadRequest(ApiResponse<FleetVehicleResponse>.Fail("Active responsible user not found."));
        var code = request.VehicleCode.Trim(); var registration = request.RegistrationNumber.Trim();
        if (await db.FleetVehicles.AnyAsync(x => x.Id != id && (x.VehicleCode == code || x.RegistrationNumber == registration), cancellationToken)) return Conflict(ApiResponse<FleetVehicleResponse>.Fail("Vehicle code or registration number already exists."));
        var actor = CurrentUserId();
        FleetVehicle item;
        if (id is null) { item = new FleetVehicle { CreatedByUserId = actor }; db.FleetVehicles.Add(item); }
        else { item = await db.FleetVehicles.Include(x => x.VehicleType).FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? null!; if (item is null) return NotFound(ApiResponse<FleetVehicleResponse>.Fail("Vehicle not found.")); item.UpdatedAt = DateTime.UtcNow; item.UpdatedByUserId = actor; }
        item.VehicleCode = code; item.RegistrationNumber = registration; item.RegistrationProvince = Clean(request.RegistrationProvince); item.VehicleTypeId = request.VehicleTypeId;
        item.Brand = Clean(request.Brand); item.Model = Clean(request.Model); item.ManufactureYear = request.ManufactureYear; item.SeatCapacityTotal = request.SeatCapacityTotal; item.PassengerCapacity = request.PassengerCapacity;
        item.FuelType = Clean(request.FuelType); item.CurrentMileage = request.CurrentMileage; item.OwningDepartmentId = request.OwningDepartmentId; item.ResponsibleUserId = request.ResponsibleUserId;
        item.Status = request.Status; item.IsActive = request.IsActive; item.Note = Clean(request.Note);
        await db.SaveChangesAsync(cancellationToken);
        var typeName = await db.FleetVehicleTypes.Where(x => x.Id == item.VehicleTypeId).Select(x => x.Name).SingleAsync(cancellationToken);
        await Audit(id is null ? "Fleet.VehicleCreated" : "Fleet.VehicleUpdated", "FleetVehicle", item.Id, item.VehicleCode);
        var response = new FleetVehicleResponse(item.Id, item.VehicleCode, item.RegistrationNumber, item.RegistrationProvince, item.VehicleTypeId, typeName, item.Brand, item.Model, item.ManufactureYear, item.SeatCapacityTotal, item.PassengerCapacity, item.FuelType, item.CurrentMileage, item.OwningDepartmentId, item.ResponsibleUserId, item.Status, item.IsActive, item.Note, item.ImagePath);
        return id is null ? CreatedAtAction(nameof(GetVehicles), ApiResponse<FleetVehicleResponse>.Ok(response)) : ApiResponse<FleetVehicleResponse>.Ok(response);
    }

    private async Task<ActionResult<ApiResponse<FleetDriverProfileResponse>>> SaveDriverProfile(Guid? id, SaveFleetDriverProfileRequest request, CancellationToken cancellationToken)
    {
        if (!FleetDriverStatuses.All.Contains(request.DriverStatus)) return BadRequest(ApiResponse<FleetDriverProfileResponse>.Fail("Invalid driver status."));
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.UserId && x.IsActive, cancellationToken);
        if (user is null) return BadRequest(ApiResponse<FleetDriverProfileResponse>.Fail("Active user not found."));
        if (await db.FleetDriverProfiles.AnyAsync(x => x.Id != id && x.UserId == request.UserId, cancellationToken)) return Conflict(ApiResponse<FleetDriverProfileResponse>.Fail("Driver profile already exists for this user."));
        var actor = CurrentUserId(); FleetDriverProfile item;
        if (id is null) { item = new FleetDriverProfile { CreatedByUserId = actor }; db.FleetDriverProfiles.Add(item); }
        else { item = await db.FleetDriverProfiles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? null!; if (item is null) return NotFound(ApiResponse<FleetDriverProfileResponse>.Fail("Driver profile not found.")); item.UpdatedAt = DateTime.UtcNow; item.UpdatedByUserId = actor; }
        item.UserId = request.UserId; item.LicenseNumber = request.LicenseNumber.Trim(); item.LicenseType = request.LicenseType.Trim(); item.LicenseIssueDate = request.LicenseIssueDate; item.LicenseExpiryDate = request.LicenseExpiryDate;
        item.CanDriveSedan = request.CanDriveSedan; item.CanDrivePickup = request.CanDrivePickup; item.CanDriveVan = request.CanDriveVan; item.CanDriveAmbulance = request.CanDriveAmbulance; item.CanDriveOther = request.CanDriveOther;
        item.DriverStatus = request.DriverStatus; item.IsActive = request.IsActive; item.Note = Clean(request.Note);
        await db.SaveChangesAsync(cancellationToken);
        await Audit(id is null ? "Fleet.DriverProfileCreated" : "Fleet.DriverProfileUpdated", "FleetDriverProfile", item.Id, user.EmployeeCode ?? user.Id.ToString());
        var response = new FleetDriverProfileResponse(item.Id, item.UserId, user.EmployeeCode ?? string.Empty, user.FullName, item.LicenseNumber, item.LicenseType, item.LicenseIssueDate, item.LicenseExpiryDate, item.CanDriveSedan, item.CanDrivePickup, item.CanDriveVan, item.CanDriveAmbulance, item.CanDriveOther, item.DriverStatus, item.IsActive, item.Note);
        return id is null ? CreatedAtAction(nameof(GetDriverProfiles), ApiResponse<FleetDriverProfileResponse>.Ok(response)) : ApiResponse<FleetDriverProfileResponse>.Ok(response);
    }

    private Guid? CurrentUserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var value) ? value : null;
    private Task Audit(string action, string entity, Guid id, string detail) => auditLogService.WriteAsync(CurrentUserId(), action, entity, id.ToString(), detail, "Success", HttpContext);
    private static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
