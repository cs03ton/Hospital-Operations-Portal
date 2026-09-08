using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFleetFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fleet_driver_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    license_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    license_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    license_issue_date = table.Column<DateOnly>(type: "date", nullable: true),
                    license_expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    can_drive_sedan = table.Column<bool>(type: "boolean", nullable: false),
                    can_drive_pickup = table.Column<bool>(type: "boolean", nullable: false),
                    can_drive_van = table.Column<bool>(type: "boolean", nullable: false),
                    can_drive_ambulance = table.Column<bool>(type: "boolean", nullable: false),
                    can_drive_other = table.Column<bool>(type: "boolean", nullable: false),
                    driver_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fleet_driver_profiles", x => x.id);
                    table.ForeignKey(
                        name: "FK_fleet_driver_profiles_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_driver_profiles_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_driver_profiles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fleet_driver_unavailability",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fleet_driver_unavailability", x => x.id);
                    table.CheckConstraint("ck_fleet_driver_unavailability_range", "end_at > start_at");
                    table.ForeignKey(
                        name: "FK_fleet_driver_unavailability_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_driver_unavailability_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fleet_vehicle_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fleet_vehicle_types", x => x.id);
                    table.CheckConstraint("ck_fleet_vehicle_types_sort_order", "sort_order >= 0");
                });

            migrationBuilder.CreateTable(
                name: "fleet_vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    registration_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    registration_province = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    vehicle_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    manufacture_year = table.Column<int>(type: "integer", nullable: true),
                    seat_capacity_total = table.Column<int>(type: "integer", nullable: false),
                    passenger_capacity = table.Column<int>(type: "integer", nullable: false),
                    fuel_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    current_mileage = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    owning_department_id = table.Column<Guid>(type: "uuid", nullable: true),
                    responsible_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    image_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fleet_vehicles", x => x.id);
                    table.CheckConstraint("ck_fleet_vehicles_capacity", "seat_capacity_total > 0 AND passenger_capacity > 0 AND passenger_capacity <= seat_capacity_total");
                    table.CheckConstraint("ck_fleet_vehicles_mileage", "current_mileage >= 0");
                    table.ForeignKey(
                        name: "FK_fleet_vehicles_departments_owning_department_id",
                        column: x => x.owning_department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_vehicles_fleet_vehicle_types_vehicle_type_id",
                        column: x => x.vehicle_type_id,
                        principalTable: "fleet_vehicle_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_vehicles_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_vehicles_users_responsible_user_id",
                        column: x => x.responsible_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_vehicles_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fleet_vehicle_unavailability",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fleet_vehicle_unavailability", x => x.id);
                    table.CheckConstraint("ck_fleet_vehicle_unavailability_range", "end_at > start_at");
                    table.ForeignKey(
                        name: "FK_fleet_vehicle_unavailability_fleet_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "fleet_vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_vehicle_unavailability_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fleet_driver_profiles_created_by_user_id",
                table: "fleet_driver_profiles",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_driver_profiles_is_active_driver_status",
                table: "fleet_driver_profiles",
                columns: new[] { "is_active", "driver_status" });

            migrationBuilder.CreateIndex(
                name: "IX_fleet_driver_profiles_updated_by_user_id",
                table: "fleet_driver_profiles",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_driver_profiles_user_id",
                table: "fleet_driver_profiles",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fleet_driver_unavailability_created_by_user_id",
                table: "fleet_driver_unavailability",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_driver_unavailability_user_id_start_at_end_at",
                table: "fleet_driver_unavailability",
                columns: new[] { "user_id", "start_at", "end_at" });

            migrationBuilder.CreateIndex(
                name: "IX_fleet_vehicles_created_by_user_id",
                table: "fleet_vehicles",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_vehicles_is_active_status_vehicle_type_id",
                table: "fleet_vehicles",
                columns: new[] { "is_active", "status", "vehicle_type_id" });

            migrationBuilder.CreateIndex(
                name: "IX_fleet_vehicles_owning_department_id",
                table: "fleet_vehicles",
                column: "owning_department_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_vehicles_registration_number",
                table: "fleet_vehicles",
                column: "registration_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fleet_vehicles_responsible_user_id",
                table: "fleet_vehicles",
                column: "responsible_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_vehicles_updated_by_user_id",
                table: "fleet_vehicles",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_vehicles_vehicle_code",
                table: "fleet_vehicles",
                column: "vehicle_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fleet_vehicles_vehicle_type_id",
                table: "fleet_vehicles",
                column: "vehicle_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_vehicle_types_code",
                table: "fleet_vehicle_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fleet_vehicle_types_is_active_sort_order",
                table: "fleet_vehicle_types",
                columns: new[] { "is_active", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_fleet_vehicle_unavailability_created_by_user_id",
                table: "fleet_vehicle_unavailability",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_vehicle_unavailability_vehicle_id_start_at_end_at",
                table: "fleet_vehicle_unavailability",
                columns: new[] { "vehicle_id", "start_at", "end_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fleet_driver_profiles");

            migrationBuilder.DropTable(
                name: "fleet_driver_unavailability");

            migrationBuilder.DropTable(
                name: "fleet_vehicle_unavailability");

            migrationBuilder.DropTable(
                name: "fleet_vehicles");

            migrationBuilder.DropTable(
                name: "fleet_vehicle_types");
        }
    }
}
