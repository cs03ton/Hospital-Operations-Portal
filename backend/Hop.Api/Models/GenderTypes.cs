namespace Hop.Api.Models;

public static class GenderTypes
{
    public const string Male = "Male";
    public const string Female = "Female";
    public const string Unknown = "Unknown";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Male,
        Female,
        Unknown
    };

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Unknown;
        }

        return value.Trim() switch
        {
            var item when item.Equals(Male, StringComparison.OrdinalIgnoreCase) => Male,
            var item when item.Equals(Female, StringComparison.OrdinalIgnoreCase) => Female,
            _ => Unknown
        };
    }

    public static string GetThaiLabel(string? value)
    {
        return Normalize(value) switch
        {
            Male => "ชาย",
            Female => "หญิง",
            _ => "ไม่ระบุ"
        };
    }
}
