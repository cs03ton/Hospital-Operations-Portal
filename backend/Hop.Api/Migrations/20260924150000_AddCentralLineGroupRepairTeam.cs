using Hop.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Hop.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260924150000_AddCentralLineGroupRepairTeam")]
public sealed class AddCentralLineGroupRepairTeam : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        ALTER TABLE line_group_destinations ADD COLUMN repair_team_code varchar(20);
        ALTER TABLE line_group_destinations ADD COLUMN repair_team_assigned_at timestamptz;
        ALTER TABLE line_group_destinations ADD CONSTRAINT ck_line_group_repair_team
            CHECK ((repair_team_code IS NULL AND repair_team_assigned_at IS NULL)
                OR (repair_team_code IN ('IT', 'GENERAL') AND repair_team_assigned_at IS NOT NULL));
        CREATE INDEX ix_line_group_destinations_repair_team_code
            ON line_group_destinations(repair_team_code) WHERE repair_team_code IS NOT NULL;
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DROP INDEX ix_line_group_destinations_repair_team_code;
        ALTER TABLE line_group_destinations DROP CONSTRAINT ck_line_group_repair_team;
        ALTER TABLE line_group_destinations DROP COLUMN repair_team_code;
        ALTER TABLE line_group_destinations DROP COLUMN repair_team_assigned_at;
        """);
}
