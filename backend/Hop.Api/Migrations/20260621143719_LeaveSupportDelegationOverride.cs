using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class LeaveSupportDelegationOverride : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "cancelled_at",
                table: "approval_delegations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "approval_delegations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "approval_override_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    leave_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_approver_id = table.Column<Guid>(type: "uuid", nullable: true),
                    override_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "text", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_approval_override_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_approval_override_logs_leave_requests_leave_request_id",
                        column: x => x.leave_request_id,
                        principalTable: "leave_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_approval_override_logs_users_original_approver_id",
                        column: x => x.original_approver_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_approval_override_logs_users_override_by_user_id",
                        column: x => x.override_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_approval_delegations_created_by",
                table: "approval_delegations",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_approval_override_logs_leave_request_id",
                table: "approval_override_logs",
                column: "leave_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_approval_override_logs_original_approver_id",
                table: "approval_override_logs",
                column: "original_approver_id");

            migrationBuilder.CreateIndex(
                name: "IX_approval_override_logs_override_by_user_id",
                table: "approval_override_logs",
                column: "override_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_approval_delegations_users_created_by",
                table: "approval_delegations",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_approval_delegations_users_created_by",
                table: "approval_delegations");

            migrationBuilder.DropTable(
                name: "approval_override_logs");

            migrationBuilder.DropIndex(
                name: "IX_approval_delegations_created_by",
                table: "approval_delegations");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                table: "approval_delegations");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "approval_delegations");
        }
    }
}
