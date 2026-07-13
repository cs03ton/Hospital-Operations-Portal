using System.Reflection;
using Hop.Api.Authorization;
using Hop.Api.Controllers;
using Xunit;

namespace Hop.Api.Tests;

public class AdminBackupCenterTests
{
    [Theory]
    [InlineData(nameof(AdminBackupsController.GetOverview), BackupPermissions.View)]
    [InlineData(nameof(AdminBackupsController.GetBackups), BackupPermissions.View)]
    [InlineData(nameof(AdminBackupsController.GetBackup), BackupPermissions.View)]
    [InlineData(nameof(AdminBackupsController.VerifyBackup), BackupPermissions.Run)]
    [InlineData(nameof(AdminBackupsController.RestorePreview), BackupPermissions.Restore)]
    [InlineData(nameof(AdminBackupsController.Restore), BackupPermissions.Restore)]
    [InlineData(nameof(AdminBackupsController.GetRestoreRuns), BackupPermissions.Restore)]
    [InlineData(nameof(AdminBackupsController.PreviewRetention), BackupPermissions.ManageRetention)]
    [InlineData(nameof(AdminBackupsController.ApplyRetention), BackupPermissions.ManageRetention)]
    public void AdminBackupEndpoints_RequireExpectedGranularPermissions(string methodName, string expectedPermission)
    {
        var methods = typeof(AdminBackupsController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => method.Name == methodName)
            .ToList();

        Assert.NotEmpty(methods);
        Assert.Contains(methods, method =>
            method.GetCustomAttribute<RequirePermissionAttribute>()?.PermissionCode == expectedPermission);
    }
}
