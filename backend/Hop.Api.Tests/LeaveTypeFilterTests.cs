using Hop.Api.Services;
using Xunit;

namespace Hop.Api.Tests;

public class LeaveTypeFilterTests
{
    [Fact]
    public void Parses_distinct_group_ids_and_rejects_invalid_groups()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        Assert.Equal([first, second], LeaveTypeFilter.ParseIds($"{first},{second},{first}"));
        Assert.Empty(LeaveTypeFilter.ParseIds($"{first},invalid"));
    }
}
