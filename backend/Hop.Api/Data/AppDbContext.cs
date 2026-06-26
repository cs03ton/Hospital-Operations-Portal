using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ApprovalLog> ApprovalLogs => Set<ApprovalLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<LeaveAttachment> LeaveAttachments => Set<LeaveAttachment>();
    public DbSet<LeaveApproval> LeaveApprovals => Set<LeaveApproval>();
    public DbSet<ApprovalChain> ApprovalChains => Set<ApprovalChain>();
    public DbSet<ApprovalChainStep> ApprovalChainSteps => Set<ApprovalChainStep>();
    public DbSet<ApprovalDelegation> ApprovalDelegations => Set<ApprovalDelegation>();
    public DbSet<ApprovalEscalationRule> ApprovalEscalationRules => Set<ApprovalEscalationRule>();
    public DbSet<ApprovalOverrideLog> ApprovalOverrideLogs => Set<ApprovalOverrideLog>();
    public DbSet<LeaveBalanceAdjustment> LeaveBalanceAdjustments => Set<LeaveBalanceAdjustment>();
    public DbSet<LeaveHoliday> LeaveHolidays => Set<LeaveHoliday>();
    public DbSet<LineDeliveryLog> LineDeliveryLogs => Set<LineDeliveryLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("departments");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.Name).HasColumnName("name");
            entity.Property(item => item.Description).HasColumnName("description");
            entity.Property(item => item.IsActive).HasColumnName("is_active");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(item => item.Name).IsUnique();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.EmployeeCode).HasColumnName("employee_code");
            entity.Property(item => item.FullName).HasColumnName("fullname");
            entity.Property(item => item.Username).HasColumnName("username");
            entity.Property(item => item.PasswordHash).HasColumnName("password_hash");
            entity.Property(item => item.Position).HasColumnName("position");
            entity.Property(item => item.Email).HasColumnName("email");
            entity.Property(item => item.PhoneNumber).HasColumnName("phone_number");
            entity.Property(item => item.LeaveContactAddress).HasColumnName("leave_contact_address");
            entity.Property(item => item.ProfileImageUrl).HasColumnName("profile_image_url");
            entity.Property(item => item.DepartmentId).HasColumnName("department_id");
            entity.Property(item => item.LeaveApprovalRuleId).HasColumnName("leave_approval_rule_id");
            entity.Property(item => item.LineUserId).HasColumnName("line_user_id");
            entity.Property(item => item.IsActive).HasColumnName("is_active");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(item => item.Username).IsUnique();
            entity.HasIndex(item => item.EmployeeCode).IsUnique();
            entity.HasIndex(item => item.LeaveApprovalRuleId);
            entity.HasOne(item => item.LeaveApprovalRule)
                .WithMany()
                .HasForeignKey(item => item.LeaveApprovalRuleId);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.Name).HasColumnName("name");
            entity.Property(item => item.Description).HasColumnName("description");
            entity.Property(item => item.IsSystemRole).HasColumnName("is_system_role");
            entity.Property(item => item.IsActive).HasColumnName("is_active");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(item => item.Name).IsUnique();
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("permissions");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.Code).HasColumnName("code");
            entity.Property(item => item.Name).HasColumnName("name");
            entity.Property(item => item.Group).HasColumnName("group_name");
            entity.Property(item => item.Action).HasColumnName("action");
            entity.Property(item => item.IsActive).HasColumnName("is_active");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(item => item.Code).IsUnique();
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("user_roles");
            entity.Property(item => item.UserId).HasColumnName("user_id");
            entity.Property(item => item.RoleId).HasColumnName("role_id");
            entity.HasKey(item => new { item.UserId, item.RoleId });
            entity.HasOne(item => item.User)
                .WithMany(user => user.UserRoles)
                .HasForeignKey(item => item.UserId);
            entity.HasOne(item => item.Role)
                .WithMany(role => role.UserRoles)
                .HasForeignKey(item => item.RoleId);
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("role_permissions");
            entity.Property(item => item.RoleId).HasColumnName("role_id");
            entity.Property(item => item.PermissionId).HasColumnName("permission_id");
            entity.HasKey(item => new { item.RoleId, item.PermissionId });
            entity.HasOne(item => item.Role)
                .WithMany(role => role.RolePermissions)
                .HasForeignKey(item => item.RoleId);
            entity.HasOne(item => item.Permission)
                .WithMany(permission => permission.RolePermissions)
                .HasForeignKey(item => item.PermissionId);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.UserId).HasColumnName("user_id");
            entity.Property(item => item.Token).HasColumnName("token");
            entity.Property(item => item.ExpiresAt).HasColumnName("expires_at");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.Property(item => item.RevokedAt).HasColumnName("revoked_at");
            entity.Property(item => item.RevokedReason).HasColumnName("revoked_reason");
            entity.Property(item => item.ReplacedByToken).HasColumnName("replaced_by_token");
            entity.Property(item => item.CreatedByIp).HasColumnName("created_by_ip");
            entity.Property(item => item.UserAgent).HasColumnName("user_agent");
            entity.Property(item => item.LastUsedAt).HasColumnName("last_used_at");
            entity.HasIndex(item => item.Token).IsUnique();
            entity.HasIndex(item => new { item.UserId, item.RevokedAt });
            entity.HasOne(item => item.User)
                .WithMany(user => user.RefreshTokens)
                .HasForeignKey(item => item.UserId);
            entity.Ignore(item => item.IsActive);
        });

        modelBuilder.Entity<ApprovalLog>(entity =>
        {
            entity.ToTable("approval_logs");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.RequestType).HasColumnName("request_type");
            entity.Property(item => item.RequestId).HasColumnName("request_id");
            entity.Property(item => item.ApproverId).HasColumnName("approver_id");
            entity.Property(item => item.Action).HasColumnName("action");
            entity.Property(item => item.Remark).HasColumnName("remark");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.UserId).HasColumnName("user_id");
            entity.Property(item => item.Channel).HasColumnName("channel");
            entity.Property(item => item.Title).HasColumnName("title");
            entity.Property(item => item.Message).HasColumnName("message");
            entity.Property(item => item.IsRead).HasColumnName("is_read");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.UserId).HasColumnName("user_id");
            entity.Property(item => item.Action).HasColumnName("action");
            entity.Property(item => item.EntityName).HasColumnName("entity_name");
            entity.Property(item => item.EntityId).HasColumnName("entity_id");
            entity.Property(item => item.Detail).HasColumnName("detail");
            entity.Property(item => item.IpAddress).HasColumnName("ip_address");
            entity.Property(item => item.Result).HasColumnName("result");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(item => item.CreatedAt);
            entity.HasOne(item => item.User)
                .WithMany()
                .HasForeignKey(item => item.UserId);
        });

        modelBuilder.Entity<LeaveType>(entity =>
        {
            entity.ToTable("leave_types");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.Code).HasColumnName("code");
            entity.Property(item => item.Name).HasColumnName("name");
            entity.Property(item => item.Description).HasColumnName("description");
            entity.Property(item => item.DefaultDaysPerYear).HasColumnName("default_days_per_year");
            entity.Property(item => item.RequiresBalance).HasColumnName("requires_balance").HasDefaultValue(true);
            entity.Property(item => item.AllowCarryOver).HasColumnName("allow_carry_over").HasDefaultValue(false);
            entity.Property(item => item.CarryOverMaxDays).HasColumnName("carry_over_max_days").HasDefaultValue(30);
            entity.Property(item => item.UseFiscalYear).HasColumnName("use_fiscal_year").HasDefaultValue(true);
            entity.Property(item => item.RequiresAttachment).HasColumnName("requires_attachment");
            entity.Property(item => item.IsPaid).HasColumnName("is_paid");
            entity.Property(item => item.IsActive).HasColumnName("is_active");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(item => item.Code).IsUnique();
        });

        modelBuilder.Entity<LeaveBalance>(entity =>
        {
            entity.ToTable("leave_balances");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.UserId).HasColumnName("user_id");
            entity.Property(item => item.LeaveTypeId).HasColumnName("leave_type_id");
            entity.Property(item => item.Year).HasColumnName("year");
            entity.Property(item => item.EntitledDays).HasColumnName("entitled_days");
            entity.Property(item => item.CarriedOverDays).HasColumnName("carried_over_days");
            entity.Property(item => item.AdjustedDays).HasColumnName("adjusted_days").HasDefaultValue(0);
            entity.Property(item => item.UsedDays).HasColumnName("used_days");
            entity.Property(item => item.PendingDays).HasColumnName("pending_days");
            entity.Property(item => item.Notes).HasColumnName("notes").HasMaxLength(1000);
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(item => new { item.UserId, item.LeaveTypeId, item.Year }).IsUnique();
            entity.HasOne(item => item.User)
                .WithMany()
                .HasForeignKey(item => item.UserId);
            entity.HasOne(item => item.LeaveType)
                .WithMany(leaveType => leaveType.LeaveBalances)
                .HasForeignKey(item => item.LeaveTypeId);
        });

        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.ToTable("leave_requests");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.RequestNumber).HasColumnName("request_number").HasMaxLength(20);
            entity.Property(item => item.UserId).HasColumnName("user_id");
            entity.Property(item => item.LeaveTypeId).HasColumnName("leave_type_id");
            entity.Property(item => item.StartDate).HasColumnName("start_date");
            entity.Property(item => item.EndDate).HasColumnName("end_date");
            entity.Property(item => item.DurationType).HasColumnName("duration_type").HasMaxLength(20).HasDefaultValue("FULL_DAY");
            entity.Property(item => item.TotalDays).HasColumnName("total_days");
            entity.Property(item => item.Reason).HasColumnName("reason");
            entity.Property(item => item.Status).HasColumnName("status");
            entity.Property(item => item.CurrentApproverId).HasColumnName("current_approver_id");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.Property(item => item.SubmittedAt).HasColumnName("submitted_at");
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(item => item.RequestNumber).IsUnique();
            entity.HasIndex(item => new { item.UserId, item.Status });
            entity.HasOne(item => item.User)
                .WithMany()
                .HasForeignKey(item => item.UserId);
            entity.HasOne(item => item.CurrentApprover)
                .WithMany()
                .HasForeignKey(item => item.CurrentApproverId);
            entity.HasOne(item => item.LeaveType)
                .WithMany(leaveType => leaveType.LeaveRequests)
                .HasForeignKey(item => item.LeaveTypeId);
        });

        modelBuilder.Entity<LeaveAttachment>(entity =>
        {
            entity.ToTable("leave_attachments");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.LeaveRequestId).HasColumnName("leave_request_id");
            entity.Property(item => item.FileName).HasColumnName("file_name");
            entity.Property(item => item.FilePath).HasColumnName("file_path");
            entity.Property(item => item.ContentType).HasColumnName("content_type");
            entity.Property(item => item.FileSizeBytes).HasColumnName("file_size_bytes");
            entity.Property(item => item.UploadedByUserId).HasColumnName("uploaded_by_user_id");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.HasOne(item => item.LeaveRequest)
                .WithMany(leaveRequest => leaveRequest.Attachments)
                .HasForeignKey(item => item.LeaveRequestId);
            entity.HasOne(item => item.UploadedByUser)
                .WithMany()
                .HasForeignKey(item => item.UploadedByUserId);
        });

        modelBuilder.Entity<LeaveApproval>(entity =>
        {
            entity.ToTable("leave_approvals");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.LeaveRequestId).HasColumnName("leave_request_id");
            entity.Property(item => item.ApproverId).HasColumnName("approver_id");
            entity.Property(item => item.ApprovalChainId).HasColumnName("approval_chain_id");
            entity.Property(item => item.ApprovalChainStepId).HasColumnName("approval_chain_step_id");
            entity.Property(item => item.StepOrder).HasColumnName("step_order");
            entity.Property(item => item.Status).HasColumnName("status");
            entity.Property(item => item.StepName).HasColumnName("step_name");
            entity.Property(item => item.RequiredPermissionCode).HasColumnName("required_permission_code");
            entity.Property(item => item.Remark).HasColumnName("remark");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.Property(item => item.ActionAt).HasColumnName("action_at");
            entity.HasIndex(item => new { item.LeaveRequestId, item.StepOrder });
            entity.HasOne(item => item.LeaveRequest)
                .WithMany(leaveRequest => leaveRequest.Approvals)
                .HasForeignKey(item => item.LeaveRequestId);
            entity.HasOne(item => item.Approver)
                .WithMany()
                .HasForeignKey(item => item.ApproverId);
            entity.HasOne(item => item.ApprovalChain)
                .WithMany()
                .HasForeignKey(item => item.ApprovalChainId);
            entity.HasOne(item => item.ApprovalChainStep)
                .WithMany()
                .HasForeignKey(item => item.ApprovalChainStepId);
        });

        modelBuilder.Entity<ApprovalChain>(entity =>
        {
            entity.ToTable("approval_chains");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.Name).HasColumnName("name");
            entity.Property(item => item.Description).HasColumnName("description");
            entity.Property(item => item.DepartmentId).HasColumnName("department_id");
            entity.Property(item => item.LeaveTypeId).HasColumnName("leave_type_id");
            entity.Property(item => item.MinimumDays).HasColumnName("minimum_days");
            entity.Property(item => item.IsActive).HasColumnName("is_active");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(item => item.Name).IsUnique();
            entity.HasOne(item => item.Department)
                .WithMany()
                .HasForeignKey(item => item.DepartmentId);
            entity.HasOne(item => item.LeaveType)
                .WithMany()
                .HasForeignKey(item => item.LeaveTypeId);
        });

        modelBuilder.Entity<ApprovalChainStep>(entity =>
        {
            entity.ToTable("approval_chain_steps");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.ApprovalChainId).HasColumnName("approval_chain_id");
            entity.Property(item => item.StepOrder).HasColumnName("step_order");
            entity.Property(item => item.Name).HasColumnName("name");
            entity.Property(item => item.ApproverRoleId).HasColumnName("approver_role_id");
            entity.Property(item => item.ApproverUserId).HasColumnName("approver_user_id");
            entity.Property(item => item.RequiredPermissionCode).HasColumnName("required_permission_code");
            entity.Property(item => item.IsActive).HasColumnName("is_active");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(item => new { item.ApprovalChainId, item.StepOrder }).IsUnique();
            entity.HasOne(item => item.ApprovalChain)
                .WithMany(chain => chain.Steps)
                .HasForeignKey(item => item.ApprovalChainId);
            entity.HasOne(item => item.ApproverRole)
                .WithMany()
                .HasForeignKey(item => item.ApproverRoleId);
            entity.HasOne(item => item.ApproverUser)
                .WithMany()
                .HasForeignKey(item => item.ApproverUserId);
        });

        modelBuilder.Entity<ApprovalDelegation>(entity =>
        {
            entity.ToTable("approval_delegations");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.ApproverUserId).HasColumnName("approver_user_id");
            entity.Property(item => item.DelegateUserId).HasColumnName("delegate_user_id");
            entity.Property(item => item.StartDate).HasColumnName("start_date");
            entity.Property(item => item.EndDate).HasColumnName("end_date");
            entity.Property(item => item.Reason).HasColumnName("reason");
            entity.Property(item => item.IsActive).HasColumnName("is_active");
            entity.Property(item => item.CreatedByUserId).HasColumnName("created_by");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at");
            entity.Property(item => item.CancelledAt).HasColumnName("cancelled_at");
            entity.HasIndex(item => new { item.ApproverUserId, item.StartDate, item.EndDate });
            entity.HasOne(item => item.ApproverUser)
                .WithMany()
                .HasForeignKey(item => item.ApproverUserId);
            entity.HasOne(item => item.DelegateUser)
                .WithMany()
                .HasForeignKey(item => item.DelegateUserId);
            entity.HasOne(item => item.CreatedByUser)
                .WithMany()
                .HasForeignKey(item => item.CreatedByUserId);
        });

        modelBuilder.Entity<ApprovalOverrideLog>(entity =>
        {
            entity.ToTable("approval_override_logs");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.LeaveRequestId).HasColumnName("leave_request_id");
            entity.Property(item => item.OriginalApproverId).HasColumnName("original_approver_id");
            entity.Property(item => item.OverrideByUserId).HasColumnName("override_by_user_id");
            entity.Property(item => item.Action).HasColumnName("action");
            entity.Property(item => item.Reason).HasColumnName("reason");
            entity.Property(item => item.IpAddress).HasColumnName("ip_address");
            entity.Property(item => item.UserAgent).HasColumnName("user_agent");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(item => item.LeaveRequestId);
            entity.HasIndex(item => item.OverrideByUserId);
            entity.HasOne(item => item.LeaveRequest)
                .WithMany()
                .HasForeignKey(item => item.LeaveRequestId);
            entity.HasOne(item => item.OriginalApprover)
                .WithMany()
                .HasForeignKey(item => item.OriginalApproverId);
            entity.HasOne(item => item.OverrideByUser)
                .WithMany()
                .HasForeignKey(item => item.OverrideByUserId);
        });

        modelBuilder.Entity<ApprovalEscalationRule>(entity =>
        {
            entity.ToTable("approval_escalation_rules");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.Name).HasColumnName("name");
            entity.Property(item => item.DepartmentId).HasColumnName("department_id");
            entity.Property(item => item.LeaveTypeId).HasColumnName("leave_type_id");
            entity.Property(item => item.EscalateAfterHours).HasColumnName("escalate_after_hours");
            entity.Property(item => item.EscalateToUserId).HasColumnName("escalate_to_user_id");
            entity.Property(item => item.EscalateToRoleId).HasColumnName("escalate_to_role_id");
            entity.Property(item => item.IsActive).HasColumnName("is_active");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(item => item.Name).IsUnique();
            entity.HasIndex(item => new { item.DepartmentId, item.LeaveTypeId, item.IsActive });
            entity.HasOne(item => item.Department)
                .WithMany()
                .HasForeignKey(item => item.DepartmentId);
            entity.HasOne(item => item.LeaveType)
                .WithMany()
                .HasForeignKey(item => item.LeaveTypeId);
            entity.HasOne(item => item.EscalateToUser)
                .WithMany()
                .HasForeignKey(item => item.EscalateToUserId);
            entity.HasOne(item => item.EscalateToRole)
                .WithMany()
                .HasForeignKey(item => item.EscalateToRoleId);
        });

        modelBuilder.Entity<LeaveBalanceAdjustment>(entity =>
        {
            entity.ToTable("leave_balance_adjustments");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.UserId).HasColumnName("user_id");
            entity.Property(item => item.LeaveTypeId).HasColumnName("leave_type_id");
            entity.Property(item => item.Year).HasColumnName("year");
            entity.Property(item => item.AdjustmentDays).HasColumnName("adjustment_days");
            entity.Property(item => item.Reason).HasColumnName("reason");
            entity.Property(item => item.AdjustedByUserId).HasColumnName("adjusted_by_user_id");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(item => new { item.UserId, item.LeaveTypeId, item.Year });
            entity.HasOne(item => item.User)
                .WithMany()
                .HasForeignKey(item => item.UserId);
            entity.HasOne(item => item.LeaveType)
                .WithMany()
                .HasForeignKey(item => item.LeaveTypeId);
            entity.HasOne(item => item.AdjustedByUser)
                .WithMany()
                .HasForeignKey(item => item.AdjustedByUserId);
        });

        modelBuilder.Entity<LeaveHoliday>(entity =>
        {
            entity.ToTable("leave_holidays");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.HolidayDate).HasColumnName("holiday_date");
            entity.Property(item => item.Name).HasColumnName("name");
            entity.Property(item => item.IsActive).HasColumnName("is_active");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(item => item.HolidayDate).IsUnique();
            entity.HasIndex(item => item.Name);
        });

        modelBuilder.Entity<LineDeliveryLog>(entity =>
        {
            entity.ToTable("line_delivery_logs");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.LeaveRequestId).HasColumnName("leave_request_id");
            entity.Property(item => item.RecipientUserId).HasColumnName("recipient_user_id");
            entity.Property(item => item.EventName).HasColumnName("event_name");
            entity.Property(item => item.Status).HasColumnName("status");
            entity.Property(item => item.Payload).HasColumnName("payload");
            entity.Property(item => item.ResponseDetail).HasColumnName("response_detail");
            entity.Property(item => item.AttemptCount).HasColumnName("attempt_count");
            entity.Property(item => item.NextRetryAt).HasColumnName("next_retry_at");
            entity.Property(item => item.SentAt).HasColumnName("sent_at");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(item => new { item.Status, item.NextRetryAt });
            entity.HasOne(item => item.LeaveRequest)
                .WithMany()
                .HasForeignKey(item => item.LeaveRequestId);
            entity.HasOne(item => item.RecipientUser)
                .WithMany()
                .HasForeignKey(item => item.RecipientUserId);
        });
    }
}
