using System;
using Hop.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260717045456_AddLeaveBalanceTransactions")]
    public partial class AddLeaveBalanceTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "leave_balance_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leave_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiscal_year = table.Column<int>(type: "integer", nullable: false),
                    transaction_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    amount_days = table.Column<decimal>(type: "numeric", nullable: false),
                    reference_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_balance_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_leave_balance_transactions_leave_types_leave_type_id",
                        column: x => x.leave_type_id,
                        principalTable: "leave_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_leave_balance_transactions_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_leave_balance_transactions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_leave_balance_transactions_created_by_user_id",
                table: "leave_balance_transactions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_leave_balance_transactions_leave_type_id",
                table: "leave_balance_transactions",
                column: "leave_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_leave_balance_transactions_reference_type_reference_id_tran~",
                table: "leave_balance_transactions",
                columns: new[] { "reference_type", "reference_id", "transaction_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_leave_balance_transactions_user_id_leave_type_id_fiscal_year",
                table: "leave_balance_transactions",
                columns: new[] { "user_id", "leave_type_id", "fiscal_year" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "leave_balance_transactions");
        }
    }
}
