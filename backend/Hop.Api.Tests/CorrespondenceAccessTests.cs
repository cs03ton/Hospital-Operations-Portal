using System.Security.Claims;
using Hop.Api.Authorization;
using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Hop.Api.Tests;

public class CorrespondenceAccessTests
{
    private static AppDbContext Db() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static ClaimsPrincipal Principal(Guid id) => new(new ClaimsIdentity([new(ClaimTypes.NameIdentifier, id.ToString()), new(ClaimTypes.Role, "SuperAdmin")], "test"));
    private static T Context<T>(T controller, Guid actor) where T : ControllerBase { controller.ControllerContext = new() { HttpContext = new DefaultHttpContext { User = Principal(actor) } }; return controller; }
    private static async Task<(User Admin, User Super, User Target, Role Memo, Role Staff, Permission Use)> Seed(AppDbContext db)
    {
        var use = new Permission { Code = CorrespondenceAccess.Permission, Name = "สารบรรณ", Group = "Correspondence", Action = "Use" };
        var memo = new Role { Name = CorrespondenceAccess.RoleName, RolePermissions = [new() { Permission = use }] };
        var admin = new User { Username = "admin", FullName = "Admin", UserRoles = [new() { Role = new() { Name = "Admin", RolePermissions = [new() { Permission = use }] } }] };
        var super = new User { Username = "super", FullName = "Super", UserRoles = [new() { Role = new() { Name = "SuperAdmin", RolePermissions = [new() { Permission = use }] } }] };
        var staff = new Role { Name = "Staff" }; var target = new User { Username = "target", FullName = "Before", UserRoles = [new() { Role = staff }] };
        db.AddRange(memo, admin, super, target); await db.SaveChangesAsync(); return (admin, super, target, memo, staff, use);
    }
    private static UpdateUserRequest Edit(User user, params Guid[] roles) => new(null, "After", roles, null, null, null, null, null, null, true, null);
    private static UsersController Users(AppDbContext db, Audit audit, Guid actor) => Context(new UsersController(db, audit, null!, new ConfigurationBuilder().Build(), new Entitlements()), actor);
    [Fact]
    public async Task Only_live_superadmin_can_grant_revoke_and_claims_cannot_spoof_it()
    {
        await using var db = Db(); var (admin, super, target, memo, staff, _) = await Seed(db); var audit = new Audit();
        var denied = await Users(db, audit, admin.Id).UpdateUser(target.Id, Edit(target, staff.Id, memo.Id));
        Assert.Equal(403, Assert.IsType<ObjectResult>(denied.Result).StatusCode); Assert.Equal("Before", target.FullName); Assert.Single(target.UserRoles); Assert.Empty(audit.Details);
        Assert.NotNull((await Users(db, audit, super.Id).UpdateUser(target.Id, Edit(target, staff.Id, memo.Id))).Value);
        Assert.Contains(target.UserRoles, r => r.RoleId == memo.Id); Assert.Contains(audit.Details, d => d.Contains(memo.Id.ToString()));
        var unchanged = await Users(db, audit, admin.Id).UpdateUser(target.Id, Edit(target, staff.Id, memo.Id)); Assert.NotNull(unchanged.Value);
        Assert.Equal(403, Assert.IsType<ObjectResult>((await Users(db, audit, admin.Id).UpdateUser(target.Id, Edit(target, staff.Id))).Result).StatusCode);
        Assert.NotNull((await Users(db, audit, super.Id).UpdateUser(target.Id, Edit(target, staff.Id))).Value);
        Assert.DoesNotContain(target.UserRoles, r => r.RoleId == memo.Id);
        super.IsActive = false; await db.SaveChangesAsync(); Assert.False(await CorrespondenceAccess.CanChangeUserRolesAsync(db, super.Id, [staff.Id], [staff.Id, memo.Id]));
    }
    [Fact]
    public async Task Create_user_cannot_assign_protected_roles_without_superadmin()
    {
        await using var db = Db(); var (admin, _, _, memo, _, _) = await Seed(db); var count = await db.Users.CountAsync();
        var result = await Users(db, new Audit(), admin.Id).CreateUser(new(null, "New", "new", "password", [memo.Id], null, null, null, null, null, null, true));
        Assert.Equal(403, Assert.IsType<ObjectResult>(result.Result).StatusCode); Assert.Equal(count, await db.Users.CountAsync());
    }
    [Fact]
    public async Task Protects_roles_permissions_and_indirect_grants_without_partial_writes()
    {
        await using var db = Db(); var (admin, super, _, memo, staff, use) = await Seed(db); var audit = new Audit(); var roles = Context(new RolesController(db, audit), admin.Id);
        Assert.Equal(403, Assert.IsType<ObjectResult>((await roles.UpdateRole(memo.Id, new("Changed", null, false))).Result).StatusCode);
        Assert.Equal(CorrespondenceAccess.RoleName, memo.Name); Assert.True(memo.IsActive);
        Assert.Equal(403, Assert.IsType<ObjectResult>((await roles.DeleteRole(memo.Id)).Result).StatusCode);
        Assert.Equal(403, Assert.IsType<ObjectResult>((await roles.UpdateRolePermissions(staff.Id, new([use.Id]))).Result).StatusCode);
        Assert.Empty(staff.RolePermissions);
        Assert.Equal(403, Assert.IsType<ObjectResult>((await roles.UpdateRolePermissions(memo.Id, new([]))).Result).StatusCode); Assert.Single(memo.RolePermissions);
        Assert.Equal(403, Assert.IsType<ObjectResult>((await roles.UpdateRole(staff.Id, new("SuperAdmin", null, true))).Result).StatusCode);
        Assert.Equal(403, Assert.IsType<ObjectResult>((await Context(new PermissionsController(db, audit), admin.Id).DeletePermission(use.Id)).Result).StatusCode);
        Assert.NotNull((await Context(new RolesController(db, audit), super.Id).UpdateRolePermissions(staff.Id, new([use.Id]))).Value);
        Assert.False(await CorrespondenceAccess.CanChangeUserRolesAsync(db, admin.Id, [], [staff.Id]));
        Assert.True((await roles.GetRole(staff.Id)).Value!.Data!.RequiresSuperAdmin);
    }
    [Fact]
    public async Task Permission_is_checked_from_database_on_every_request_including_revocation()
    {
        await using var db = Db(); var (_, _, target, memo, _, _) = await Seed(db);
        async Task<bool> Allowed() { var requirement = new PermissionRequirement(CorrespondenceAccess.Permission); var context = new AuthorizationHandlerContext([requirement], Principal(target.Id), null); await new PermissionAuthorizationHandler(db).HandleAsync(context); return context.HasSucceeded; }
        Assert.False(await Allowed()); var link = new UserRole { UserId = target.Id, RoleId = memo.Id }; db.UserRoles.Add(link); await db.SaveChangesAsync(); Assert.True(await Allowed());
        db.UserRoles.Remove(link); await db.SaveChangesAsync(); Assert.False(await Allowed());
        Assert.Equal(CorrespondenceAccess.Permission, typeof(CorrespondenceController).GetCustomAttributes(typeof(RequirePermissionAttribute), true).Cast<RequirePermissionAttribute>().Single().PermissionCode);
    }
    private sealed class Audit : IAuditLogService
    {
        public List<string> Details { get; } = [];
        public Task WriteAsync(Guid? userId, string action, string resource, string? resourceId, string? detail, string result = "Success", HttpContext? httpContext = null) { Details.Add(detail ?? ""); return Task.CompletedTask; }
    }
    private sealed class Entitlements : ILeaveEntitlementService
    {
        public Task<LeaveEntitlementInitializationResult> InitializeAsync(Guid userId, int fiscalYear, DateOnly effectiveDate, Guid? initiatedByUserId, string reason, CancellationToken cancellationToken = default)
            => Task.FromResult(new LeaveEntitlementInitializationResult(userId, fiscalYear, 0, 0, 0, [], []));
    }
}
