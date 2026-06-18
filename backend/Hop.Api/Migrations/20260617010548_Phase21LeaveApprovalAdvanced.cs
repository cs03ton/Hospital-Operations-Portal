using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase21LeaveApprovalAdvanced : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "approval_chain_id",
                table: "leave_approvals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "approval_chain_step_id",
                table: "leave_approvals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "required_permission_code",
                table: "leave_approvals",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "step_name",
                table: "leave_approvals",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "approval_chains",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    department_id = table.Column<Guid>(type: "uuid", nullable: true),
                    leave_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    minimum_days = table.Column<decimal>(type: "numeric", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_approval_chains", x => x.id);
                    table.ForeignKey(
                        name: "FK_approval_chains_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_approval_chains_leave_types_leave_type_id",
                        column: x => x.leave_type_id,
                        principalTable: "leave_types",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "leave_balance_adjustments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leave_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    adjustment_days = table.Column<decimal>(type: "numeric", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    adjusted_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_balance_adjustments", x => x.id);
                    table.ForeignKey(
                        name: "FK_leave_balance_adjustments_leave_types_leave_type_id",
                        column: x => x.leave_type_id,
                        principalTable: "leave_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_leave_balance_adjustments_users_adjusted_by_user_id",
                        column: x => x.adjusted_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_leave_balance_adjustments_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "leave_holidays",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    holiday_date = table.Column<DateOnly>(type: "date", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_holidays", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "line_delivery_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    leave_request_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recipient_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_name = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    payload = table.Column<string>(type: "text", nullable: false),
                    response_detail = table.Column<string>(type: "text", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    next_retry_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_line_delivery_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_line_delivery_logs_leave_requests_leave_request_id",
                        column: x => x.leave_request_id,
                        principalTable: "leave_requests",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_line_delivery_logs_users_recipient_user_id",
                        column: x => x.recipient_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "approval_chain_steps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    approval_chain_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_order = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    approver_role_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approver_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    required_permission_code = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_approval_chain_steps", x => x.id);
                    table.ForeignKey(
                        name: "FK_approval_chain_steps_approval_chains_approval_chain_id",
                        column: x => x.approval_chain_id,
                        principalTable: "approval_chains",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_approval_chain_steps_roles_approver_role_id",
                        column: x => x.approver_role_id,
                        principalTable: "roles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_approval_chain_steps_users_approver_user_id",
                        column: x => x.approver_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_leave_approvals_approval_chain_id",
                table: "leave_approvals",
                column: "approval_chain_id");

            migrationBuilder.CreateIndex(
                name: "IX_leave_approvals_approval_chain_step_id",
                table: "leave_approvals",
                column: "approval_chain_step_id");

            migrationBuilder.CreateIndex(
                name: "IX_approval_chain_steps_approval_chain_id_step_order",
                table: "approval_chain_steps",
                columns: new[] { "approval_chain_id", "step_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_approval_chain_steps_approver_role_id",
                table: "approval_chain_steps",
                column: "approver_role_id");

            migrationBuilder.CreateIndex(
                name: "IX_approval_chain_steps_approver_user_id",
                table: "approval_chain_steps",
                column: "approver_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_approval_chains_department_id",
                table: "approval_chains",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_approval_chains_leave_type_id",
                table: "approval_chains",
                column: "leave_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_approval_chains_name",
                table: "approval_chains",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_leave_balance_adjustments_adjusted_by_user_id",
                table: "leave_balance_adjustments",
                column: "adjusted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_leave_balance_adjustments_leave_type_id",
                table: "leave_balance_adjustments",
                column: "leave_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_leave_balance_adjustments_user_id_leave_type_id_year",
                table: "leave_balance_adjustments",
                columns: new[] { "user_id", "leave_type_id", "year" });

            migrationBuilder.CreateIndex(
                name: "IX_leave_holidays_holiday_date",
                table: "leave_holidays",
                column: "holiday_date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_line_delivery_logs_leave_request_id",
                table: "line_delivery_logs",
                column: "leave_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_line_delivery_logs_recipient_user_id",
                table: "line_delivery_logs",
                column: "recipient_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_line_delivery_logs_status_next_retry_at",
                table: "line_delivery_logs",
                columns: new[] { "status", "next_retry_at" });

            migrationBuilder.AddForeignKey(
                name: "FK_leave_approvals_approval_chain_steps_approval_chain_step_id",
                table: "leave_approvals",
                column: "approval_chain_step_id",
                principalTable: "approval_chain_steps",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_leave_approvals_approval_chains_approval_chain_id",
                table: "leave_approvals",
                column: "approval_chain_id",
                principalTable: "approval_chains",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_leave_approvals_approval_chain_steps_approval_chain_step_id",
                table: "leave_approvals");

            migrationBuilder.DropForeignKey(
                name: "FK_leave_approvals_approval_chains_approval_chain_id",
                table: "leave_approvals");

            migrationBuilder.DropTable(
                name: "approval_chain_steps");

            migrationBuilder.DropTable(
                name: "leave_balance_adjustments");

            migrationBuilder.DropTable(
                name: "leave_holidays");

            migrationBuilder.DropTable(
                name: "line_delivery_logs");

            migrationBuilder.DropTable(
                name: "approval_chains");

            migrationBuilder.DropIndex(
                name: "IX_leave_approvals_approval_chain_id",
                table: "leave_approvals");

            migrationBuilder.DropIndex(
                name: "IX_leave_approvals_approval_chain_step_id",
                table: "leave_approvals");

            migrationBuilder.DropColumn(
                name: "approval_chain_id",
                table: "leave_approvals");

            migrationBuilder.DropColumn(
                name: "approval_chain_step_id",
                table: "leave_approvals");

            migrationBuilder.DropColumn(
                name: "required_permission_code",
                table: "leave_approvals");

            migrationBuilder.DropColumn(
                name: "step_name",
                table: "leave_approvals");
        }
    }
}
