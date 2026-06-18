using System.IdentityModel.Tokens.Jwt;
using Hop.Api.Authorization;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Hop.Api.Tests;

public class AuthAndPermissionTests
{
    [Fact]
    public void RequirePermissionAttribute_BuildsPermissionPolicyName()
    {
        var attribute = new RequirePermissionAttribute("LeaveManagement.View");

        Assert.Equal("LeaveManagement.View", attribute.PermissionCode);
        Assert.Equal("Permission:LeaveManagement.View", attribute.Policy);
    }

    [Fact]
    public void JwtTokenService_GeneratesAccessTokenWithUserClaims()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-secret-key-that-is-long-enough-32",
                ["Jwt:Issuer"] = "Hop.Api.Tests",
                ["Jwt:Audience"] = "Hop.Client.Tests"
            })
            .Build();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "tester",
            FullName = "Test User"
        };
        var service = new JwtTokenService(configuration);

        var token = service.GenerateAccessToken(user, "Staff");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("Hop.Api.Tests", jwt.Issuer);
        Assert.Contains(jwt.Claims, claim => claim.Type == "username" && claim.Value == "tester");
        Assert.Contains(jwt.Claims, claim => claim.Type == "fullname" && claim.Value == "Test User");
    }
}
