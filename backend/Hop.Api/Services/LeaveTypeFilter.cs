namespace Hop.Api.Services;

public static class LeaveTypeFilter
{
    public static Guid[] ParseIds(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return [];
        var parts = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || parts.Length > 100 || parts.Any(part => !Guid.TryParse(part, out _))) return [];
        return parts.Select(Guid.Parse).Distinct().ToArray();
    }
}
