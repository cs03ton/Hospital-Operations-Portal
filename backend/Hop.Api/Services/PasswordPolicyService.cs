using Hop.Api.Configuration;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Microsoft.Extensions.Options;

namespace Hop.Api.Services;

public sealed class PasswordPolicyService(IOptions<PasswordPolicyOptions> options) : IPasswordPolicyService
{
    private readonly PasswordPolicyOptions policy = options.Value;

    public PasswordPolicyResponse GetPolicy()
    {
        return new PasswordPolicyResponse(
            policy.MinimumLength,
            policy.RequireUppercase,
            policy.RequireLowercase,
            policy.RequireDigit,
            policy.RequireSpecialCharacter,
            policy.DisallowUsername);
    }

    public IReadOnlyList<string> Validate(string password, string? username)
    {
        var errors = new List<string>();
        var minimumLength = Math.Max(1, policy.MinimumLength);

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("กรุณากรอกรหัสผ่านใหม่");
            return errors;
        }

        if (password.Length < minimumLength)
        {
            errors.Add($"รหัสผ่านต้องมีความยาวอย่างน้อย {minimumLength} ตัวอักษร");
        }

        if (policy.RequireUppercase && !password.Any(char.IsUpper))
        {
            errors.Add("รหัสผ่านต้องมีตัวพิมพ์ใหญ่อย่างน้อย 1 ตัว");
        }

        if (policy.RequireLowercase && !password.Any(char.IsLower))
        {
            errors.Add("รหัสผ่านต้องมีตัวพิมพ์เล็กอย่างน้อย 1 ตัว");
        }

        if (policy.RequireDigit && !password.Any(char.IsDigit))
        {
            errors.Add("รหัสผ่านต้องมีตัวเลขอย่างน้อย 1 ตัว");
        }

        if (policy.RequireSpecialCharacter && !password.Any(IsSpecialCharacter))
        {
            errors.Add("รหัสผ่านต้องมีอักขระพิเศษอย่างน้อย 1 ตัว");
        }

        if (policy.DisallowUsername &&
            !string.IsNullOrWhiteSpace(username) &&
            password.Contains(username.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("รหัสผ่านต้องไม่มีชื่อผู้ใช้เป็นส่วนหนึ่งของรหัสผ่าน");
        }

        return errors;
    }

    private static bool IsSpecialCharacter(char value)
    {
        return !char.IsLetterOrDigit(value);
    }
}
