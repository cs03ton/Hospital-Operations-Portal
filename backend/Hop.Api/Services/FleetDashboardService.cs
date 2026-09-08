using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Hop.Api.Configuration;

namespace Hop.Api.Services;

public sealed record FleetEffectivePermissions(IReadOnlySet<string> Permissions, IReadOnlyList<string> DelegatedPermissions)
{
    public bool Has(string permission) => Permissions.Contains(permission);
    public bool HasAny(params string[] permissions) => permissions.Any(Permissions.Contains);
}

public interface IFleetEffectivePermissionService
{
    Task<FleetEffectivePermissions> ResolveAsync(Guid userId, DateTime atUtc, CancellationToken ct);
}

public sealed class FleetEffectivePermissionService(AppDbContext db) : IFleetEffectivePermissionService
{
    public async Task<FleetEffectivePermissions> ResolveAsync(Guid userId, DateTime atUtc, CancellationToken ct)
    {
        var direct = await db.UserRoles.AsNoTracking()
            .Where(x => x.UserId == userId && x.Role != null && x.Role.IsActive)
            .SelectMany(x => x.Role!.RolePermissions)
            .Where(x => x.Permission != null && x.Permission.IsActive && x.Permission.Code.StartsWith("Fleet"))
            .Select(x => x.Permission!.Code).Distinct().ToListAsync(ct);

        var delegated = await db.ApprovalDelegations.AsNoTracking()
            .Where(x => x.IsActive && x.Scope == "FLEET" && x.DelegateUserId == userId && x.StartAt != null && x.EndAt != null && x.StartAt <= atUtc && x.EndAt > atUtc && x.RequiredPermissionCode != null)
            .Where(x => db.UserRoles.Any(ur => ur.UserId == x.ApproverUserId && ur.Role != null && ur.Role.IsActive &&
                ur.Role.RolePermissions.Any(rp => rp.Permission != null && rp.Permission.IsActive && rp.Permission.Code == x.RequiredPermissionCode)))
            .Select(x => x.RequiredPermissionCode!).Distinct().ToListAsync(ct);

        return new FleetEffectivePermissions(direct.Concat(delegated).ToHashSet(StringComparer.Ordinal), delegated);
    }
}

public interface IFleetDashboardService
{
    Task<FleetDashboardDto?> GetAsync(Guid userId, CancellationToken ct);
}

public sealed class FleetDashboardService(AppDbContext db, IFleetEffectivePermissionService effectivePermissions, IOptions<FleetOperationsOptions> options) : IFleetDashboardService
{
    private static readonly string[] RequesterPermissions = [FleetPermissions.RequestViewOwn, FleetPermissions.RequestCreate];
    private static readonly string[] DriverPermissions =
        [FleetPermissions.DriverViewOwnJobs, FleetPermissions.DriverViewOwn, FleetPermissions.DriverViewJobs];
    private static readonly string[] DispatcherPermissions = [FleetPermissions.DispatchView, FleetPermissions.DispatchAssign];
    private static readonly string[] ReviewerPermissions = [FleetPermissions.AdminReviewApprove, FleetPermissions.AdminReviewReturn, FleetPermissions.AdminReviewReject];
    private static readonly string[] DirectorPermissions = [FleetPermissions.DirectorApprove, FleetPermissions.DirectorReturn, FleetPermissions.DirectorReject];

    public async Task<FleetDashboardDto?> GetAsync(Guid userId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var permissions = await effectivePermissions.ResolveAsync(userId, now, ct);
        var canRequester = permissions.HasAny(RequesterPermissions);
        var canDriver = permissions.HasAny(DriverPermissions);
        var canDispatch = permissions.HasAny(DispatcherPermissions);
        var canReview = permissions.HasAny(ReviewerPermissions);
        var canDirector = permissions.HasAny(DirectorPermissions);
        var canCalendar = permissions.Has(FleetPermissions.CalendarView);
        var canReports = permissions.HasAny(FleetPermissions.ReportView, FleetPermissions.ReportExport);
        var canManage = permissions.Has(FleetPermissions.SettingsManage);
        var canManageFeedback = permissions.Has(FleetPermissions.FeedbackViewManagement);
        if (!(canRequester || canDriver || canDispatch || canReview || canDirector || canCalendar || canReports || canManage || canManageFeedback || permissions.Has(FleetPermissions.DashboardView))) return null;

        var (dayStart, dayEnd, monthStart) = GetBangkokDashboardRange(now);
        var operational = canDispatch || canReview || canDirector || canManage;
        var sharedRequests = db.FleetRequests.AsNoTracking().Where(x => x.DepartureAt < dayEnd && x.ExpectedReturnAt >= dayStart);
        if (!operational)
        {
            if (canDriver) sharedRequests = sharedRequests.Where(x => x.Assignments.Any(a => a.IsActive && a.DriverUserId == userId));
            else sharedRequests = sharedRequests.Where(x => x.RequesterUserId == userId);
        }

        var todayJobs = await sharedRequests.CountAsync(ct);
        var availableVehicles = await db.FleetVehicles.AsNoTracking().CountAsync(x => x.IsActive && x.Status == FleetVehicleStatuses.Available, ct);
        var inUseVehicles = await db.FleetVehicles.AsNoTracking().CountAsync(x => x.IsActive && (x.Status == FleetVehicleStatuses.InUse || x.Status == FleetVehicleStatuses.Reserved), ct);
        var unavailableVehicles = await db.FleetVehicles.AsNoTracking().CountAsync(x => x.IsActive && x.Status != FleetVehicleStatuses.Available && x.Status != FleetVehicleStatuses.InUse && x.Status != FleetVehicleStatuses.Reserved, ct);

        FleetDashboardRequesterDto? requester = null;
        if (canRequester)
        {
            var own = db.FleetRequests.AsNoTracking().Where(x => x.RequesterUserId == userId);
            var statusCounts = await own.GroupBy(x => x.Status).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
            var next = await own.Where(x => x.DepartureAt >= now && x.Status != FleetRequestStatuses.Cancelled && x.Status != FleetRequestStatuses.Rejected)
                .OrderBy(x => x.DepartureAt).Select(x => new FleetDashboardRequestPreviewDto(x.Id, x.RequestNo, x.DepartureAt, x.Destination, x.Status,
                    x.Assignments.Where(a => a.IsActive).Select(a => a.Vehicle!.VehicleCode).FirstOrDefault(),
                    x.Assignments.Where(a => a.IsActive).Select(a => a.DriverUser!.FullName).FirstOrDefault())).FirstOrDefaultAsync(ct);
            var actionRequired = await own.CountAsync(x => x.Status == FleetRequestStatuses.Draft || (x.Status == FleetRequestStatuses.Returned && x.ReturnTarget == FleetReturnTargets.Requester) || x.Status == FleetRequestStatuses.CancellationPending, ct);
            requester = new(statusCounts, next, actionRequired);
        }

        FleetDashboardDriverDto? driver = null;
        var myDriverJobs = 0;
        if (canDriver)
        {
            var ownAssignments = db.FleetAssignments.AsNoTracking().Where(x => x.IsActive && x.DriverUserId == userId);
            myDriverJobs = await ownAssignments.CountAsync(x => x.FleetRequest!.Status == FleetRequestStatuses.PendingDriverAck || x.FleetRequest.Status == FleetRequestStatuses.Ready || x.FleetRequest.Status == FleetRequestStatuses.InProgress, ct);
            var driverToday = await ownAssignments.CountAsync(x => x.FleetRequest!.DepartureAt < dayEnd && x.FleetRequest.ExpectedReturnAt >= dayStart, ct);
            var upcoming = await ownAssignments.CountAsync(x => x.FleetRequest!.DepartureAt >= dayEnd, ct);
            var completed = await ownAssignments.CountAsync(x => x.FleetRequest!.Status == FleetRequestStatuses.Completed && x.FleetRequest.ExpectedReturnAt >= monthStart, ct);
            var distance = await db.FleetTripRecords.AsNoTracking().Where(x => x.DriverUserId == userId && x.ActualEndAt >= monthStart && x.EndMileage != null).SumAsync(x => (decimal?)(x.EndMileage!.Value - x.StartMileage), ct) ?? 0;
            driver = new(driverToday, upcoming, myDriverJobs, completed, distance);
        }

        FleetDashboardDispatcherDto? dispatcher = null;
        var dispatchBadge = 0;
        var reviewBadge = 0;
        if (canDispatch)
        {
            dispatchBadge = await db.FleetRequests.AsNoTracking().CountAsync(x => x.Status == FleetRequestStatuses.PendingDispatch, ct);
            reviewBadge = await db.FleetRequests.AsNoTracking().CountAsync(x => x.Status == FleetRequestStatuses.Returned && x.ReturnTarget == FleetReturnTargets.Dispatcher, ct);
            var availableDrivers = await db.FleetDriverProfiles.AsNoTracking().CountAsync(x => x.IsActive && x.DriverStatus == FleetDriverStatuses.Available, ct);
            var busyDrivers = await db.FleetAssignments.AsNoTracking().Where(x => x.IsActive && (x.FleetRequest!.Status == FleetRequestStatuses.Ready || x.FleetRequest.Status == FleetRequestStatuses.InProgress)).Select(x => x.DriverUserId).Distinct().CountAsync(ct);
            var overdue = await db.FleetRequests.AsNoTracking().CountAsync(x => x.ExpectedReturnAt < now && x.Status == FleetRequestStatuses.InProgress, ct);
            dispatcher = new(dispatchBadge, reviewBadge, availableVehicles, inUseVehicles, availableDrivers, busyDrivers, overdue);
        }

        var adminReviewer = canReview ? await ApprovalAsync(FleetRequestStatuses.PendingAdminReview, "Fleet.ADMIN_", now, dayStart, dayEnd, ct) : null;
        var director = canDirector ? await ApprovalAsync(FleetRequestStatuses.PendingDirector, "Fleet.DIRECTOR_", now, dayStart, dayEnd, ct) : null;
        var approvalBadge = (adminReviewer?.Pending ?? 0) + (director?.Pending ?? 0);

        FleetDashboardAdminDto? admin = null;
        if (canManage)
        {
            var requests = await db.FleetRequests.AsNoTracking().CountAsync(x => x.CreatedAt >= monthStart, ct);
            var trips = await db.FleetTripRecords.AsNoTracking().CountAsync(x => x.ActualStartAt >= monthStart, ct);
            var distance = await db.FleetTripRecords.AsNoTracking().Where(x => x.ActualEndAt >= monthStart && x.EndMileage != null).SumAsync(x => (decimal?)(x.EndMileage!.Value - x.StartMileage), ct) ?? 0;
            var cancelled = await db.FleetRequests.AsNoTracking().CountAsync(x => x.CancelledAt >= monthStart, ct);
            var overdue = await db.FleetRequests.AsNoTracking().CountAsync(x => x.ExpectedReturnAt < now && x.Status == FleetRequestStatuses.InProgress, ct);
            var failedOutbox = await db.OutboxMessages.AsNoTracking().CountAsync(x => x.Scope == "FLEET" && (x.Status == "FAILED" || x.Status == "RETRY"), ct);
            admin = new(requests, trips, distance, cancelled, overdue, failedOutbox);
        }

        FleetDashboardFeedbackDto? feedback = null;
        if (canManageFeedback)
        {
            var feedbackTrips = db.FleetTripRecords.AsNoTracking()
                .Where(x => x.ActualEndAt >= monthStart && x.FleetRequest!.Status == FleetRequestStatuses.Completed);
            var eligible = await feedbackTrips.SelectMany(x => x.Participants)
                .CountAsync(x => x.IsActualParticipant && x.ParticipantType == FleetPassengerTypes.Employee && x.UserId != null && x.UserId != x.Trip!.DriverUserId, ct);
            var feedbackRows = feedbackTrips.SelectMany(x => x.Feedbacks);
            var feedbackCount = await feedbackRows.CountAsync(ct);
            var threshold = Math.Clamp(options.Value.FeedbackAttentionThreshold, 1, 5);
            feedback = new(
                feedbackCount,
                eligible == 0 ? 0 : Math.Round(feedbackCount * 100m / eligible, 2),
                await feedbackRows.Select(x => (double?)x.OverallRating).AverageAsync(ct),
                await feedbackRows.Select(x => (double?)x.SafetyRating).AverageAsync(ct),
                await feedbackRows.CountAsync(x => x.HasIncident, ct),
                await feedbackRows.CountAsync(x => x.HasIncident || x.OverallRating <= threshold || x.SafetyRating <= threshold, ct),
                threshold);
        }

        var capabilities = new FleetDashboardCapabilitiesDto(canRequester, canDriver, canDispatch, canReview, canDirector, canCalendar, canReports, canManage, permissions.DelegatedPermissions);
        return new(capabilities, new(dispatchBadge, reviewBadge, approvalBadge, myDriverJobs), new(todayJobs, availableVehicles, inUseVehicles, unavailableVehicles), requester, driver, dispatcher, adminReviewer, director, admin, feedback, now);
    }

    internal static (DateTime DayStartUtc, DateTime DayEndUtc, DateTime MonthStartUtc) GetBangkokDashboardRange(DateTime utcNow)
    {
        var normalizedUtc = utcNow.Kind == DateTimeKind.Utc
            ? utcNow
            : DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(normalizedUtc, BangkokTimeZone);
        var localDay = DateOnly.FromDateTime(localNow);
        var localMonth = new DateOnly(localDay.Year, localDay.Month, 1);
        return (
            BangkokDateStartUtc(localDay),
            BangkokDateStartUtc(localDay.AddDays(1)),
            BangkokDateStartUtc(localMonth));
    }

    private static DateTime BangkokDateStartUtc(DateOnly date)
    {
        var localMidnight = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(localMidnight, BangkokTimeZone);
    }

    private static readonly TimeZoneInfo BangkokTimeZone = ResolveBangkokTimeZone();

    private static TimeZoneInfo ResolveBangkokTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
    }

    private async Task<FleetDashboardApprovalDto> ApprovalAsync(string pendingStatus, string actionPrefix, DateTime now, DateTime dayStart, DateTime dayEnd, CancellationToken ct)
    {
        var query = db.FleetRequests.AsNoTracking();
        var pending = await query.CountAsync(x => x.Status == pendingStatus, ct);
        var urgent = await query.CountAsync(x => x.Status == pendingStatus && x.IsUrgent, ct);
        var near = await query.CountAsync(x => x.Status == pendingStatus && x.DepartureAt >= now && x.DepartureAt < now.AddDays(2), ct);
        var actions = db.FleetRequestStatusHistories.AsNoTracking().Where(x => x.CreatedAt >= dayStart && x.CreatedAt < dayEnd && x.Action.StartsWith(actionPrefix));
        var approved = await actions.CountAsync(x => x.Action.EndsWith("APPROVE"), ct);
        var returned = await actions.CountAsync(x => x.Action.EndsWith("RETURN"), ct);
        var rejected = await actions.CountAsync(x => x.Action.EndsWith("REJECT"), ct);
        return new(pending, urgent, near, approved, returned, rejected);
    }
}
