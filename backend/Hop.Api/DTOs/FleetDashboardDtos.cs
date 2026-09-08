namespace Hop.Api.DTOs;

public sealed record FleetDashboardCapabilitiesDto(
    bool CanViewOwnRequests, bool CanViewOwnTrips, bool CanDispatch, bool CanAdminReview,
    bool CanDirectorApprove, bool CanViewCalendar, bool CanViewReports, bool CanManageFleet,
    IReadOnlyList<string> DelegatedPermissions);

public sealed record FleetDashboardBadgesDto(int DispatchQueue, int ReviewQueue, int ApprovalQueue, int MyDriverJobs);
public sealed record FleetDashboardSharedDto(int TodayJobs, int AvailableVehicles, int InUseVehicles, int UnavailableVehicles);
public sealed record FleetDashboardRequestPreviewDto(Guid Id, string RequestNo, DateTime DepartureAt, string Destination, string Status, string? Vehicle, string? Driver);
public sealed record FleetDashboardRequesterDto(IReadOnlyDictionary<string, int> StatusCounts, FleetDashboardRequestPreviewDto? NextTrip, int ActionRequired);
public sealed record FleetDashboardDriverDto(int TodayJobs, int UpcomingJobs, int ActionRequired, int CompletedThisMonth, decimal DistanceThisMonth);
public sealed record FleetDashboardDispatcherDto(int PendingDispatch, int ReturnedToDispatcher, int AvailableVehicles, int BusyVehicles, int AvailableDrivers, int BusyDrivers, int OverdueJobs);
public sealed record FleetDashboardApprovalDto(int Pending, int Urgent, int NearDeparture, int CompletedToday, int ReturnedToday, int RejectedToday);
public sealed record FleetDashboardAdminDto(int RequestsThisMonth, int TripsThisMonth, decimal DistanceThisMonth, int CancelledThisMonth, int OverdueJobs, int FailedOutbox);
public sealed record FleetDashboardFeedbackDto(int FeedbackCount, decimal ResponseRate, double? OverallAverage, double? SafetyAverage, int IncidentCount, int AttentionCount, int AttentionThreshold);

public sealed record FleetDashboardDto(
    FleetDashboardCapabilitiesDto Capabilities,
    FleetDashboardBadgesDto Badges,
    FleetDashboardSharedDto Shared,
    FleetDashboardRequesterDto? Requester,
    FleetDashboardDriverDto? Driver,
    FleetDashboardDispatcherDto? Dispatcher,
    FleetDashboardApprovalDto? AdminReviewer,
    FleetDashboardApprovalDto? Director,
    FleetDashboardAdminDto? Admin,
    FleetDashboardFeedbackDto? Feedback,
    DateTime GeneratedAt);
