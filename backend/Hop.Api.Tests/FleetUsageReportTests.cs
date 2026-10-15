using System.Security.Claims;
using System.Text;
using Hop.Api.Configuration;
using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hop.Api.Tests;

public sealed class FleetUsageReportTests
{
    [Fact]
    public async Task Csv_contains_real_requests_and_summary_but_excludes_drafts_and_cancelled()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var inRange = new DateTime(2026, 10, 1, 18, 0, 0, DateTimeKind.Utc); // 2 October, Bangkok
        db.FleetRequests.AddRange(
            new FleetRequest { RequestNo = "F-1", Destination = "โรงพยาบาลจังหวัด", SubmittedAt = inRange, DepartureAt = inRange.AddMonths(1), ExpectedReturnAt = inRange.AddMonths(1).AddHours(2), Status = FleetRequestStatuses.Completed,
                Purpose = "ส่งต่อผู้ป่วย", MissionType = FleetReferPolicy.MissionType, PatientName = "สมชาย ทดสอบ", ContactPersonName = "ผู้ประสานงาน", ContactPhone = "0812345678", PassengerCount = 2,
                RequesterUser = new User { FullName = "ผู้ขอทดสอบ" },
                Passengers = [new FleetRequestPassenger { FullName = "ผู้ร่วมเดินทาง" }],
                Assignments = [new FleetAssignment { IsActive = true, Vehicle = new FleetVehicle { VehicleCode = "AMB-01", RegistrationNumber = "กข 1234" }, DriverUser = new User { FullName = "คนขับทดสอบ" } }] },
            new FleetRequest { RequestNo = "F-2", Destination = "นอกช่วง", SubmittedAt = inRange.AddMonths(-1), DepartureAt = inRange, Status = FleetRequestStatuses.Ready },
            new FleetRequest { RequestNo = "F-3", Destination = "ยกเลิก", SubmittedAt = inRange, Status = FleetRequestStatuses.Cancelled },
            new FleetRequest { RequestNo = "F-4", Destination = "แบบร่าง", Status = FleetRequestStatuses.Draft });
        await db.SaveChangesAsync();
        Assert.Equal(inRange, (await db.FleetRequests.SingleAsync(x => x.RequestNo == "F-1")).SubmittedAt);
        var exportedRequest = await db.FleetRequests.SingleAsync(x => x.RequestNo == "F-1");
        db.FleetTripRecords.Add(new FleetTripRecord { FleetRequestId = exportedRequest.Id, ActualStartAt = inRange.AddMonths(1), ActualEndAt = inRange.AddMonths(1).AddHours(2), StartMileage = 100, EndMileage = 140 });
        await db.SaveChangesAsync();

        var controller = new FleetUsageReportsController(db, new FleetDateRangeService(Options.Create(new FleetOperationsOptions())))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())]));
        var query = new FleetUsageExportQuery(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31), null, null);
        var export = Assert.IsType<FileContentResult>(await controller.Export(query, default));
        var csv = Encoding.UTF8.GetString(export.FileContents);
        Assert.Equal("จำนวนคำขอทั้งหมด,1", csv.Split('\n').First(x => x.StartsWith("จำนวนคำขอทั้งหมด")).Trim());
        Assert.Contains("สรุปตามจุดหมาย", csv);
        Assert.Contains("เสร็จสิ้น", csv);
        Assert.Contains("02/10/2569", csv);
        Assert.Contains("ผู้ร่วมเดินทาง", csv);
        Assert.Contains("AMB-01", csv);
        Assert.Contains("กข 1234", csv);
        Assert.Contains("คนขับทดสอบ", csv);
        Assert.Contains("ผู้ประสานงาน", csv);
        Assert.Contains("\"40\"", csv);
        Assert.Contains("ทxxxบ", csv);
        Assert.DoesNotContain("สมชาย ทดสอบ", csv);
        Assert.Contains("F-1", csv);
        Assert.DoesNotContain("F-2", csv);
        Assert.DoesNotContain("F-3", csv);
        Assert.DoesNotContain("F-4", csv);
    }
}
