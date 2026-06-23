using Xunit;

namespace Hop.Api.Tests;

public class LeaveCleanupScriptTests
{
    [Fact]
    public void ClearLeaveDevDataScript_DoesNotDeleteCoreIdentityTables()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "database", "scripts", "clear-leave-dev-data.sql"));
        var script = File.ReadAllText(path);

        Assert.Contains("DEVELOPMENT ONLY", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM users", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM departments", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM roles", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM permissions", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DELETE FROM leave_requests", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DELETE FROM approval_chains", script, StringComparison.OrdinalIgnoreCase);
    }
}
