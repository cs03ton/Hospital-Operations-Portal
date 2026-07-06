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
[Route("api/[controller]")]
[Authorize]
public class UsersController(AppDbContext db, IAuditLogService auditLogService, IHostEnvironment environment) : ControllerBase
{
    [HttpGet("{id:guid}/profile-image")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProfileImage(Guid id)
    {
        var user = await db.Users
            .AsNoTracking()
            .Where(item => item.Id == id && item.IsActive)
            .Select(item => new
            {
                item.ProfileImagePath,
                item.ProfileImageContentType,
                item.ProfileImageUpdatedAt
            })
            .FirstOrDefaultAsync();

        if (user is null || string.IsNullOrWhiteSpace(user.ProfileImagePath))
        {
            return NotFound();
        }

        var absolutePath = ResolveStoragePath(user.ProfileImagePath);
        if (absolutePath is null || !System.IO.File.Exists(absolutePath))
        {
            return NotFound();
        }

        Response.Headers.CacheControl = "public, max-age=86400";
        Response.Headers.ETag = $"\"{user.ProfileImageUpdatedAt?.Ticks ?? 0}\"";
        return PhysicalFile(absolutePath, user.ProfileImageContentType ?? "application/octet-stream");
    }

    [HttpGet]
    [RequirePermission("UserManagement.View")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserResponse>>>> GetUsers()
    {
        var users = await LoadUsers()
            .OrderBy(user => user.FullName)
            .ToListAsync();

        return ApiResponse<IReadOnlyList<UserResponse>>.Ok(users.Select(ToResponse).ToList());
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("UserManagement.View")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> GetUser(Guid id)
    {
        var user = await LoadUsers().FirstOrDefaultAsync(item => item.Id == id);
        if (user is null)
        {
            return NotFound(ApiResponse<UserResponse>.Fail("User not found."));
        }

        return ApiResponse<UserResponse>.Ok(ToResponse(user));
    }

    [HttpPost]
    [RequirePermission("UserManagement.Create")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> CreateUser(CreateUserRequest request)
    {
        var username = request.Username.Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            return BadRequest(ApiResponse<UserResponse>.Fail("Username is required."));
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(ApiResponse<UserResponse>.Fail("Password is required."));
        }

        if (await db.Users.AnyAsync(user => user.Username == username))
        {
            return BadRequest(ApiResponse<UserResponse>.Fail("Username already exists."));
        }

        if (!string.IsNullOrWhiteSpace(request.EmployeeCode) &&
            await db.Users.AnyAsync(user => user.EmployeeCode == request.EmployeeCode))
        {
            return BadRequest(ApiResponse<UserResponse>.Fail("Employee code already exists."));
        }

        var roleIds = request.RoleIds.Distinct().ToList();
        var roles = await db.Roles
            .Where(role => roleIds.Contains(role.Id) && role.IsActive)
            .ToListAsync();

        if (roleIds.Count == 0 || roles.Count != roleIds.Count)
        {
            return BadRequest(ApiResponse<UserResponse>.Fail("One or more roles were not found."));
        }

        if (request.DepartmentId is not null &&
            !await db.Departments.AnyAsync(department => department.Id == request.DepartmentId))
        {
            return BadRequest(ApiResponse<UserResponse>.Fail("Department not found."));
        }

        if (request.LeaveApprovalRuleId is not null &&
            !await db.ApprovalChains.AnyAsync(rule => rule.Id == request.LeaveApprovalRuleId && rule.IsActive))
        {
            return BadRequest(ApiResponse<UserResponse>.Fail("Approval rule not found."));
        }

        var lineUserIdResult = await ValidateAndNormalizeLineUserIdAsync(request.LineUserId, null);
        if (lineUserIdResult.Error is not null)
        {
            return BadRequest(ApiResponse<UserResponse>.Fail(lineUserIdResult.Error));
        }

        var employmentType = NormalizeEmploymentType(request.EmploymentType);
        if (employmentType is not null && !EmploymentTypes.All.Contains(employmentType))
        {
            return BadRequest(ApiResponse<UserResponse>.Fail("ประเภทพนักงานไม่ถูกต้อง"));
        }
        var gender = GenderTypes.Normalize(request.Gender);

        var user = new User
        {
            EmployeeCode = request.EmployeeCode,
            FullName = request.Fullname,
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            DepartmentId = request.DepartmentId,
            LeaveApprovalRuleId = request.LeaveApprovalRuleId,
            Gender = gender,
            EmploymentType = employmentType,
            EmploymentStartDate = request.EmploymentStartDate,
            LineUserId = lineUserIdResult.LineUserId,
            IsActive = request.IsActive
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        await SyncLineUserBindingAsync(user, null);

        foreach (var role in roles)
        {
            db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        }

        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "User.Create", "User", user.Id.ToString(), $"Created user {user.Username}.", "Success", HttpContext);

        var created = await LoadUsers().SingleAsync(item => item.Id == user.Id);
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, ApiResponse<UserResponse>.Ok(ToResponse(created)));
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("UserManagement.Edit")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> UpdateUser(Guid id, UpdateUserRequest request)
    {
        var user = await db.Users
            .Include(item => item.UserRoles)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (user is null)
        {
            return NotFound(ApiResponse<UserResponse>.Fail("User not found."));
        }

        if (!string.IsNullOrWhiteSpace(request.EmployeeCode) &&
            await db.Users.AnyAsync(item => item.Id != id && item.EmployeeCode == request.EmployeeCode))
        {
            return BadRequest(ApiResponse<UserResponse>.Fail("Employee code already exists."));
        }

        var roleIds = request.RoleIds.Distinct().ToList();
        var roles = await db.Roles
            .Where(role => roleIds.Contains(role.Id) && role.IsActive)
            .ToListAsync();

        if (roleIds.Count == 0 || roles.Count != roleIds.Count)
        {
            return BadRequest(ApiResponse<UserResponse>.Fail("One or more roles were not found."));
        }

        if (request.DepartmentId is not null &&
            !await db.Departments.AnyAsync(department => department.Id == request.DepartmentId))
        {
            return BadRequest(ApiResponse<UserResponse>.Fail("Department not found."));
        }

        if (request.LeaveApprovalRuleId is not null &&
            !await db.ApprovalChains.AnyAsync(rule => rule.Id == request.LeaveApprovalRuleId && rule.IsActive))
        {
            return BadRequest(ApiResponse<UserResponse>.Fail("Approval rule not found."));
        }

        var lineUserIdResult = await ValidateAndNormalizeLineUserIdAsync(request.LineUserId, id);
        if (lineUserIdResult.Error is not null)
        {
            return BadRequest(ApiResponse<UserResponse>.Fail(lineUserIdResult.Error));
        }

        var employmentType = NormalizeEmploymentType(request.EmploymentType);
        if (employmentType is not null && !EmploymentTypes.All.Contains(employmentType))
        {
            return BadRequest(ApiResponse<UserResponse>.Fail("ประเภทพนักงานไม่ถูกต้อง"));
        }
        var gender = GenderTypes.Normalize(request.Gender);
        var previousLineUserId = user.LineUserId;

        user.EmployeeCode = request.EmployeeCode;
        user.FullName = request.Fullname;
        user.DepartmentId = request.DepartmentId;
        user.LeaveApprovalRuleId = request.LeaveApprovalRuleId;
        user.Gender = gender;
        user.EmploymentType = employmentType;
        user.EmploymentStartDate = request.EmploymentStartDate;
        user.LineUserId = lineUserIdResult.LineUserId;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        db.UserRoles.RemoveRange(user.UserRoles);
        foreach (var role in roles)
        {
            db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        }

        await SyncLineUserBindingAsync(user, previousLineUserId);
        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "User.Edit", "User", user.Id.ToString(), $"Updated user {user.Username}.", "Success", HttpContext);

        var updated = await LoadUsers().SingleAsync(item => item.Id == id);
        return ApiResponse<UserResponse>.Ok(ToResponse(updated));
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("UserManagement.Delete")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await db.Users.FirstOrDefaultAsync(item => item.Id == id);
        if (user is null)
        {
            return NotFound(ApiResponse<string>.Fail("User not found."));
        }

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await auditLogService.WriteAsync(GetCurrentUserId(), "User.Delete", "User", user.Id.ToString(), $"Deactivated user {user.Username}.", "Success", HttpContext);

        return NoContent();
    }

    private IQueryable<User> LoadUsers()
    {
        return db.Users
            .Include(user => user.Department)
            .Include(user => user.LeaveApprovalRule)
            .Include(user => user.UserRoles)
                .ThenInclude(userRole => userRole.Role);
    }

    private static UserResponse ToResponse(User user)
    {
        var roleIds = user.UserRoles
            .Select(userRole => userRole.RoleId)
            .ToList();

        var roles = user.UserRoles
            .Select(userRole => userRole.Role?.Name)
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role!)
            .ToList();

        return new UserResponse(
            user.Id,
            user.EmployeeCode,
            user.FullName,
            user.Username,
            user.Position,
            user.Email,
            user.PhoneNumber,
            user.LeaveContactAddress,
            GenderTypes.Normalize(user.Gender),
            user.EmploymentType,
            user.EmploymentStartDate,
            BuildProfileImageUrl(user),
            !string.IsNullOrWhiteSpace(user.ProfileImagePath),
            user.ProfileImageUpdatedAt,
            roleIds,
            roles,
            user.DepartmentId,
            user.Department?.Name,
            user.LeaveApprovalRuleId,
            user.LeaveApprovalRule?.Name,
            user.LineUserId,
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt
        );
    }

    private static string? NormalizeEmploymentType(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    }

    private async Task<(string? LineUserId, string? Error)> ValidateAndNormalizeLineUserIdAsync(string? value, Guid? currentUserId)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return (null, null);
        }

        var lineUserId = value.Trim();
        var duplicateUser = await db.Users
            .AsNoTracking()
            .Where(item => item.LineUserId == lineUserId && (currentUserId == null || item.Id != currentUserId))
            .Select(item => item.Username)
            .FirstOrDefaultAsync();
        if (!string.IsNullOrWhiteSpace(duplicateUser))
        {
            return (null, $"LINE User ID นี้ถูกใช้กับบัญชี {duplicateUser} แล้ว");
        }

        var duplicateBinding = await db.LineUserBindings
            .AsNoTracking()
            .Where(item =>
                item.LineUserId == lineUserId &&
                item.Status == "Bound" &&
                item.UserId != null &&
                (currentUserId == null || item.UserId != currentUserId))
            .Select(item => item.User != null ? item.User.Username : null)
            .FirstOrDefaultAsync();
        if (!string.IsNullOrWhiteSpace(duplicateBinding))
        {
            return (null, $"LINE User ID นี้เชื่อมต่อกับบัญชี {duplicateBinding} แล้ว");
        }

        return (lineUserId, null);
    }

    private async Task SyncLineUserBindingAsync(User user, string? previousLineUserId)
    {
        if (!string.IsNullOrWhiteSpace(previousLineUserId) && previousLineUserId != user.LineUserId)
        {
            var previousBinding = await db.LineUserBindings
                .FirstOrDefaultAsync(item => item.LineUserId == previousLineUserId && item.UserId == user.Id && item.Status == "Bound");
            if (previousBinding is not null)
            {
                previousBinding.Status = "Unbound";
                previousBinding.UnboundAt = DateTime.UtcNow;
                previousBinding.UpdatedAt = DateTime.UtcNow;
            }
        }

        if (string.IsNullOrWhiteSpace(user.LineUserId))
        {
            return;
        }

        var binding = await db.LineUserBindings.FirstOrDefaultAsync(item => item.LineUserId == user.LineUserId);
        if (binding is null)
        {
            db.LineUserBindings.Add(new LineUserBinding
            {
                LineUserId = user.LineUserId,
                UserId = user.Id,
                Status = "Bound",
                BoundAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            return;
        }

        binding.UserId = user.Id;
        binding.Status = "Bound";
        binding.BoundAt ??= DateTime.UtcNow;
        binding.UnboundAt = null;
        binding.UpdatedAt = DateTime.UtcNow;
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

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
