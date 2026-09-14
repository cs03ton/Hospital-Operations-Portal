using Hop.Api.Authorization;
using Hop.Api.Models;
using Hop.Api.Services;
using Xunit;

namespace Hop.Api.Tests;

public class RepairWorkflowTests
{
    [Theory]
    [InlineData("Submitted", "start", "InProgress")]
    [InlineData("InProgress", "wait", "WaitingParts")]
    [InlineData("WaitingParts", "resume", "InProgress")]
    [InlineData("InProgress", "solve", "Resolved")]
    [InlineData("Submitted", "return", "Returned")]
    [InlineData("Closed", "solve", null)]
    [InlineData("Resolved", "solve", null)]
    [InlineData("Returned", "start", null)]
    public void TeamTransitions(string status, string action, string? expected)
    {
        var r = new RepairRequest { TeamCode = "IT", Status = status };
        var access = new RepairAccess(Guid.NewGuid(), [RepairPermissions.WorkIT]);
        Assert.Equal(expected, RepairWorkflow.Next(r, action, access));
        Assert.Null(RepairWorkflow.Next(r, action, new RepairAccess(Guid.NewGuid(), [RepairPermissions.WorkGeneral])));
    }

    [Theory]
    [InlineData("Resolved", "accept", "Closed")]
    [InlineData("Resolved", "reject-solution", "InProgress")]
    [InlineData("Closed", "reopen", "Submitted")]
    [InlineData("Returned", "resubmit", "Submitted")]
    public void OnlyRequesterCanAcceptOrReopen(string status, string action, string next)
    {
        var user = Guid.NewGuid(); var r = new RepairRequest { RequesterId = user, TeamCode = "IT", Status = status };
        Assert.Equal(next, RepairWorkflow.Next(r, action, new(user, [RepairPermissions.ViewOwn])));
        Assert.Null(RepairWorkflow.Next(r, action, new(Guid.NewGuid(), [RepairPermissions.WorkIT, RepairPermissions.Manage])));
    }

    [Fact]
    public void DualTeamPermissions_AreExplicit_AndCancellationRespectsStartedWork()
    {
        var user = Guid.NewGuid(); var r = new RepairRequest { RequesterId = user, TeamCode = "IT", HasStarted = true };
        var a = new RepairAccess(user, [RepairPermissions.ViewOwn, RepairPermissions.WorkIT, RepairPermissions.WorkGeneral]);
        Assert.True(a.Work("IT")); Assert.True(a.Work("GENERAL"));
        Assert.Null(RepairWorkflow.Next(r, "cancel", a));
        Assert.Equal("Cancelled", RepairWorkflow.Next(r, "cancel", new(user, [RepairPermissions.Manage])));
        Assert.False(new RepairAccess(Guid.NewGuid(), [RepairPermissions.Create]).View(r));
    }
}
