using Hop.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260914090000_AddDirectorRepairViewAll")]
public sealed class AddDirectorRepairViewAll : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO role_permissions (role_id, permission_id)
            SELECT r.id, p.id
            FROM roles r
            CROSS JOIN permissions p
            WHERE r.name = 'Director'
              AND r.is_active
              AND p.code = 'RepairManagement.ViewAll'
              AND p.is_active
              AND NOT EXISTS (
                  SELECT 1 FROM role_permissions rp
                  WHERE rp.role_id = r.id AND rp.permission_id = p.id
              );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM role_permissions rp
            USING roles r, permissions p
            WHERE rp.role_id = r.id
              AND rp.permission_id = p.id
              AND r.name = 'Director'
              AND p.code = 'RepairManagement.ViewAll';
            """);
    }
}
