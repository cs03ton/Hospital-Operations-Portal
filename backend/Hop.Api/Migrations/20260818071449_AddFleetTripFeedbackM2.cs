using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFleetTripFeedbackM2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fleet_trip_feedbacks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fleet_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    driver_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    submitted_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    punctuality_rating = table.Column<int>(type: "integer", nullable: false),
                    safety_rating = table.Column<int>(type: "integer", nullable: false),
                    service_rating = table.Column<int>(type: "integer", nullable: false),
                    overall_rating = table.Column<int>(type: "integer", nullable: false),
                    vehicle_condition_rating = table.Column<int>(type: "integer", nullable: false),
                    vehicle_cleanliness_rating = table.Column<int>(type: "integer", nullable: false),
                    has_incident = table.Column<bool>(type: "boolean", nullable: false),
                    incident_category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_token = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fleet_trip_feedbacks", x => x.id);
                    table.CheckConstraint("ck_fleet_trip_feedback_incident", "(has_incident = FALSE AND incident_category IS NULL) OR (has_incident = TRUE AND incident_category IS NOT NULL AND comment IS NOT NULL)");
                    table.CheckConstraint("ck_fleet_trip_feedback_incident_category", "incident_category IS NULL OR incident_category IN ('DRIVING','PUNCTUALITY','SERVICE','VEHICLE_CONDITION','CLEANLINESS','OTHER')");
                    table.CheckConstraint("ck_fleet_trip_feedback_ratings", "punctuality_rating BETWEEN 1 AND 5 AND safety_rating BETWEEN 1 AND 5 AND service_rating BETWEEN 1 AND 5 AND overall_rating BETWEEN 1 AND 5 AND vehicle_condition_rating BETWEEN 1 AND 5 AND vehicle_cleanliness_rating BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_fleet_trip_feedbacks_fleet_assignments_vehicle_assignment_id",
                        column: x => x.vehicle_assignment_id,
                        principalTable: "fleet_assignments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_trip_feedbacks_fleet_requests_fleet_request_id",
                        column: x => x.fleet_request_id,
                        principalTable: "fleet_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_trip_feedbacks_fleet_trip_records_trip_id",
                        column: x => x.trip_id,
                        principalTable: "fleet_trip_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_trip_feedbacks_fleet_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "fleet_vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_trip_feedbacks_users_driver_user_id",
                        column: x => x.driver_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_trip_feedbacks_users_submitted_by_user_id",
                        column: x => x.submitted_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_trip_feedbacks_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fleet_trip_feedbacks_driver_user_id_submitted_at",
                table: "fleet_trip_feedbacks",
                columns: new[] { "driver_user_id", "submitted_at" });

            migrationBuilder.CreateIndex(
                name: "IX_fleet_trip_feedbacks_fleet_request_id",
                table: "fleet_trip_feedbacks",
                column: "fleet_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_trip_feedbacks_submitted_by_user_id_submitted_at",
                table: "fleet_trip_feedbacks",
                columns: new[] { "submitted_by_user_id", "submitted_at" });

            migrationBuilder.CreateIndex(
                name: "IX_fleet_trip_feedbacks_trip_id_submitted_by_user_id",
                table: "fleet_trip_feedbacks",
                columns: new[] { "trip_id", "submitted_by_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fleet_trip_feedbacks_updated_by_user_id",
                table: "fleet_trip_feedbacks",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_trip_feedbacks_vehicle_assignment_id",
                table: "fleet_trip_feedbacks",
                column: "vehicle_assignment_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_trip_feedbacks_vehicle_id_submitted_at",
                table: "fleet_trip_feedbacks",
                columns: new[] { "vehicle_id", "submitted_at" });

            migrationBuilder.Sql("""
                INSERT INTO role_permissions (role_id, permission_id)
                SELECT r.id, p.id
                FROM roles r
                CROSS JOIN permissions p
                WHERE r.is_active = TRUE
                  AND p.is_active = TRUE
                  AND p.code IN ('FleetFeedback.Create', 'FleetFeedback.ViewOwn')
                ON CONFLICT (role_id, permission_id) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM role_permissions rp
                USING permissions p
                WHERE rp.permission_id = p.id
                  AND p.code IN ('FleetFeedback.Create', 'FleetFeedback.ViewOwn');
                """);

            migrationBuilder.DropTable(
                name: "fleet_trip_feedbacks");
        }
    }
}
