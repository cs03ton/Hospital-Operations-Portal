using Hop.Api.DTOs;
using Xunit;

namespace Hop.Api.Tests;

public class UserProfileTests
{
    [Fact]
    public void UpdateUserProfileRequest_DoesNotExposeRestrictedFields()
    {
        var propertyNames = typeof(UpdateUserProfileRequest)
            .GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("Fullname", propertyNames);
        Assert.Contains("Position", propertyNames);
        Assert.Contains("PhoneNumber", propertyNames);
        Assert.Contains("LeaveContactAddress", propertyNames);

        Assert.DoesNotContain("ProfileImageUrl", propertyNames);
        Assert.DoesNotContain("RoleIds", propertyNames);
        Assert.DoesNotContain("DepartmentId", propertyNames);
        Assert.DoesNotContain("LeaveApprovalRuleId", propertyNames);
        Assert.DoesNotContain("LineUserId", propertyNames);
        Assert.DoesNotContain("IsActive", propertyNames);
        Assert.DoesNotContain("Permissions", propertyNames);
    }
}
