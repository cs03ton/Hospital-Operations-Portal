using Hop.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260625090000_AddFiscalYearLeaveBalance")]
    public partial class AddFiscalYearLeaveBalance : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "allow_carry_over",
                table: "leave_types",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "carry_over_max_days",
                table: "leave_types",
                type: "numeric",
                nullable: false,
                defaultValue: 30m);

            migrationBuilder.AddColumn<bool>(
                name: "use_fiscal_year",
                table: "leave_types",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "carried_over_days",
                table: "leave_balances",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "allow_carry_over", table: "leave_types");
            migrationBuilder.DropColumn(name: "carry_over_max_days", table: "leave_types");
            migrationBuilder.DropColumn(name: "use_fiscal_year", table: "leave_types");
            migrationBuilder.DropColumn(name: "carried_over_days", table: "leave_balances");
        }
    }
}
