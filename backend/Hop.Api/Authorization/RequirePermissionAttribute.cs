using Microsoft.AspNetCore.Authorization;

namespace Hop.Api.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permissionCode)
    {
        PermissionCode = permissionCode;
        Policy = $"{PermissionPolicyProvider.PolicyPrefix}{permissionCode}";
    }

    public string PermissionCode { get; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireAnyPermissionAttribute : AuthorizeAttribute
{
    public RequireAnyPermissionAttribute(params string[] permissionCodes)
    {
        PermissionCodes = permissionCodes;
        Policy = $"{PermissionPolicyProvider.AnyPolicyPrefix}{string.Join(",", permissionCodes)}";
    }

    public IReadOnlyList<string> PermissionCodes { get; }
}
