using Hop.Api.DTOs;

namespace Hop.Api.Interfaces;

public interface IPasswordPolicyService
{
    PasswordPolicyResponse GetPolicy();
    IReadOnlyList<string> Validate(string password, string? username);
}
