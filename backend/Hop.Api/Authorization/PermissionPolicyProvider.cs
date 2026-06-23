using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Hop.Api.Authorization;

public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public const string PolicyPrefix = "Permission:";
    public const string AnyPolicyPrefix = "PermissionAny:";

    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(AnyPolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var permissionCodes = policyName[AnyPolicyPrefix.Length..]
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var anyPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permissionCodes))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(anyPolicy);
        }

        if (!policyName.StartsWith(PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return base.GetPolicyAsync(policyName);
        }

        var permissionCode = policyName[PolicyPrefix.Length..];
        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permissionCode))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
