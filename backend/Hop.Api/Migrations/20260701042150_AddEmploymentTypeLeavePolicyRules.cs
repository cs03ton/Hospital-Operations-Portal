using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEmploymentTypeLeavePolicyRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "employment_start_date",
                table: "users",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "employment_type",
                table: "users",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "leave_policy_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employment_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    leave_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiscal_year = table.Column<int>(type: "integer", nullable: true),
                    entitlement_days = table.Column<decimal>(type: "numeric", nullable: false),
                    max_paid_days = table.Column<decimal>(type: "numeric", nullable: true),
                    allow_carry_over = table.Column<bool>(type: "boolean", nullable: false),
                    carry_over_max_days = table.Column<decimal>(type: "numeric", nullable: true),
                    max_accumulated_days = table.Column<decimal>(type: "numeric", nullable: true),
                    min_service_months = table.Column<int>(type: "integer", nullable: true),
                    min_service_years = table.Column<int>(type: "integer", nullable: true),
                    prorate_if_service_less_than_year = table.Column<bool>(type: "boolean", nullable: false),
                    first_year_entitlement_days = table.Column<decimal>(type: "numeric", nullable: true),
                    first_year_paid_days = table.Column<decimal>(type: "numeric", nullable: true),
                    is_paid = table.Column<bool>(type: "boolean", nullable: false),
                    max_extended_days = table.Column<decimal>(type: "numeric", nullable: true),
                    social_security_max_days = table.Column<decimal>(type: "numeric", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_policy_rules", x => x.id);
                    table.ForeignKey(
                        name: "FK_leave_policy_rules_leave_types_leave_type_id",
                        column: x => x.leave_type_id,
                        principalTable: "leave_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_employment_type",
                table: "users",
                column: "employment_type");

            migrationBuilder.CreateIndex(
                name: "IX_leave_policy_rules_employment_type_leave_type_id_fiscal_yea~",
                table: "leave_policy_rules",
                columns: new[] { "employment_type", "leave_type_id", "fiscal_year", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_leave_policy_rules_leave_type_id",
                table: "leave_policy_rules",
                column: "leave_type_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "leave_policy_rules");

            migrationBuilder.DropIndex(
                name: "IX_users_employment_type",
                table: "users");

            migrationBuilder.DropColumn(
                name: "employment_start_date",
                table: "users");

            migrationBuilder.DropColumn(
                name: "employment_type",
                table: "users");
        }
    }
}
