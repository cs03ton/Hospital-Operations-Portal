using System.Reflection;
using Hop.Api.Authorization;
using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hop.Api.Tests;

public class LeaveSupportDelegationOverrideTests
{
    [Fact]
    public async Task CreateDelegation_RequiresReason()
    {
        await using var db = CreateDbContext();
        var controller = new ApprovalDelegationsController(db, new NoopAuditLogService());

        var result = await controller.CreateDelegation(new SaveApprovalDelegationRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 3),
            "",
            true));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateDelegation_BlocksDuplicateActiveDateRange()
    {
        await using var db = CreateDbContext();
        var approverId = Guid.NewGuid();
        db.ApprovalDelegations.Add(new ApprovalDelegation
        {
            ApproverUserId = approverId,
            DelegateUserId = Guid.NewGuid(),
            StartDate = new DateOnly(2026, 7, 1),
            EndDate = new DateOnly(2026, 7, 5),
            Reason = "Existing",
            IsActive = true
        });
        await db.SaveChangesAsync();
        var controller = new ApprovalDelegationsController(db, new NoopAuditLogService());

        var result = await controller.CreateDelegation(new SaveApprovalDelegationRequest(
            approverId,
            Guid.NewGuid(),
            new DateOnly(2026, 7, 3),
            new DateOnly(2026, 7, 7),
            "Overlap",
            true));

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public void OverrideEndpoints_RequireOverridePermission()
    {
        var approve = typeof(LeaveRequestsController).GetMethod(nameof(LeaveRequestsController.OverrideApproveLeaveRequest));
        var reject = typeof(LeaveRequestsController).GetMethod(nameof(LeaveRequestsController.OverrideRejectLeaveRequest));

        Assert.Equal(LeavePermissions.Override, GetPermission(approve));
        Assert.Equal(LeavePermissions.Override, GetPermission(reject));
    }

    private static string? GetPermission(MethodInfo? method)
    {
        return method?.GetCustomAttribute<RequirePermissionAttribute>()?.PermissionCode;
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private sealed class NoopAuditLogService : IAuditLogService
    {
        public Task WriteAsync(Guid? userId, string action, string resource, string? resourceId, string? detail, string result = "Success", HttpContext? httpContext = null)
        {
            return Task.CompletedTask;
        }
    }
}
