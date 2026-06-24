using Hop.Api.Interfaces;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace Hop.Api.Tests;

public class LeavePdfTests
{
    [Fact]
    public void SimplePdfWriter_CreateA4_ReturnsPdfBytes()
    {
        var bytes = SimplePdfWriter.CreateA4(
            [new PdfLine("แบบฟอร์มใบลา", 50, 790, 18)],
            logo: null);

        Assert.True(bytes.Length > 100);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public void LeavePdfService_GeneratesQuestPdfWithThaiFont_WhenFontIsAvailable()
    {
        var fontPath = OperatingSystem.IsWindows()
            ? @"C:\Windows\Fonts\tahoma.ttf"
            : Path.Combine(AppContext.BaseDirectory, "assets", "fonts", "NotoSansThai-Regular.ttf");
        if (!File.Exists(fontPath))
        {
            return;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LeavePdf:FontPath"] = fontPath,
                ["LeavePdf:FontFamily"] = "HOP Test Thai",
                ["LeavePdf:FontSize"] = "16",
                ["LeavePdf:LineHeight"] = "1.2"
            })
            .Build();
        var service = new LeavePdfService(new TestWebHostEnvironment(), configuration);
        var request = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            RequestNumber = "LV-202606-001",
            StartDate = new DateOnly(2026, 6, 20),
            EndDate = new DateOnly(2026, 6, 20),
            DurationType = "FULL_DAY",
            TotalDays = 1,
            Reason = "ทดสอบภาษาไทยในใบคำขอลา",
            Status = "Approved",
            User = new User
            {
                FullName = "นายทดสอบ ระบบลา",
                EmployeeCode = "IT001",
                Position = "เจ้าหน้าที่",
                PhoneNumber = "0812345678",
                LeaveContactAddress = "อำเภอนาหมื่น จังหวัดน่าน",
                Department = new Department { Name = "แผนกเทคโนโลยีสารสนเทศ" }
            },
            LeaveType = new LeaveType { Code = "annual", Name = "ลาพักผ่อน" }
        };

        var bytes = service.GenerateLeaveRequestPdf(
            request,
            new LeavePdfRenderContext("โรงพยาบาลนาหมื่น", "0.1.0", null, []));

        Assert.True(bytes.Length > 1000);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "Hop.Api.Tests";
        public string WebRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
