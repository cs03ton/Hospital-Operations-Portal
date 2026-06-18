using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Data;

public static class DevelopmentDataSeeder
{
    private static readonly (string Name, string Description)[] RoleSeeds =
    [
        ("SuperAdmin", "Full system administration access"),
        ("Admin", "Operational administration access"),
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
        "LeaveBalance",
        "LeaveHoliday",
        "LeaveAttachment",
        "RepairManagement",
        "BorrowManagement",
        "InventoryManagement",
        "ReportManagement",
        "SystemSettings"
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

    private static readonly (string Code, string Name, string Description, decimal DefaultDays, bool RequiresAttachment)[] LeaveTypeSeeds =
    [
        ("AnnualLeave", "Annual Leave", "Annual vacation leave", 10, false),
        ("SickLeave", "Sick Leave", "Medical sick leave", 30, true),
        ("PersonalLeave", "Personal Leave", "Personal business leave", 3, false),
        ("MaternityLeave", "Maternity Leave", "Maternity leave", 98, true)
    ];

    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            await db.Database.EnsureCreatedAsync();

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

            var department = await db.Departments.FirstOrDefaultAsync(dept => dept.Name == "Information Technology");
            if (department is null)
            {
                department = new Department
                {
                    Name = "Information Technology",
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

            await db.SaveChangesAsync();

            var superAdminRole = await db.Roles.SingleAsync(role => role.Name == "SuperAdmin");
            var adminRole = await db.Roles.SingleAsync(role => role.Name == "Admin");
            var admin = await db.Users
                .Include(user => user.UserRoles)
                .FirstOrDefaultAsync(user => user.Username == "admin");

            if (admin is null)
            {
                admin = new User
                {
                    EmployeeCode = "ADMIN",
                    FullName = "Default Administrator",
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@1234"),
                    DepartmentId = department.Id,
                    IsActive = true
                };

                db.Users.Add(admin);
                await db.SaveChangesAsync();
            }

            if (!admin.UserRoles.Any(userRole => userRole.RoleId == superAdminRole.Id))
            {
                db.UserRoles.Add(new UserRole
                {
                    UserId = admin.Id,
                    RoleId = superAdminRole.Id
                });
            }

            var allPermissionIds = await db.Permissions.Select(permission => permission.Id).ToListAsync();
            foreach (var roleId in new[] { superAdminRole.Id, adminRole.Id })
            {
                var existingPermissionIds = await db.RolePermissions
                    .Where(item => item.RoleId == roleId)
                    .Select(item => item.PermissionId)
                    .ToListAsync();

                foreach (var permissionId in allPermissionIds.Except(existingPermissionIds))
                {
                    db.RolePermissions.Add(new RolePermission
                    {
                        RoleId = roleId,
                        PermissionId = permissionId
                    });
                }
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
            logger.LogWarning(ex, "Database seed skipped. Start PostgreSQL and run the app again to seed defaults.");
        }
    }
}
