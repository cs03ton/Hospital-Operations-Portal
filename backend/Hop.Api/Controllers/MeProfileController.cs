using System.Security.Claims;
using System.Text.RegularExpressions;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/me/profile")]
[Authorize]
public class MeProfileController(AppDbContext db, IAuditLogService auditLogService) : ControllerBase
{
    private static readonly Regex PhoneRegex = new(@"^[0-9+\-\s()]{6,30}$", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    [HttpGet]
    public async Task<ActionResult<ApiResponse<UserProfileResponse>>> GetProfile()
    {
        var user = await LoadCurrentUser();
        if (user is null)
        {
            return Unauthorized(ApiResponse<UserProfileResponse>.Fail("Invalid access token."));
        }

        return ApiResponse<UserProfileResponse>.Ok(ToResponse(user));
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<UserProfileResponse>>> UpdateProfile(UpdateUserProfileRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<UserProfileResponse>.Fail("Invalid access token."));
        }

        var user = await db.Users.FirstOrDefaultAsync(item => item.Id == userId);
        if (user is null || !user.IsActive)
        {
            return Unauthorized(ApiResponse<UserProfileResponse>.Fail("User not found."));
        }

        var fullname = request.Fullname.Trim();
        if (string.IsNullOrWhiteSpace(fullname))
        {
            return BadRequest(ApiResponse<UserProfileResponse>.Fail("กรุณากรอกชื่อ-นามสกุล"));
        }

        var phoneNumber = NormalizeOptional(request.PhoneNumber);
        if (!string.IsNullOrWhiteSpace(phoneNumber) && !PhoneRegex.IsMatch(phoneNumber))
        {
            return BadRequest(ApiResponse<UserProfileResponse>.Fail("รูปแบบเบอร์โทรศัพท์ไม่ถูกต้อง"));
        }

        var email = NormalizeOptional(request.Email);
        if (!string.IsNullOrWhiteSpace(email) && !EmailRegex.IsMatch(email))
        {
            return BadRequest(ApiResponse<UserProfileResponse>.Fail("รูปแบบอีเมลไม่ถูกต้อง"));
        }

        var updatedFields = new List<string>();
        if (user.FullName != fullname)
        {
            user.FullName = fullname;
            updatedFields.Add("fullname");
        }
        UpdateIfChanged(user.Position, NormalizeOptional(request.Position), "position", value => user.Position = value, updatedFields);
        UpdateIfChanged(user.Email, email, "email", value => user.Email = value, updatedFields);
        UpdateIfChanged(user.PhoneNumber, phoneNumber, "phoneNumber", value => user.PhoneNumber = value, updatedFields);
        UpdateIfChanged(user.LeaveContactAddress, NormalizeOptional(request.LeaveContactAddress), "leaveContactAddress", value => user.LeaveContactAddress = value, updatedFields);
        UpdateIfChanged(user.ProfileImageUrl, NormalizeOptional(request.ProfileImageUrl), "profileImageUrl", value => user.ProfileImageUrl = value, updatedFields);

        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        if (updatedFields.Count > 0)
        {
            await auditLogService.WriteAsync(
                userId,
                "UserProfile.Updated",
                "User",
                userId.Value.ToString(),
                $"Updated profile fields: {string.Join(", ", updatedFields)}",
                "Success",
                HttpContext);
        }

        var updated = await LoadCurrentUser();
        return ApiResponse<UserProfileResponse>.Ok(ToResponse(updated!));
    }

    private async Task<User?> LoadCurrentUser()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return null;
        }

        return await db.Users
            .AsNoTracking()
            .Include(item => item.Department)
            .Include(item => item.LeaveApprovalRule)
            .Include(item => item.UserRoles)
                .ThenInclude(item => item.Role)!
                    .ThenInclude(item => item!.RolePermissions)
                        .ThenInclude(item => item.Permission)
            .FirstOrDefaultAsync(item => item.Id == userId && item.IsActive);
    }

    private static UserProfileResponse ToResponse(User user)
    {
        var roles = user.UserRoles
            .Where(item => item.Role?.IsActive == true)
            .Select(item => item.Role!.Name)
            .OrderBy(item => item)
            .ToList();

        var permissions = user.UserRoles
            .Where(item => item.Role?.IsActive == true)
            .SelectMany(item => item.Role!.RolePermissions)
            .Where(item => item.Permission?.IsActive == true)
            .Select(item => item.Permission!.Code)
            .Distinct()
            .OrderBy(item => item)
            .ToList();

        return new UserProfileResponse(
            user.Id,
            user.EmployeeCode,
            user.FullName,
            user.Username,
            user.Position,
            user.Email,
            user.PhoneNumber,
            user.LeaveContactAddress,
            user.ProfileImageUrl,
            roles,
            user.DepartmentId,
            user.Department?.Name,
            user.LeaveApprovalRuleId,
            user.LeaveApprovalRule?.Name,
            user.LineUserId,
            user.IsActive,
            permissions);
    }

    private static void UpdateIfChanged(string? current, string? next, string fieldName, Action<string?> assign, List<string> updatedFields)
    {
        if (current == next)
        {
            return;
        }

        assign(next);
        updatedFields.Add(fieldName);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
