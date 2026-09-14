using System.Security.Claims;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Hop.Api.Configuration;
using Microsoft.Extensions.Options;
using Hop.Api.Interfaces;
using SkiaSharp;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Hop.Api.Tests;

public sealed class RepairPostgresFactAttribute : FactAttribute
{
    public RepairPostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HOP_REPAIR_TEST_CONNECTION_STRING")))
            Skip = "Requires local PostgreSQL via HOP_REPAIR_TEST_CONNECTION_STRING.";
    }
}

public sealed class RepairsPostgresTests : IAsyncLifetime
{
    private readonly string name = "hop_repair_test_" + Guid.NewGuid().ToString("N");
    private string? connection;
    private string? admin;
    private Guid requester, it, general;
    public async Task InitializeAsync()
    {
        admin = Environment.GetEnvironmentVariable("HOP_REPAIR_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(admin)) return;
        var builder = new NpgsqlConnectionStringBuilder(admin);
        if (builder.Host is not ("localhost" or "127.0.0.1" or "::1")) throw new InvalidOperationException("Local test database only.");
        await using var c = new NpgsqlConnection(admin); await c.OpenAsync();
        await new NpgsqlCommand($"CREATE DATABASE {name}", c).ExecuteNonQueryAsync();
        builder.Database = name; connection = builder.ConnectionString;
        await using var db = Db();
        await db.GetService<IMigrator>().MigrateAsync("20260909090000_CorrectFleetRolePermissions");
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "deploy/sql/06-prd-repair-schema.sql"))) root = root.Parent;
        if (root == null) throw new InvalidOperationException("Repair deployment SQL not found.");
        var schema = await File.ReadAllTextAsync(Path.Combine(root.FullName, "deploy/sql/06-prd-repair-schema.sql"));
        var master = await File.ReadAllTextAsync(Path.Combine(root.FullName, "deploy/sql/07-prd-repair-master-data.sql"));
        await db.Database.ExecuteSqlRawAsync(schema);
        await db.Database.ExecuteSqlRawAsync(schema);
        await db.Database.ExecuteSqlRawAsync(master);
        await db.Database.ExecuteSqlRawAsync(master);
        Assert.Contains("20260911074410_AddRepairManagement", await db.Database.GetAppliedMigrationsAsync());
        // A blank database has schema but no application bootstrap roles.
        if (!await db.Roles.AnyAsync(x => x.Name == "Staff"))
        {
            db.Roles.Add(new Role { Id = Guid.NewGuid(), Name = "Staff", IsActive = true });
            await db.SaveChangesAsync();
        }
        await db.Database.ExecuteSqlRawAsync(RepairSeed.Sql);
        await db.Database.ExecuteSqlRawAsync(RepairSeed.Sql);
        requester = await AddUser(db, "requester", "Staff");
        it = await AddUser(db, "it", "ช่าง IT");
        general = await AddUser(db, "general", "ช่างทั่วไป");
    }
    public async Task DisposeAsync()
    {
        if (connection is null) return;
        NpgsqlConnection.ClearAllPools();
        await using var c = new NpgsqlConnection(admin); await c.OpenAsync();
        await new NpgsqlCommand($"DROP DATABASE {name} WITH (FORCE)", c).ExecuteNonQueryAsync();
    }
    private AppDbContext Db() => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options);
    private static async Task<Guid> AddUser(AppDbContext db, string username, string roleName)
    {
        var role = await db.Roles.SingleAsync(x => x.Name == roleName);
        var user = new User { Id = Guid.NewGuid(), Username = username, FullName = username, PasswordHash = "not-a-login-hash" };
        db.Add(user); db.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        await db.SaveChangesAsync(); return user.Id;
    }
    private RepairsController Controller(AppDbContext db, Guid user) => new(db) { ControllerContext = new ControllerContext {
        HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.ToString())], "test")) } } };
    private async Task<RepairRequest> Create()
    {
        await using var db = Db();
        var category = await db.Set<RepairCategory>().FirstAsync(x => x.TeamCode == "IT");
        var result = Assert.IsType<OkObjectResult>(await Controller(db, requester).Create(new(category.Id, "Printer", "broken", "Room", "Contact"), default));
        return Assert.IsType<RepairRequest>(Assert.IsType<ApiResponse<object>>(result.Value).Data);
    }
    private async Task<RepairRequest> Act(RepairRequest r, Guid user, string action, Guid? solver = null, RepairInput? input = null)
    {
        await using var db = Db();
        var result = Assert.IsType<OkObjectResult>(await Controller(db, user).Change(r.Id, action,
            new(r.ConcurrencyToken, "test note", SolverId: solver, Request: input), default));
        return Assert.IsType<RepairRequest>(Assert.IsType<ApiResponse<object>>(result.Value).Data);
    }

    [RepairPostgresFact]
    public async Task Workflow_PreservesRounds_WaitingAndRejectedSolutions()
    {
        var r = await Create();
        r = await Act(r, it, "start");
        r = await Act(r, it, "wait");
        r = await Act(r, it, "resume");
        r = await Act(r, it, "solve", it);
        r = await Act(r, requester, "reject-solution");
        r = await Act(r, it, "solve", it);
        await using (var acceptanceDb = Db())
        {
            var accepted = Assert.IsType<OkObjectResult>(await Controller(acceptanceDb, requester).Change(r.Id, "accept", new(r.ConcurrencyToken, new string('a', 2000)), default));
            r = Assert.IsType<RepairRequest>(Assert.IsType<ApiResponse<object>>(accepted.Value).Data);
        }
        r = await Act(r, requester, "reopen");
        Assert.Equal(2, r.CurrentRound);
        await using var db = Db();
        Assert.Equal(2, await db.Set<RepairRound>().CountAsync(x => x.RequestId == r.Id));
        Assert.Equal(2, await db.Set<RepairEvent>().CountAsync(x => x.RequestId == r.Id && x.Action == "solve"));
        Assert.NotNull((await db.Set<RepairWaitingPeriod>().SingleAsync(x => x.RequestId == r.Id)).EndedAt);
        var dispatchedActions = await db.Set<RepairDispatch>()
            .Where(x => x.RequestId == r.Id)
            .Join(db.Set<RepairEvent>(), dispatch => dispatch.EventId, repairEvent => repairEvent.Id, (_, repairEvent) => repairEvent.Action)
            .ToListAsync();
        Assert.Equal(8, dispatchedActions.Count);
        Assert.Contains("submit", dispatchedActions);
        Assert.Contains("start", dispatchedActions);
        Assert.Contains("resume", dispatchedActions);
        Assert.Equal(2, dispatchedActions.Count(x => x == "solve"));
        Assert.Contains("reject-solution", dispatchedActions);
        Assert.Contains("accept", dispatchedActions);
        Assert.Contains("reopen", dispatchedActions);
    }

    [RepairPostgresFact]
    public async Task CrossTeam_ReturnResubmit_AndInvalidSolver()
    {
        var r = await Create();
        await using (var db = Db()) Assert.IsType<NotFoundResult>(await Controller(db, general).Detail(r.Id, default));
        r = await Act(r, it, "return");
        await using (var db = Db())
        {
            var c = await db.Set<RepairCategory>().FirstAsync(x => x.TeamCode == "GENERAL");
            r = await Act(r, requester, "resubmit", input: new(c.Id, "Water", "leak", "Room", "Contact"));
        }
        await using (var db = Db()) Assert.IsType<NotFoundResult>(await Controller(db, it).Detail(r.Id, default));
        r = await Act(r, general, "start");
        await using var verify = Db();
        Assert.IsType<BadRequestObjectResult>(await Controller(verify, general).Change(r.Id, "solve", new(r.ConcurrencyToken, "fix", SolverId: it), default));
        Assert.Equal("InProgress", (await verify.Set<RepairRequest>().AsNoTracking().SingleAsync(x => x.Id == r.Id)).Status);
    }

    [RepairPostgresFact]
    public async Task ConcurrentWrites_Conflict_AndNumbersUnique()
    {
        var requests = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Create()));
        Assert.Equal(8, requests.Select(x => x.Number).Distinct().Count());
        var r = requests[0];
        var results = await Task.WhenAll(Enumerable.Range(0, 2).Select(async _ => {
            await using var db = Db(); return await Controller(db, it).Change(r.Id, "start", new(r.ConcurrencyToken, ""), default);
        }));
        Assert.Single(results.OfType<OkObjectResult>());
        Assert.Single(results.OfType<ConflictObjectResult>());
        await using var check = Db();
        Assert.Equal(1, await check.Set<RepairEvent>().CountAsync(x => x.RequestId == r.Id && x.Action == "start"));
        Assert.Equal(2, await check.Set<RepairTeam>().CountAsync());
        Assert.Equal(11, await check.Set<RepairCategory>().CountAsync());
    }

    [RepairPostgresFact]
    public async Task NotificationDispatch_IsScoped_Deduplicated_AndRetries()
    {
        var r = await Create();
        await using var setup = Db();
        var group = await setup.LineGroupDestinations.SingleAsync(x => x.Module == "REPAIR_IT");
        group.Status = "Active"; group.EndpointUrl = "https://notify.example.test/send"; group.ClientId = "test"; group.ClientSecretProtected = "test";
        await setup.SaveChangesAsync();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["Line:PublicAppUrl"] = "https://hop.example.test", ["Repairs:AllowedNotificationHosts:0"] = "notify.example.test"
        }).Build();
        var client = new TestPushClient();
        async Task Send()
        {
            await using var db = Db();
            await new RepairDeliveryService(db, client, new LineConfigurationResolver(Options.Create(new LineOptions()), config), config).ProcessAsync(default);
        }
        await Send(); // Known transient response retries later, not in the request transaction.
        await using (var db = Db())
        {
            var job = await db.Set<RepairDispatch>().SingleAsync(x => x.RequestId == r.Id);
            Assert.Equal("Pending", job.Status); job.AvailableAt = DateTime.UtcNow.AddMinutes(-1); await db.SaveChangesAsync();
        }
        await Task.WhenAll(Send(), Send());
        await Send();
        Assert.Equal(2, client.Calls);
        Assert.Equal("REPAIR_IT", client.Module);
        Assert.DoesNotContain("broken", client.Text);
        Assert.Contains("/repairs/", client.Text);
        Assert.Contains("HOP · ระบบแจ้งซ่อม", client.FlexContents);
        Assert.Contains("REP-", client.FlexContents);
        Assert.Contains("หัวข้องาน", client.FlexContents);
        Assert.Contains("ประเภทงาน", client.FlexContents);
        Assert.Contains("สถานที่", client.FlexContents);
        Assert.Contains("หน่วยงานผู้แจ้ง", client.FlexContents);
        Assert.Contains("ความเร่งด่วน", client.FlexContents);
        await using var verify = Db();
        Assert.Equal("Sent", (await verify.Set<RepairDispatch>().SingleAsync(x => x.RequestId == r.Id)).Status);
    }

    [RepairPostgresFact]
    public async Task NotificationDispatch_RendersWorkflowStatusEvents()
    {
        var r = await Create();
        r = await Act(r, it, "start");
        r = await Act(r, it, "solve", it);
        r = await Act(r, requester, "accept");
        await using (var setup = Db())
        {
            var group = await setup.LineGroupDestinations.SingleAsync(x => x.Module == "REPAIR_IT");
            group.Status = "Active"; group.EndpointUrl = "https://notify.example.test/send";
            group.ClientId = "test"; group.ClientSecretProtected = "test";
            await setup.SaveChangesAsync();
        }
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["Line:PublicAppUrl"] = "https://hop.example.test", ["Repairs:AllowedNotificationHosts:0"] = "notify.example.test"
        }).Build();
        var client = new TestPushClient(failFirst: false);
        await using (var db = Db())
            await new RepairDeliveryService(db, client, new LineConfigurationResolver(Options.Create(new LineOptions()), config), config).ProcessAsync(default);

        Assert.Equal(4, client.Calls);
        Assert.Contains("Repair.Submitted", client.EventNames);
        Assert.Contains("Repair.Started", client.EventNames);
        Assert.Contains("Repair.Solved", client.EventNames);
        Assert.Contains("Repair.Closed", client.EventNames);
        Assert.Contains(client.FlexMessages, message => message.Contains("กำลังดำเนินการ") && message.Contains("ผู้ดำเนินการ"));
        Assert.Contains(client.FlexMessages, message => message.Contains("ซ่อมเสร็จ รอตรวจรับ") && message.Contains("ผู้แก้ไขหลัก"));
        Assert.Contains(client.FlexMessages, message => message.Contains("ปิดใบงานแล้ว") && message.Contains("ผู้ตรวจรับ"));
        Assert.DoesNotContain(client.FlexMessages, message => message.Contains("Contact") || message.Contains("broken"));
    }

    [RepairPostgresFact]
    public async Task Images_AreValidatedAndScopeProtected()
    {
        var r = await Create();
        var root = Path.Combine(Path.GetTempPath(), "hop-repair-images-" + Guid.NewGuid().ToString("N"));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:RootPath"] = root }).Build();
        RepairImagesController Images(AppDbContext db, Guid actor) => new(db, config, new CleanScan(), new FileTypeValidationService()) {
            ControllerContext = Controller(db, actor).ControllerContext
        };
        try
        {
            using var bitmap = new SKBitmap(8, 8); bitmap.Erase(SKColors.Green);
            using var image = SKImage.FromBitmap(bitmap); using var png = image.Encode(SKEncodedImageFormat.Png, 90);
            using var stream = new MemoryStream(png.ToArray());
            var file = new FormFile(stream, 0, stream.Length, "files", "test.png") { Headers = new HeaderDictionary(), ContentType = "image/png" };
            await using (var db = Db()) Assert.IsType<NotFoundResult>(await Images(db, general).Upload(r.Id, r.ConcurrencyToken, [file], default));
            await using (var db = Db()) Assert.IsType<BadRequestObjectResult>(await Images(db, requester).Upload(r.Id, r.ConcurrencyToken, [file,file,file,file,file,file], default));
            await using (var db = Db()) Assert.IsType<OkObjectResult>(await Images(db, requester).Upload(r.Id, r.ConcurrencyToken, [file], default));
            await using var verify = Db();
            var stored = await verify.Set<RepairImage>().SingleAsync(x => x.RequestId == r.Id);
            Assert.IsType<NotFoundResult>(await Images(verify, general).Download(stored.Id, default));
            Assert.IsType<PhysicalFileResult>(await Images(verify, it).Download(stored.Id, default));
            Assert.IsType<ConflictObjectResult>(await Images(verify, requester).Upload(r.Id, r.ConcurrencyToken, [file], default));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private sealed class CleanScan : IFileScanningService
    {
        public Task<FileScanResult> ScanAsync(IFormFile file, CancellationToken cancellationToken = default) => Task.FromResult(new FileScanResult(true, "Test", "clean"));
    }

    [RepairPostgresFact]
    public async Task HttpEndpoints_EnforceAuthenticationAndPermissions()
    {
        var request = await Create();
        using var factory = new RepairApiFactory(connection!);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions {
            BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
        });
        foreach (var path in new[] { "/api/repairs", "/api/repairs/options", "/api/repairs/summary", "/api/repairs/settings", $"/api/repairs/{request.Id}", $"/api/repairs/images/{request.Id}" })
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(path)).StatusCode);
        using var scope = factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        await using var db = Db();
        var staff = await db.Users.SingleAsync(x => x.Id == requester);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenService.GenerateAccessToken(staff, "Staff"));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/repairs/{request.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/repairs/settings")).StatusCode);
        staff.IsActive = false; await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/repairs/{request.Id}")).StatusCode);
        var unauthorized = new User { Id = Guid.NewGuid(), Username = "no-permissions", FullName = "test", PasswordHash = "not-a-login-hash" };
        db.Add(unauthorized); await db.SaveChangesAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenService.GenerateAccessToken(unauthorized, "Staff"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/repairs")).StatusCode);
    }

    [RepairPostgresFact]
    public async Task PriorityRoute_IsMapped_AndReturnsDistinctHttpOutcomes()
    {
        var request = await Create();
        using var factory = new RepairApiFactory(connection!);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions {
            BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
        });
        var path = $"/api/repairs/{request.Id}/actions/priority";

        var anonymous = await client.PostAsJsonAsync(path, new RepairAction(request.ConcurrencyToken, "route probe", "Normal"));
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        using var scope = factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        await using var db = Db();
        var requesterUser = await db.Users.SingleAsync(x => x.Id == requester);
        var technician = await db.Users.SingleAsync(x => x.Id == it);
        var requesterAccess = await RepairWorkflow.Access(db, requester, default);
        Assert.True(requesterAccess.View(request));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenService.GenerateAccessToken(requesterUser, "Staff"));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/repairs/{request.Id}")).StatusCode);
        var forbidden = await client.PostAsJsonAsync(path, new RepairAction(request.ConcurrencyToken, "not a technician", "Normal"));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenService.GenerateAccessToken(technician, "ช่าง IT"));
        var missing = await client.PostAsJsonAsync($"/api/repairs/{Guid.NewGuid()}/actions/priority",
            new RepairAction(Guid.NewGuid(), "missing request", "Normal"));
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        var invalid = await client.PostAsJsonAsync(path, new RepairAction(request.ConcurrencyToken, "invalid priority", "Critical"));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        var staleToken = request.ConcurrencyToken;
        foreach (var priority in new[] { "Normal", "Urgent", "Emergency" })
        {
            var response = await client.PostAsJsonAsync(path, new RepairAction(request.ConcurrencyToken, $"set {priority}", priority));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var payload = await response.Content.ReadFromJsonAsync<ApiResponse<RepairRequest>>();
            Assert.NotNull(payload?.Data);
            Assert.Equal(priority, payload.Data.Priority);
            request = payload.Data;
        }

        var conflict = await client.PostAsJsonAsync(path, new RepairAction(staleToken, "stale update", "Normal"));
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
    }

    [RepairPostgresFact]
    public async Task DualRoles_AndContributors_KeepActorSeparateFromSolver()
    {
        await using var db = Db();
        var otherIt = await AddUser(db, "other-it", "ช่าง IT");
        var generalRole = await db.Roles.SingleAsync(x => x.Name == "ช่างทั่วไป");
        db.UserRoles.Add(new UserRole { UserId = it, RoleId = generalRole.Id });
        await db.SaveChangesAsync();
        var access = await RepairWorkflow.Access(db, it, default);
        Assert.True(access.Work("IT")); Assert.True(access.Work("GENERAL"));
        var request = await Act(await Create(), otherIt, "start");
        Assert.IsType<OkObjectResult>(await Controller(db, otherIt).Change(request.Id, "solve",
            new(request.ConcurrencyToken, "Solved together", SolverId: it, ContributorIds: [otherIt]), default));
        var solved = await db.Set<RepairEvent>().SingleAsync(x => x.RequestId == request.Id && x.Action == "solve");
        Assert.Equal(otherIt, solved.ActorId); Assert.Equal(it, solved.SolverId);
        Assert.Equal(otherIt, (await db.Set<RepairContributor>().SingleAsync(x => x.EventId == solved.Id)).UserId);
        Assert.Single(await db.Notifications.Where(x => x.ReferenceId == request.Id.ToString()).ToListAsync());
    }

    private sealed class RepairApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?> {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["Jwt:Key"] = "repair-integration-only-key-32-characters-long",
                ["Jwt:Issuer"] = "Hop.Repair.Tests", ["Jwt:Audience"] = "Hop.Repair.Tests",
                ["Line:Enabled"] = "false", ["FileScan:Enabled"] = "false",
                ["Auth:TokenStorageMode"] = "LocalStorage", ["Database:SeedOnStartup"] = "false"
            }));
            builder.ConfigureServices(services => {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));
                services.RemoveAll<IHostedService>();
                // Program captures its signing key before the factory configuration override.
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options => {
                    options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("repair-integration-only-key-32-characters-long"));
                    options.TokenValidationParameters.ValidIssuer = "Hop.Repair.Tests";
                    options.TokenValidationParameters.ValidAudience = "Hop.Repair.Tests";
                });
            });
        }
    }
    private sealed class TestPushClient(bool failFirst = true) : ILineGroupPushClient
    {
        private readonly object gate = new();
        public int Calls;
        public string? Module, Text, FlexContents;
        public List<string> EventNames { get; } = [];
        public List<string> FlexMessages { get; } = [];
        public Task<LineGroupPushResult> PushTextAsync(string groupId, string text, CancellationToken ct) => throw new InvalidOperationException();
        public Task<LineGroupPushResult> PushMessageAsync(LineGroupDestination destination, FleetGroupRenderedMessage message, CancellationToken ct)
        {
            bool shouldFail;
            lock (gate)
            {
                Calls++;
                Module = destination.Module; Text = message.Text; FlexContents = message.FlexContentsJson;
                EventNames.Add(message.CanonicalEventType);
                FlexMessages.Add(message.FlexContentsJson ?? "");
                shouldFail = failFirst && Calls == 1;
            }
            return Task.FromResult(shouldFail
                ? new LineGroupPushResult(false, true, false, "CUSTOM_HTTP_503", "test")
                : new LineGroupPushResult(true, false, false, null, null));
        }
    }
}
