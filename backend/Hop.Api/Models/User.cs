namespace Hop.Api.Models;

public class User
{
    public Guid Id { get; set; }
    public string? EmployeeCode { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? LeaveContactAddress { get; set; }
    public string Gender { get; set; } = GenderTypes.Unknown;
    public string? EmploymentType { get; set; }
    public DateOnly? EmploymentStartDate { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? ProfileImagePath { get; set; }
    public string? ProfileImageFileName { get; set; }
    public string? ProfileImageContentType { get; set; }
    public DateTime? ProfileImageUpdatedAt { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? LeaveApprovalRuleId { get; set; }
    public string? LineUserId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Department? Department { get; set; }
    public ApprovalChain? LeaveApprovalRule { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
