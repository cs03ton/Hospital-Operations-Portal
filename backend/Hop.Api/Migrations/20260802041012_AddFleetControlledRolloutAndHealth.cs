using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFleetControlledRolloutAndHealth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fleet_rollout_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    uat_user_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    uat_role_codes = table.Column<string[]>(type: "text[]", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_token = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fleet_rollout_settings", x => x.id);
                    table.CheckConstraint("ck_fleet_rollout_mode", "mode IN ('Disabled', 'UATOnly', 'Enabled')");
                    table.ForeignKey(
                        name: "FK_fleet_rollout_settings_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fleet_rollout_settings_created_at",
                table: "fleet_rollout_settings",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_rollout_settings_updated_by_user_id",
                table: "fleet_rollout_settings",
                column: "updated_by_user_id");

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX ux_fleet_rollout_settings_singleton ON fleet_rollout_settings ((true));

                INSERT INTO permissions (id, code, name, group_name, action, is_active, created_at)
                SELECT gen_random_uuid(), item.code, item.name, 'FleetHealth', item.action, true, NOW()
                FROM (VALUES
                    ('FleetHealth.View', 'ดู Fleet Health Center', 'View'),
                    ('FleetHealth.Manage', 'จัดการ Fleet Health และ Rollout', 'Manage')
                ) AS item(code, name, action)
                WHERE NOT EXISTS (SELECT 1 FROM permissions p WHERE p.code = item.code);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM permissions p
                WHERE p.code IN ('FleetHealth.View', 'FleetHealth.Manage')
                  AND NOT EXISTS (SELECT 1 FROM role_permissions rp WHERE rp.permission_id = p.id);
                """);
            migrationBuilder.DropTable(
                name: "fleet_rollout_settings");
        }
    }
}
