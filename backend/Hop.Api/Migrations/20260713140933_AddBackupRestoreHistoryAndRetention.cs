using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBackupRestoreHistoryAndRetention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "backup_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    backup_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    file_name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    file_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    checksum = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    duration_ms = table.Column<long>(type: "bigint", nullable: true),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    verified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_backup_runs", x => x.id);
                    table.ForeignKey(
                        name: "FK_backup_runs_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_backup_runs_users_deleted_by_user_id",
                        column: x => x.deleted_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_backup_runs_users_verified_by_user_id",
                        column: x => x.verified_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "restore_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    backup_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    restore_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    target_environment = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    target_database = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    duration_ms = table.Column<long>(type: "bigint", nullable: true),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    confirmation_method = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    pre_restore_backup_run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_restore_runs", x => x.id);
                    table.ForeignKey(
                        name: "FK_restore_runs_backup_runs_backup_run_id",
                        column: x => x.backup_run_id,
                        principalTable: "backup_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_restore_runs_backup_runs_pre_restore_backup_run_id",
                        column: x => x.pre_restore_backup_run_id,
                        principalTable: "backup_runs",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_restore_runs_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_backup_runs_backup_type_status_started_at",
                table: "backup_runs",
                columns: new[] { "backup_type", "status", "started_at" });

            migrationBuilder.CreateIndex(
                name: "IX_backup_runs_created_by_user_id",
                table: "backup_runs",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_backup_runs_deleted_by_user_id",
                table: "backup_runs",
                column: "deleted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_backup_runs_file_path",
                table: "backup_runs",
                column: "file_path",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_backup_runs_verified_by_user_id",
                table: "backup_runs",
                column: "verified_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_restore_runs_backup_run_id_status_started_at",
                table: "restore_runs",
                columns: new[] { "backup_run_id", "status", "started_at" });

            migrationBuilder.CreateIndex(
                name: "IX_restore_runs_created_by_user_id",
                table: "restore_runs",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_restore_runs_pre_restore_backup_run_id",
                table: "restore_runs",
                column: "pre_restore_backup_run_id");

            migrationBuilder.Sql("""
                INSERT INTO permissions (id, code, name, group_name, action, is_active, created_at)
                SELECT gen_random_uuid(), item.code, item.name, item.group_name, item.action, true, NOW()
                FROM (VALUES
                    ('System.Backup.View', 'ดู Backup Center', 'SystemBackup', 'View'),
                    ('System.Backup.Run', 'ตรวจสอบและบันทึก Backup', 'SystemBackup', 'Run'),
                    ('System.Backup.Restore', 'ดำเนินการ Restore Backup', 'SystemBackup', 'Restore'),
                    ('System.Backup.ManageRetention', 'จัดการ Retention Backup', 'SystemBackup', 'ManageRetention')
                ) AS item(code, name, group_name, action)
                WHERE NOT EXISTS (
                    SELECT 1 FROM permissions p WHERE p.code = item.code
                );

                INSERT INTO role_permissions (role_id, permission_id)
                SELECT r.id, p.id
                FROM roles r
                CROSS JOIN permissions p
                WHERE r.name = 'SuperAdmin'
                  AND p.code IN ('System.Backup.View', 'System.Backup.Run', 'System.Backup.Restore', 'System.Backup.ManageRetention')
                  AND NOT EXISTS (
                      SELECT 1
                      FROM role_permissions rp
                      WHERE rp.role_id = r.id AND rp.permission_id = p.id
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM role_permissions rp
                USING permissions p
                WHERE rp.permission_id = p.id
                  AND p.code IN ('System.Backup.View', 'System.Backup.Run', 'System.Backup.Restore', 'System.Backup.ManageRetention');

                DELETE FROM permissions
                WHERE code IN ('System.Backup.View', 'System.Backup.Run', 'System.Backup.Restore', 'System.Backup.ManageRetention');
                """);

            migrationBuilder.DropTable(
                name: "restore_runs");

            migrationBuilder.DropTable(
                name: "backup_runs");
        }
    }
}
