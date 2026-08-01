using Hop.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Hop.Api.Tests;

public sealed class LineLiffSecurityTests
{
    [Fact]
    public void LoginWithLineLiff_AllowsAnonymousAccess()
    {
        var actionAttributes = typeof(AuthController)
            .GetMethod(nameof(AuthController.LoginWithLineLiff))!
            .GetCustomAttributes(inherit: true);

        Assert.Contains(actionAttributes, attribute => attribute is AllowAnonymousAttribute);
    }

    [Fact]
    public void MeLineLink_RequiresAuthenticatedUser()
    {
        var controllerAttributes = typeof(MeLineController).GetCustomAttributes(inherit: true);
        var actionAttributes = typeof(MeLineController)
            .GetMethod(nameof(MeLineController.Link))!
            .GetCustomAttributes(inherit: true);

        Assert.Contains(controllerAttributes, attribute => attribute is AuthorizeAttribute);
        Assert.DoesNotContain(actionAttributes, attribute => attribute is AllowAnonymousAttribute);
    }
}
