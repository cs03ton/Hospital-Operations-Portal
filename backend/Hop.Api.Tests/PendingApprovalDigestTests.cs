using Hop.Api.Services;
using Xunit;

namespace Hop.Api.Tests;

public sealed class PendingApprovalDigestTests
{
    [Theory]
    [InlineData(7, 59, -1)]
    [InlineData(8, 0, 0)]
    [InlineData(12, 29, 0)]
    [InlineData(12, 30, 1)]
    [InlineData(16, 0, 2)]
    public void Selects_latest_due_slot(int hour, int minute, int expected) =>
        Assert.Equal(expected, PendingApprovalDigest.CurrentSlot(new TimeOnly(hour, minute)));

    [Fact]
    public void Formats_only_nonzero_work_and_uses_thai_year()
    {
        var day = new DateOnly(2026, 10, 21);
        var text = PendingApprovalDigest.Format(day, new TimeOnly(8, 0),
            [("ผู้อำนวยการ", 1, 1, 2), ("หัวหน้าสูงสุดฝ่ายพยาบาล", 0, 0, 0)]);

        Assert.Equal("🔔 สรุปงานรออนุมัติ\n📅 21/10/2569\n🕒 รอบเวลา 08:00 น.\n\n👤 ผู้อำนวยการ\n📄 คำขอลา: 1 คำขอ\n↩️ ยกเลิกใบลา: 1 คำขอ\n🚗 คำขอใช้รถ: 2 คำขอ", text);
        Assert.DoesNotContain("หัวหน้าสูงสุดฝ่ายพยาบาล", text);
        Assert.Null(PendingApprovalDigest.Format(day, new TimeOnly(8, 0), [("ผู้อำนวยการ", 0, 0, 0)]));
        Assert.NotEqual(PendingApprovalDigest.StableId(day, 0, true), PendingApprovalDigest.StableId(day, 0, false));
        Assert.NotEqual(PendingApprovalDigest.StableId(day, 0, true), PendingApprovalDigest.StableId(day, 1, true));
    }

    [Theory]
    [InlineData(8, 0, "08:00")]
    [InlineData(12, 30, "12:30")]
    [InlineData(16, 0, "16:00")]
    public void Formats_each_slot_and_separates_approvers_without_zero_work(int hour, int minute, string expectedTime)
    {
        var text = PendingApprovalDigest.Format(new DateOnly(2026, 10, 21), new TimeOnly(hour, minute),
            [("ผู้อำนวยการ", 0, 0, 2), ("หัวหน้าสูงสุดฝ่ายพยาบาล", 1, 0, 0), ("หัวหน้าสูงสุดฝ่ายบริหาร", 0, 3, 0)]);

        Assert.StartsWith($"🔔 สรุปงานรออนุมัติ\n📅 21/10/2569\n🕒 รอบเวลา {expectedTime} น.", text);
        Assert.Contains("\n\n👤 ผู้อำนวยการ\n🚗 คำขอใช้รถ: 2 คำขอ", text);
        Assert.Contains("\n\n👤 หัวหน้าสูงสุดฝ่ายพยาบาล\n📄 คำขอลา: 1 คำขอ", text);
        Assert.EndsWith("\n\n👤 หัวหน้าสูงสุดฝ่ายบริหาร\n↩️ ยกเลิกใบลา: 3 คำขอ", text);
        Assert.DoesNotContain(": 0 คำขอ", text);
    }
}
