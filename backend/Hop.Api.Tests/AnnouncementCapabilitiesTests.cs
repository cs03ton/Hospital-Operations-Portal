using Hop.Api.Authorization;
using Hop.Api.Controllers;
using Hop.Api.Models;
using Xunit;

namespace Hop.Api.Tests;

public sealed class AnnouncementCapabilitiesTests
{
    [Fact]
    public void EditOwn_is_limited_to_the_creator()
    {
        var ownerId = Guid.NewGuid();
        var item = new Announcement { CreatedByUserId = ownerId, Status = AnnouncementStatuses.Draft };
        var permissions = new HashSet<string> { AnnouncementPermissions.EditOwn };

        Assert.True(AdminAnnouncementsController.BuildCapabilities(item, ownerId, permissions).CanEdit);
        Assert.False(AdminAnnouncementsController.BuildCapabilities(item, Guid.NewGuid(), permissions).CanEdit);
    }

    [Fact]
    public void Actions_follow_individual_permissions_and_status()
    {
        var item = new Announcement { CreatedByUserId = Guid.NewGuid(), Status = AnnouncementStatuses.Draft };
        var permissions = new HashSet<string> { AnnouncementPermissions.Publish, AnnouncementPermissions.DeleteDraft };

        var capabilities = AdminAnnouncementsController.BuildCapabilities(item, Guid.NewGuid(), permissions);

        Assert.True(capabilities.CanPublish);
        Assert.True(capabilities.CanDelete);
        Assert.False(capabilities.CanEdit);
        Assert.False(capabilities.CanArchive);
        Assert.False(capabilities.CanCancel);
        Assert.False(capabilities.CanDuplicate);
    }

    [Fact]
    public void Archived_or_cancelled_announcements_cannot_be_edited_or_deleted()
    {
        var item = new Announcement { CreatedByUserId = Guid.NewGuid(), Status = AnnouncementStatuses.Archived };
        var capabilities = AdminAnnouncementsController.BuildCapabilities(item, item.CreatedByUserId, new HashSet<string> { AnnouncementPermissions.Manage });

        Assert.False(capabilities.CanEdit);
        Assert.False(capabilities.CanDelete);
        Assert.False(capabilities.CanArchive);
    }
}
