using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(IPendingApprovalNotificationService notificationService, AppDbContext db) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LeaveNotificationItemResponse>>>> GetMyNotifications(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<IReadOnlyList<LeaveNotificationItemResponse>>.Fail("Invalid access token."));
        }

        var items = new List<LeaveNotificationItemResponse>();
        if (await HasPermissionAsync(userId.Value, LeavePermissions.ViewPendingApproval))
        {
            var pendingApprovals = await notificationService.GetMyPendingApprovalsAsync(userId.Value, cancellationToken);
            items.AddRange(pendingApprovals.Select(item => new LeaveNotificationItemResponse(
                $"approval-{item.RequestId}",
                "ApprovalPending",
                item.RequestId,
                $"คำขอลา {item.RequestNumber ?? "-"} รออนุมัติขั้นที่ {item.CurrentStep}",
                $"{item.EmployeeName ?? "-"} · {item.LeaveType ?? "-"} · {item.StartDate:dd/MM/yyyy}-{item.EndDate:dd/MM/yyyy} · ส่งเมื่อ {FormatDateTime(item.SubmittedAt)}",
                item.SubmittedAt ?? DateTime.UtcNow,
                true,
                $"/leave/{item.RequestId}"
            )));
        }

        var ownRequests = await db.LeaveRequests
            .AsNoTracking()
            .Include(item => item.LeaveType)
            .Include(item => item.CurrentApprover)
            .Include(item => item.Approvals)
            .Where(item => item.UserId == userId.Value && item.Status != "Draft")
            .OrderByDescending(item => item.UpdatedAt ?? item.SubmittedAt ?? item.CreatedAt)
            .Take(8)
            .ToListAsync(cancellationToken);
        items.AddRange(ownRequests.Select(ToOwnerNotification));

        return ApiResponse<IReadOnlyList<LeaveNotificationItemResponse>>.Ok(
            items
                .OrderByDescending(item => item.CreatedAt)
                .Take(12)
                .ToList()
        );
    }

    private async Task<bool> HasPermissionAsync(Guid userId, string permissionCode)
    {
        return await db.UserRoles
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.Role != null && item.Role.IsActive)
            .SelectMany(item => item.Role!.RolePermissions)
            .AnyAsync(item => item.Permission != null && item.Permission.IsActive && item.Permission.Code == permissionCode);
    }

    private static LeaveNotificationItemResponse ToOwnerNotification(LeaveRequest request)
    {
        var requestCode = request.RequestNumber ?? "-";
        var currentStepName = request.Approvals
            .OrderBy(item => item.StepOrder)
            .FirstOrDefault(item => item.Status == "Pending")
            ?.StepName;
        var createdAt = request.UpdatedAt ?? request.SubmittedAt ?? request.CreatedAt;
        var title = request.Status switch
        {
            "Pending" => $"คำขอลา {requestCode} อยู่ระหว่างอนุมัติ",
            "Approved" => $"คำขอลา {requestCode} อนุมัติแล้ว",
            "Rejected" => $"คำขอลา {requestCode} ไม่อนุมัติ",
            "Cancelled" => $"คำขอลา {requestCode} ยกเลิกแล้ว",
            _ => $"คำขอลา {requestCode}"
        };
        var message = request.Status switch
        {
            "Pending" when !string.IsNullOrWhiteSpace(currentStepName) && !string.IsNullOrWhiteSpace(request.CurrentApprover?.FullName) =>
                $"คำขอลา {requestCode} ถูกส่งไปยัง {currentStepName} แล้ว และรออนุมัติจาก {request.CurrentApprover.FullName}",
            "Pending" when !string.IsNullOrWhiteSpace(request.CurrentApprover?.FullName) =>
                $"คำขอลา {requestCode} รออนุมัติจาก {request.CurrentApprover.FullName}",
            "Pending" => $"คำขอลา {requestCode} ส่งคำขอแล้วและอยู่ระหว่างรออนุมัติ",
            "Approved" => $"คำขอลา {requestCode} ได้รับการอนุมัติแล้ว",
            "Rejected" => $"คำขอลา {requestCode} ถูกไม่อนุมัติ",
            "Cancelled" => $"คำขอลา {requestCode} ถูกยกเลิกแล้ว",
            _ => $"คำขอลา {requestCode} มีการอัปเดตสถานะ"
        };

        return new LeaveNotificationItemResponse(
            $"owner-{request.Id}-{request.Status}",
            $"Leave{request.Status}",
            request.Id,
            title,
            message,
            createdAt,
            request.Status is "Pending" or "Rejected",
            $"/leave/{request.Id}"
        );
    }

    private static string FormatDateTime(DateTime? value)
    {
        return value is null ? "-" : value.Value.ToString("dd/MM/yyyy HH:mm");
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
