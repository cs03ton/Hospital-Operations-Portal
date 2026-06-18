using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Hop.Api.Tests.E2E;

public class PostgresApiE2ETests
{
    [Fact]
    public async Task LeaveWorkflow_UsesRealPostgres_WhenConfigured()
    {
        var connectionString = Environment.GetEnvironmentVariable("HOP_E2E_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await using var factory = new HopApiFactory(connectionString);
        using var client = factory.CreateClient();
        await factory.ResetDatabaseAsync();

        var unauthorizedReport = await client.GetAsync("/api/reports/leaves");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorizedReport.StatusCode);

        var login = await PostApi<LoginResponse>(client, "/api/auth/login", new LoginRequest("admin", "Admin@1234"));
        Assert.False(string.IsNullOrWhiteSpace(login.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(login.RefreshToken));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var refresh = await PostApi<LoginResponse>(client, "/api/auth/refresh-token", new RefreshTokenRequest(login.RefreshToken));
        Assert.False(string.IsNullOrWhiteSpace(refresh.AccessToken));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refresh.AccessToken);

        var leaveTypes = await GetApi<IReadOnlyList<LeaveTypeResponse>>(client, "/api/leave-types");
        var leaveType = leaveTypes.First();
        var draft = await PostApi<LeaveRequestResponse>(client, "/api/leave-requests", new SaveLeaveRequestRequest(
            leaveType.Id,
            DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(7)),
            DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(7)),
            1,
            "E2E leave request"));

        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent("pdf"u8.ToArray())
        {
            Headers = { ContentType = new MediaTypeHeaderValue("application/pdf") }
        }, "file", "sample.pdf");
        var uploadResponse = await client.PostAsync($"/api/leave-requests/{draft.Id}/attachments", form);
        uploadResponse.EnsureSuccessStatusCode();
        var attachment = (await uploadResponse.Content.ReadFromJsonAsync<ApiResponse<LeaveAttachmentResponse>>())!.Data;
        Assert.NotNull(attachment);

        using var anonymousClient = factory.CreateClient();
        var forbiddenDownload = await anonymousClient.GetAsync($"/api/leave-attachments/{attachment.Id}/download");
        Assert.Equal(HttpStatusCode.Unauthorized, forbiddenDownload.StatusCode);

        var download = await client.GetAsync($"/api/leave-attachments/{attachment.Id}/download");
        download.EnsureSuccessStatusCode();

        var submitted = await PostApi<LeaveRequestResponse>(client, $"/api/leave-requests/{draft.Id}/submit", new { });
        Assert.Equal("Pending", submitted.Status);

        var approved = await PostApi<LeaveRequestResponse>(client, $"/api/leave-requests/{draft.Id}/approve", new LeaveDecisionRequest("E2E approve"));
        Assert.Equal("Approved", approved.Status);

        var balance = await GetApi<IReadOnlyList<LeaveBalanceResponse>>(client, "/api/leave-balances/me");
        Assert.Contains(balance, item => item.UsedDays >= 1);

        var rejectDraft = await PostApi<LeaveRequestResponse>(client, "/api/leave-requests", new SaveLeaveRequestRequest(
            leaveType.Id,
            DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(9)),
            DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(9)),
            1,
            "E2E reject request"));
        await PostApi<LeaveRequestResponse>(client, $"/api/leave-requests/{rejectDraft.Id}/submit", new { });
        var rejected = await PostApi<LeaveRequestResponse>(client, $"/api/leave-requests/{rejectDraft.Id}/reject", new LeaveDecisionRequest("E2E reject"));
        Assert.Equal("Rejected", rejected.Status);

        var pdf = await client.GetAsync($"/api/leave-requests/{draft.Id}/pdf");
        pdf.EnsureSuccessStatusCode();
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);

        var report = await client.GetAsync("/api/reports/leaves/export-excel");
        report.EnsureSuccessStatusCode();
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", report.Content.Headers.ContentType?.MediaType);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await db.LineDeliveryLogs.AnyAsync(item => item.LeaveRequestId == draft.Id));
    }

    private static async Task<T> GetApi<T>(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
        Assert.NotNull(payload);
        Assert.NotNull(payload.Data);
        return payload.Data;
    }

    private static async Task<T> PostApi<T>(HttpClient client, string url, object body)
    {
        var response = await client.PostAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
        Assert.NotNull(payload);
        Assert.NotNull(payload.Data);
        return payload.Data;
    }

    private sealed class HopApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = connectionString,
                    ["Jwt:Key"] = "e2e-test-secret-key-that-is-long-enough-32",
                    ["Jwt:Issuer"] = "Hop.Api.E2E",
                    ["Jwt:Audience"] = "Hop.Client.E2E",
                    ["Line:Enabled"] = "false",
                    ["Storage:RootPath"] = Path.Combine(Path.GetTempPath(), "hop-e2e-storage"),
                    ["Auth:TokenStorageMode"] = "LocalStorage",
                    ["FileScan:Enabled"] = "false"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
            });
        }

        public async Task ResetDatabaseAsync()
        {
            await using var scope = Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("E2ESeed");
            await DevelopmentDataSeeder.SeedAsync(scope.ServiceProvider, logger);
        }
    }
}
