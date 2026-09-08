namespace Hop.Api.DTOs;

public sealed record FleetFeedbackTripSummaryDto(
    Guid TripId, Guid FleetRequestId, string RequestNo, DateTime CompletedAt, string Destination,
    Guid VehicleId, string Vehicle, Guid DriverUserId, string Driver, string Department, string MissionType,
    int EligibleParticipants, int FeedbackCount, decimal ResponseRate, double? OverallAverage,
    double? SafetyAverage, double? PunctualityAverage, double? ServiceAverage,
    double? VehicleConditionAverage, double? VehicleCleanlinessAverage, int IncidentCount);

public sealed record FleetFeedbackDriverSummaryDto(
    Guid DriverUserId, string Driver, int Trips, int EligibleParticipants, int FeedbackCount,
    decimal ResponseRate, double? PunctualityAverage, double? SafetyAverage,
    double? ServiceAverage, double? OverallAverage, int IncidentCount);

public sealed record FleetFeedbackVehicleSummaryDto(
    Guid VehicleId, string Vehicle, string RegistrationNumber, int Trips, int FeedbackCount,
    double? VehicleConditionAverage, double? VehicleCleanlinessAverage, int IncidentCount);

public sealed record FleetFeedbackManagementSummaryDto(
    int Trips, int EligibleParticipants, int FeedbackCount, decimal ResponseRate,
    double? OverallAverage, double? SafetyAverage, int IncidentCount, int AttentionCount,
    int AttentionThreshold);

public sealed record FleetFeedbackAttentionDto(
    Guid FeedbackId, Guid TripId, Guid FleetRequestId, string RequestNo, DateTime CompletedAt,
    string Destination, string Vehicle, Guid DriverUserId, string Driver,
    int OverallRating, int SafetyRating, bool HasIncident, string? IncidentCategory,
    string? Comment, DateTime SubmittedAt);

public sealed record FleetFeedbackTrendPointDto(
    int Year, int Month, int FeedbackCount, double? OverallAverage,
    double? SafetyAverage, double? PunctualityAverage);

public sealed record FleetFeedbackReportPageDto<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalItems, int TotalPages);
