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
public class MeProfileController(AppDbContext db, IAuditLogService auditLogService, IHostEnvironment environment) : ControllerBase
{
    private static readonly Regex PhoneRegex = new(@"^[0-9+\-\s()]{6,30}$", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly HashSet<string> AllowedProfileImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };
    private static readonly Dictionary<string, string> ProfileImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };
    private const long MaxProfileImageBytes = 2 * 1024 * 1024;

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

    [HttpPost("image")]
    [RequestSizeLimit(MaxProfileImageBytes + 1024)]
    public async Task<ActionResult<ApiResponse<ProfileImageUploadResponse>>> UploadProfileImage(IFormFile? file)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<ProfileImageUploadResponse>.Fail("Invalid access token."));
        }

        var user = await db.Users.FirstOrDefaultAsync(item => item.Id == userId && item.IsActive);
        if (user is null)
        {
            return Unauthorized(ApiResponse<ProfileImageUploadResponse>.Fail("User not found."));
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse<ProfileImageUploadResponse>.Fail("กรุณาเลือกไฟล์รูปโปรไฟล์"));
        }

        if (file.Length > MaxProfileImageBytes)
        {
            return BadRequest(ApiResponse<ProfileImageUploadResponse>.Fail("ไฟล์รูปโปรไฟล์ต้องมีขนาดไม่เกิน 2 MB"));
        }

        if (!AllowedProfileImageTypes.Contains(file.ContentType))
        {
            return BadRequest(ApiResponse<ProfileImageUploadResponse>.Fail("รองรับเฉพาะไฟล์ JPG, PNG หรือ WEBP เท่านั้น"));
        }

        var extension = ProfileImageExtensions[file.ContentType];
        var storageDirectory = GetProfileImageDirectory(user.Id);
        Directory.CreateDirectory(storageDirectory);
        DeleteExistingProfileImageFiles(storageDirectory);

        var fileName = $"avatar{extension}";
        var absolutePath = Path.Combine(storageDirectory, fileName);
        await using (var stream = System.IO.File.Create(absolutePath))
        {
            await file.CopyToAsync(stream);
        }

        user.ProfileImagePath = ToStorageRelativePath(absolutePath);
        user.ProfileImageFileName = fileName;
        user.ProfileImageContentType = file.ContentType;
        user.ProfileImageUpdatedAt = DateTime.UtcNow;
        user.ProfileImageUrl = null;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await auditLogService.WriteAsync(
            userId,
            "UserProfile.ImageUploaded",
            "User",
            userId.Value.ToString(),
            $"Uploaded profile image {fileName}, {file.ContentType}, {file.Length} bytes.",
            "Success",
            HttpContext);

        return ApiResponse<ProfileImageUploadResponse>.Ok(new ProfileImageUploadResponse(
            BuildProfileImageUrl(user)!,
            "อัปโหลดรูปโปรไฟล์เรียบร้อยแล้ว"));
    }

    [HttpDelete("image")]
    public async Task<IActionResult> DeleteProfileImage()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<string>.Fail("Invalid access token."));
        }

        var user = await db.Users.FirstOrDefaultAsync(item => item.Id == userId && item.IsActive);
        if (user is null)
        {
            return Unauthorized(ApiResponse<string>.Fail("User not found."));
        }

        if (!string.IsNullOrWhiteSpace(user.ProfileImagePath))
        {
            var absolutePath = ResolveStoragePath(user.ProfileImagePath);
            if (absolutePath is not null && System.IO.File.Exists(absolutePath))
            {
                System.IO.File.Delete(absolutePath);
            }
        }

        user.ProfileImagePath = null;
        user.ProfileImageFileName = null;
        user.ProfileImageContentType = null;
        user.ProfileImageUpdatedAt = null;
        user.ProfileImageUrl = null;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await auditLogService.WriteAsync(
            userId,
            "UserProfile.ImageDeleted",
            "User",
            userId.Value.ToString(),
            "Deleted profile image.",
            "Success",
            HttpContext);

        return NoContent();
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
            BuildProfileImageUrl(user),
            !string.IsNullOrWhiteSpace(user.ProfileImagePath),
            user.ProfileImageUpdatedAt,
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

    private string GetProfileImageDirectory(Guid userId)
    {
        return Path.Combine(environment.ContentRootPath, "storage", "profile-images", userId.ToString("N"));
    }

    private string ToStorageRelativePath(string absolutePath)
    {
        var storageRoot = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "storage"));
        var fullPath = Path.GetFullPath(absolutePath);
        if (!fullPath.StartsWith(storageRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid profile image storage path.");
        }

        return Path.GetRelativePath(environment.ContentRootPath, fullPath).Replace('\\', '/');
    }

    private string? ResolveStoragePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        var storageRoot = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "storage"));
        var absolutePath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, relativePath));
        return absolutePath.StartsWith(storageRoot, StringComparison.OrdinalIgnoreCase) ? absolutePath : null;
    }

    private static void DeleteExistingProfileImageFiles(string storageDirectory)
    {
        if (!Directory.Exists(storageDirectory))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(storageDirectory, "avatar.*"))
        {
            System.IO.File.Delete(file);
        }
    }

    private static string? BuildProfileImageUrl(User user)
    {
        if (string.IsNullOrWhiteSpace(user.ProfileImagePath))
        {
            return user.ProfileImageUrl;
        }

        var version = user.ProfileImageUpdatedAt?.Ticks ?? user.UpdatedAt?.Ticks ?? DateTime.UtcNow.Ticks;
        return $"/api/users/{user.Id}/profile-image?v={version}";
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
