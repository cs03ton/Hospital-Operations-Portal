using System;
using Hop.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260724090000_AddLineLiffSupport")]
public partial class AddLineLiffSupport : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "last_login_at",
            table: "line_user_bindings",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.Sql("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_line_user_bindings_user_id_active_bound"
            ON line_user_bindings (user_id)
            WHERE user_id IS NOT NULL AND status = 'Bound';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS "IX_line_user_bindings_user_id_active_bound";
            """);

        migrationBuilder.DropColumn(
            name: "last_login_at",
            table: "line_user_bindings");
    }
}
