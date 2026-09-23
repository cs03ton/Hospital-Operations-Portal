using System.Net;
using System.Text;
using Hop.Api.DTOs;
using Hop.Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Hop.Api.Tests;

public class CalendarInputHttpTests
{
    private static TestServer CreateServer() => new(new WebHostBuilder()
        .ConfigureServices(services => services.AddControllers().AddApplicationPart(typeof(CalendarProbeController).Assembly).AddCalendarInput())
        .Configure(app => { app.UseRouting(); app.UseEndpoints(endpoints => endpoints.MapControllers()); }));

    [Theory]
    [InlineData("create", "2567-02-29", "2024-02-29", HttpStatusCode.OK)]
    [InlineData("edit", "2569-09-24", "2026-09-24", HttpStatusCode.OK)]
    [InlineData("preview", "2026-09-24", "2026-09-24", HttpStatusCode.OK)]
    [InlineData("create", "2568-02-29", "", HttpStatusCode.BadRequest)]
    [InlineData("edit", "2568-02-29", "", HttpStatusCode.BadRequest)]
    [InlineData("preview", "2568-02-29", "", HttpStatusCode.BadRequest)]
    public async Task LeaveDtosNormalizeBeforeApiModelValidation(string route, string input, string expected, HttpStatusCode status)
    {
        using var server = CreateServer();
        using var client = server.CreateClient();
        using var response = await client.PostAsync("/calendar-probe/" + route, new StringContent(
            $$"""{"leaveTypeId":"00000000-0000-0000-0000-000000000001","startDate":"{{input}}","endDate":"{{input}}","durationType":"FULL_DAY","totalDays":1,"reason":"test"}""", Encoding.UTF8, "application/json"));
        Assert.Equal(status, response.StatusCode);
        if (status == HttpStatusCode.OK) Assert.Contains($"\"startDate\":\"{expected}\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task QueryDatesAndYearsUseTheSameRules()
    {
        using var server = CreateServer();
        using var client = server.CreateClient();
        var result = await client.GetStringAsync("/calendar-probe?date=2567-02-29&year=2569");
        Assert.Contains("\"date\":\"2024-02-29\"", result);
        Assert.Contains("\"year\":2569", result);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/calendar-probe?date=2568-02-29&year=2569")).StatusCode);
    }

    [Fact]
    public async Task CalendarYearBodyDoesNotConvertNumericLimits()
    {
        using var server = CreateServer();
        using var client = server.CreateClient();
        var response = await client.PostAsync("/calendar-probe/year", new StringContent(
            "{\"year\":2569,\"manufactureYear\":2567,\"daysPerYear\":2500}", Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"year\":2569", body);
        Assert.Contains("\"manufactureYear\":2024", body);
        Assert.Contains("\"daysPerYear\":2500", body);
    }
}

// Only this test assembly exposes the probe. Uses production DTOs and MVC registration.
[ApiController, Route("calendar-probe")]
public class CalendarProbeController : ControllerBase
{
    [HttpPost("create"), HttpPost("edit")]
    public object Save(SaveLeaveRequestRequest request) => request;
    [HttpPost("preview")]
    public object Preview(LeavePolicyPreviewRequest request) => request;
    [HttpGet]
    public object Query([FromQuery] DateOnly date, [FromQuery] int year) => new { date, year };
    [HttpPost("year")]
    public object Year(YearProbe request) => request;
}
public record YearProbe(int Year, int? ManufactureYear, int DaysPerYear);
