using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class LeaveOperationsReliability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "approval_delegations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    approver_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    delegate_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_approval_delegations", x => x.id);
                    table.ForeignKey(
                        name: "FK_approval_delegations_users_approver_user_id",
                        column: x => x.approver_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_approval_delegations_users_delegate_user_id",
                        column: x => x.delegate_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "approval_escalation_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: true),
                    leave_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    escalate_after_hours = table.Column<int>(type: "integer", nullable: false),
                    escalate_to_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    escalate_to_role_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_approval_escalation_rules", x => x.id);
                    table.ForeignKey(
                        name: "FK_approval_escalation_rules_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_approval_escalation_rules_leave_types_leave_type_id",
                        column: x => x.leave_type_id,
                        principalTable: "leave_types",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_approval_escalation_rules_roles_escalate_to_role_id",
                        column: x => x.escalate_to_role_id,
                        principalTable: "roles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_approval_escalation_rules_users_escalate_to_user_id",
                        column: x => x.escalate_to_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_approval_delegations_approver_user_id_start_date_end_date",
                table: "approval_delegations",
                columns: new[] { "approver_user_id", "start_date", "end_date" });

            migrationBuilder.CreateIndex(
                name: "IX_approval_delegations_delegate_user_id",
                table: "approval_delegations",
                column: "delegate_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_approval_escalation_rules_department_id_leave_type_id_is_ac~",
                table: "approval_escalation_rules",
                columns: new[] { "department_id", "leave_type_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_approval_escalation_rules_escalate_to_role_id",
                table: "approval_escalation_rules",
                column: "escalate_to_role_id");

            migrationBuilder.CreateIndex(
                name: "IX_approval_escalation_rules_escalate_to_user_id",
                table: "approval_escalation_rules",
                column: "escalate_to_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_approval_escalation_rules_leave_type_id",
                table: "approval_escalation_rules",
                column: "leave_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_approval_escalation_rules_name",
                table: "approval_escalation_rules",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "approval_delegations");

            migrationBuilder.DropTable(
                name: "approval_escalation_rules");
        }
    }
}
