using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hop.Api.Tests;

public class LeaveHolidayImportTests
{
    [Theory]
    [InlineData("1/1/2027", "2027-01-01")]
    [InlineData("6/4/2027", "2027-04-06")]
    [InlineData("13/04/2027", "2027-04-13")]
    [InlineData("06/04/2570", "2027-04-06")]
    [InlineData("2570-04-06", "2027-04-06")]
    [InlineData("2027-04-06", "2027-04-06")]
    public void Parses_calendar_dates_without_changing_global_API_parser(string input, string expected)
    {
        Assert.True(LeaveHolidayImportReader.TryParseDate(input, out var date));
        Assert.Equal(expected, date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        Assert.False(CalendarDateInput.TryParse("6/4/2027", out _));
    }

    [Theory]
    [InlineData("31/2/2027")]
    [InlineData("4/13/2027")]
    [InlineData("1/1/27")]
    [InlineData("not a date")]
    public void Rejects_invalid_or_ambiguous_dates(string input) => Assert.False(LeaveHolidayImportReader.TryParseDate(input, out _));

    [Fact]
    public async Task Example_two_column_CSV_allows_same_name_on_other_days_and_years()
    {
        await using var db = Database();
        db.LeaveHolidays.Add(new LeaveHoliday { HolidayDate = new(2026, 1, 1), Name = "วันขึ้นปีใหม่" });
        await db.SaveChangesAsync();
        var controller = Controller(db);
        var result = await Preview(controller, Csv("วันที่,ชื่อวันหยุด\n1/1/2027,วันขึ้นปีใหม่\n6/4/2027,วันจักรี\n13/4/2027,วันสงกรานต์\n14/4/2027,วันสงกรานต์\n15/4/2027,วันสงกรานต์"));
        Assert.Equal(5, result.ValidRows); Assert.Equal(0, result.InvalidRows);
        var confirm = await controller.ConfirmImport(new(result.Rows.Select(x => new LeaveHolidayImportRowRequest(x.HolidayDate!.Value, x.Name, x.HolidayType)).ToArray()));
        Assert.Equal(5, confirm.Value!.Data!.AddedCount);
        Assert.Equal(6, await db.LeaveHolidays.CountAsync());
        Assert.All(await db.LeaveHolidays.ToListAsync(), x => Assert.True(x.IsActive));
    }

    [Fact]
    public async Task Legacy_third_column_and_quoted_names_remain_supported()
    {
        await using var db = Database();
        var result = await Preview(Controller(db), Csv("วันที่,ชื่อวันหยุด,ประเภทวันหยุด\n2027-01-01,\"วันหยุด, พิเศษ\",Local"));
        var row = Assert.Single(result.Rows); Assert.True(row.IsValid); Assert.Equal("Local", row.HolidayType); Assert.Equal("วันหยุด, พิเศษ", row.Name);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Excel_real_dates_use_workbook_epoch_and_shared_string_headers(bool date1904)
    {
        var date = new DateTime(2027, 4, 6);
        var serial = date1904 ? (date - new DateTime(1904, 1, 1)).TotalDays : date.ToOADate();
        var file = Xlsx($"<row r='1'><c r='A1' t='s'><v>0</v></c><c r='B1' t='s'><v>1</v></c></row><row r='2'><c r='A2'><v>{serial.ToString(CultureInfo.InvariantCulture)}</v></c><c r='B2' t='s'><v>2</v></c></row>", date1904);
        await using var db = Database();
        var row = Assert.Single((await Preview(Controller(db), file)).Rows);
        Assert.True(row.IsValid); Assert.Equal(new DateOnly(2027, 4, 6), row.HolidayDate); Assert.Equal("วันจักรี", row.Name);
    }

    [Fact]
    public async Task Excel_text_dates_and_sparse_columns_do_not_shift_empty_cells()
    {
        var file = Xlsx($"<row r='1'>{Cell("A1", "วันที่")}{Cell("C1", "ชื่อวันหยุด")}</row>" +
            $"<row r='2'>{Cell("A2", "13/4/2570")}{Cell("C2", "วันสงกรานต์")}</row>" +
            $"<row r='3'>{Cell("C3", "วันที่หายไป")}</row>");
        await using var db = Database();
        var result = await Preview(Controller(db), file);
        Assert.Equal(new DateOnly(2027, 4, 13), result.Rows[0].HolidayDate); Assert.True(result.Rows[0].IsValid);
        Assert.Null(result.Rows[1].HolidayDate); Assert.Equal("วันที่หายไป", result.Rows[1].Name); Assert.False(result.Rows[1].IsValid);
    }

    [Fact]
    public async Task Duplicated_dates_in_file_or_database_block_whole_confirm_without_overwriting()
    {
        await using var db = Database();
        var original = new LeaveHoliday { HolidayDate = new(2027, 1, 1), Name = "เดิม", IsActive = false };
        db.LeaveHolidays.Add(original); await db.SaveChangesAsync();
        var controller = Controller(db);
        var preview = await Preview(controller, Csv("วันที่,ชื่อวันหยุด\n1/1/2027,เปลี่ยนชื่อ\n13/4/2027,วันสงกรานต์\n13/04/2570,วันสงกรานต์"));
        Assert.Equal(3, preview.InvalidRows);
        Assert.All(preview.Rows, row => Assert.Contains("วันที่ซ้ำ", row.Errors));
        var result = await controller.ConfirmImport(new([new(new(2027, 1, 1), "เปลี่ยนชื่อ", "National"), new(new(2027, 4, 14), "ใหม่", "National")]));
        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Single(await db.LeaveHolidays.ToListAsync()); Assert.Equal("เดิม", original.Name); Assert.False(original.IsActive);
    }

    [Fact]
    public async Task Confirm_rechecks_database_after_successful_preview()
    {
        await using var db = Database(); var controller = Controller(db);
        Assert.Equal(1, (await Preview(controller, Csv("วันที่,ชื่อวันหยุด\n6/4/2027,วันจักรี"))).ValidRows);
        db.LeaveHolidays.Add(new() { HolidayDate = new(2027, 4, 6), Name = "เพิ่มระหว่างตรวจ" }); await db.SaveChangesAsync();
        var result = await controller.ConfirmImport(new([new(new(2027, 4, 6), "วันจักรี", "National")]));
        Assert.IsType<BadRequestObjectResult>(result.Result); Assert.Equal(1, await db.LeaveHolidays.CountAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("วันที่,ชื่อวันหยุด\n")]
    [InlineData("วันที่,หัวข้ออื่น\n1/1/2027,ผิด")]
    [InlineData("วันที่,วันที่,ชื่อวันหยุด\n1/1/2027,1/1/2027,ผิด")]
    [InlineData("วันที่,ชื่อวันหยุด\n31/2/2027,ผิด")]
    [InlineData("วันที่,ชื่อวันหยุด\n1/1/2027,")]
    public async Task Empty_missing_headers_and_invalid_rows_return_preview_errors(string csv)
    {
        await using var db = Database(); var result = await Preview(Controller(db), Csv(csv));
        Assert.True(result.InvalidRows > 0); Assert.Equal(0, await db.LeaveHolidays.CountAsync()); Assert.NotEmpty(result.Rows[0].Errors);
    }

    [Fact]
    public async Task Corrupt_XLSX_and_limits_are_reported_as_preview_errors()
    {
        Assert.NotNull((await LeaveHolidayImportReader.ReadAsync(File("broken.xlsx", Encoding.UTF8.GetBytes("broken"))))[0].Error);
        Assert.NotNull((await LeaveHolidayImportReader.ReadAsync(File("large.csv", new byte[2 * 1024 * 1024 + 1])))[0].Error);
        Assert.NotNull((await LeaveHolidayImportReader.ReadAsync(Csv("วันที่,ชื่อวันหยุด\n" + string.Join('\n', Enumerable.Repeat("1/1/2027,ทดสอบ", 5001)))))[0].Error);
    }

    [Fact]
    public async Task Downloaded_template_roundtrips_with_repeated_names_and_UTF8_BOM()
    {
        await using var db = Database(); var controller = Controller(db);
        var template = Assert.IsType<FileContentResult>(controller.DownloadImportTemplate());
        Assert.Equal(new byte[] { 239, 187, 191 }, template.FileContents.Take(3));
        var result = await Preview(controller, File("template.csv", template.FileContents));
        Assert.Equal(5, result.ValidRows); Assert.Equal(0, result.InvalidRows);
        Assert.Equal(new[] { 2, 3, 4, 5, 6 }, result.Rows.Select(x => x.RowNumber));
    }

    private static AppDbContext Database() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static LeaveHolidaysController Controller(AppDbContext db) => new(db, new Audit()) { ControllerContext = new() { HttpContext = new DefaultHttpContext() } };
    private static async Task<LeaveHolidayImportPreviewResponse> Preview(LeaveHolidaysController controller, IFormFile file) => (await controller.PreviewImport(file)).Value!.Data!;
    private static IFormFile Csv(string content) => File("holidays.csv", Encoding.UTF8.GetBytes(content));
    private static IFormFile File(string name, byte[] bytes) => new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", name);
    private static string Cell(string reference, string value) => $"<c r='{reference}' t='inlineStr'><is><t>{value}</t></is></c>";
    private static IFormFile Xlsx(string rows, bool date1904 = false)
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            void Entry(string name, string xml) { using var writer = new StreamWriter(zip.CreateEntry(name).Open()); writer.Write(xml); }
            Entry("xl/workbook.xml", $"<workbook xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main' xmlns:r='http://schemas.openxmlformats.org/officeDocument/2006/relationships'><workbookPr date1904='{(date1904 ? 1 : 0)}'/><sheets><sheet name='วันหยุด' sheetId='2' r:id='rId2'/></sheets></workbook>");
            Entry("xl/_rels/workbook.xml.rels", "<Relationships xmlns='http://schemas.openxmlformats.org/package/2006/relationships'><Relationship Id='rId2' Target='worksheets/sheet2.xml'/></Relationships>");
            Entry("xl/sharedStrings.xml", "<sst xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main'><si><t>วันที่</t></si><si><t>ชื่อวันหยุด</t></si><si><t>วันจักรี</t></si></sst>");
            Entry("xl/worksheets/sheet2.xml", $"<worksheet xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main'><sheetData>{rows}</sheetData></worksheet>");
        }
        return File("holidays.xlsx", stream.ToArray());
    }
    private sealed class Audit : IAuditLogService
    {
        public Task WriteAsync(Guid? userId, string action, string resource, string? resourceId, string? detail, string result = "Success", HttpContext? httpContext = null) => Task.CompletedTask;
    }
}
