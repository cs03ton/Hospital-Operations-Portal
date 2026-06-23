using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Hop.Api.Data;

public static class DevelopmentDataSeeder
{
    private const string ItDepartmentName = "Information Technology";
    private const string StandardItPassword = "Nm@12345";
    private const string ItStaffApprovalRuleName = "IT-STAFF";
    private const string ItHeadApprovalRuleName = "IT-HEAD";
    private const string DirectorApprovalRuleName = "DIRECTOR";

    private static readonly string[] RetiredDevelopmentUsernames =
    [
        "qa_notify_approver1_25690620205022",
        "qa_notify_approver2_25690620205022",
        "qa_notify_approver3_25690620205022",
        "qa_notify_requester_25690620205022",
        "qa_notify_unrelated_25690620205022",
        "manager.it01",
        "staff.it01",
        "staff.other01",
        "head.it01"
    ];

    private static readonly (string Username, string EmployeeCode, string FullName, string RoleName)[] StandardItUsers =
    [
        ("admin_support", "IT-ADMIN", "ผู้ดูแลระบบ", "Admin"),
        ("staff01", "IT-001", "เจ้าหน้าที่ 01", "Staff"),
        ("staff02", "IT-002", "เจ้าหน้าที่ 02", "Staff"),
        ("head01", "IT-HEAD", "หัวหน้าหน่วยงาน", "DepartmentHead"),
        ("director01", "IT-DIR", "ผู้อำนวยการ", "Director")
    ];

    private static readonly (string Name, string Description)[] RoleSeeds =
    [
        ("SuperAdmin", "Full system administration access"),
        ("Admin", "Operational administration access"),
        ("LeaveAdmin", "Leave administration access"),
        ("Director", "Executive approval and reporting access"),
        ("DepartmentHead", "Department approval access"),
        ("Staff", "Standard user access")
    ];

    private static readonly string[] PermissionGroups =
    [
        "Dashboard",
        "UserManagement",
        "DepartmentManagement",
        "RoleManagement",
        "LeaveManagement",
        "ApprovalChain",
        "ApprovalDelegation",
        "LeaveBalance",
        "LeaveHoliday",
        "LeaveAttachment",
        "ReportManagement",
        "SystemSettings"
    ];

    private static readonly string[] DisabledPhase1PermissionGroups =
    [
        "RepairManagement",
        "BorrowManagement",
        "InventoryManagement"
    ];

    private static readonly string[] PermissionActions =
    [
        "View",
        "Create",
        "Edit",
        "Delete",
        "Approve",
        "Export",
        "Manage"
    ];

    private static readonly (string Code, string Name, string Group, string Action)[] GranularLeavePermissions =
    [
        ("LeaveRequest.ViewOwn", "ดูคำขอลาของตนเอง", "LeaveRequest", "ViewOwn"),
        ("LeaveRequest.ViewPendingApproval", "ดูคำขอที่รอฉันอนุมัติ", "LeaveRequest", "ViewPendingApproval"),
        ("LeaveRequest.ViewDepartment", "ดูคำขอลาในหน่วยงาน", "LeaveRequest", "ViewDepartment"),
        ("LeaveRequest.ViewAll", "ดูคำขอลาทั้งหมด", "LeaveRequest", "ViewAll"),
        ("LeaveRequest.Create", "สร้างคำขอลา", "LeaveRequest", "Create"),
        ("LeaveRequest.EditOwn", "แก้ไขคำขอลาของตนเอง", "LeaveRequest", "EditOwn"),
        ("LeaveRequest.CancelOwn", "ยกเลิกคำขอลาของตนเอง", "LeaveRequest", "CancelOwn"),
        ("LeaveApproval.ApproveCurrentStep", "อนุมัติขั้นตอนปัจจุบัน", "LeaveApproval", "ApproveCurrentStep"),
        ("LeaveApproval.Delegate", "มอบหมายผู้อนุมัติ", "LeaveApproval", "Delegate"),
        ("LeaveApproval.Override", "อนุมัติแทนกรณีพิเศษ", "LeaveApproval", "Override"),
        ("LeaveApprovalDelegation.Manage", "จัดการการมอบหมายอนุมัติ", "LeaveApprovalDelegation", "Manage"),
        ("LeaveApprovalEscalation.Manage", "จัดการ escalation งานอนุมัติ", "LeaveApprovalEscalation", "Manage"),
        ("LeaveSupport.ViewAll", "มุมมองช่วยเหลือระบบลา", "LeaveSupport", "ViewAll"),
        ("LeaveAdmin.ManageTypes", "จัดการประเภทการลา", "LeaveAdmin", "ManageTypes"),
        ("LeaveAdmin.ManageBalances", "จัดการยอดวันลา", "LeaveAdmin", "ManageBalances"),
        ("LeaveAdmin.ManageHolidays", "จัดการวันหยุดราชการ", "LeaveAdmin", "ManageHolidays"),
        ("LeaveAdmin.ManageApprovalChains", "จัดการกฎการอนุมัติวันลา", "LeaveAdmin", "ManageApprovalChains")
    ];

    private static readonly (string Code, string Name, string Description, decimal DefaultDays, bool RequiresAttachment)[] LeaveTypeSeeds =
    [
        ("AnnualLeave", "ลาพักผ่อน", "Annual vacation leave", 10, false),
        ("SickLeave", "ลาป่วย", "Medical sick leave", 30, true),
        ("PersonalLeave", "ลากิจ", "Personal business leave", 3, false),
        ("MaternityLeave", "ลาคลอด", "Maternity leave", 98, true),
        ("OrdinationLeave", "ลาอุปสมบท", "Ordination leave", 15, false),
        ("StudyLeave", "ลาศึกษาต่อ", "Study leave", 0, false),
        ("OtherLeave", "อื่น ๆ", "Other leave", 0, false)
    ];

    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

        try
        {
            var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                throw new InvalidOperationException(
                    $"Database has pending EF Core migrations: {string.Join(", ", pendingMigrations)}. Run migrations before startup seeding.");
            }

            foreach (var roleSeed in RoleSeeds)
            {
                var role = await db.Roles.FirstOrDefaultAsync(item => item.Name == roleSeed.Name);
                if (role is null)
                {
                    db.Roles.Add(new Role
                    {
                        Name = roleSeed.Name,
                        Description = roleSeed.Description,
                        IsSystemRole = true,
                        IsActive = true
                    });
                }
                else
                {
                    role.Description ??= roleSeed.Description;
                    role.IsSystemRole = true;
                    role.IsActive = true;
                }
            }

            var department = await db.Departments.FirstOrDefaultAsync(dept => dept.Name == ItDepartmentName);
            if (department is null)
            {
                department = new Department
                {
                    Name = ItDepartmentName,
                    Description = "Default IT department"
                };
                db.Departments.Add(department);
            }

            await db.SaveChangesAsync();

            var legacyPermissionCodes = new[]
            {
                "dashboard.view",
                "users.manage",
                "departments.manage",
                "approvals.manage",
                "roles.view"
            };
            var legacyPermissions = await db.Permissions
                .Where(permission => legacyPermissionCodes.Contains(permission.Code))
                .ToListAsync();
            if (legacyPermissions.Count > 0)
            {
                db.Permissions.RemoveRange(legacyPermissions);
                await db.SaveChangesAsync();
            }

            var disabledPhase1Permissions = await db.Permissions
                .Where(permission => DisabledPhase1PermissionGroups.Contains(permission.Group))
                .ToListAsync();
            if (disabledPhase1Permissions.Count > 0)
            {
                db.Permissions.RemoveRange(disabledPhase1Permissions);
                await db.SaveChangesAsync();
            }

            foreach (var group in PermissionGroups)
            {
                foreach (var action in PermissionActions)
                {
                    var code = $"{group}.{action}";
                    if (!await db.Permissions.AnyAsync(permission => permission.Code == code))
                    {
                        db.Permissions.Add(new Permission
                        {
                            Code = code,
                            Name = code,
                            Group = group,
                            Action = action,
                            IsActive = true
                        });
                    }
                }
            }

            foreach (var permissionSeed in GranularLeavePermissions)
            {
                var permission = await db.Permissions.FirstOrDefaultAsync(item => item.Code == permissionSeed.Code);
                if (permission is null)
                {
                    db.Permissions.Add(new Permission
                    {
                        Code = permissionSeed.Code,
                        Name = permissionSeed.Name,
                        Group = permissionSeed.Group,
                        Action = permissionSeed.Action,
                        IsActive = true
                    });
                }
                else
                {
                    permission.Name = permissionSeed.Name;
                    permission.Group = permissionSeed.Group;
                    permission.Action = permissionSeed.Action;
                    permission.IsActive = true;
                }
            }

            await db.SaveChangesAsync();

            var superAdminRole = await db.Roles.SingleAsync(role => role.Name == "SuperAdmin");
            var adminRole = await db.Roles.SingleAsync(role => role.Name == "Admin");
            var leaveAdminRole = await db.Roles.SingleAsync(role => role.Name == "LeaveAdmin");
            var directorRole = await db.Roles.SingleAsync(role => role.Name == "Director");
            var departmentHeadRole = await db.Roles.SingleAsync(role => role.Name == "DepartmentHead");
            var staffRole = await db.Roles.SingleAsync(role => role.Name == "Staff");
            var adminUsername = configuration["Seed:AdminUsername"] ?? configuration["SEED_ADMIN_USERNAME"] ?? "admin";
            var admin = await db.Users
                .Include(user => user.UserRoles)
                .FirstOrDefaultAsync(user => user.Username == adminUsername);

            var shouldCreateDefaultAdmin = configuration.GetValue<bool?>("Seed:CreateDefaultAdmin")
                ?? configuration.GetValue<bool?>("SEED_CREATE_DEFAULT_ADMIN")
                ?? !environment.IsProduction();

            if (admin is null && shouldCreateDefaultAdmin)
            {
                var adminPassword = configuration["Seed:AdminPassword"] ?? configuration["SEED_ADMIN_PASSWORD"];
                if (string.IsNullOrWhiteSpace(adminPassword))
                {
                    if (environment.IsProduction())
                    {
                        throw new InvalidOperationException("Seed:AdminPassword is required when creating a bootstrap admin in production.");
                    }

                    adminPassword = "Admin@1234";
                }

                if (environment.IsProduction() && adminPassword == "Admin@1234")
                {
                    throw new InvalidOperationException("The development admin password cannot be used in production.");
                }

                var adminEmployeeCode = configuration["Seed:AdminEmployeeCode"] ?? configuration["SEED_ADMIN_EMPLOYEE_CODE"] ?? "ADMIN";
                var adminFullName = configuration["Seed:AdminFullName"] ?? configuration["SEED_ADMIN_FULLNAME"] ?? "Default Administrator";

                admin = new User
                {
                    EmployeeCode = adminEmployeeCode,
                    FullName = adminFullName,
                    Username = adminUsername,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                    DepartmentId = department.Id,
                    IsActive = true
                };

                db.Users.Add(admin);
                await db.SaveChangesAsync();
                logger.LogInformation("Bootstrap admin user {Username} was created by seed configuration.", adminUsername);
            }

            if (admin is not null && !admin.UserRoles.Any(userRole => userRole.RoleId == superAdminRole.Id))
            {
                db.UserRoles.Add(new UserRole
                {
                    UserId = admin.Id,
                    RoleId = superAdminRole.Id
                });
            }

            var allPermissionIds = await db.Permissions
                .Where(permission => permission.Code != "LeaveRequest.Create")
                .Select(permission => permission.Id)
                .ToListAsync();
            await GrantPermissionIds(db, superAdminRole.Id, allPermissionIds);
            await RevokePermissions(db, superAdminRole.Id, "LeaveRequest.Create");
            await RevokePermissions(db, adminRole.Id, "LeaveRequest.Create");
            await GrantPermissions(db, staffRole.Id,
                "Dashboard.View",
                "LeaveRequest.ViewOwn",
                "LeaveRequest.Create",
                "LeaveRequest.EditOwn",
                "LeaveRequest.CancelOwn");
            await GrantPermissions(db, departmentHeadRole.Id,
                "Dashboard.View",
                "LeaveRequest.ViewOwn",
                "LeaveRequest.ViewPendingApproval",
                "LeaveRequest.ViewDepartment",
                "LeaveRequest.Create",
                "LeaveRequest.EditOwn",
                "LeaveRequest.CancelOwn",
                "LeaveApproval.ApproveCurrentStep");
            await GrantPermissions(db, directorRole.Id,
                "Dashboard.View",
                "LeaveRequest.ViewOwn",
                "LeaveRequest.ViewPendingApproval",
                "LeaveRequest.Create",
                "LeaveRequest.EditOwn",
                "LeaveRequest.CancelOwn",
                "LeaveApproval.ApproveCurrentStep");
            await GrantPermissions(db, leaveAdminRole.Id,
                "Dashboard.View",
                "LeaveRequest.ViewDepartment",
                "LeaveAdmin.ManageTypes",
                "LeaveAdmin.ManageBalances",
                "LeaveAdmin.ManageHolidays",
                "LeaveAdmin.ManageApprovalChains");
            await GrantPermissions(db, adminRole.Id,
                "Dashboard.View",
                "UserManagement.View",
                "UserManagement.Create",
                "UserManagement.Edit",
                "UserManagement.Delete",
                "UserManagement.Manage",
                "DepartmentManagement.View",
                "DepartmentManagement.Create",
                "DepartmentManagement.Edit",
                "DepartmentManagement.Delete",
                "DepartmentManagement.Manage",
                "RoleManagement.View",
                "RoleManagement.Create",
                "RoleManagement.Edit",
                "RoleManagement.Delete",
                "RoleManagement.Manage",
                "LeaveRequest.ViewOwn",
                "LeaveRequest.ViewPendingApproval",
                "LeaveRequest.ViewDepartment",
                "LeaveApproval.ApproveCurrentStep",
                "LeaveApproval.Delegate",
                "LeaveApprovalDelegation.Manage",
                "LeaveApprovalEscalation.Manage",
                "LeaveSupport.ViewAll",
                "LeaveAdmin.ManageTypes",
                "LeaveAdmin.ManageBalances",
                "LeaveAdmin.ManageHolidays",
                "LeaveAdmin.ManageApprovalChains");

            var shouldCreateStandardItUsers = configuration.GetValue<bool?>("Seed:CreateStandardItUsers")
                ?? configuration.GetValue<bool?>("SEED_CREATE_STANDARD_IT_USERS")
                ?? !environment.IsProduction();
            if (shouldCreateStandardItUsers)
            {
                await RetireDevelopmentUsers(db, logger);
                await SeedStandardItUsersAndApprovalChain(
                    db,
                    department,
                    new Dictionary<string, Role>
                    {
                        ["Admin"] = adminRole,
                        ["Staff"] = staffRole,
                        ["DepartmentHead"] = departmentHeadRole,
                        ["Director"] = directorRole
                    },
                    logger);
            }

            foreach (var leaveTypeSeed in LeaveTypeSeeds)
            {
                var leaveType = await db.LeaveTypes.FirstOrDefaultAsync(item => item.Code == leaveTypeSeed.Code);
                if (leaveType is null)
                {
                    db.LeaveTypes.Add(new LeaveType
                    {
                        Code = leaveTypeSeed.Code,
                        Name = leaveTypeSeed.Name,
                        Description = leaveTypeSeed.Description,
                        DefaultDaysPerYear = leaveTypeSeed.DefaultDays,
                        RequiresAttachment = leaveTypeSeed.RequiresAttachment,
                        IsPaid = true,
                        IsActive = true
                    });
                }
                else
                {
                    leaveType.Name = leaveTypeSeed.Name;
                    leaveType.Description = leaveTypeSeed.Description;
                    leaveType.DefaultDaysPerYear = leaveTypeSeed.DefaultDays;
                    leaveType.RequiresAttachment = leaveTypeSeed.RequiresAttachment;
                    leaveType.IsPaid = true;
                    leaveType.IsActive = true;
                }
            }

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            if (environment.IsProduction())
            {
                logger.LogError(ex, "Database seed failed in production.");
                throw;
            }

            logger.LogWarning(ex, "Database seed skipped. Start PostgreSQL and run the app again to seed defaults.");
        }
    }

    private static async Task GrantPermissions(AppDbContext db, Guid roleId, params string[] permissionCodes)
    {
        var permissionIds = await db.Permissions
            .Where(permission => permissionCodes.Contains(permission.Code))
            .Select(permission => permission.Id)
            .ToListAsync();

        await GrantPermissionIds(db, roleId, permissionIds);
    }

    private static async Task GrantPermissionIds(AppDbContext db, Guid roleId, IReadOnlyList<Guid> permissionIds)
    {
        var existingPermissionIds = await db.RolePermissions
            .Where(item => item.RoleId == roleId)
            .Select(item => item.PermissionId)
            .ToListAsync();

        foreach (var permissionId in permissionIds.Except(existingPermissionIds))
        {
            db.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            });
        }
    }

    private static async Task RevokePermissions(AppDbContext db, Guid roleId, params string[] permissionCodes)
    {
        var permissionIds = await db.Permissions
            .Where(permission => permissionCodes.Contains(permission.Code))
            .Select(permission => permission.Id)
            .ToListAsync();

        var existing = await db.RolePermissions
            .Where(item => item.RoleId == roleId && permissionIds.Contains(item.PermissionId))
            .ToListAsync();

        if (existing.Count > 0)
        {
            db.RolePermissions.RemoveRange(existing);
        }
    }

    private static async Task RetireDevelopmentUsers(AppDbContext db, ILogger logger)
    {
        var retiredUsers = await db.Users
            .Where(user => RetiredDevelopmentUsernames.Contains(user.Username))
            .ToListAsync();

        foreach (var user in retiredUsers)
        {
            if (!user.IsActive)
            {
                continue;
            }

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            logger.LogInformation("Development test user {Username} was disabled by seed reset.", user.Username);
        }
    }

    private static async Task SeedStandardItUsersAndApprovalChain(
        AppDbContext db,
        Department department,
        IReadOnlyDictionary<string, Role> rolesByName,
        ILogger logger)
    {
        var usersByUsername = new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase);

        foreach (var userSeed in StandardItUsers)
        {
            var user = await db.Users
                .Include(item => item.UserRoles)
                .FirstOrDefaultAsync(item => item.Username == userSeed.Username);

            if (user is null)
            {
                user = new User
                {
                    Username = userSeed.Username,
                    CreatedAt = DateTime.UtcNow
                };
                db.Users.Add(user);
                logger.LogInformation("Standard IT user {Username} was created by development seed.", userSeed.Username);
            }

            user.EmployeeCode = userSeed.EmployeeCode;
            user.FullName = userSeed.FullName;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(StandardItPassword);
            user.DepartmentId = department.Id;
            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;

            var role = rolesByName[userSeed.RoleName];
            var existingRoles = user.UserRoles.ToList();
            foreach (var existingRole in existingRoles.Where(item => item.RoleId != role.Id))
            {
                db.UserRoles.Remove(existingRole);
            }

            if (!existingRoles.Any(item => item.RoleId == role.Id))
            {
                db.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id
                });
            }

            usersByUsername[userSeed.Username] = user;
        }

        await db.SaveChangesAsync();

        var adminSupport = usersByUsername["admin_support"];
        var staff01 = usersByUsername["staff01"];
        var staff02 = usersByUsername["staff02"];
        var head = usersByUsername["head01"];
        var director = usersByUsername["director01"];

        var itStaffRule = await EnsureApprovalRule(db, ItStaffApprovalRuleName, "เจ้าหน้าที่ IT → หัวหน้าหน่วยงาน → ผู้อำนวยการ", department.Id);
        UpsertApprovalStep(itStaffRule, 1, "หัวหน้าหน่วยงาน", head.Id);
        UpsertApprovalStep(itStaffRule, 2, "ผู้อำนวยการ", director.Id);
        DeactivateExtraSteps(itStaffRule, 2);

        var itHeadRule = await EnsureApprovalRule(db, ItHeadApprovalRuleName, "หัวหน้าหน่วยงาน IT → ผู้อำนวยการ", department.Id);
        UpsertApprovalStep(itHeadRule, 1, "ผู้อำนวยการ", director.Id);
        DeactivateExtraSteps(itHeadRule, 1);

        var directorRule = await EnsureApprovalRule(db, DirectorApprovalRuleName, "ผู้อำนวยการ → ผู้ดูแลระบบสำรอง", department.Id);
        UpsertApprovalStep(directorRule, 1, "ผู้อนุมัติสำรอง", adminSupport.Id);
        DeactivateExtraSteps(directorRule, 1);

        staff01.LeaveApprovalRuleId = itStaffRule.Id;
        staff02.LeaveApprovalRuleId = itStaffRule.Id;
        head.LeaveApprovalRuleId = itHeadRule.Id;
        director.LeaveApprovalRuleId = directorRule.Id;
        adminSupport.LeaveApprovalRuleId = itHeadRule.Id;

        await db.SaveChangesAsync();
        logger.LogInformation("Standard IT approval rules {StaffRule}, {HeadRule}, {DirectorRule} were verified by development seed.", ItStaffApprovalRuleName, ItHeadApprovalRuleName, DirectorApprovalRuleName);
    }

    private static async Task<ApprovalChain> EnsureApprovalRule(AppDbContext db, string name, string description, Guid departmentId)
    {
        var approvalRule = await db.ApprovalChains
            .Include(chain => chain.Steps)
            .FirstOrDefaultAsync(chain => chain.Name == name);

        if (approvalRule is null)
        {
            approvalRule = new ApprovalChain
            {
                Id = Guid.NewGuid(),
                Name = name,
                CreatedAt = DateTime.UtcNow
            };
            db.ApprovalChains.Add(approvalRule);
        }

        approvalRule.Description = description;
        approvalRule.DepartmentId = departmentId;
        approvalRule.LeaveTypeId = null;
        approvalRule.MinimumDays = 0;
        approvalRule.IsActive = true;
        approvalRule.UpdatedAt = DateTime.UtcNow;

        return approvalRule;
    }

    private static void DeactivateExtraSteps(ApprovalChain approvalRule, int maxStepOrder)
    {
        foreach (var extraStep in approvalRule.Steps.Where(step => step.StepOrder > maxStepOrder))
        {
            extraStep.IsActive = false;
            extraStep.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static void UpsertApprovalStep(ApprovalChain approvalChain, int stepOrder, string name, Guid approverUserId)
    {
        var step = approvalChain.Steps.FirstOrDefault(item => item.StepOrder == stepOrder);
        if (step is null)
        {
            approvalChain.Steps.Add(new ApprovalChainStep
            {
                StepOrder = stepOrder,
                CreatedAt = DateTime.UtcNow
            });
            step = approvalChain.Steps.First(item => item.StepOrder == stepOrder);
        }

        step.Name = name;
        step.ApproverRoleId = null;
        step.ApproverUserId = approverUserId;
        step.RequiredPermissionCode = "LeaveApproval.ApproveCurrentStep";
        step.IsActive = true;
        step.UpdatedAt = DateTime.UtcNow;
    }
}
