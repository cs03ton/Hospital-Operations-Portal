using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class LeaveWorkflowSessionsAuditRetention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_refresh_tokens_user_id\";");

            migrationBuilder.AddColumn<string>(
                name: "created_by_ip",
                table: "refresh_tokens",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_used_at",
                table: "refresh_tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "replaced_by_token",
                table: "refresh_tokens",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "revoked_reason",
                table: "refresh_tokens",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_agent",
                table: "refresh_tokens",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_user_id_revoked_at",
                table: "refresh_tokens",
                columns: new[] { "user_id", "revoked_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_user_id_revoked_at",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "created_by_ip",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "last_used_at",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "replaced_by_token",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "revoked_reason",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "user_agent",
                table: "refresh_tokens");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");
        }
    }
}
