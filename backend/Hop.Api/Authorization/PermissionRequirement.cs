using Microsoft.AspNetCore.Authorization;

namespace Hop.Api.Authorization;

public sealed class PermissionRequirement(params string[] permissionCodes) : IAuthorizationRequirement
{
    public IReadOnlyList<string> PermissionCodes { get; } = permissionCodes;
}
