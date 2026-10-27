using System.Security.Claims;
using Hop.Api.Authorization;
using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hop.Api.Tests;

public class RepairCategoryChangeTests
{
    [Theory]
    [InlineData("Submitted", false)]
    [InlineData("InProgress", true)]
    [InlineData("WaitingParts", true)]
    [InlineData("Returned", true)]
    [InlineData("Closed", true)]
    public async Task Admin_can_change_category_in_same_team_preserving_work_and_appending_history(string status, bool started)
    {
        await using var db = Db(); var f = await Seed(db, [RepairPermissions.Manage, RepairPermissions.ViewAll]);
        f.Request.Status = status; f.Request.HasStarted = started; await db.SaveChangesAsync();
        var token = f.Request.ConcurrencyToken;
        var result = await Controller(db, f.Actor).ChangeCategory(f.Request.Id, new(f.Same.Id, "แก้ประเภทให้ตรงกับงาน", token), default);
        Assert.IsType<OkObjectResult>(result); Assert.Equal(f.Same.Id, f.Request.CategoryId); Assert.Equal("IT", f.Request.TeamCode);
        Assert.Equal(status, f.Request.Status); Assert.Equal(started, f.Request.HasStarted); Assert.Equal("Urgent", f.Request.Priority);
        Assert.Equal("อาการเดิม", f.Request.Description); Assert.Equal(2, f.Request.CurrentRound); Assert.NotEqual(token, f.Request.ConcurrencyToken);
        var events = await db.Set<RepairEvent>().OrderBy(x => x.CreatedAt).ToListAsync(); Assert.Equal(2, events.Count);
        var changed = events.Single(x => x.Action == "category-change"); Assert.Equal(status, changed.FromStatus); Assert.Equal(status, changed.ToStatus);
        Assert.Contains("เดิม (IT)", changed.Note); Assert.Contains("ใหม่ (IT)", changed.Note); Assert.Contains("แก้ประเภทให้ตรงกับงาน", changed.Note); Assert.Equal(f.Actor, changed.ActorId);
        var audit = await db.AuditLogs.SingleAsync(); Assert.Contains(f.Old.Id.ToString(), audit.OldValue); Assert.Contains(f.Same.Id.ToString(), audit.NewValue);
        Assert.Single(await db.Set<RepairRound>().ToListAsync()); Assert.Single(await db.Set<RepairWaitingPeriod>().ToListAsync());
        Assert.Empty(await db.DomainEvents.ToListAsync()); Assert.Empty(await db.Notifications.ToListAsync());
    }

    [Fact]
    public async Task Transfer_before_start_changes_visibility_without_starting_a_new_round()
    {
        await using var db = Db(); var f = await Seed(db, [RepairPermissions.Manage, RepairPermissions.WorkIT]);
        var result = Assert.IsType<OkObjectResult>(await Controller(db, f.Actor).ChangeCategory(f.Request.Id, new(f.Other.Id, "ส่งทีมที่ถูกต้อง", f.Request.ConcurrencyToken), default));
        Assert.Equal("GENERAL", f.Request.TeamCode); Assert.Equal("Submitted", f.Request.Status); Assert.False(f.Request.HasStarted);
        var payload = System.Text.Json.JsonSerializer.Serialize(result.Value);
        Assert.Contains("\"canView\":false", payload);
        Assert.False(new RepairAccess(Guid.NewGuid(), [RepairPermissions.WorkIT]).View(f.Request));
        Assert.True(new RepairAccess(Guid.NewGuid(), [RepairPermissions.WorkGeneral]).View(f.Request));
        Assert.Equal(2, f.Request.CurrentRound);
    }

    [Theory]
    [InlineData("Resolved")]
    [InlineData("Cancelled")]
    public async Task Finished_requests_are_not_reclassified(string status)
    {
        await using var db = Db(); var f = await Seed(db, [RepairPermissions.Manage, RepairPermissions.ViewAll]);
        f.Request.Status = status; await db.SaveChangesAsync();
        Assert.IsType<BadRequestObjectResult>(await Controller(db, f.Actor).ChangeCategory(f.Request.Id, new(f.Same.Id, "เหตุผล", f.Request.ConcurrencyToken), default));
        await Unchanged(db, f);
    }

    [Theory]
    [InlineData("InProgress")]
    [InlineData("WaitingParts")]
    [InlineData("Returned")]
    [InlineData("Closed")]
    public async Task Started_request_cannot_move_to_another_team_even_after_return(string status)
    {
        await using var db = Db(); var f = await Seed(db, [RepairPermissions.Manage, RepairPermissions.ViewAll]);
        f.Request.Status = status; f.Request.HasStarted = true; await db.SaveChangesAsync();
        Assert.IsType<BadRequestObjectResult>(await Controller(db, f.Actor).ChangeCategory(f.Request.Id, new(f.Other.Id, "เหตุผล", f.Request.ConcurrencyToken), default));
        await Unchanged(db, f);
    }

    [Theory]
    [InlineData("reason")]
    [InlineData("inactive")]
    [InlineData("same")]
    [InlineData("missing")]
    [InlineData("token")]
    public async Task Invalid_changes_do_not_write_partial_events_or_audit(string issue)
    {
        await using var db = Db(); var f = await Seed(db, [RepairPermissions.Manage, RepairPermissions.ViewAll]);
        if (issue == "inactive") { f.Same.IsActive = false; await db.SaveChangesAsync(); }
        var result = await Controller(db, f.Actor).ChangeCategory(f.Request.Id,
            new(issue == "same" ? f.Old.Id : issue == "missing" ? Guid.NewGuid() : f.Same.Id,
                issue == "reason" ? " " : "เหตุผล", issue == "token" ? Guid.NewGuid() : f.Request.ConcurrencyToken), default);
        Assert.True(result is BadRequestObjectResult or ConflictObjectResult); await Unchanged(db, f);
    }

    [Theory]
    [InlineData(RepairPermissions.WorkIT)]
    [InlineData(RepairPermissions.ViewOwn)]
    public async Task Technicians_and_requesters_without_management_permission_cannot_call_API(string permission)
    {
        await using var db = Db(); var f = await Seed(db, [permission]);
        Assert.Equal(403, Assert.IsType<ObjectResult>(await Controller(db, f.Actor).ChangeCategory(f.Request.Id, new(f.Same.Id, "เหตุผล", f.Request.ConcurrencyToken), default)).StatusCode);
        await Unchanged(db, f);
    }

    [Fact]
    public async Task Manage_permission_does_not_grant_extra_visibility()
    {
        await using var db = Db(); var f = await Seed(db, [RepairPermissions.Manage]);
        Assert.IsType<NotFoundResult>(await Controller(db, f.Actor).ChangeCategory(f.Request.Id, new(f.Same.Id, "เหตุผล", f.Request.ConcurrencyToken), default));
        await Unchanged(db, f);
    }

    [Fact]
    public async Task Reclassifying_closed_work_preserves_acceptance_date_repair_result_and_round()
    {
        await using var db = Db(); var f = await Seed(db, [RepairPermissions.Manage, RepairPermissions.ViewAll]);
        f.Request.Status = "Closed"; f.Request.HasStarted = true;
        var round = await db.Set<RepairRound>().SingleAsync();
        var closedAt = new DateTime(2026, 10, 27, 4, 0, 0, DateTimeKind.Utc);
        round.ClosedAt = closedAt; round.AcceptedById = f.Actor; round.AcceptanceNote = "ตรวจรับเรียบร้อย";
        var solve = new RepairEvent { RequestId = f.Request.Id, Round = 2, Action = "solve", ActorId = f.Actor, SolverId = f.Actor,
            FromStatus = "InProgress", ToStatus = "Resolved", Note = "เปลี่ยนแบตเตอรี่แล้ว" };
        db.Add(solve); await db.SaveChangesAsync();
        Assert.True(RepairWorkflow.CanChangeCategory(f.Request, new(f.Actor, [RepairPermissions.Manage, RepairPermissions.ViewAll])));
        Assert.IsType<OkObjectResult>(await Controller(db, f.Actor).ChangeCategory(f.Request.Id, new(f.Same.Id, "แก้หมวดหลังปิดงาน", f.Request.ConcurrencyToken), default));
        db.ChangeTracker.Clear();
        var saved = await db.Set<RepairRequest>().SingleAsync(); var savedRound = await db.Set<RepairRound>().SingleAsync();
        Assert.Equal("Closed", saved.Status); Assert.Equal(2, saved.CurrentRound); Assert.True(saved.HasStarted);
        Assert.Equal(closedAt, savedRound.ClosedAt); Assert.Equal(f.Actor, savedRound.AcceptedById); Assert.Equal("ตรวจรับเรียบร้อย", savedRound.AcceptanceNote);
        var savedSolve = await db.Set<RepairEvent>().SingleAsync(x => x.Id == solve.Id);
        Assert.Equal("เปลี่ยนแบตเตอรี่แล้ว", savedSolve.Note); Assert.Equal(f.Actor, savedSolve.SolverId);
        var change = await db.Set<RepairEvent>().SingleAsync(x => x.Action == "category-change"); Assert.Equal("Closed", change.FromStatus); Assert.Equal("Closed", change.ToStatus);
        Assert.Single(await db.AuditLogs.ToListAsync()); Assert.Empty(await db.DomainEvents.ToListAsync()); Assert.Empty(await db.Notifications.ToListAsync());
    }

    private static async Task Unchanged(AppDbContext db, Fixture f)
    {
        Assert.Equal(f.Old.Id, (await db.Set<RepairRequest>().SingleAsync()).CategoryId);
        Assert.Single(await db.Set<RepairEvent>().ToListAsync()); Assert.Empty(await db.AuditLogs.ToListAsync());
    }
    private static AppDbContext Db() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static RepairsController Controller(AppDbContext db, Guid actor) => new(db, null!) { ControllerContext = new() { HttpContext = new DefaultHttpContext
        { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, actor.ToString())], "test")) } } };
    private sealed record Fixture(RepairRequest Request, RepairCategory Old, RepairCategory Same, RepairCategory Other, Guid Actor);
    private static async Task<Fixture> Seed(AppDbContext db, string[] permissions)
    {
        var user = new User { Username = "admin", FullName = "ผู้ดูแล", UserRoles = [new UserRole { Role = new Role { Name = "test", IsActive = true,
            RolePermissions = permissions.Select(code => new RolePermission { Permission = new Permission { Code = code, Name = code, IsActive = true } }).ToList() } }] };
        db.Add(user);
        var old = new RepairCategory { Name = "เดิม", TeamCode = "IT" }; var same = new RepairCategory { Name = "ใหม่", TeamCode = "IT" }; var other = new RepairCategory { Name = "ช่างทั่วไป", TeamCode = "GENERAL" };
        var request = new RepairRequest { Number = 3, CategoryId = old.Id, TeamCode = "IT", RequesterId = user.Id, Description = "อาการเดิม", Priority = "Urgent", CurrentRound = 2 };
        db.AddRange(old, same, other, request, new RepairEvent { RequestId = request.Id, Action = "submit", ActorId = user.Id },
            new RepairRound { RequestId = request.Id, Number = 2 }, new RepairWaitingPeriod { RequestId = request.Id, Round = 2 });
        await db.SaveChangesAsync(); return new(request, old, same, other, user.Id);
    }
}
