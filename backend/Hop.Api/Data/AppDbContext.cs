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
    public DbSet<LeavePolicyRule> LeavePolicyRules => Set<LeavePolicyRule>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<LeaveBalanceRolloverRun> LeaveBalanceRolloverRuns => Set<LeaveBalanceRolloverRun>();
    public DbSet<LeaveBalanceSnapshot> LeaveBalanceSnapshots => Set<LeaveBalanceSnapshot>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<LeaveAttachment> LeaveAttachments => Set<LeaveAttachment>();
    public DbSet<LeaveApproval> LeaveApprovals => Set<LeaveApproval>();
    public DbSet<LeaveCancellationRequest> LeaveCancellationRequests => Set<LeaveCancellationRequest>();
    public DbSet<LeaveCancellationApproval> LeaveCancellationApprovals => Set<LeaveCancellationApproval>();
    public DbSet<LeaveBalanceTransaction> LeaveBalanceTransactions => Set<LeaveBalanceTransaction>();
    public DbSet<ApprovalChain> ApprovalChains => Set<ApprovalChain>();
    public DbSet<ApprovalChainStep> ApprovalChainSteps => Set<ApprovalChainStep>();
    public DbSet<ApprovalDelegation> ApprovalDelegations => Set<ApprovalDelegation>();
    public DbSet<ApprovalEscalationRule> ApprovalEscalationRules => Set<ApprovalEscalationRule>();
    public DbSet<ApprovalOverrideLog> ApprovalOverrideLogs => Set<ApprovalOverrideLog>();
    public DbSet<LeaveBalanceAdjustment> LeaveBalanceAdjustments => Set<LeaveBalanceAdjustment>();
    public DbSet<LeaveHoliday> LeaveHolidays => Set<LeaveHoliday>();
    public DbSet<LineDeliveryLog> LineDeliveryLogs => Set<LineDeliveryLog>();
    public DbSet<LineUserBinding> LineUserBindings => Set<LineUserBinding>();
    public DbSet<LinePairingCode> LinePairingCodes => Set<LinePairingCode>();
    public DbSet<LineConnectToken> LineConnectTokens => Set<LineConnectToken>();
    public DbSet<BackupRun> BackupRuns => Set<BackupRun>();
    public DbSet<RestoreRun> RestoreRuns => Set<RestoreRun>();
    public DbSet<DiagnosticRun> DiagnosticRuns => Set<DiagnosticRun>();
    public DbSet<SupportBundle> SupportBundles => Set<SupportBundle>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        SanitizePostgresTextValues();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        SanitizePostgresTextValues();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

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
            entity.Property(item => item.Gender).HasColumnName("gender").HasMaxLength(20).HasDefaultValue(GenderTypes.Unknown);
            entity.Property(item => item.EmploymentType).HasColumnName("employment_type").HasMaxLength(80);
            entity.Property(item => item.EmploymentStartDate).HasColumnName("employment_start_date");
            entity.Property(item => item.ProfileImageUrl).HasColumnName("profile_image_url");
            entity.Property(item => item.ProfileImagePath).HasColumnName("profile_image_path");
            entity.Property(item => item.ProfileImageFileName).HasColumnName("profile_image_file_name");
            entity.Property(item => item.ProfileImageContentType).HasColumnName("profile_image_content_type");
            entity.Property(item => item.ProfileImageUpdatedAt).HasColumnName("profile_image_updated_at");
            entity.Property(item => item.DepartmentId).HasColumnName("department_id");
            entity.Property(item => item.LeaveApprovalRuleId).HasColumnName("leave_approval_rule_id");
            entity.Property(item => item.LineUserId).HasColumnName("line_user_id");
            entity.Property(item => item.IsActive).HasColumnName("is_active");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at");
            entity.Property(item => item.PasswordChangedAt).HasColumnName("password_changed_at");
            entity.HasIndex(item => item.Username).IsUnique();
            entity.HasIndex(item => item.EmployeeCode).IsUnique();
            entity.HasIndex(item => item.LeaveApprovalRuleId);
            entity.HasIndex(item => item.EmploymentType);
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
            entity.Property(item => item.Category).HasColumnName("category").HasMaxLength(80).HasDefaultValue("Leave");
            entity.Property(item => item.NotificationType).HasColumnName("notification_type").HasMaxLength(40).HasDefaultValue("Information");
            entity.Property(item => item.Priority).HasColumnName("priority").HasMaxLength(40).HasDefaultValue("Information");
            entity.Property(item => item.TargetRole).HasColumnName("target_role").HasMaxLength(80);
            entity.Property(item => item.Title).HasColumnName("title");
            entity.Property(item => item.Message).HasColumnName("message");
            entity.Property(item => item.ActionUrl).HasColumnName("action_url");
            entity.Property(item => item.ReferenceEntity).HasColumnName("reference_entity").HasMaxLength(120);
            entity.Property(item => item.ReferenceId).HasColumnName("reference_id").HasMaxLength(120);
            entity.Property(item => item.ExpiresAt).HasColumnName("expires_at");
            entity.Property(item => item.ArchivedAt).HasColumnName("archived_at");
            entity.Property(item => item.IsRead).HasColumnName("is_read");
            entity.Property(item => item.ReadAt).HasColumnName("read_at");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(item => new { item.UserId, item.IsRead, item.NotificationType });
            entity.HasIndex(item => new { item.TargetRole, item.Category });
            entity.HasIndex(item => item.ExpiresAt);
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

        modelBuilder.Entity<LeavePolicyRule>(entity =>
        {
            entity.ToTable("leave_policy_rules");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.EmploymentType).HasColumnName("employment_type").HasMaxLength(80);
            entity.Property(item => item.LeaveTypeId).HasColumnName("leave_type_id");
            entity.Property(item => item.FiscalYear).HasColumnName("fiscal_year");
            entity.Property(item => item.EntitlementDays).HasColumnName("entitlement_days");
            entity.Property(item => item.MaxPaidDays).HasColumnName("max_paid_days");
            entity.Property(item => item.AllowCarryOver).HasColumnName("allow_carry_over");
            entity.Property(item => item.CarryOverMaxDays).HasColumnName("carry_over_max_days");
            entity.Property(item => item.MaxAccumulatedDays).HasColumnName("max_accumulated_days");
            entity.Property(item => item.MinServiceMonths).HasColumnName("min_service_months");
            entity.Property(item => item.MinServiceYears).HasColumnName("min_service_years");
            entity.Property(item => item.ProrateIfServiceLessThanYear).HasColumnName("prorate_if_service_less_than_year");
            entity.Property(item => item.FirstYearEntitlementDays).HasColumnName("first_year_entitlement_days");
            entity.Property(item => item.FirstYearPaidDays).HasColumnName("first_year_paid_days");
            entity.Property(item => item.IsPaid).HasColumnName("is_paid");
            entity.Property(item => item.MaxExtendedDays).HasColumnName("max_extended_days");
            entity.Property(item => item.SocialSecurityMaxDays).HasColumnName("social_security_max_days");
            entity.Property(item => item.Notes).HasColumnName("notes").HasMaxLength(1000);
            entity.Property(item => item.IsActive).HasColumnName("is_active");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(item => new { item.EmploymentType, item.LeaveTypeId, item.FiscalYear, item.IsActive });
            entity.HasOne(item => item.LeaveType)
                .WithMany()
                .HasForeignKey(item => item.LeaveTypeId);
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
            entity.Property(item => item.ReturnedForRevisionAt).HasColumnName("returned_for_revision_at");
            entity.Property(item => item.ReturnedForRevisionByUserId).HasColumnName("returned_for_revision_by_user_id");
            entity.Property(item => item.RevisionReason).HasColumnName("revision_reason").HasMaxLength(1000);
            entity.Property(item => item.RevisionCount).HasColumnName("revision_count").HasDefaultValue(0);
            entity.Property(item => item.LastResubmittedAt).HasColumnName("last_resubmitted_at");
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(item => item.RequestNumber).IsUnique();
            entity.HasIndex(item => new { item.UserId, item.Status });
            entity.HasOne(item => item.User)
                .WithMany()
                .HasForeignKey(item => item.UserId);
            entity.HasOne(item => item.CurrentApprover)
                .WithMany()
                .HasForeignKey(item => item.CurrentApproverId);
            entity.HasOne(item => item.ReturnedForRevisionByUser)
                .WithMany()
                .HasForeignKey(item => item.ReturnedForRevisionByUserId);
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
            entity.Property(item => item.ReturnedAt).HasColumnName("returned_at");
            entity.Property(item => item.ReturnReason).HasColumnName("return_reason").HasMaxLength(1000);
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

        modelBuilder.Entity<LeaveCancellationRequest>(entity =>
        {
            entity.ToTable("leave_cancellation_requests");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.CancellationRequestNumber).HasColumnName("cancellation_request_number").HasMaxLength(24);
            entity.Property(item => item.OriginalLeaveRequestId).HasColumnName("original_leave_request_id");
            entity.Property(item => item.RequesterUserId).HasColumnName("requester_user_id");
            entity.Property(item => item.LeaveTypeId).HasColumnName("leave_type_id");
            entity.Property(item => item.OriginalLeaveDays).HasColumnName("original_leave_days");
            entity.Property(item => item.Reason).HasColumnName("reason").HasMaxLength(1000);
            entity.Property(item => item.Status).HasColumnName("status").HasMaxLength(40);
            entity.Property(item => item.ApprovalChainId).HasColumnName("approval_chain_id");
            entity.Property(item => item.CurrentApproverId).HasColumnName("current_approver_id");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at");
            entity.Property(item => item.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(item => item.SubmittedAt).HasColumnName("submitted_at");
            entity.Property(item => item.ApprovedAt).HasColumnName("approved_at");
            entity.Property(item => item.RejectedAt).HasColumnName("rejected_at");
            entity.Property(item => item.CancelledAt).HasColumnName("cancelled_at");
            entity.Property(item => item.ReturnedForRevisionAt).HasColumnName("returned_for_revision_at");
            entity.Property(item => item.ReturnedForRevisionByUserId).HasColumnName("returned_for_revision_by_user_id");
            entity.Property(item => item.RevisionReason).HasColumnName("revision_reason").HasMaxLength(1000);
            entity.Property(item => item.RevisionCount).HasColumnName("revision_count").HasDefaultValue(0);
            entity.Property(item => item.LastResubmittedAt).HasColumnName("last_resubmitted_at");
            entity.Property(item => item.BalanceRestoredAt).HasColumnName("balance_restored_at");
            entity.HasIndex(item => item.CancellationRequestNumber).IsUnique();
            entity.HasIndex(item => item.OriginalLeaveRequestId);
            entity.HasIndex(item => new { item.RequesterUserId, item.Status });
            entity.HasIndex(item => item.Status);
            entity.HasIndex(item => item.OriginalLeaveRequestId)
                .HasFilter("\"status\" IN ('Draft', 'Pending', 'ReturnedForRevision')")
                .IsUnique();
            entity.HasOne(item => item.OriginalLeaveRequest)
                .WithMany(request => request.CancellationRequests)
                .HasForeignKey(item => item.OriginalLeaveRequestId);
            entity.HasOne(item => item.RequesterUser)
                .WithMany()
                .HasForeignKey(item => item.RequesterUserId);
            entity.HasOne(item => item.LeaveType)
                .WithMany()
                .HasForeignKey(item => item.LeaveTypeId);
            entity.HasOne(item => item.ApprovalChain)
                .WithMany()
                .HasForeignKey(item => item.ApprovalChainId);
            entity.HasOne(item => item.CurrentApprover)
                .WithMany()
                .HasForeignKey(item => item.CurrentApproverId);
            entity.HasOne(item => item.CreatedByUser)
                .WithMany()
                .HasForeignKey(item => item.CreatedByUserId);
            entity.HasOne(item => item.ReturnedForRevisionByUser)
                .WithMany()
                .HasForeignKey(item => item.ReturnedForRevisionByUserId);
        });

        modelBuilder.Entity<LeaveCancellationApproval>(entity =>
        {
            entity.ToTable("leave_cancellation_approvals");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.LeaveCancellationRequestId).HasColumnName("leave_cancellation_request_id");
            entity.Property(item => item.ApproverId).HasColumnName("approver_id");
            entity.Property(item => item.ApprovalChainId).HasColumnName("approval_chain_id");
            entity.Property(item => item.ApprovalChainStepId).HasColumnName("approval_chain_step_id");
            entity.Property(item => item.StepOrder).HasColumnName("step_order");
            entity.Property(item => item.Status).HasColumnName("status").HasMaxLength(40);
            entity.Property(item => item.StepName).HasColumnName("step_name").HasMaxLength(200);
            entity.Property(item => item.RequiredPermissionCode).HasColumnName("required_permission_code").HasMaxLength(120);
            entity.Property(item => item.Remark).HasColumnName("remark").HasMaxLength(1000);
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.Property(item => item.ActionAt).HasColumnName("action_at");
            entity.Property(item => item.ReturnedAt).HasColumnName("returned_at");
            entity.Property(item => item.ReturnReason).HasColumnName("return_reason").HasMaxLength(1000);
            entity.HasIndex(item => new { item.LeaveCancellationRequestId, item.StepOrder }).IsUnique();
            entity.HasOne(item => item.LeaveCancellationRequest)
                .WithMany(request => request.Approvals)
                .HasForeignKey(item => item.LeaveCancellationRequestId);
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

        modelBuilder.Entity<LeaveBalanceTransaction>(entity =>
        {
            entity.ToTable("leave_balance_transactions");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.UserId).HasColumnName("user_id");
            entity.Property(item => item.LeaveTypeId).HasColumnName("leave_type_id");
            entity.Property(item => item.FiscalYear).HasColumnName("fiscal_year");
            entity.Property(item => item.TransactionType).HasColumnName("transaction_type").HasMaxLength(80);
            entity.Property(item => item.AmountDays).HasColumnName("amount_days");
            entity.Property(item => item.ReferenceType).HasColumnName("reference_type").HasMaxLength(120);
            entity.Property(item => item.ReferenceId).HasColumnName("reference_id");
            entity.Property(item => item.Reason).HasColumnName("reason").HasMaxLength(1000);
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.Property(item => item.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.HasIndex(item => new { item.ReferenceType, item.ReferenceId, item.TransactionType }).IsUnique();
            entity.HasIndex(item => new { item.UserId, item.LeaveTypeId, item.FiscalYear });
            entity.HasOne(item => item.User)
                .WithMany()
                .HasForeignKey(item => item.UserId);
            entity.HasOne(item => item.LeaveType)
                .WithMany()
                .HasForeignKey(item => item.LeaveTypeId);
            entity.HasOne(item => item.CreatedByUser)
                .WithMany()
                .HasForeignKey(item => item.CreatedByUserId);
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

        modelBuilder.Entity<LeaveBalanceRolloverRun>(entity =>
        {
            entity.ToTable("leave_balance_rollover_runs");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.FromFiscalYear).HasColumnName("from_fiscal_year");
            entity.Property(item => item.ToFiscalYear).HasColumnName("to_fiscal_year");
            entity.Property(item => item.Status).HasColumnName("status").HasMaxLength(40);
            entity.Property(item => item.FiltersJson).HasColumnName("filters_json");
            entity.Property(item => item.Total).HasColumnName("total");
            entity.Property(item => item.CreatedCount).HasColumnName("created_count");
            entity.Property(item => item.UpdatedCount).HasColumnName("updated_count");
            entity.Property(item => item.SkippedCount).HasColumnName("skipped_count");
            entity.Property(item => item.BlockedCount).HasColumnName("blocked_count");
            entity.Property(item => item.Reason).HasColumnName("reason");
            entity.Property(item => item.StartedAt).HasColumnName("started_at");
            entity.Property(item => item.CompletedAt).HasColumnName("completed_at");
            entity.Property(item => item.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.HasIndex(item => new { item.FromFiscalYear, item.ToFiscalYear, item.Status });
            entity.HasOne(item => item.CreatedByUser)
                .WithMany()
                .HasForeignKey(item => item.CreatedByUserId);
        });

        modelBuilder.Entity<LeaveBalanceSnapshot>(entity =>
        {
            entity.ToTable("leave_balance_snapshots");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.RolloverRunId).HasColumnName("rollover_run_id");
            entity.Property(item => item.UserId).HasColumnName("user_id");
            entity.Property(item => item.LeaveTypeId).HasColumnName("leave_type_id");
            entity.Property(item => item.FiscalYear).HasColumnName("fiscal_year");
            entity.Property(item => item.EntitlementDays).HasColumnName("entitlement_days");
            entity.Property(item => item.CarriedOverDays).HasColumnName("carried_over_days");
            entity.Property(item => item.AdjustedDays).HasColumnName("adjusted_days");
            entity.Property(item => item.UsedDays).HasColumnName("used_days");
            entity.Property(item => item.PendingDays).HasColumnName("pending_days");
            entity.Property(item => item.AvailableDays).HasColumnName("available_days");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.Property(item => item.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.HasIndex(item => new { item.UserId, item.LeaveTypeId, item.FiscalYear });
            entity.HasOne(item => item.RolloverRun)
                .WithMany()
                .HasForeignKey(item => item.RolloverRunId);
            entity.HasOne(item => item.User)
                .WithMany()
                .HasForeignKey(item => item.UserId);
            entity.HasOne(item => item.LeaveType)
                .WithMany()
                .HasForeignKey(item => item.LeaveTypeId);
            entity.HasOne(item => item.CreatedByUser)
                .WithMany()
                .HasForeignKey(item => item.CreatedByUserId);
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

        modelBuilder.Entity<LineUserBinding>(entity =>
        {
            entity.ToTable("line_user_bindings");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.LineUserId).HasColumnName("line_user_id").HasMaxLength(80);
            entity.Property(item => item.DisplayName).HasColumnName("display_name").HasMaxLength(200);
            entity.Property(item => item.PictureUrl).HasColumnName("picture_url").HasMaxLength(1000);
            entity.Property(item => item.UserId).HasColumnName("user_id");
            entity.Property(item => item.Status).HasColumnName("status").HasMaxLength(40);
            entity.Property(item => item.LastEventType).HasColumnName("last_event_type").HasMaxLength(40);
            entity.Property(item => item.LastEventAt).HasColumnName("last_event_at");
            entity.Property(item => item.BoundAt).HasColumnName("bound_at");
            entity.Property(item => item.UnboundAt).HasColumnName("unbound_at");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(item => item.LineUserId).IsUnique();
            entity.HasIndex(item => item.UserId).IsUnique().HasFilter("\"user_id\" IS NOT NULL AND \"status\" = 'Bound'");
            entity.HasIndex(item => item.Status);
            entity.HasOne(item => item.User)
                .WithMany()
                .HasForeignKey(item => item.UserId);
        });

        modelBuilder.Entity<LinePairingCode>(entity =>
        {
            entity.ToTable("line_pairing_codes");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.UserId).HasColumnName("user_id");
            entity.Property(item => item.Code).HasColumnName("code").HasMaxLength(20);
            entity.Property(item => item.Status).HasColumnName("status").HasMaxLength(40);
            entity.Property(item => item.ExpiresAt).HasColumnName("expires_at");
            entity.Property(item => item.UsedAt).HasColumnName("used_at");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(item => item.Code).IsUnique();
            entity.HasIndex(item => new { item.UserId, item.Status });
            entity.HasIndex(item => item.ExpiresAt);
            entity.HasOne(item => item.User)
                .WithMany()
                .HasForeignKey(item => item.UserId);
        });

        modelBuilder.Entity<LineConnectToken>(entity =>
        {
            entity.ToTable("line_connect_tokens");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.UserId).HasColumnName("user_id");
            entity.Property(item => item.Token).HasColumnName("token").HasMaxLength(120);
            entity.Property(item => item.ShortCode).HasColumnName("short_code").HasMaxLength(20);
            entity.Property(item => item.Status).HasColumnName("status").HasMaxLength(40);
            entity.Property(item => item.ExpiresAt).HasColumnName("expires_at");
            entity.Property(item => item.UsedAt).HasColumnName("used_at");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.Property(item => item.CreatedByIp).HasColumnName("created_by_ip").HasMaxLength(80);
            entity.Property(item => item.LineUserId).HasColumnName("line_user_id").HasMaxLength(80);
            entity.Property(item => item.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
            entity.HasIndex(item => item.Token).IsUnique();
            entity.HasIndex(item => item.ShortCode).IsUnique();
            entity.HasIndex(item => new { item.UserId, item.Status });
            entity.HasIndex(item => item.ExpiresAt);
            entity.HasOne(item => item.User)
                .WithMany()
                .HasForeignKey(item => item.UserId);
        });

        modelBuilder.Entity<BackupRun>(entity =>
        {
            entity.ToTable("backup_runs");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.BackupType).HasColumnName("backup_type").HasMaxLength(40);
            entity.Property(item => item.Status).HasColumnName("status").HasMaxLength(40);
            entity.Property(item => item.FileName).HasColumnName("file_name").HasMaxLength(260);
            entity.Property(item => item.FilePath).HasColumnName("file_path").HasMaxLength(1000);
            entity.Property(item => item.FileSizeBytes).HasColumnName("file_size_bytes");
            entity.Property(item => item.Checksum).HasColumnName("checksum").HasMaxLength(128);
            entity.Property(item => item.StartedAt).HasColumnName("started_at");
            entity.Property(item => item.CompletedAt).HasColumnName("completed_at");
            entity.Property(item => item.DurationMs).HasColumnName("duration_ms");
            entity.Property(item => item.ErrorMessage).HasColumnName("error_message").HasMaxLength(1000);
            entity.Property(item => item.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(item => item.VerifiedAt).HasColumnName("verified_at");
            entity.Property(item => item.VerifiedByUserId).HasColumnName("verified_by_user_id");
            entity.Property(item => item.DeletedAt).HasColumnName("deleted_at");
            entity.Property(item => item.DeletedByUserId).HasColumnName("deleted_by_user_id");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(item => item.FilePath).IsUnique();
            entity.HasIndex(item => new { item.BackupType, item.Status, item.StartedAt });
            entity.HasOne(item => item.CreatedByUser)
                .WithMany()
                .HasForeignKey(item => item.CreatedByUserId);
            entity.HasOne(item => item.VerifiedByUser)
                .WithMany()
                .HasForeignKey(item => item.VerifiedByUserId);
            entity.HasOne(item => item.DeletedByUser)
                .WithMany()
                .HasForeignKey(item => item.DeletedByUserId);
        });

        modelBuilder.Entity<RestoreRun>(entity =>
        {
            entity.ToTable("restore_runs");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.BackupRunId).HasColumnName("backup_run_id");
            entity.Property(item => item.RestoreType).HasColumnName("restore_type").HasMaxLength(40);
            entity.Property(item => item.TargetEnvironment).HasColumnName("target_environment").HasMaxLength(80);
            entity.Property(item => item.TargetDatabase).HasColumnName("target_database").HasMaxLength(200);
            entity.Property(item => item.Status).HasColumnName("status").HasMaxLength(40);
            entity.Property(item => item.Reason).HasColumnName("reason").HasMaxLength(1000);
            entity.Property(item => item.StartedAt).HasColumnName("started_at");
            entity.Property(item => item.CompletedAt).HasColumnName("completed_at");
            entity.Property(item => item.DurationMs).HasColumnName("duration_ms");
            entity.Property(item => item.ErrorMessage).HasColumnName("error_message").HasMaxLength(1000);
            entity.Property(item => item.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(item => item.ConfirmationMethod).HasColumnName("confirmation_method").HasMaxLength(80);
            entity.Property(item => item.PreRestoreBackupRunId).HasColumnName("pre_restore_backup_run_id");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(item => new { item.BackupRunId, item.Status, item.StartedAt });
            entity.HasOne(item => item.BackupRun)
                .WithMany()
                .HasForeignKey(item => item.BackupRunId);
            entity.HasOne(item => item.PreRestoreBackupRun)
                .WithMany()
                .HasForeignKey(item => item.PreRestoreBackupRunId);
            entity.HasOne(item => item.CreatedByUser)
                .WithMany()
                .HasForeignKey(item => item.CreatedByUserId);
        });

        modelBuilder.Entity<DiagnosticRun>(entity =>
        {
            entity.ToTable("diagnostic_runs");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.DiagnosticType).HasColumnName("diagnostic_type").HasMaxLength(80);
            entity.Property(item => item.Status).HasColumnName("status").HasMaxLength(40);
            entity.Property(item => item.StartedAt).HasColumnName("started_at");
            entity.Property(item => item.CompletedAt).HasColumnName("completed_at");
            entity.Property(item => item.DurationMs).HasColumnName("duration_ms");
            entity.Property(item => item.ResultSummary).HasColumnName("result_summary").HasMaxLength(2000);
            entity.Property(item => item.ReferenceId).HasColumnName("reference_id").HasMaxLength(120);
            entity.Property(item => item.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(item => item.ErrorMessage).HasColumnName("error_message").HasMaxLength(1000);
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(item => new { item.DiagnosticType, item.StartedAt });
            entity.HasIndex(item => item.ReferenceId);
            entity.HasOne(item => item.CreatedByUser)
                .WithMany()
                .HasForeignKey(item => item.CreatedByUserId);
        });

        modelBuilder.Entity<SupportBundle>(entity =>
        {
            entity.ToTable("support_bundles");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.FileName).HasColumnName("file_name").HasMaxLength(260);
            entity.Property(item => item.FilePath).HasColumnName("file_path").HasMaxLength(1000);
            entity.Property(item => item.FileSizeBytes).HasColumnName("file_size_bytes");
            entity.Property(item => item.Checksum).HasColumnName("checksum").HasMaxLength(128);
            entity.Property(item => item.ExpiresAt).HasColumnName("expires_at");
            entity.Property(item => item.Reason).HasColumnName("reason").HasMaxLength(1000);
            entity.Property(item => item.Status).HasColumnName("status").HasMaxLength(40);
            entity.Property(item => item.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.Property(item => item.DownloadedAt).HasColumnName("downloaded_at");
            entity.Property(item => item.DeletedAt).HasColumnName("deleted_at");
            entity.HasIndex(item => new { item.Status, item.ExpiresAt });
            entity.HasIndex(item => item.CreatedAt);
            entity.HasOne(item => item.CreatedByUser)
                .WithMany()
                .HasForeignKey(item => item.CreatedByUserId);
        });
    }

    private void SanitizePostgresTextValues()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            foreach (var property in entry.Properties)
            {
                if (property.Metadata.ClrType != typeof(string) ||
                    property.CurrentValue is not string value ||
                    !value.Contains('\0'))
                {
                    continue;
                }

                property.CurrentValue = value.Replace("\0", string.Empty);
            }
        }
    }
}
