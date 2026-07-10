namespace Hop.Api.Configuration;

public sealed class PasswordPolicyOptions
{
    public int MinimumLength { get; set; } = 8;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireDigit { get; set; } = true;
    public bool RequireSpecialCharacter { get; set; } = true;
    public int PasswordHistoryCount { get; set; }
    public int ExpireDays { get; set; }
    public bool DisallowUsername { get; set; } = true;
}
