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

        var items = await BuildRoleBasedNotificationsAsync(userId.Value, cancellationToken);

        return ApiResponse<IReadOnlyList<LeaveNotificationItemResponse>>.Ok(
            items
                .OrderBy(item => item.NotificationType == "ActionRequired" ? 0 : 1)
                .ThenBy(item => GetPriorityOrder(item.Priority))
                .ThenByDescending(item => item.CreatedAt)
                .Take(12)
                .ToList()
        );
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<LeaveNotificationItemResponse>>>> GetNotificationCenter(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? filter = null,
        [FromQuery] string? category = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<PagedResponse<LeaveNotificationItemResponse>>.Fail("Invalid access token."));
        }

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var items = await BuildRoleBasedNotificationsAsync(userId.Value, cancellationToken);
        items = ApplyFilter(items, filter, category, search).ToList();

        var totalItems = items.Count;
        var pagedItems = items
            .OrderBy(item => item.NotificationType == "ActionRequired" ? 0 : 1)
            .ThenBy(item => GetPriorityOrder(item.Priority))
            .ThenByDescending(item => item.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return ApiResponse<PagedResponse<LeaveNotificationItemResponse>>.Ok(new PagedResponse<LeaveNotificationItemResponse>(
            pagedItems,
            page,
            pageSize,
            totalItems,
            (int)Math.Ceiling(totalItems / (double)pageSize)
        ));
    }

    [HttpGet("badge")]
    public async Task<ActionResult<ApiResponse<int>>> GetBadgeCount(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<int>.Fail("Invalid access token."));
        }

        var items = await BuildRoleBasedNotificationsAsync(userId.Value, cancellationToken);
        return ApiResponse<int>.Ok(items.Count(item => item.NotificationType == "ActionRequired" && item.Unread));
    }

    [HttpPost("{id:guid}/read")]
    public async Task<ActionResult<ApiResponse<NotificationReadResponse>>> MarkAsRead(Guid id, [FromServices] IAuditLogService auditLogService, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<NotificationReadResponse>.Fail("Invalid access token."));
        }

        var roleNames = (await GetRoleNamesAsync(userId.Value, cancellationToken)).ToList();
        var notification = await db.Notifications
            .FirstOrDefaultAsync(item =>
                item.Id == id &&
                item.ArchivedAt == null &&
                (item.UserId == userId.Value || (item.TargetRole != null && roleNames.Contains(item.TargetRole))),
                cancellationToken);

        if (notification is null)
        {
            return NotFound(ApiResponse<NotificationReadResponse>.Fail("Notification not found."));
        }

        notification.IsRead = true;
        notification.ReadAt ??= DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(userId.Value, "Notification.Read", "Notification", id.ToString(), notification.Title, httpContext: HttpContext);

        return ApiResponse<NotificationReadResponse>.Ok(new NotificationReadResponse(notification.Id, notification.IsRead, notification.ReadAt));
    }

    [HttpPost("{id:guid}/archive")]
    public async Task<ActionResult<ApiResponse<NotificationReadResponse>>> Archive(Guid id, [FromServices] IAuditLogService auditLogService, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<NotificationReadResponse>.Fail("Invalid access token."));
        }

        var notification = await db.Notifications
            .FirstOrDefaultAsync(item => item.Id == id && item.UserId == userId.Value, cancellationToken);

        if (notification is null)
        {
            return NotFound(ApiResponse<NotificationReadResponse>.Fail("Notification not found."));
        }

        notification.ArchivedAt = DateTime.UtcNow;
        notification.IsRead = true;
        notification.ReadAt ??= DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(userId.Value, "Notification.Archived", "Notification", id.ToString(), notification.Title, httpContext: HttpContext);

        return ApiResponse<NotificationReadResponse>.Ok(new NotificationReadResponse(notification.Id, notification.IsRead, notification.ReadAt));
    }

    private async Task<IReadOnlyList<LeaveNotificationItemResponse>> BuildRoleBasedNotificationsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var roleNames = await GetRoleNamesAsync(userId, cancellationToken);
        var permissions = await GetPermissionCodesAsync(userId, cancellationToken);
        var items = new List<LeaveNotificationItemResponse>();

        items.AddRange(await GetPersistedNotificationsAsync(userId, roleNames, cancellationToken));

        if (permissions.Contains(LeavePermissions.ViewPendingApproval))
        {
            items.AddRange(await GetApprovalNotificationsAsync(userId, cancellationToken));
        }

        if (permissions.Contains(LeavePermissions.ViewOwn))
        {
            items.AddRange(await GetOwnerLeaveNotificationsAsync(userId, cancellationToken));
        }

        if (roleNames.Contains("DepartmentHead"))
        {
            items.AddRange(await GetDepartmentHeadNotificationsAsync(userId, cancellationToken));
        }

        if (roleNames.Contains("Director"))
        {
            items.AddRange(await GetDirectorNotificationsAsync(cancellationToken));
        }

        if (roleNames.Contains("Admin") || roleNames.Contains("SuperAdmin"))
        {
            items.AddRange(await GetAdminNotificationsAsync(roleNames.Contains("SuperAdmin"), cancellationToken));
        }

        return items
            .Where(item => item.ExpiresAt is null || item.ExpiresAt > DateTime.UtcNow)
            .GroupBy(item => item.NotificationType == "ActionRequired" && item.RequestId is not null
                ? $"ActionRequired:{item.RequestId}"
                : item.Id)
            .Select(group => group
                .OrderBy(item => item.Id.StartsWith("approval-", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                .ThenByDescending(item => item.CreatedAt)
                .First())
            .ToList();
    }

    private async Task<IReadOnlyList<LeaveNotificationItemResponse>> GetPersistedNotificationsAsync(Guid userId, HashSet<string> roleNames, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var roleNameList = roleNames.ToList();
        var notifications = await db.Notifications
            .AsNoTracking()
            .Where(item => item.ArchivedAt == null)
            .Where(item => item.ExpiresAt == null || item.ExpiresAt > now)
            .Where(item => item.UserId == userId || (item.TargetRole != null && roleNameList.Contains(item.TargetRole)))
            .OrderByDescending(item => item.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        return notifications.Select(item => new LeaveNotificationItemResponse(
            item.Id.ToString(),
            item.Category,
            TryParseGuid(item.ReferenceId),
            item.Title,
            item.Message,
            item.CreatedAt,
            !item.IsRead,
            item.ActionUrl ?? string.Empty,
            item.Category,
            item.Priority,
            item.NotificationType,
            item.TargetRole,
            item.ReferenceEntity,
            item.ReferenceId,
            item.ExpiresAt
        )).ToList();
    }

    private async Task<IReadOnlyList<LeaveNotificationItemResponse>> GetApprovalNotificationsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var pendingApprovals = await notificationService.GetMyPendingApprovalsAsync(userId, cancellationToken);
        return pendingApprovals.Select(item => new LeaveNotificationItemResponse(
            $"approval-{item.RequestId}",
            "ApprovalPending",
            item.RequestId,
            $"คำขอลา {item.RequestNumber ?? "-"} รออนุมัติขั้นที่ {item.CurrentStep}",
            $"{item.EmployeeName ?? "-"} · {item.LeaveType ?? "-"} · {item.StartDate:dd/MM/yyyy}-{item.EndDate:dd/MM/yyyy} · ส่งเมื่อ {FormatDateTime(item.SubmittedAt)}",
            item.SubmittedAt ?? DateTime.UtcNow,
            true,
            $"/leave/{item.RequestId}",
            "Leave",
            item.Priority,
            "ActionRequired",
            "Approver",
            "LeaveRequest",
            item.RequestId.ToString()
        )).ToList();
    }

    private async Task<IReadOnlyList<LeaveNotificationItemResponse>> GetOwnerLeaveNotificationsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var ownRequests = await db.LeaveRequests
            .AsNoTracking()
            .Include(item => item.LeaveType)
            .Include(item => item.CurrentApprover)
            .Include(item => item.Approvals)
            .Where(item => item.UserId == userId && item.Status != "Draft")
            .OrderByDescending(item => item.UpdatedAt ?? item.SubmittedAt ?? item.CreatedAt)
            .Take(8)
            .ToListAsync(cancellationToken);

        return ownRequests.Select(ToOwnerNotification).ToList();
    }

    private async Task<IReadOnlyList<LeaveNotificationItemResponse>> GetDepartmentHeadNotificationsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var departmentId = await db.Users
            .AsNoTracking()
            .Where(item => item.Id == userId)
            .Select(item => item.DepartmentId)
            .FirstOrDefaultAsync(cancellationToken);

        if (departmentId is null)
        {
            return [];
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var tomorrow = today.AddDays(1);
        var teamToday = await db.LeaveRequests
            .AsNoTracking()
            .CountAsync(item =>
                item.User != null &&
                item.User.DepartmentId == departmentId &&
                item.Status == "Approved" &&
                item.StartDate <= today &&
                item.EndDate >= today,
                cancellationToken);
        var teamTomorrow = await db.LeaveRequests
            .AsNoTracking()
            .CountAsync(item =>
                item.User != null &&
                item.User.DepartmentId == departmentId &&
                item.Status == "Approved" &&
                item.StartDate <= tomorrow &&
                item.EndDate >= tomorrow,
                cancellationToken);

        var items = new List<LeaveNotificationItemResponse>();
        if (teamToday > 0)
        {
            items.Add(CreateSystemNotification("head-team-today", "ลูกทีมลาวันนี้", $"วันนี้มีลูกทีมลางาน {teamToday} คน", "Leave", "Information", "Information", "/leave/calendar"));
        }

        if (teamTomorrow > 0)
        {
            items.Add(CreateSystemNotification("head-team-tomorrow", "ลูกทีมลาพรุ่งนี้", $"พรุ่งนี้มีลูกทีมลางาน {teamTomorrow} คน", "Leave", "Information", "Information", "/leave/calendar"));
        }

        return items;
    }

    private async Task<IReadOnlyList<LeaveNotificationItemResponse>> GetDirectorNotificationsAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var onLeaveToday = await db.LeaveRequests
            .AsNoTracking()
            .Where(item => item.Status == "Approved" && item.StartDate <= today && item.EndDate >= today)
            .Select(item => item.UserId)
            .Distinct()
            .CountAsync(cancellationToken);

        return onLeaveToday > 0
            ? [CreateSystemNotification("director-leave-today", "จำนวนผู้ลาวันนี้", $"วันนี้มีเจ้าหน้าที่ลางาน {onLeaveToday} คน", "Leave", "Information", "Information", "/leave/calendar")]
            : [];
    }

    private async Task<IReadOnlyList<LeaveNotificationItemResponse>> GetAdminNotificationsAsync(bool includeSecurity, CancellationToken cancellationToken)
    {
        var items = new List<LeaveNotificationItemResponse>();
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var nextYear = now.Year + 1;
        var hasNextYearHolidays = await db.LeaveHolidays
            .AsNoTracking()
            .AnyAsync(item => item.IsActive && item.HolidayDate.Year == nextYear, cancellationToken);

        if (!hasNextYearHolidays)
        {
            items.Add(CreateSystemNotification("admin-next-year-holidays", "ยังไม่ได้ตั้งค่าวันหยุดปีใหม่", $"ยังไม่พบวันหยุดราชการปี {nextYear}", "Backup", "High", "ActionRequired", "/admin/leave-holidays"));
        }

        var failedLine = await db.LineDeliveryLogs
            .AsNoTracking()
            .CountAsync(item => item.Status == "Failed", cancellationToken);
        if (failedLine > 0)
        {
            items.Add(CreateSystemNotification("admin-line-failed", "LINE Delivery Failed", $"มีรายการส่ง LINE ล้มเหลว {failedLine} รายการ", "Notification", "High", "ActionRequired"));
        }

        if (includeSecurity)
        {
            var failedLogin = await db.AuditLogs
                .AsNoTracking()
                .CountAsync(item => item.CreatedAt >= todayStart && item.Action.Contains("Login") && item.Result != "Success", cancellationToken);
            var denied = await db.AuditLogs
                .AsNoTracking()
                .CountAsync(item => item.CreatedAt >= todayStart && item.Action.Contains("PermissionDenied"), cancellationToken);

            if (failedLogin >= 3)
            {
                items.Add(CreateSystemNotification("superadmin-failed-login", "Login Failed หลายครั้ง", $"วันนี้มี Login ล้มเหลว {failedLogin} ครั้ง", "User", "Critical", "ActionRequired", "/admin/audit-logs"));
            }

            if (denied > 0)
            {
                items.Add(CreateSystemNotification("superadmin-permission-denied", "Permission Denied ผิดปกติ", $"วันนี้มีเหตุการณ์ถูกปฏิเสธสิทธิ์ {denied} ครั้ง", "User", "High", "ActionRequired", "/admin/audit-logs"));
            }

            items.Add(CreateSystemNotification("superadmin-database-health", "Database Health", "ฐานข้อมูลตอบสนองตามปกติ", "System", "Success", "Information"));
            items.Add(CreateSystemNotification("superadmin-system-health", "System Health", "API พร้อมใช้งาน", "System", "Success", "Information"));
        }

        return items;
    }

    private async Task<HashSet<string>> GetPermissionCodesAsync(Guid userId, CancellationToken cancellationToken)
    {
        return (await db.UserRoles
                .AsNoTracking()
                .Where(item => item.UserId == userId && item.Role != null && item.Role.IsActive)
                .SelectMany(item => item.Role!.RolePermissions)
                .Where(item => item.Permission != null && item.Permission.IsActive)
                .Select(item => item.Permission!.Code)
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private async Task<HashSet<string>> GetRoleNamesAsync(Guid userId, CancellationToken cancellationToken)
    {
        return (await db.UserRoles
                .AsNoTracking()
                .Where(item => item.UserId == userId && item.Role != null && item.Role.IsActive)
                .Select(item => item.Role!.Name)
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
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
            request.Status is "Rejected",
            $"/leave/{request.Id}",
            "Leave",
            GetOwnerPriority(request.Status),
            "Information",
            "Staff",
            "LeaveRequest",
            request.Id.ToString(),
            request.Status switch
            {
                "Approved" => createdAt.AddDays(30),
                "Cancelled" => createdAt.AddDays(30),
                "Rejected" => createdAt.AddDays(30),
                _ => createdAt.AddDays(7)
            }
        );
    }

    private static LeaveNotificationItemResponse CreateSystemNotification(string id, string title, string message, string category, string priority, string notificationType, string path = "")
    {
        return new LeaveNotificationItemResponse(
            id,
            category,
            null,
            title,
            message,
            DateTime.UtcNow,
            notificationType == "ActionRequired",
            path,
            category,
            priority,
            notificationType,
            null,
            null,
            null
        );
    }

    private static IEnumerable<LeaveNotificationItemResponse> ApplyFilter(
        IEnumerable<LeaveNotificationItemResponse> items,
        string? filter,
        string? category,
        string? search)
    {
        var query = items;
        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = filter.Trim().ToLowerInvariant() switch
            {
                "action-required" => query.Where(item => item.NotificationType == "ActionRequired"),
                "unread" => query.Where(item => item.Unread),
                "read" => query.Where(item => !item.Unread),
                _ => query
            };
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(item => string.Equals(item.Category, category.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(item =>
                item.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                item.Message.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        return query;
    }

    private static int GetPriorityOrder(string priority)
    {
        return priority switch
        {
            "Critical" => 0,
            "High" => 1,
            "Normal" => 2,
            "Information" => 3,
            "Success" => 4,
            _ => 5
        };
    }

    private static string GetOwnerPriority(string status)
    {
        return status switch
        {
            "Rejected" => "High",
            "Approved" => "Success",
            "Cancelled" => "Information",
            _ => "Information"
        };
    }

    private static Guid? TryParseGuid(string? value)
    {
        return Guid.TryParse(value, out var id) ? id : null;
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
