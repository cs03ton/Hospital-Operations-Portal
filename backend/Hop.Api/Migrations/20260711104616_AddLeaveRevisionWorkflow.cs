using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveRevisionWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "last_resubmitted_at",
                table: "leave_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "returned_for_revision_at",
                table: "leave_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "returned_for_revision_by_user_id",
                table: "leave_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "revision_count",
                table: "leave_requests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "revision_reason",
                table: "leave_requests",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "return_reason",
                table: "leave_approvals",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "returned_at",
                table: "leave_approvals",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_leave_requests_returned_for_revision_by_user_id",
                table: "leave_requests",
                column: "returned_for_revision_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_leave_requests_users_returned_for_revision_by_user_id",
                table: "leave_requests",
                column: "returned_for_revision_by_user_id",
                principalTable: "users",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_leave_requests_users_returned_for_revision_by_user_id",
                table: "leave_requests");

            migrationBuilder.DropIndex(
                name: "IX_leave_requests_returned_for_revision_by_user_id",
                table: "leave_requests");

            migrationBuilder.DropColumn(
                name: "last_resubmitted_at",
                table: "leave_requests");

            migrationBuilder.DropColumn(
                name: "returned_for_revision_at",
                table: "leave_requests");

            migrationBuilder.DropColumn(
                name: "returned_for_revision_by_user_id",
                table: "leave_requests");

            migrationBuilder.DropColumn(
                name: "revision_count",
                table: "leave_requests");

            migrationBuilder.DropColumn(
                name: "revision_reason",
                table: "leave_requests");

            migrationBuilder.DropColumn(
                name: "return_reason",
                table: "leave_approvals");

            migrationBuilder.DropColumn(
                name: "returned_at",
                table: "leave_approvals");
        }
    }
}
