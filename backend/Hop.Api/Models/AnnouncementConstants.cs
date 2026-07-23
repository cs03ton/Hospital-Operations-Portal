namespace Hop.Api.Models;

public static class AnnouncementStatuses
{
    public const string Draft = "Draft";
    public const string Scheduled = "Scheduled";
    public const string Published = "Published";
    public const string Expired = "Expired";
    public const string Archived = "Archived";
    public const string Cancelled = "Cancelled";
}

public static class AnnouncementPriorities
{
    public const string Normal = "Normal";
    public const string Important = "Important";
    public const string Critical = "Critical";
}

public static class AnnouncementTargetTypes
{
    public const string Everyone = "Everyone";
    public const string Role = "Role";
    public const string Department = "Department";
    public const string User = "User";
    public const string Permission = "Permission";
}
