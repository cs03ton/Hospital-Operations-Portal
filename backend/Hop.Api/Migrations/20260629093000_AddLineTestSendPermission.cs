using Hop.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260629093000_AddLineTestSendPermission")]
    public partial class AddLineTestSendPermission : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO permissions (id, code, name, group_name, action, is_active, created_at)
                SELECT gen_random_uuid(), 'System.Line.TestSend', 'ทดสอบส่งข้อความ LINE', 'System', 'LineTestSend', true, NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM permissions WHERE code = 'System.Line.TestSend'
                );

                INSERT INTO role_permissions (role_id, permission_id)
                SELECT r.id, p.id
                FROM roles r
                CROSS JOIN permissions p
                WHERE r.name IN ('Admin', 'SuperAdmin')
                  AND p.code = 'System.Line.TestSend'
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
                    SELECT id FROM permissions WHERE code = 'System.Line.TestSend'
                );

                DELETE FROM permissions WHERE code = 'System.Line.TestSend';
                """);
        }
    }
}
