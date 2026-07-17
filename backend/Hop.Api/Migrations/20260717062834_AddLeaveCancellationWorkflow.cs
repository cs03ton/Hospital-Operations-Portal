using System;
using Hop.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260717062834_AddLeaveCancellationWorkflow")]
    public partial class AddLeaveCancellationWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "leave_cancellation_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cancellation_request_number = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    original_leave_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requester_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leave_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_leave_days = table.Column<decimal>(type: "numeric", nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    approval_chain_id = table.Column<Guid>(type: "uuid", nullable: true),
                    current_approver_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    returned_for_revision_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    returned_for_revision_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    revision_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    revision_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_resubmitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    balance_restored_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_cancellation_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_leave_cancellation_requests_approval_chains_approval_chain_~",
                        column: x => x.approval_chain_id,
                        principalTable: "approval_chains",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_leave_cancellation_requests_leave_requests_original_leave_r~",
                        column: x => x.original_leave_request_id,
                        principalTable: "leave_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_leave_cancellation_requests_leave_types_leave_type_id",
                        column: x => x.leave_type_id,
                        principalTable: "leave_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_leave_cancellation_requests_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_leave_cancellation_requests_users_current_approver_id",
                        column: x => x.current_approver_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_leave_cancellation_requests_users_requester_user_id",
                        column: x => x.requester_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_leave_cancellation_requests_users_returned_for_revision_by_~",
                        column: x => x.returned_for_revision_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "leave_cancellation_approvals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    leave_cancellation_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approver_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approval_chain_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approval_chain_step_id = table.Column<Guid>(type: "uuid", nullable: true),
                    step_order = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    step_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    required_permission_code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    remark = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    action_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    returned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    return_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_cancellation_approvals", x => x.id);
                    table.ForeignKey(
                        name: "FK_leave_cancellation_approvals_approval_chain_steps_approval_~",
                        column: x => x.approval_chain_step_id,
                        principalTable: "approval_chain_steps",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_leave_cancellation_approvals_approval_chains_approval_chain~",
                        column: x => x.approval_chain_id,
                        principalTable: "approval_chains",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_leave_cancellation_approvals_leave_cancellation_requests_le~",
                        column: x => x.leave_cancellation_request_id,
                        principalTable: "leave_cancellation_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_leave_cancellation_approvals_users_approver_id",
                        column: x => x.approver_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_leave_cancellation_approvals_approval_chain_id",
                table: "leave_cancellation_approvals",
                column: "approval_chain_id");

            migrationBuilder.CreateIndex(
                name: "IX_leave_cancellation_approvals_approval_chain_step_id",
                table: "leave_cancellation_approvals",
                column: "approval_chain_step_id");

            migrationBuilder.CreateIndex(
                name: "IX_leave_cancellation_approvals_approver_id",
                table: "leave_cancellation_approvals",
                column: "approver_id");

            migrationBuilder.CreateIndex(
                name: "IX_leave_cancellation_approvals_leave_cancellation_request_id_~",
                table: "leave_cancellation_approvals",
                columns: new[] { "leave_cancellation_request_id", "step_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_leave_cancellation_requests_approval_chain_id",
                table: "leave_cancellation_requests",
                column: "approval_chain_id");

            migrationBuilder.CreateIndex(
                name: "IX_leave_cancellation_requests_cancellation_request_number",
                table: "leave_cancellation_requests",
                column: "cancellation_request_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_leave_cancellation_requests_created_by_user_id",
                table: "leave_cancellation_requests",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_leave_cancellation_requests_current_approver_id",
                table: "leave_cancellation_requests",
                column: "current_approver_id");

            migrationBuilder.CreateIndex(
                name: "IX_leave_cancellation_requests_leave_type_id",
                table: "leave_cancellation_requests",
                column: "leave_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_leave_cancellation_requests_original_leave_request_id",
                table: "leave_cancellation_requests",
                column: "original_leave_request_id",
                unique: true,
                filter: "\"status\" IN ('Draft', 'Pending', 'ReturnedForRevision')");

            migrationBuilder.CreateIndex(
                name: "IX_leave_cancellation_requests_requester_user_id_status",
                table: "leave_cancellation_requests",
                columns: new[] { "requester_user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_leave_cancellation_requests_returned_for_revision_by_user_id",
                table: "leave_cancellation_requests",
                column: "returned_for_revision_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_leave_cancellation_requests_status",
                table: "leave_cancellation_requests",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "leave_cancellation_approvals");

            migrationBuilder.DropTable(
                name: "leave_cancellation_requests");
        }
    }
}
