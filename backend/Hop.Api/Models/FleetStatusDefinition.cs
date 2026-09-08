namespace Hop.Api.Models;

public class FleetStatusDefinition
{
    public Guid Id { get; set; }
    public string Domain { get; set; } = FleetStatusDomains.Request;
    public string Code { get; set; } = string.Empty;
    public string ThaiName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public static class FleetStatusDomains
{
    public const string Request = "REQUEST";
}
