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
