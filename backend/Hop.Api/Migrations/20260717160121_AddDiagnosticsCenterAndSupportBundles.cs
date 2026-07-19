using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDiagnosticsCenterAndSupportBundles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO permissions (id, code, name, group_name, action, is_active, created_at)
                SELECT gen_random_uuid(), item.code, item.name, 'SystemDiagnostics', item.action, true, NOW()
                FROM (VALUES
                    ('System.Diagnostics.View', 'ดู Diagnostics Center', 'View'),
                    ('System.Diagnostics.Run', 'รัน Diagnostics Test', 'Run'),
                    ('System.Diagnostics.Export', 'สร้างและดาวน์โหลด Support Bundle', 'Export')
                ) AS item(code, name, action)
                WHERE NOT EXISTS (
                    SELECT 1 FROM permissions p WHERE p.code = item.code
                );

                INSERT INTO role_permissions (role_id, permission_id)
                SELECT r.id, p.id
                FROM roles r
                CROSS JOIN permissions p
                WHERE r.name IN ('Admin', 'SuperAdmin')
                  AND p.code IN ('System.Diagnostics.View', 'System.Diagnostics.Run', 'System.Diagnostics.Export')
                  AND NOT EXISTS (
                      SELECT 1 FROM role_permissions rp
                      WHERE rp.role_id = r.id AND rp.permission_id = p.id
                  );
                """);

            migrationBuilder.CreateTable(
                name: "diagnostic_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    diagnostic_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    duration_ms = table.Column<long>(type: "bigint", nullable: true),
                    result_summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    reference_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_diagnostic_runs", x => x.id);
                    table.ForeignKey(
                        name: "FK_diagnostic_runs_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "support_bundles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    file_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    checksum = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    downloaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_support_bundles", x => x.id);
                    table.ForeignKey(
                        name: "FK_support_bundles_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_diagnostic_runs_created_by_user_id",
                table: "diagnostic_runs",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_diagnostic_runs_diagnostic_type_started_at",
                table: "diagnostic_runs",
                columns: new[] { "diagnostic_type", "started_at" });

            migrationBuilder.CreateIndex(
                name: "IX_diagnostic_runs_reference_id",
                table: "diagnostic_runs",
                column: "reference_id");

            migrationBuilder.CreateIndex(
                name: "IX_support_bundles_created_at",
                table: "support_bundles",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_support_bundles_created_by_user_id",
                table: "support_bundles",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_support_bundles_status_expires_at",
                table: "support_bundles",
                columns: new[] { "status", "expires_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "diagnostic_runs");

            migrationBuilder.DropTable(
                name: "support_bundles");

            migrationBuilder.Sql("""
                DELETE FROM role_permissions rp
                USING permissions p
                WHERE rp.permission_id = p.id
                  AND p.code IN ('System.Diagnostics.View', 'System.Diagnostics.Run', 'System.Diagnostics.Export');

                DELETE FROM permissions
                WHERE code IN ('System.Diagnostics.View', 'System.Diagnostics.Run', 'System.Diagnostics.Export');
                """);
        }
    }
}
