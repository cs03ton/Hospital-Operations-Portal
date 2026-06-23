using Hop.Api.Data;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed class LeaveValidationService(
    AppDbContext db,
    ILeaveCalendarService calendarService,
    ILeaveBalanceValidationService balanceValidationService) : ILeaveValidationService
{
    public async Task<LeaveValidationResult> ValidateDraftAsync(LeaveRequest leaveRequest, Guid? excludeLeaveRequestId = null)
    {
        var durationType = LeaveDurationTypes.Normalize(leaveRequest.DurationType);
        if (string.IsNullOrWhiteSpace(durationType))
        {
            return new LeaveValidationResult(false, "ประเภทช่วงเวลาการลาไม่ถูกต้อง", 0);
        }

        leaveRequest.DurationType = durationType;
        if (leaveRequest.EndDate < leaveRequest.StartDate)
        {
            return new LeaveValidationResult(false, "วันที่สิ้นสุดต้องไม่น้อยกว่าวันที่เริ่มลา", 0);
        }

        var requestedHalfDay = LeaveDurationTypes.IsHalfDay(durationType);
        if (requestedHalfDay && leaveRequest.StartDate != leaveRequest.EndDate)
        {
            return new LeaveValidationResult(false, "การลาครึ่งวันต้องเลือกวันที่เริ่มลาและวันที่สิ้นสุดเป็นวันเดียวกัน", 0);
        }

        var calculatedDays = await calendarService.CalculateBusinessDaysAsync(
            leaveRequest.StartDate,
            leaveRequest.EndDate,
            requestedHalfDay);

        if (calculatedDays <= 0)
        {
            return new LeaveValidationResult(false, "ช่วงวันที่เลือกไม่มีวันทำการ กรุณาเลือกวันที่ลาใหม่", calculatedDays);
        }

        var hasOverlap = await db.LeaveRequests
            .AsNoTracking()
            .Where(item => item.UserId == leaveRequest.UserId)
            .Where(item => item.Id != excludeLeaveRequestId)
            .Where(item => item.Status == "Pending" || item.Status == "Approved")
            .AnyAsync(item => item.StartDate <= leaveRequest.EndDate && item.EndDate >= leaveRequest.StartDate);

        if (hasOverlap)
        {
            return new LeaveValidationResult(false, "ไม่สามารถขอลาซ้ำหรือทับซ้อนกับคำขอที่รออนุมัติหรืออนุมัติแล้ว", calculatedDays);
        }

        var leaveType = leaveRequest.LeaveType ?? await db.LeaveTypes.FindAsync(leaveRequest.LeaveTypeId);
        if (leaveType is null || !leaveType.IsActive)
        {
            return new LeaveValidationResult(false, "ไม่พบประเภทการลาที่เปิดใช้งาน", calculatedDays);
        }

        var balanceValidation = await balanceValidationService.ValidateAvailableBalanceAsync(leaveRequest, leaveType, calculatedDays);
        if (!balanceValidation.IsValid)
        {
            return new LeaveValidationResult(false, balanceValidation.Message, calculatedDays);
        }

        return new LeaveValidationResult(true, null, calculatedDays);
    }

    public async Task<LeaveValidationResult> ValidateSubmitAsync(LeaveRequest leaveRequest)
    {
        var draftValidation = await ValidateDraftAsync(leaveRequest, leaveRequest.Id);
        if (!draftValidation.IsValid)
        {
            return draftValidation;
        }

        var leaveType = leaveRequest.LeaveType ?? await db.LeaveTypes.FindAsync(leaveRequest.LeaveTypeId);
        if (leaveType is null || !leaveType.IsActive)
        {
            return new LeaveValidationResult(false, "ไม่พบประเภทการลาที่เปิดใช้งาน", draftValidation.CalculatedDays);
        }

        if (leaveType.RequiresAttachment)
        {
            var hasAttachment = await db.LeaveAttachments
                .AsNoTracking()
                .AnyAsync(item => item.LeaveRequestId == leaveRequest.Id);
            if (!hasAttachment)
            {
                return new LeaveValidationResult(false, "ประเภทการลานี้ต้องแนบไฟล์ประกอบก่อนส่งคำขอ", draftValidation.CalculatedDays);
            }
        }

        return draftValidation;
    }
}
