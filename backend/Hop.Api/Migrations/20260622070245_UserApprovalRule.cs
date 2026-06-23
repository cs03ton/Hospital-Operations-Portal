using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class UserApprovalRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "leave_approval_rule_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_leave_approval_rule_id",
                table: "users",
                column: "leave_approval_rule_id");

            migrationBuilder.AddForeignKey(
                name: "FK_users_approval_chains_leave_approval_rule_id",
                table: "users",
                column: "leave_approval_rule_id",
                principalTable: "approval_chains",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_approval_chains_leave_approval_rule_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_leave_approval_rule_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "leave_approval_rule_id",
                table: "users");
        }
    }
}
