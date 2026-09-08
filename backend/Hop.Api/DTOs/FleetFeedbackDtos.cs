using System.ComponentModel.DataAnnotations;

namespace Hop.Api.DTOs;

public sealed record FleetFeedbackContextDto(
    Guid TripId,
    string RequestNo,
    DateTime TripDate,
    string Destination,
    string VehicleDisplay,
    string DriverDisplay,
    DateTime CompletedAt,
    DateTime FeedbackDeadline,
    bool CanSubmitFeedback,
    string FeedbackStatus);

public sealed record FleetFeedbackOwnDto(
    Guid Id,
    Guid TripId,
    string RequestNo,
    int PunctualityRating,
    int SafetyRating,
    int ServiceRating,
    int OverallRating,
    int VehicleConditionRating,
    int VehicleCleanlinessRating,
    bool HasIncident,
    string? IncidentCategory,
    string? Comment,
    DateTime SubmittedAt);

public sealed class SubmitFleetTripFeedbackRequest : IValidatableObject
{
    [Range(1, 5)] public int PunctualityRating { get; set; }
    [Range(1, 5)] public int SafetyRating { get; set; }
    [Range(1, 5)] public int ServiceRating { get; set; }
    [Range(1, 5)] public int OverallRating { get; set; }
    [Range(1, 5)] public int VehicleConditionRating { get; set; }
    [Range(1, 5)] public int VehicleCleanlinessRating { get; set; }
    public bool HasIncident { get; set; }
    [MaxLength(40)] public string? IncidentCategory { get; set; }
    [MaxLength(2000)] public string? Comment { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var category = IncidentCategory?.Trim().ToUpperInvariant();
        var comment = Comment?.Trim();
        if (HasIncident)
        {
            if (string.IsNullOrWhiteSpace(category)) yield return new("กรุณาเลือกประเภทเหตุการณ์", [nameof(IncidentCategory)]);
            else if (!Models.FleetFeedbackIncidentCategories.All.Contains(category)) yield return new("ประเภทเหตุการณ์ไม่ถูกต้อง", [nameof(IncidentCategory)]);
            if (string.IsNullOrWhiteSpace(comment)) yield return new("กรุณาระบุรายละเอียดเหตุการณ์", [nameof(Comment)]);
        }
        else if (!string.IsNullOrWhiteSpace(category))
        {
            yield return new("ไม่ต้องระบุประเภทเหตุการณ์เมื่อเลือกว่าปลอดเหตุการณ์", [nameof(IncidentCategory)]);
        }
    }
}
