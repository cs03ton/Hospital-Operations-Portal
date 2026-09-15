using Hop.Api.Data;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Hop.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260915090000_AddCentralLineGroupManagement")]
public partial class AddCentralLineGroupManagement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO permissions (id, code, name, group_name, action, is_active, created_at)
            VALUES
              (gen_random_uuid(), 'LineGroup.View', 'ดูกลุ่มแจ้งเตือนส่วนกลาง', 'LineGroup', 'View', TRUE, NOW()),
              (gen_random_uuid(), 'LineGroup.Manage', 'จัดการกลุ่มและเหตุการณ์แจ้งเตือนส่วนกลาง', 'LineGroup', 'Manage', TRUE, NOW())
            ON CONFLICT (code) DO UPDATE SET name = EXCLUDED.name, group_name = EXCLUDED.group_name,
              action = EXCLUDED.action, is_active = TRUE;

            INSERT INTO role_permissions (role_id, permission_id)
            SELECT r.id, p.id FROM roles r CROSS JOIN permissions p
            WHERE r.name IN ('Admin', 'SuperAdmin') AND r.is_active
              AND p.code IN ('LineGroup.View', 'LineGroup.Manage') AND p.is_active
            ON CONFLICT DO NOTHING;

            INSERT INTO line_group_event_subscriptions (id, destination_id, event_type, is_enabled, created_at)
            SELECT gen_random_uuid(), d.id, e.event_type,
              CASE
                WHEN d.module = 'REPAIR_IT' OR d.module = 'REPAIR_GENERAL' THEN e.event_type LIKE 'Repair.%'
                ELSE FALSE
              END,
              NOW()
            FROM line_group_destinations d
            CROSS JOIN (VALUES
              ('Repair.Submitted'), ('Repair.Resubmitted'), ('Repair.Reopened'), ('Repair.Started'),
              ('Repair.Resumed'), ('Repair.Solved'), ('Repair.Closed'), ('MeetingRoom.BookingCreated')
            ) AS e(event_type)
            ON CONFLICT (destination_id, event_type) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM role_permissions WHERE permission_id IN
              (SELECT id FROM permissions WHERE code IN ('LineGroup.View', 'LineGroup.Manage'));
            DELETE FROM permissions WHERE code IN ('LineGroup.View', 'LineGroup.Manage');
            DELETE FROM line_group_event_subscriptions
              WHERE event_type IN ('Repair.Submitted','Repair.Resubmitted','Repair.Reopened','Repair.Started',
                'Repair.Resumed','Repair.Solved','Repair.Closed','MeetingRoom.BookingCreated');
            """);
    }
}
