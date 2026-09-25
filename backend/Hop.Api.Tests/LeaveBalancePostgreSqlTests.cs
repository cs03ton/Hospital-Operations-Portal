using System.Security.Claims;
using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace Hop.Api.Tests;

public sealed class LeaveBalancePostgreSqlTests
{
    [Fact]
    public async Task ConcurrentFinalApprovals_CreateOneBalanceAndKeepBothUsageUpdates()
    {
        var adminConnection = Environment.GetEnvironmentVariable("HOP_LEAVE_TEST_ADMIN_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(adminConnection)) return;
        var adminOptions = new NpgsqlConnectionStringBuilder(adminConnection);
        if (adminOptions.Host is not ("localhost" or "127.0.0.1"))
            throw new InvalidOperationException("Leave integration tests require local PostgreSQL.");

        var databaseName = $"hop_leave_test_{Guid.NewGuid():N}";
        var testOptions = new NpgsqlConnectionStringBuilder(adminConnection) { Database = databaseName };
        await using var admin = new NpgsqlConnection(adminConnection);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE {databaseName}", admin)) await create.ExecuteNonQueryAsync();
        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(testOptions.ConnectionString).Options;
            Guid directorId;
            Guid userId;
            Guid typeId;
            Guid[] requestIds;
            await using (var setup = new AppDbContext(options))
            {
                await setup.Database.MigrateAsync();
                var staff = new User { Id = Guid.NewGuid(), Username = "staff", FullName = "Staff", IsActive = true };
                var director = new User { Id = Guid.NewGuid(), Username = "director", FullName = "Director", IsActive = true };
                var type = new LeaveType { Id = Guid.NewGuid(), Code = "VACATION_LEAVE", Name = "Vacation", DefaultDaysPerYear = 10, RequiresBalance = true, IsActive = true };
                var permission = new Permission { Id = Guid.NewGuid(), Code = "LeaveApproval.ApproveCurrentStep", Name = "Approve", IsActive = true };
                var role = new Role { Id = Guid.NewGuid(), Name = "Director", IsActive = true };
                setup.AddRange(staff, director, type, permission, role,
                    new RolePermission { RoleId = role.Id, PermissionId = permission.Id },
                    new UserRole { UserId = director.Id, RoleId = role.Id });
                requestIds = [Guid.NewGuid(), Guid.NewGuid()];
                foreach (var id in requestIds)
                {
                    var request = new LeaveRequest
                    {
                        Id = id, UserId = staff.Id, LeaveTypeId = type.Id, StartDate = new DateOnly(2026, 9, 1),
                        EndDate = new DateOnly(2026, 9, 2), DurationType = "FULL_DAY", TotalDays = 2,
                        Reason = "Test", Status = "Pending", CurrentApproverId = director.Id,
                        SubmittedAt = DateTime.UtcNow
                    };
                    request.Approvals.Add(new LeaveApproval
                    {
                        Id = Guid.NewGuid(), LeaveRequestId = id, ApproverId = director.Id, StepOrder = 1,
                        StepName = "Director", RequiredPermissionCode = permission.Code, Status = "Pending"
                    });
                    setup.LeaveRequests.Add(request);
                }
                await setup.SaveChangesAsync();
                directorId = director.Id;
                userId = staff.Id;
                typeId = type.Id;
            }

            async Task Approve(Guid requestId)
            {
                await using var db = new AppDbContext(options);
                var controller = Controller(db, directorId);
                var result = await controller.ApproveLeaveRequest(requestId, new LeaveDecisionRequest("Approved"));
                Assert.IsType<ApiResponse<LeaveRequestResponse>>(result.Value);
            }
            await Task.WhenAll(requestIds.Select(Approve));

            await using var verify = new AppDbContext(options);
            var balance = Assert.Single(await verify.LeaveBalances.Where(x => x.UserId == userId && x.LeaveTypeId == typeId && x.Year == 2026).ToListAsync());
            Assert.Equal(4, balance.UsedDays);
            Assert.Equal(0, balance.PendingDays);
            Assert.Equal(2, await verify.LeaveRequests.CountAsync(x => requestIds.Contains(x.Id) && x.Status == "Approved"));
        }
        finally
        {
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS {databaseName} WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static LeaveRequestsController Controller(AppDbContext db, Guid actorId)
    {
        var controller = new LeaveRequestsController(db, new Phase1CriticalLeaveWorkflowTests.NoopAuditLogService(), new Phase1CriticalLeaveWorkflowTests.ValidLeaveValidationService(),
            new LeavePolicyService(db), new LeaveCalendarService(db), new Phase1CriticalLeaveWorkflowTests.EmptyApprovalChainService(),
            new Phase1CriticalLeaveWorkflowTests.FakeAttachmentStorageService(), new Phase1CriticalLeaveWorkflowTests.FakeLeavePdfService(), new Phase1CriticalLeaveWorkflowTests.CleanFileScanningService(),
            new CaptureNotificationPublisher(), new LeaveRequestAccessService(db), new Phase1CriticalLeaveWorkflowTests.StaticRequestNumberService(),
            new ConfigurationBuilder().Build(), NullLogger<LeaveRequestsController>.Instance);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, actorId.ToString())], "Test"))
        } };
        return controller;
    }

    private sealed class CaptureNotificationPublisher : ILeaveNotificationEventPublisher
    {
        public Task PublishAsync(string eventName, Guid leaveRequestId, Guid? recipientUserId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
