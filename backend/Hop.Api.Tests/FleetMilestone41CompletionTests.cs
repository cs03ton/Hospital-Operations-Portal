using Hop.Api.Models;
using Hop.Api.Services;
using Xunit;

namespace Hop.Api.Tests;

public sealed class FleetMilestone41CompletionTests
{
    private static readonly DateTime Start = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = Start.AddHours(10);

    [Theory]
    [MemberData(nameof(Intervals))]
    public void Interval_union_clamps_and_deduplicates((DateTime Start, DateTime End)[] intervals, decimal expected) => Assert.Equal(expected, FleetIntervalAggregation.UnionHours(intervals, Start, End));
    public static IEnumerable<object[]> Intervals()
    {
        yield return [new[] { (Start.AddHours(1), Start.AddHours(2)) }, 1m];
        yield return [new[] { (Start.AddHours(-2), Start.AddHours(2)) }, 2m];
        yield return [new[] { (Start.AddHours(8), End.AddHours(2)) }, 2m];
        yield return [new[] { (Start.AddHours(-2), End.AddHours(2)) }, 10m];
        yield return [new[] { (End.AddHours(1), End.AddHours(2)) }, 0m];
        yield return [new[] { (Start.AddHours(1), Start.AddHours(2)), (Start.AddHours(3), Start.AddHours(4)) }, 2m];
        yield return [new[] { (Start.AddHours(1), Start.AddHours(4)), (Start.AddHours(3), Start.AddHours(6)) }, 5m];
        yield return [new[] { (Start.AddHours(1), Start.AddHours(8)), (Start.AddHours(3), Start.AddHours(4)) }, 7m];
        yield return [new[] { (Start.AddHours(1), Start.AddHours(3)), (Start.AddHours(3), Start.AddHours(5)) }, 4m];
    }

    [Fact]
    public void Maintenance_overlapping_unavailability_is_not_double_counted() => Assert.Equal(5m, FleetIntervalAggregation.UnionHours([(Start, Start.AddHours(3)), (Start.AddHours(2), Start.AddHours(5))], Start, End));

    [Fact]
    public async Task Normal_mileage_does_not_require_override_permission()
    {
        var service = new FleetMaintenanceMileageAuthorizationService(new Permission(false), new Delegation()); var result = await service.Validate(Guid.NewGuid(), 100, 110, null, default); Assert.False(result.IsOverride); Assert.Equal(110, result.RetainedMileage);
    }
    [Fact]
    public async Task Lower_mileage_without_permission_is_forbidden()
    {
        var service = new FleetMaintenanceMileageAuthorizationService(new Permission(false), new Delegation()); await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.Validate(Guid.NewGuid(), 100, 90, "meter replaced", default));
    }
    [Fact]
    public async Task Lower_mileage_requires_reason()
    {
        var service = new FleetMaintenanceMileageAuthorizationService(new Permission(true), new Delegation()); await Assert.ThrowsAsync<ArgumentException>(() => service.Validate(Guid.NewGuid(), 100, 90, "", default));
    }
    [Fact]
    public async Task Authorized_override_retains_vehicle_mileage()
    {
        var service = new FleetMaintenanceMileageAuthorizationService(new Permission(true), new Delegation()); var result = await service.Validate(Guid.NewGuid(), 100, 90, "meter replaced", default); Assert.True(result.IsOverride); Assert.Equal(100, result.RetainedMileage);
    }
    private sealed class Permission(bool allowed) : IWorkflowPermissionValidator { public Task<bool> HasAsync(Guid userId, string permission, CancellationToken ct) => Task.FromResult(allowed); }
    private sealed class Delegation : IWorkflowDelegationResolver { public Task<ApprovalDelegation?> ResolveAsync(Guid delegateUserId, string scope, string permission, DateTime atUtc, CancellationToken ct) => Task.FromResult<ApprovalDelegation?>(null); }
}
