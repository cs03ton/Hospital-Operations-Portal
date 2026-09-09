using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.Interfaces;
using Hop.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Hop.Api.Tests;

public sealed class LeaveDependencyInjectionTests
{
    [Fact]
    public void Production_leave_graph_has_one_selected_constructor_per_service()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase($"leave-di-{Guid.NewGuid()}"));
        services.AddScoped<IAuditLogService, FakeAuditLogService>();
        services.AddScoped<ILeavePolicyService, LeavePolicyService>();
        services.AddScoped<ILeaveEntitlementService, LeaveEntitlementService>();
        services.AddScoped<ILeaveBalanceValidationService, LeaveBalanceValidationService>();
        services.AddScoped<ILeaveBalanceReconciliationService, LeaveBalanceReconciliationService>();
        services.AddScoped<ILeaveBalanceRolloverService, LeaveBalanceRolloverService>();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        Assert.IsType<LeavePolicyService>(scope.ServiceProvider.GetRequiredService<ILeavePolicyService>());
        Assert.IsType<LeaveEntitlementService>(scope.ServiceProvider.GetRequiredService<ILeaveEntitlementService>());
        Assert.IsType<LeaveBalanceValidationService>(scope.ServiceProvider.GetRequiredService<ILeaveBalanceValidationService>());
        Assert.IsType<LeaveBalanceRolloverService>(scope.ServiceProvider.GetRequiredService<ILeaveBalanceRolloverService>());
        Assert.IsType<LeaveBalancesController>(
            ActivatorUtilities.CreateInstance<LeaveBalancesController>(scope.ServiceProvider));
    }

    private sealed class FakeAuditLogService : IAuditLogService
    {
        public Task WriteAsync(
            Guid? userId,
            string action,
            string resource,
            string? resourceId,
            string? detail,
            string result = "Success",
            HttpContext? httpContext = null) => Task.CompletedTask;
    }
}
