using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAnnouncementNotificationChannels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "line_notification_queued_at",
                table: "announcements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "notification_config_version",
                table: "announcements",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "notification_dispatch_error",
                table: "announcements",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "notification_dispatch_status",
                table: "announcements",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "notification_sent_at",
                table: "announcements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "notify_in_app",
                table: "announcements",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "notify_via_line",
                table: "announcements",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "announcement_notification_deliveries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    announcement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    queued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    last_error_code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    last_error_message_sanitized = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    notification_id = table.Column<Guid>(type: "uuid", nullable: true),
                    line_queue_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_announcement_notification_deliveries", x => x.id);
                    table.ForeignKey(
                        name: "FK_announcement_notification_deliveries_announcements_announce~",
                        column: x => x.announcement_id,
                        principalTable: "announcements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_announcement_notification_deliveries_line_delivery_logs_lin~",
                        column: x => x.line_queue_id,
                        principalTable: "line_delivery_logs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_announcement_notification_deliveries_notifications_notifica~",
                        column: x => x.notification_id,
                        principalTable: "notifications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_announcement_notification_deliveries_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_announcements_notify_in_app_notify_via_line",
                table: "announcements",
                columns: new[] { "notify_in_app", "notify_via_line" });

            migrationBuilder.CreateIndex(
                name: "IX_announcement_notification_deliveries_announcement_id_channe~",
                table: "announcement_notification_deliveries",
                columns: new[] { "announcement_id", "channel", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_announcement_notification_deliveries_idempotency_key",
                table: "announcement_notification_deliveries",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_announcement_notification_deliveries_line_queue_id",
                table: "announcement_notification_deliveries",
                column: "line_queue_id");

            migrationBuilder.CreateIndex(
                name: "IX_announcement_notification_deliveries_notification_id",
                table: "announcement_notification_deliveries",
                column: "notification_id");

            migrationBuilder.CreateIndex(
                name: "IX_announcement_notification_deliveries_user_id_channel",
                table: "announcement_notification_deliveries",
                columns: new[] { "user_id", "channel" });

            migrationBuilder.Sql("""
                INSERT INTO permissions (id, code, name, group_name, action, is_active, created_at)
                VALUES
                    (gen_random_uuid(), 'Announcement.Notification.Configure', 'กำหนดช่องทางแจ้งเตือนประกาศ', 'AnnouncementNotification', 'Configure', true, NOW()),
                    (gen_random_uuid(), 'Announcement.Notification.Preview', 'ดูตัวอย่างผู้รับแจ้งเตือนประกาศ', 'AnnouncementNotification', 'Preview', true, NOW()),
                    (gen_random_uuid(), 'Announcement.Notification.SendInApp', 'ส่ง Notification Bell สำหรับประกาศ', 'AnnouncementNotification', 'SendInApp', true, NOW()),
                    (gen_random_uuid(), 'Announcement.Notification.SendLine', 'ส่ง LINE สำหรับประกาศ', 'AnnouncementNotification', 'SendLine', true, NOW()),
                    (gen_random_uuid(), 'Announcement.Notification.ViewDelivery', 'ดูสถานะการส่งแจ้งเตือนประกาศ', 'AnnouncementNotification', 'ViewDelivery', true, NOW()),
                    (gen_random_uuid(), 'Announcement.Notification.RetryFailed', 'ส่งแจ้งเตือนประกาศที่ล้มเหลวซ้ำ', 'AnnouncementNotification', 'RetryFailed', true, NOW())
                ON CONFLICT (code) DO UPDATE
                SET name = EXCLUDED.name,
                    group_name = EXCLUDED.group_name,
                    action = EXCLUDED.action,
                    is_active = true;

                INSERT INTO role_permissions (role_id, permission_id)
                SELECT roles.id, permissions.id
                FROM roles
                CROSS JOIN permissions
                WHERE roles.name IN ('Admin', 'SuperAdmin', 'LeaveAdmin')
                  AND permissions.code IN (
                    'Announcement.Notification.Configure',
                    'Announcement.Notification.Preview',
                    'Announcement.Notification.SendInApp',
                    'Announcement.Notification.SendLine',
                    'Announcement.Notification.ViewDelivery',
                    'Announcement.Notification.RetryFailed'
                  )
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "announcement_notification_deliveries");

            migrationBuilder.DropIndex(
                name: "IX_announcements_notify_in_app_notify_via_line",
                table: "announcements");

            migrationBuilder.DropColumn(
                name: "line_notification_queued_at",
                table: "announcements");

            migrationBuilder.DropColumn(
                name: "notification_config_version",
                table: "announcements");

            migrationBuilder.DropColumn(
                name: "notification_dispatch_error",
                table: "announcements");

            migrationBuilder.DropColumn(
                name: "notification_dispatch_status",
                table: "announcements");

            migrationBuilder.DropColumn(
                name: "notification_sent_at",
                table: "announcements");

            migrationBuilder.DropColumn(
                name: "notify_in_app",
                table: "announcements");

            migrationBuilder.DropColumn(
                name: "notify_via_line",
                table: "announcements");

            migrationBuilder.Sql("""
                DELETE FROM role_permissions
                WHERE permission_id IN (
                    SELECT id FROM permissions
                    WHERE code IN (
                        'Announcement.Notification.Configure',
                        'Announcement.Notification.Preview',
                        'Announcement.Notification.SendInApp',
                        'Announcement.Notification.SendLine',
                        'Announcement.Notification.ViewDelivery',
                        'Announcement.Notification.RetryFailed'
                    )
                );

                DELETE FROM permissions
                WHERE code IN (
                    'Announcement.Notification.Configure',
                    'Announcement.Notification.Preview',
                    'Announcement.Notification.SendInApp',
                    'Announcement.Notification.SendLine',
                    'Announcement.Notification.ViewDelivery',
                    'Announcement.Notification.RetryFailed'
                );
                """);
        }
    }
}
