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

    private static readonly (string Username, string EmployeeCode, string FullName, string RoleName, string? EmploymentType, string Gender)[] StandardItUsers =
    [
        ("admin_support", "IT-ADMIN", "ผู้ดูแลระบบ", "Admin", null, GenderTypes.Unknown),
        ("staff01", "IT-001", "เจ้าหน้าที่ 01", "Staff", EmploymentTypes.MophEmployee, GenderTypes.Female),
        ("staff02", "IT-002", "เจ้าหน้าที่ 02", "Staff", EmploymentTypes.MophEmployee, GenderTypes.Male),
        ("head01", "IT-HEAD", "หัวหน้าหน่วยงาน", "DepartmentHead", EmploymentTypes.CivilServant, GenderTypes.Male),
        ("director01", "IT-DIR", "ผู้อำนวยการ", "Director", EmploymentTypes.CivilServant, GenderTypes.Male)
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
        ("LeaveBalance.Rollover", "ยกยอดวันลา", "LeaveBalance", "Rollover"),
        ("LeaveAdmin.ManageHolidays", "จัดการวันหยุดราชการ", "LeaveAdmin", "ManageHolidays"),
        ("LeaveAdmin.ManageApprovalChains", "จัดการกฎการอนุมัติวันลา", "LeaveAdmin", "ManageApprovalChains"),
        ("System.Line.TestSend", "ทดสอบส่งข้อความ LINE", "System", "LineTestSend")
    ];

    private sealed record LeaveTypeSeed(string Code, string Name, string Description, decimal DefaultDays, bool RequiresAttachment, bool RequiresBalance, bool AllowCarryOver, decimal CarryOverMaxDays, bool UseFiscalYear, bool IsPaid, string[] LegacyCodes);

    private sealed record LeavePolicyRuleSeed(
        string EmploymentType,
        string LeaveTypeCode,
        decimal EntitlementDays,
        decimal? MaxPaidDays = null,
        bool AllowCarryOver = false,
        decimal? CarryOverMaxDays = null,
        decimal? MaxAccumulatedDays = null,
        int? MinServiceMonths = null,
        int? MinServiceYears = null,
        bool ProrateIfServiceLessThanYear = false,
        decimal? FirstYearEntitlementDays = null,
        decimal? FirstYearPaidDays = null,
        bool IsPaid = true,
        decimal? MaxExtendedDays = null,
        decimal? SocialSecurityMaxDays = null,
        string? Notes = null);

    private static readonly LeaveTypeSeed[] LeaveTypeSeeds =
    [
        new("SICK_LEAVE", "ลาป่วย", "Medical sick leave", 30, true, true, false, 0, true, true, ["SickLeave"]),
        new("PERSONAL_LEAVE", "ลากิจส่วนตัว", "Personal business leave", 45, false, true, false, 0, true, true, ["PersonalLeave"]),
        new("VACATION_LEAVE", "ลาพักผ่อน", "Annual vacation leave", 10, false, true, true, 30, true, true, ["AnnualLeave"]),
        new("MATERNITY_LEAVE", "ลาคลอดบุตร", "Maternity leave", 90, true, false, false, 0, true, true, ["MaternityLeave"]),
        new("ORDINATION_LEAVE", "ลาบวช", "Ordination leave", 120, false, false, false, 0, false, true, ["OrdinationLeave"]),
        new("STUDY_LEAVE", "ลาศึกษาต่อ", "Study leave", 0, false, false, false, 0, false, false, ["StudyLeave"]),
        new("OTHER_LEAVE", "อื่น ๆ", "Other leave", 0, false, false, false, 0, false, false, ["OtherLeave"])
    ];

    private static readonly LeavePolicyRuleSeed[] LeavePolicyRuleSeeds =
    [
        new(EmploymentTypes.CivilServant, "SICK_LEAVE", 60, MaxPaidDays: 60, MaxExtendedDays: 120, Notes: "กรณีเกิน 60 วัน ผู้อำนวยการอาจพิจารณาได้รวมไม่เกิน 120 วัน"),
        new(EmploymentTypes.CivilServant, "PERSONAL_LEAVE", 45, MaxPaidDays: 45, FirstYearEntitlementDays: 15),
        new(EmploymentTypes.CivilServant, "VACATION_LEAVE", 10, MaxPaidDays: 10, AllowCarryOver: true, CarryOverMaxDays: 30, MaxAccumulatedDays: 30, MinServiceMonths: 6),
        new(EmploymentTypes.CivilServant, "MATERNITY_LEAVE", 90, MaxPaidDays: 90),
        new(EmploymentTypes.CivilServant, "ORDINATION_LEAVE", 120, MaxPaidDays: 120, MinServiceMonths: 12, Notes: "ใช้ตามระเบียบราชการและเงื่อนไขหน่วยงาน"),

        new(EmploymentTypes.GovernmentEmployee, "SICK_LEAVE", 30, MaxPaidDays: 30),
        new(EmploymentTypes.GovernmentEmployee, "PERSONAL_LEAVE", 10, MaxPaidDays: 10),
        new(EmploymentTypes.GovernmentEmployee, "VACATION_LEAVE", 10, MaxPaidDays: 10, AllowCarryOver: true, CarryOverMaxDays: 15, MaxAccumulatedDays: 15, MinServiceMonths: 6, FirstYearEntitlementDays: 0),
        new(EmploymentTypes.GovernmentEmployee, "MATERNITY_LEAVE", 90, MaxPaidDays: 45, Notes: "ได้รับค่าจ้างไม่เกิน 45 วันตาม policy"),
        new(EmploymentTypes.GovernmentEmployee, "ORDINATION_LEAVE", 120, MaxPaidDays: 120, MinServiceYears: 4, Notes: "ต้องทำงานไม่น้อยกว่า 4 ปี"),

        new(EmploymentTypes.MophEmployee, "SICK_LEAVE", 45, MaxPaidDays: 45),
        new(EmploymentTypes.MophEmployee, "PERSONAL_LEAVE", 15, MaxPaidDays: 15, FirstYearPaidDays: 6, Notes: "ปีแรกได้รับค่าจ้างไม่เกิน 6 วัน"),
        new(EmploymentTypes.MophEmployee, "VACATION_LEAVE", 10, MaxPaidDays: 10, AllowCarryOver: true, CarryOverMaxDays: 15, MaxAccumulatedDays: 15, MinServiceMonths: 6, FirstYearEntitlementDays: 0),
        new(EmploymentTypes.MophEmployee, "MATERNITY_LEAVE", 90, MaxPaidDays: 45),
        new(EmploymentTypes.MophEmployee, "ORDINATION_LEAVE", 120, MaxPaidDays: 120, MinServiceYears: 4, Notes: "ต้องทำงานไม่น้อยกว่า 4 ปี"),

        new(EmploymentTypes.TemporaryEmployeeMonthly, "SICK_LEAVE", 15, MaxPaidDays: 15, MinServiceMonths: 6, FirstYearEntitlementDays: 8, FirstYearPaidDays: 8),
        new(EmploymentTypes.TemporaryEmployeeMonthly, "PERSONAL_LEAVE", 10, IsPaid: false, Notes: "ลาได้แต่ไม่ได้รับค่าจ้าง"),
        new(EmploymentTypes.TemporaryEmployeeMonthly, "VACATION_LEAVE", 10, MaxPaidDays: 10, MinServiceMonths: 6, FirstYearEntitlementDays: 0),
        new(EmploymentTypes.TemporaryEmployeeMonthly, "MATERNITY_LEAVE", 90, MaxPaidDays: 45),
        new(EmploymentTypes.TemporaryEmployeeMonthly, "ORDINATION_LEAVE", 120, IsPaid: false, Notes: "ไม่มีสิทธิได้รับค่าจ้างระหว่างลา"),

        new(EmploymentTypes.TemporaryEmployeeDaily, "SICK_LEAVE", 15, IsPaid: false, FirstYearEntitlementDays: 8, MaxExtendedDays: 30, SocialSecurityMaxDays: 90, Notes: "รายวันไม่มีสิทธิได้รับค่าจ้างจากหน่วยงาน อาจใช้สิทธิประกันสังคมตามเงื่อนไข"),
        new(EmploymentTypes.TemporaryEmployeeDaily, "PERSONAL_LEAVE", 10, IsPaid: false, Notes: "ลาได้แต่ไม่ได้รับค่าจ้าง"),
        new(EmploymentTypes.TemporaryEmployeeDaily, "VACATION_LEAVE", 10, IsPaid: false, MinServiceMonths: 6, FirstYearEntitlementDays: 0, Notes: "ไม่สะสมวันลาพักผ่อน"),
        new(EmploymentTypes.TemporaryEmployeeDaily, "MATERNITY_LEAVE", 0, IsPaid: false, SocialSecurityMaxDays: 90, Notes: "ใช้สิทธิประกันสังคมตามเงื่อนไข"),
        new(EmploymentTypes.TemporaryEmployeeDaily, "ORDINATION_LEAVE", 120, IsPaid: false, Notes: "ไม่มีสิทธิได้รับค่าจ้างระหว่างลา")
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
                "LeaveBalance.Rollover",
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
                "LeaveBalance.Rollover",
                "LeaveAdmin.ManageHolidays",
                "LeaveAdmin.ManageApprovalChains",
                "System.Line.TestSend");

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

            await EnsureLeaveTypesAndPolicyRules(db);
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

    private static async Task EnsureLeaveTypesAndPolicyRules(AppDbContext db)
    {
        foreach (var leaveTypeSeed in LeaveTypeSeeds)
        {
            var candidateCodes = leaveTypeSeed.LegacyCodes.Append(leaveTypeSeed.Code).ToArray();
            var matches = await db.LeaveTypes
                .Where(item => candidateCodes.Contains(item.Code))
                .OrderByDescending(item => item.Code == leaveTypeSeed.Code)
                .ThenBy(item => item.CreatedAt)
                .ToListAsync();

            var leaveType = matches.FirstOrDefault();
            if (leaveType is null)
            {
                leaveType = new LeaveType
                {
                    Code = leaveTypeSeed.Code,
                    CreatedAt = DateTime.UtcNow
                };
                db.LeaveTypes.Add(leaveType);
            }

            leaveType.Code = leaveTypeSeed.Code;
            leaveType.Name = leaveTypeSeed.Name;
            leaveType.Description = leaveTypeSeed.Description;
            leaveType.DefaultDaysPerYear = leaveTypeSeed.DefaultDays;
            leaveType.RequiresBalance = leaveTypeSeed.RequiresBalance;
            leaveType.AllowCarryOver = leaveTypeSeed.AllowCarryOver;
            leaveType.CarryOverMaxDays = leaveTypeSeed.CarryOverMaxDays;
            leaveType.UseFiscalYear = leaveTypeSeed.UseFiscalYear;
            leaveType.RequiresAttachment = leaveTypeSeed.RequiresAttachment;
            leaveType.IsPaid = leaveTypeSeed.IsPaid;
            leaveType.IsActive = true;
            leaveType.UpdatedAt = DateTime.UtcNow;

            foreach (var duplicate in matches.Skip(1))
            {
                duplicate.IsActive = false;
                duplicate.UpdatedAt = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync();

        var leaveTypesByCode = await db.LeaveTypes.ToDictionaryAsync(item => item.Code, item => item);
        foreach (var seed in LeavePolicyRuleSeeds)
        {
            if (!leaveTypesByCode.TryGetValue(seed.LeaveTypeCode, out var leaveType))
            {
                continue;
            }

            var rule = await db.LeavePolicyRules.FirstOrDefaultAsync(item =>
                item.EmploymentType == seed.EmploymentType &&
                item.LeaveTypeId == leaveType.Id &&
                item.FiscalYear == null);

            if (rule is null)
            {
                rule = new LeavePolicyRule
                {
                    EmploymentType = seed.EmploymentType,
                    LeaveTypeId = leaveType.Id,
                    CreatedAt = DateTime.UtcNow
                };
                db.LeavePolicyRules.Add(rule);
            }

            rule.EntitlementDays = seed.EntitlementDays;
            rule.MaxPaidDays = seed.MaxPaidDays;
            rule.AllowCarryOver = seed.AllowCarryOver;
            rule.CarryOverMaxDays = seed.CarryOverMaxDays;
            rule.MaxAccumulatedDays = seed.MaxAccumulatedDays;
            rule.MinServiceMonths = seed.MinServiceMonths;
            rule.MinServiceYears = seed.MinServiceYears;
            rule.ProrateIfServiceLessThanYear = seed.ProrateIfServiceLessThanYear;
            rule.FirstYearEntitlementDays = seed.FirstYearEntitlementDays;
            rule.FirstYearPaidDays = seed.FirstYearPaidDays;
            rule.IsPaid = seed.IsPaid;
            rule.MaxExtendedDays = seed.MaxExtendedDays;
            rule.SocialSecurityMaxDays = seed.SocialSecurityMaxDays;
            rule.Notes = seed.Notes;
            rule.IsActive = true;
            rule.UpdatedAt = DateTime.UtcNow;
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
            user.Gender = userSeed.Gender;
            user.EmploymentType = userSeed.EmploymentType;
            user.EmploymentStartDate = userSeed.EmploymentType is null ? null : new DateOnly(2024, 10, 1);
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
