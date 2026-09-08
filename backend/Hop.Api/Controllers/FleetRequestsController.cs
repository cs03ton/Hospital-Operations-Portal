using System.Data;
using System.Security.Claims;
using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/fleet/requests")]
[Authorize]
public sealed class FleetRequestsController(AppDbContext db, FleetRequestNumberService numberService, FleetAvailabilityService availabilityService) : ControllerBase
{
    [HttpGet("personnel-options")]
    [RequireAnyPermission(FleetPermissions.RequestCreate, FleetPermissions.RequestEditOwn)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FleetPersonnelOptionDto>>>> GetPersonnelOptions(
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var query = db.Users.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => EF.Functions.ILike(x.FullName, $"%{term}%") ||
                                     (x.EmployeeCode != null && EF.Functions.ILike(x.EmployeeCode, $"%{term}%")) ||
                                     (x.Department != null && EF.Functions.ILike(x.Department.Name, $"%{term}%")));
        }

        var items = await query
            .OrderBy(x => x.FullName)
            .Take(50)
            .Select(x => new FleetPersonnelOptionDto(x.Id, x.FullName, x.EmployeeCode, x.DepartmentId, x.Department != null ? x.Department.Name : null))
            .ToListAsync(ct);
        return ApiResponse<IReadOnlyList<FleetPersonnelOptionDto>>.Ok(items);
    }

    [HttpGet("mine")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FleetRequestDto>>>> GetMine(CancellationToken ct)
    {
        var userId = CurrentUserId(); if (userId is null) return Unauthorized(ApiResponse<IReadOnlyList<FleetRequestDto>>.Fail("Invalid access token."));
        var query = BaseQuery();
        if (!await HasPermission(FleetPermissions.RequestViewAll, ct))
            query = query.Where(x => x.RequesterUserId == userId || x.Passengers.Any(passenger => passenger.UserId == userId));
        var items = await query.OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        return ApiResponse<IReadOnlyList<FleetRequestDto>>.Ok(items.Select(ToDto).ToList());
    }

    [HttpGet("mine/paged")]
    public async Task<ActionResult<ApiResponse<PagedResponse<FleetRequestDto>>>> GetMinePaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] string? sortDirection = "desc",
        CancellationToken ct = default)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized(ApiResponse<PagedResponse<FleetRequestDto>>.Fail("Invalid access token."));

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = BaseQuery().AsNoTracking();
        if (!await HasPermission(FleetPermissions.RequestViewAll, ct))
            query = query.Where(x => x.RequesterUserId == userId || x.Passengers.Any(passenger => passenger.UserId == userId));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => EF.Functions.ILike(x.RequestNo, $"%{term}%") ||
                                     EF.Functions.ILike(x.Purpose, $"%{term}%") ||
                                     EF.Functions.ILike(x.Destination, $"%{term}%"));
        }
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim().ToUpperInvariant());
        if (dateFrom is not null) query = query.Where(x => x.DepartureAt >= dateFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        if (dateTo is not null) query = query.Where(x => x.DepartureAt < dateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

        var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        query = sortBy?.ToLowerInvariant() switch
        {
            "requestno" => descending ? query.OrderByDescending(x => x.RequestNo).ThenByDescending(x => x.Id) : query.OrderBy(x => x.RequestNo).ThenBy(x => x.Id),
            "departureat" => descending ? query.OrderByDescending(x => x.DepartureAt).ThenByDescending(x => x.Id) : query.OrderBy(x => x.DepartureAt).ThenBy(x => x.Id),
            "status" => descending ? query.OrderByDescending(x => x.Status).ThenByDescending(x => x.Id) : query.OrderBy(x => x.Status).ThenBy(x => x.Id),
            _ => descending ? query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id) : query.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id)
        };

        var totalItems = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var result = new PagedResponse<FleetRequestDto>(items.Select(ToDto).ToList(), page, pageSize, totalItems, (int)Math.Ceiling(totalItems / (double)pageSize));
        return ApiResponse<PagedResponse<FleetRequestDto>>.Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<FleetRequestDto>>> Get(Guid id, CancellationToken ct)
    {
        var item = await BaseQuery().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound(ApiResponse<FleetRequestDto>.Fail("Fleet request not found."));
        var canReview = await HasPermission(FleetPermissions.AdminReviewApprove, ct) ||
                        await HasPermission(FleetPermissions.AdminReviewReturn, ct) ||
                        await HasPermission(FleetPermissions.AdminReviewReject, ct);
        var canApprove = await HasPermission(FleetPermissions.DirectorApprove, ct) ||
                         await HasPermission(FleetPermissions.DirectorReturn, ct) ||
                         await HasPermission(FleetPermissions.DirectorReject, ct);
        var currentUserId = CurrentUserId();
        var isPassenger = currentUserId is not null && item.Passengers.Any(passenger => passenger.UserId == currentUserId);
        if (item.RequesterUserId != currentUserId &&
            !isPassenger &&
            !await HasPermission(FleetPermissions.RequestViewAll, ct) &&
            !await HasPermission(FleetPermissions.DispatchView, ct) &&
            !canReview &&
            !canApprove) return Forbid();
        return ApiResponse<FleetRequestDto>.Ok(ToDto(item));
    }

    [HttpPost]
    [RequirePermission(FleetPermissions.RequestCreate)]
    public async Task<ActionResult<ApiResponse<FleetRequestDto>>> Create(SaveFleetRequestDto request, CancellationToken ct)
    {
        var actor = CurrentUserId(); if (actor is null) return Unauthorized(ApiResponse<FleetRequestDto>.Fail("Invalid access token."));
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == actor && x.IsActive, ct);
        if (user is null) return Forbid();
        var validation = await ValidatePassengers(request, actor.Value, ct); if (validation is not null) return BadRequest(ApiResponse<FleetRequestDto>.Fail(validation));
        if (request.RequestedVehicleTypeId is not null && !await db.FleetVehicleTypes.AnyAsync(x => x.Id == request.RequestedVehicleTypeId && x.IsActive, ct)) return BadRequest(ApiResponse<FleetRequestDto>.Fail("Active vehicle type not found."));

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var now = DateTime.UtcNow;
        var item = new FleetRequest { RequestNo = await numberService.GenerateAsync(now, ct), RequesterUserId = actor.Value, RequesterDepartmentId = user.DepartmentId, RequestDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(now, BangkokTimeZone())), CreatedAt = now, CreatedByUserId = actor.Value };
        ApplyDraft(item, request, actor.Value);
        db.FleetRequests.Add(item);
        ReplacePassengers(item, request.Passengers);
        AddHistory(item, null, FleetRequestStatuses.Draft, "Fleet.RequestCreated", actor.Value, null, null);
        AddAudit(actor, "Fleet.RequestCreated", item.Id, $"Created {item.RequestNo}");
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, ApiResponse<FleetRequestDto>.Ok(ToDto(await BaseQuery().SingleAsync(x => x.Id == item.Id, ct))));
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(FleetPermissions.RequestEditOwn)]
    public async Task<ActionResult<ApiResponse<FleetRequestDto>>> Update(Guid id, SaveFleetRequestDto request, CancellationToken ct)
    {
        var actor = CurrentUserId(); if (actor is null) return Unauthorized(ApiResponse<FleetRequestDto>.Fail("Invalid access token."));
        var item = await db.FleetRequests.Include(x => x.Passengers).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound(ApiResponse<FleetRequestDto>.Fail("Fleet request not found."));
        if (item.RequesterUserId != actor) return Forbid();
        if (item.Status != FleetRequestStatuses.Draft && !(item.Status == FleetRequestStatuses.Returned && item.ReturnTarget == FleetReturnTargets.Requester)) return Conflict(ApiResponse<FleetRequestDto>.Fail("Request is not editable in its current state."));
        if (request.ConcurrencyToken != item.ConcurrencyToken) return Conflict(ApiResponse<FleetRequestDto>.Fail("Request was changed by another user. Reload and try again."));
        var validation = await ValidatePassengers(request, actor.Value, ct); if (validation is not null) return BadRequest(ApiResponse<FleetRequestDto>.Fail(validation));
        ApplyDraft(item, request, actor.Value); ReplacePassengers(item, request.Passengers); item.ConcurrencyToken = Guid.NewGuid();
        AddAudit(actor, "Fleet.RequestUpdated", item.Id, $"Updated {item.RequestNo}");
        if (!await TrySave(ct)) return Conflict(ApiResponse<FleetRequestDto>.Fail("Request was changed by another user. Reload and try again."));
        return ApiResponse<FleetRequestDto>.Ok(ToDto(await BaseQuery().SingleAsync(x => x.Id == id, ct)));
    }

    [HttpPost("{id:guid}/submit")]
    [RequirePermission(FleetPermissions.RequestSubmit)]
    public Task<ActionResult<ApiResponse<FleetRequestDto>>> Submit(Guid id, FleetTransitionRequest request, CancellationToken ct) => TransitionRequester(id, request, "submit", ct);

    [HttpPost("{id:guid}/cancel")]
    [RequirePermission(FleetPermissions.RequestCancel)]
    public Task<ActionResult<ApiResponse<FleetRequestDto>>> Cancel(Guid id, FleetTransitionRequest request, CancellationToken ct) => TransitionRequester(id, request, "cancel", ct);

    [HttpGet("dispatcher/queue")]
    [RequirePermission(FleetPermissions.DispatchView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FleetRequestDto>>>> GetDispatcherQueue(CancellationToken ct)
    {
        var items = await BaseQuery().Where(x => x.Status == FleetRequestStatuses.PendingDispatch || (x.Status == FleetRequestStatuses.Returned && x.ReturnTarget == FleetReturnTargets.Dispatcher)).OrderBy(x => x.IsUrgent ? 0 : 1).ThenBy(x => x.DepartureAt).ToListAsync(ct);
        return ApiResponse<IReadOnlyList<FleetRequestDto>>.Ok(items.Select(ToDto).ToList());
    }

    [HttpGet("{id:guid}/availability")]
    [RequirePermission(FleetPermissions.DispatchView)]
    public async Task<ActionResult<ApiResponse<FleetAvailabilityResponse>>> Availability(Guid id, CancellationToken ct)
    {
        var item = await db.FleetRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return item is null ? NotFound(ApiResponse<FleetAvailabilityResponse>.Fail("Fleet request not found.")) : ApiResponse<FleetAvailabilityResponse>.Ok(await availabilityService.GetAsync(item, ct));
    }

    [HttpPost("{id:guid}/assign")]
    [RequirePermission(FleetPermissions.DispatchAssign)]
    public async Task<ActionResult<ApiResponse<FleetRequestDto>>> Assign(Guid id, FleetAssignRequest request, CancellationToken ct)
    {
        var actor = CurrentUserId(); if (actor is null) return Unauthorized(ApiResponse<FleetRequestDto>.Fail("Invalid access token."));
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var item = await db.FleetRequests.Include(x => x.Assignments).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound(ApiResponse<FleetRequestDto>.Fail("Fleet request not found."));
        if (item.Status != FleetRequestStatuses.PendingDispatch && !(item.Status == FleetRequestStatuses.Returned && item.ReturnTarget == FleetReturnTargets.Dispatcher)) return Conflict(ApiResponse<FleetRequestDto>.Fail("Request is not waiting for dispatch."));
        if (request.ConcurrencyToken != item.ConcurrencyToken) return Conflict(ApiResponse<FleetRequestDto>.Fail("Request was changed by another user."));
        if (item.Assignments.Any(x => x.IsActive)) return Conflict(ApiResponse<FleetRequestDto>.Fail("Request already has an active assignment."));
        if (db.Database.IsRelational())
        {
            foreach (var lockKey in new[] { request.VehicleId, request.DriverUserId }.OrderBy(x => x))
                await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({lockKey.ToString()}))", ct);
        }
        var available = await availabilityService.GetAsync(item, ct);
        var vehicle = available.Vehicles.FirstOrDefault(x => x.Id == request.VehicleId); var driver = available.Drivers.FirstOrDefault(x => x.Id == request.DriverUserId);
        if (vehicle is null || !vehicle.IsAvailable) return Conflict(ApiResponse<FleetRequestDto>.Fail($"Vehicle is unavailable: {string.Join(", ", vehicle?.Reasons ?? [])}"));
        if (driver is null || !driver.IsAvailable) return Conflict(ApiResponse<FleetRequestDto>.Fail($"Driver is unavailable: {string.Join(", ", driver?.Reasons ?? [])}"));
        db.FleetAssignments.Add(new FleetAssignment { FleetRequestId = item.Id, VehicleId = request.VehicleId, DriverUserId = request.DriverUserId, AssignedByUserId = actor.Value, AssignmentReason = Clean(request.Reason) });
        var from = item.Status; item.Status = FleetRequestStatuses.PendingAdminReview; item.ReturnTarget = null; item.UpdatedAt = DateTime.UtcNow; item.UpdatedByUserId = actor; item.ConcurrencyToken = Guid.NewGuid();
        AddHistory(item, from, item.Status, "Fleet.VehicleAssigned", actor.Value, null, request.Reason); AddAudit(actor, "Fleet.VehicleAssigned", item.Id, $"Assigned vehicle {request.VehicleId} and driver {request.DriverUserId}");
        if (!await TrySave(ct)) return Conflict(ApiResponse<FleetRequestDto>.Fail("Concurrent assignment detected."));
        await tx.CommitAsync(ct);
        return ApiResponse<FleetRequestDto>.Ok(ToDto(await BaseQuery().SingleAsync(x => x.Id == id, ct)));
    }

    [HttpPost("{id:guid}/return")]
    [RequirePermission(FleetPermissions.DispatchReturn)]
    public Task<ActionResult<ApiResponse<FleetRequestDto>>> DispatcherReturn(Guid id, FleetTransitionRequest request, CancellationToken ct) => DispatcherTransition(id, request, false, ct);

    [HttpPost("{id:guid}/reject")]
    [RequirePermission(FleetPermissions.DispatchReject)]
    public Task<ActionResult<ApiResponse<FleetRequestDto>>> DispatcherReject(Guid id, FleetTransitionRequest request, CancellationToken ct) => DispatcherTransition(id, request, true, ct);

    private async Task<ActionResult<ApiResponse<FleetRequestDto>>> TransitionRequester(Guid id, FleetTransitionRequest request, string action, CancellationToken ct)
    {
        var actor = CurrentUserId(); if (actor is null) return Unauthorized(ApiResponse<FleetRequestDto>.Fail("Invalid access token."));
        var item = await db.FleetRequests.Include(x => x.Passengers).Include(x => x.Assignments).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound(ApiResponse<FleetRequestDto>.Fail("Fleet request not found."));
        if (item.RequesterUserId != actor) return Forbid();
        if (request.ConcurrencyToken != item.ConcurrencyToken) return Conflict(ApiResponse<FleetRequestDto>.Fail("Request was changed by another user."));
        var from = item.Status;
        if (action == "submit")
        {
            if (from != FleetRequestStatuses.Draft && !(from == FleetRequestStatuses.Returned && item.ReturnTarget == FleetReturnTargets.Requester)) return Conflict(ApiResponse<FleetRequestDto>.Fail("Only draft or requester-returned requests can be submitted."));
            if (item.Passengers.Count != item.PassengerCount || item.PassengerCount <= 0) return BadRequest(ApiResponse<FleetRequestDto>.Fail("Passenger list must match passenger count."));
            item.Status = FleetRequestStatuses.PendingDispatch; item.SubmittedAt = DateTime.UtcNow; item.ReturnTarget = null;
            AddHistory(item, from, item.Status, "Fleet.RequestSubmitted", actor.Value, null, null); AddAudit(actor, "Fleet.RequestSubmitted", item.Id, $"Submitted {item.RequestNo}");
        }
        else
        {
            if (!new[] { FleetRequestStatuses.Draft, FleetRequestStatuses.PendingDispatch, FleetRequestStatuses.PendingAdminReview, FleetRequestStatuses.PendingDirector, FleetRequestStatuses.Returned }.Contains(from)) return Conflict(ApiResponse<FleetRequestDto>.Fail("After Director approval, create a cancellation request instead of cancelling directly."));
            if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest(ApiResponse<FleetRequestDto>.Fail("Cancellation reason is required."));
            item.Status = FleetRequestStatuses.Cancelled; item.CancelledAt = DateTime.UtcNow; item.CancellationReason = request.Reason.Trim(); item.ReturnTarget = null;
            foreach (var assignment in item.Assignments.Where(x => x.IsActive)) { assignment.IsActive = false; assignment.AssignmentStatus = FleetAssignmentStatuses.Cancelled; }
            db.FleetCancellationRequests.Add(new FleetCancellationRequest { FleetRequestId = item.Id, RequestedByUserId = actor.Value, PreviousStatus = from, Reason = request.Reason.Trim(), CompletedAt = DateTime.UtcNow });
            AddHistory(item, from, item.Status, "Fleet.RequestCancelled", actor.Value, null, request.Reason); AddAudit(actor, "Fleet.RequestCancelled", item.Id, request.Reason);
        }
        item.UpdatedAt = DateTime.UtcNow; item.UpdatedByUserId = actor; item.ConcurrencyToken = Guid.NewGuid();
        if (!await TrySave(ct)) return Conflict(ApiResponse<FleetRequestDto>.Fail("Request was changed by another user."));
        return ApiResponse<FleetRequestDto>.Ok(ToDto(await BaseQuery().SingleAsync(x => x.Id == id, ct)));
    }

    private async Task<ActionResult<ApiResponse<FleetRequestDto>>> DispatcherTransition(Guid id, FleetTransitionRequest request, bool reject, CancellationToken ct)
    {
        var actor = CurrentUserId(); if (actor is null) return Unauthorized(ApiResponse<FleetRequestDto>.Fail("Invalid access token."));
        if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest(ApiResponse<FleetRequestDto>.Fail("Reason is required."));
        var item = await db.FleetRequests.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound(ApiResponse<FleetRequestDto>.Fail("Fleet request not found."));
        if (item.Status != FleetRequestStatuses.PendingDispatch) return Conflict(ApiResponse<FleetRequestDto>.Fail("Request is not waiting for dispatch."));
        if (request.ConcurrencyToken != item.ConcurrencyToken) return Conflict(ApiResponse<FleetRequestDto>.Fail("Request was changed by another user."));
        var from = item.Status; item.Status = reject ? FleetRequestStatuses.Rejected : FleetRequestStatuses.Returned; item.ReturnTarget = reject ? null : FleetReturnTargets.Requester; item.UpdatedAt = DateTime.UtcNow; item.UpdatedByUserId = actor; item.ConcurrencyToken = Guid.NewGuid();
        var audit = reject ? "Fleet.RequestRejected" : "Fleet.RequestReturned";
        AddHistory(item, from, item.Status, audit, actor.Value, item.ReturnTarget, request.Reason); AddAudit(actor, audit, item.Id, request.Reason);
        if (!await TrySave(ct)) return Conflict(ApiResponse<FleetRequestDto>.Fail("Request was changed by another user."));
        return ApiResponse<FleetRequestDto>.Ok(ToDto(await BaseQuery().SingleAsync(x => x.Id == id, ct)));
    }

    private IQueryable<FleetRequest> BaseQuery() => db.FleetRequests.AsNoTracking().Include(x => x.RequesterUser).ThenInclude(x => x!.Department).Include(x => x.RequestedVehicleType).Include(x => x.Passengers).Include(x => x.StatusHistories).ThenInclude(x => x.ActorUser).Include(x => x.Assignments).ThenInclude(x => x.Vehicle).Include(x => x.Assignments).ThenInclude(x => x.DriverUser).AsSplitQuery();
    private void ApplyDraft(FleetRequest x, SaveFleetRequestDto r, Guid actor) { x.Purpose = r.Purpose.Trim(); x.MissionType = r.MissionType.Trim(); x.RequestedVehicleTypeId = r.RequestedVehicleTypeId; x.Destination = r.Destination.Trim(); x.ContactPersonName = r.ContactPersonName.Trim(); x.ContactPhone = r.ContactPhone.Trim(); x.DepartureAt = r.DepartureAt; x.ExpectedReturnAt = r.ExpectedReturnAt; x.PassengerCount = r.PassengerCount; x.SpecialRequirement = Clean(r.SpecialRequirement); x.IsUrgent = r.IsUrgent; x.UrgentReason = Clean(r.UrgentReason); x.UpdatedAt = DateTime.UtcNow; x.UpdatedByUserId = actor; }
    private void ReplacePassengers(FleetRequest item, IReadOnlyList<SaveFleetPassengerDto> passengers) { db.FleetRequestPassengers.RemoveRange(item.Passengers); item.Passengers = passengers.OrderBy(x => x.SortOrder).Select(x => new FleetRequestPassenger { UserId = x.UserId, FullName = x.FullName.Trim(), PositionOrOrganization = Clean(x.PositionOrOrganization), Phone = Clean(x.Phone), PassengerType = x.PassengerType, IsRequester = x.IsRequester, SortOrder = x.SortOrder }).ToList(); }
    private async Task<string?> ValidatePassengers(SaveFleetRequestDto request, Guid requester, CancellationToken ct) { if (request.Passengers.Any(x => x.PassengerType is not (FleetPassengerTypes.Employee or FleetPassengerTypes.External))) return "Invalid passenger type."; if (request.Passengers.Any(x => x.PassengerType == FleetPassengerTypes.Employee && x.UserId is null)) return "Employee passenger must reference a user."; if (request.Passengers.Any(x => x.PassengerType == FleetPassengerTypes.External && x.UserId is not null)) return "External passenger cannot reference a user."; if (request.Passengers.Any(x => x.IsRequester && x.UserId != requester)) return "Requester passenger must reference the signed-in user."; var ids = request.Passengers.Where(x => x.UserId != null).Select(x => x.UserId!.Value).Distinct().ToList(); return await db.Users.CountAsync(x => ids.Contains(x.Id) && x.IsActive, ct) == ids.Count ? null : "Passenger list contains an inactive or unknown user."; }
    private void AddHistory(FleetRequest item, string? from, string to, string action, Guid actor, string? target, string? reason) => item.StatusHistories.Add(new FleetRequestStatusHistory { FromStatus = from, ToStatus = to, Action = action, ActorUserId = actor, ReturnTarget = target, Reason = Clean(reason), CorrelationId = HttpContext.TraceIdentifier });
    private void AddAudit(Guid? actor, string action, Guid id, string? detail) => db.AuditLogs.Add(new AuditLog { UserId = actor, Action = action, EntityName = "FleetRequest", EntityId = id.ToString(), Detail = detail, IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() });
    private async Task<bool> HasPermission(string permission, CancellationToken ct) { var id = CurrentUserId(); return id is not null && await db.UserRoles.Where(x => x.UserId == id && x.Role != null && x.Role.IsActive).SelectMany(x => x.Role!.RolePermissions).AnyAsync(x => x.Permission != null && x.Permission.IsActive && x.Permission.Code == permission, ct); }
    private async Task<bool> TrySave(CancellationToken ct) { try { await db.SaveChangesAsync(ct); return true; } catch (DbUpdateConcurrencyException) { return false; } catch (DbUpdateException) { return false; } }
    private Guid? CurrentUserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static TimeZoneInfo BangkokTimeZone() { try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok"); } catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); } }
    private static FleetRequestDto ToDto(FleetRequest x) { var a = x.Assignments.FirstOrDefault(y => y.IsActive); return new(x.Id, x.RequestNo, x.RequesterUserId, x.RequesterUser?.FullName ?? "-", x.RequesterDepartmentId, x.RequesterUser?.Department?.Name, x.RequestDate, x.Purpose, x.MissionType, x.RequestedVehicleTypeId, x.RequestedVehicleType?.Name, x.Destination, x.ContactPersonName, x.ContactPhone, x.DepartureAt, x.ExpectedReturnAt, x.PassengerCount, x.SpecialRequirement, x.IsUrgent, x.UrgentReason, x.Status, x.ReturnTarget, x.SubmittedAt, x.CancelledAt, x.CancellationReason, x.CreatedAt, x.UpdatedAt, x.ConcurrencyToken, x.Passengers.OrderBy(y => y.SortOrder).Select(y => new FleetPassengerDto(y.Id, y.UserId, y.FullName, y.PositionOrOrganization, y.Phone, y.PassengerType, y.IsRequester, y.SortOrder)).ToList(), x.StatusHistories.OrderBy(y => y.CreatedAt).Select(y => new FleetStatusHistoryDto(y.Id, y.FromStatus, y.ToStatus, y.Action, y.ReturnTarget, y.Reason, y.ActorUserId, y.ActorUser?.FullName, y.CreatedAt)).ToList(), a is null ? null : new FleetAssignmentDto(a.Id, a.VehicleId, a.Vehicle?.VehicleCode ?? "-", a.Vehicle?.RegistrationNumber ?? "-", a.DriverUserId, a.DriverUser?.FullName ?? "-", a.AssignedAt, a.AssignmentStatus, a.IsActive, a.ConcurrencyToken, a.ReplacedAssignmentId)); }
}
