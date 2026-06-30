using Hop.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260629090000_AddRoleBasedNotifications")]
    public partial class AddRoleBasedNotifications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "action_url",
                table: "notifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "archived_at",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "notifications",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "Leave");

            migrationBuilder.AddColumn<DateTime>(
                name: "expires_at",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "notification_type",
                table: "notifications",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Information");

            migrationBuilder.AddColumn<string>(
                name: "priority",
                table: "notifications",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Information");

            migrationBuilder.AddColumn<DateTime>(
                name: "read_at",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reference_entity",
                table: "notifications",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reference_id",
                table: "notifications",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "target_role",
                table: "notifications",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_expires_at",
                table: "notifications",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_target_role_category",
                table: "notifications",
                columns: new[] { "target_role", "category" });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_user_id_is_read_notification_type",
                table: "notifications",
                columns: new[] { "user_id", "is_read", "notification_type" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_notifications_expires_at", table: "notifications");
            migrationBuilder.DropIndex(name: "IX_notifications_target_role_category", table: "notifications");
            migrationBuilder.DropIndex(name: "IX_notifications_user_id_is_read_notification_type", table: "notifications");

            migrationBuilder.DropColumn(name: "action_url", table: "notifications");
            migrationBuilder.DropColumn(name: "archived_at", table: "notifications");
            migrationBuilder.DropColumn(name: "category", table: "notifications");
            migrationBuilder.DropColumn(name: "expires_at", table: "notifications");
            migrationBuilder.DropColumn(name: "notification_type", table: "notifications");
            migrationBuilder.DropColumn(name: "priority", table: "notifications");
            migrationBuilder.DropColumn(name: "read_at", table: "notifications");
            migrationBuilder.DropColumn(name: "reference_entity", table: "notifications");
            migrationBuilder.DropColumn(name: "reference_id", table: "notifications");
            migrationBuilder.DropColumn(name: "target_role", table: "notifications");
        }
    }
}
