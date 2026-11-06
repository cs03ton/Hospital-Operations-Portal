using System.Security.Claims;
using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Hop.Api.Tests;

public class UserProfilePrefixTests
{
    [Fact]
    public async Task ReadsAndUpdatesPrefixSeparatelyWithAudit()
    {
        await using var db = Database();
        var user = new User { FullName = "ทดสอบ ชื่อเดิม", Username = "prefix", NamePrefix = "นาย" };
        db.Users.Add(user); await db.SaveChangesAsync();
        var audit = new Audit(); var controller = Controller(db, user.Id, audit);
        Assert.Equal("นาย", (await controller.GetProfile()).Value!.Data!.NamePrefix);
        var result = await controller.UpdateProfile(new(user.FullName, null, null, null, null, " นพ. "));
        Assert.Equal("นพ.", result.Value!.Data!.NamePrefix);
        Assert.Equal("ทดสอบ ชื่อเดิม", user.FullName);
        Assert.Contains("namePrefix", audit.Detail);
        await controller.UpdateProfile(new(user.FullName, null, null, null, null));
        Assert.Equal("นพ.", user.NamePrefix);
        await controller.UpdateProfile(new(user.FullName, null, null, null, null, " "));
        Assert.Null(user.NamePrefix);
    }

    [Fact]
    public async Task RejectsTooLongPrefixBeforeAnyChanges()
    {
        await using var db = Database();
        var user = new User { FullName = "เดิม", Username = "prefix", NamePrefix = "นางสาว" };
        db.Users.Add(user); await db.SaveChangesAsync();
        var audit = new Audit();
        var result = await Controller(db, user.Id, audit).UpdateProfile(new("ใหม่", null, null, null, null, new string('ก', 101)));
        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("เดิม", user.FullName); Assert.Equal("นางสาว", user.NamePrefix);
        Assert.Null(audit.Detail);
    }

    [Fact]
    public async Task UnauthenticatedUserCannotUpdatePrefix()
    {
        await using var db = Database();
        var controller = Controller(db, null, new Audit());
        Assert.IsType<UnauthorizedObjectResult>((await controller.UpdateProfile(new("ชื่อ", null, null, null, null, "นาย"))).Result);
    }

    private static AppDbContext Database() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static MeProfileController Controller(AppDbContext db, Guid? id, Audit audit)
    {
        var controller = new MeProfileController(db, audit, null!, null!, null!, new ConfigurationBuilder().Build(), null!);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        if (id.HasValue) controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id.ToString()!)], "test"));
        return controller;
    }
    private sealed class Audit : IAuditLogService
    {
        public string? Detail;
        public Task WriteAsync(Guid? userId, string action, string resource, string? resourceId, string? detail, string result = "Success", HttpContext? httpContext = null)
        { Detail = detail; return Task.CompletedTask; }
    }
}
