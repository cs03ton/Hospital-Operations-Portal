using Hop.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260709120000_AddDocumentationCenterPermissions")]
    public partial class AddDocumentationCenterPermissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO permissions (id, code, name, group_name, action, is_active, created_at)
                SELECT gen_random_uuid(), item.code, item.name, item.group_name, item.action, true, NOW()
                FROM (VALUES
                    ('Documentation.View', 'ดูศูนย์คู่มือการใช้งาน', 'Documentation', 'View'),
                    ('Documentation.AdminView', 'ดูคู่มือสำหรับผู้ดูแลระบบ', 'Documentation', 'AdminView'),
                    ('Documentation.Manage', 'จัดการคู่มือการใช้งาน', 'Documentation', 'Manage')
                ) AS item(code, name, group_name, action)
                WHERE NOT EXISTS (
                    SELECT 1 FROM permissions p WHERE p.code = item.code
                );

                INSERT INTO role_permissions (role_id, permission_id)
                SELECT r.id, p.id
                FROM roles r
                CROSS JOIN permissions p
                WHERE r.name IN ('Staff', 'DepartmentHead', 'Director', 'LeaveAdmin', 'Admin', 'SuperAdmin')
                  AND p.code = 'Documentation.View'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM role_permissions rp
                      WHERE rp.role_id = r.id AND rp.permission_id = p.id
                  );

                INSERT INTO role_permissions (role_id, permission_id)
                SELECT r.id, p.id
                FROM roles r
                CROSS JOIN permissions p
                WHERE r.name IN ('Admin', 'SuperAdmin')
                  AND p.code IN ('Documentation.AdminView', 'Documentation.Manage')
                  AND NOT EXISTS (
                      SELECT 1
                      FROM role_permissions rp
                      WHERE rp.role_id = r.id AND rp.permission_id = p.id
                  );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM role_permissions
                WHERE permission_id IN (
                    SELECT id FROM permissions
                    WHERE code IN ('Documentation.View', 'Documentation.AdminView', 'Documentation.Manage')
                );

                DELETE FROM permissions
                WHERE code IN ('Documentation.View', 'Documentation.AdminView', 'Documentation.Manage');
                """);
        }
    }
}
