using System.Reflection;
using Hop.Api.Authorization;
using Hop.Api.Controllers;
using Xunit;

namespace Hop.Api.Tests;

public sealed class FleetCalendarAuthorizationTests
{
    [Fact]
    public void Get_requires_calendar_view_permission()
    {
        var method = typeof(FleetCalendarController).GetMethod(nameof(FleetCalendarController.Get));

        Assert.NotNull(method);
        Assert.Equal(
            FleetPermissions.CalendarView,
            method!.GetCustomAttribute<RequirePermissionAttribute>()?.PermissionCode);
    }
}
