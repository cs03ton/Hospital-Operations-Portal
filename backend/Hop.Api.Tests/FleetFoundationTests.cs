using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hop.Api.Tests;

public sealed class FleetFoundationTests
{
    [Fact]
    public void FleetPermissionCodes_FollowExistingConvention_AndAreUnique()
    {
        var values = typeof(FleetPermissions).GetFields().Select(x => (string)x.GetRawConstantValue()!).ToArray();
        Assert.Equal(values.Length, values.Distinct(StringComparer.Ordinal).Count());
        Assert.All(values, value => Assert.Matches("^[A-Z][A-Za-z]+\\.[A-Z][A-Za-z]+$", value));
    }

    [Fact]
    public void FleetModel_UsesExistingUserAsDriverIdentity_AndProtectsUniqueness()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var model = db.Model.FindEntityType(typeof(FleetDriverProfile))!;
        Assert.Contains(model.GetForeignKeys(), fk => fk.PrincipalEntityType.ClrType == typeof(User) && fk.Properties.Single().Name == nameof(FleetDriverProfile.UserId));
        Assert.Contains(model.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == nameof(FleetDriverProfile.UserId));
    }

    [Fact]
    public void FleetStatusSets_ContainOnlySupportedMvpValues()
    {
        Assert.Equal(6, FleetVehicleStatuses.All.Count);
        Assert.Equal(3, FleetDriverStatuses.All.Count);
        Assert.Contains(FleetVehicleStatuses.Maintenance, FleetVehicleStatuses.All);
        Assert.Contains(FleetDriverStatuses.Suspended, FleetDriverStatuses.All);
    }
}
