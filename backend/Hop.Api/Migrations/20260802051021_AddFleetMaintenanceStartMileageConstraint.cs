using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFleetMaintenanceStartMileageConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "ck_fleet_maintenance_record_start_mileage",
                table: "fleet_vehicle_maintenance_records",
                sql: "start_mileage IS NULL OR start_mileage >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_fleet_maintenance_record_start_mileage",
                table: "fleet_vehicle_maintenance_records");
        }
    }
}
