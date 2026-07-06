using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionReadyLeaveRollover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "leave_balance_rollover_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_fiscal_year = table.Column<int>(type: "integer", nullable: false),
                    to_fiscal_year = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    filters_json = table.Column<string>(type: "text", nullable: true),
                    total = table.Column<int>(type: "integer", nullable: false),
                    created_count = table.Column<int>(type: "integer", nullable: false),
                    updated_count = table.Column<int>(type: "integer", nullable: false),
                    skipped_count = table.Column<int>(type: "integer", nullable: false),
                    blocked_count = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_balance_rollover_runs", x => x.id);
                    table.ForeignKey(
                        name: "FK_leave_balance_rollover_runs_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "leave_balance_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rollover_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leave_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiscal_year = table.Column<int>(type: "integer", nullable: false),
                    entitlement_days = table.Column<decimal>(type: "numeric", nullable: false),
                    carried_over_days = table.Column<decimal>(type: "numeric", nullable: false),
                    adjusted_days = table.Column<decimal>(type: "numeric", nullable: false),
                    used_days = table.Column<decimal>(type: "numeric", nullable: false),
                    pending_days = table.Column<decimal>(type: "numeric", nullable: false),
                    available_days = table.Column<decimal>(type: "numeric", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_balance_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_leave_balance_snapshots_leave_balance_rollover_runs_rollove~",
                        column: x => x.rollover_run_id,
                        principalTable: "leave_balance_rollover_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_leave_balance_snapshots_leave_types_leave_type_id",
                        column: x => x.leave_type_id,
                        principalTable: "leave_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_leave_balance_snapshots_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_leave_balance_snapshots_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_leave_balance_rollover_runs_created_by_user_id",
                table: "leave_balance_rollover_runs",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_leave_balance_rollover_runs_from_fiscal_year_to_fiscal_year~",
                table: "leave_balance_rollover_runs",
                columns: new[] { "from_fiscal_year", "to_fiscal_year", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_leave_balance_snapshots_created_by_user_id",
                table: "leave_balance_snapshots",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_leave_balance_snapshots_leave_type_id",
                table: "leave_balance_snapshots",
                column: "leave_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_leave_balance_snapshots_rollover_run_id",
                table: "leave_balance_snapshots",
                column: "rollover_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_leave_balance_snapshots_user_id_leave_type_id_fiscal_year",
                table: "leave_balance_snapshots",
                columns: new[] { "user_id", "leave_type_id", "fiscal_year" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "leave_balance_snapshots");

            migrationBuilder.DropTable(
                name: "leave_balance_rollover_runs");
        }
    }
}
