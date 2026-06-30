using Hop.Api.Models;

namespace Hop.Api.Services;

public static class LeaveLineMessageTemplates
{
    public static string SubmittedToApprover(LeaveRequest request)
    {
        return string.Join('\n',
            "แจ้งเตือนคำขอลา",
            $"ผู้ขอ: {request.User?.FullName ?? "-"}",
            $"ประเภท: {request.LeaveType?.Name ?? "-"}",
            $"วันที่: {FormatDate(request.StartDate)} - {FormatDate(request.EndDate)}",
            "สถานะ: รออนุมัติ",
            "กรุณาเข้าสู่ระบบเพื่อตรวจสอบ");
    }

    public static string NextApprover(LeaveRequest request, string? previousApproverName)
    {
        return string.Join('\n',
            "แจ้งเตือนคำขอลารออนุมัติขั้นถัดไป",
            $"ผู้ขอ: {request.User?.FullName ?? "-"}",
            $"ประเภท: {request.LeaveType?.Name ?? "-"}",
            $"วันที่: {FormatDate(request.StartDate)} - {FormatDate(request.EndDate)}",
            $"อนุมัติแล้วโดย: {previousApproverName ?? "-"}",
            "สถานะ: รออนุมัติจากคุณ");
    }

    public static string ApprovedToRequester(LeaveRequest request)
    {
        return string.Join('\n',
            "คำขอลาของคุณได้รับการอนุมัติแล้ว",
            $"เลขที่คำขอ: {RequestCode(request)}",
            $"ประเภท: {request.LeaveType?.Name ?? "-"}",
            $"วันที่: {FormatDate(request.StartDate)} - {FormatDate(request.EndDate)}",
            $"จำนวนวัน: {request.TotalDays:0.##}",
            "สถานะ: อนุมัติแล้ว");
    }

    public static string RejectedToRequester(LeaveRequest request, string? approverName, string? comment)
    {
        return string.Join('\n',
            "คำขอลาของคุณไม่ได้รับการอนุมัติ",
            $"เลขที่คำขอ: {RequestCode(request)}",
            $"ประเภท: {request.LeaveType?.Name ?? "-"}",
            $"วันที่: {FormatDate(request.StartDate)} - {FormatDate(request.EndDate)}",
            $"ผู้พิจารณา: {approverName ?? "-"}",
            $"เหตุผล: {Blank(comment)}");
    }

    public static string CancelledToApprover(LeaveRequest request)
    {
        return string.Join('\n',
            "คำขอลาถูกยกเลิกแล้ว",
            $"ผู้ขอ: {request.User?.FullName ?? "-"}",
            $"เลขที่คำขอ: {RequestCode(request)}",
            "สถานะ: ยกเลิกแล้ว");
    }

    private static string RequestCode(LeaveRequest request)
    {
        return request.RequestNumber ?? request.Id.ToString("N")[..8].ToUpperInvariant();
    }

    private static string Blank(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }

    private static string FormatDate(DateOnly date)
    {
        return $"{date.Day:00}/{date.Month:00}/{date.Year + 543}";
    }
}
