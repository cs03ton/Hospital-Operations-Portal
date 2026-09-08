using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFleetRequesterDispatcherWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fleet_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_no = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    requester_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requester_department_id = table.Column<Guid>(type: "uuid", nullable: true),
                    request_date = table.Column<DateOnly>(type: "date", nullable: false),
                    purpose = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    mission_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    requested_vehicle_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    destination = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    contact_person_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    contact_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    departure_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expected_return_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    passenger_count = table.Column<int>(type: "integer", nullable: false),
                    special_requirement = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_urgent = table.Column<bool>(type: "boolean", nullable: false),
                    urgent_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    return_target = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_token = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fleet_requests", x => x.id);
                    table.CheckConstraint("ck_fleet_requests_passenger_count", "passenger_count > 0");
                    table.CheckConstraint("ck_fleet_requests_time_range", "expected_return_at > departure_at");
                    table.ForeignKey(
                        name: "FK_fleet_requests_departments_requester_department_id",
                        column: x => x.requester_department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_requests_fleet_vehicle_types_requested_vehicle_type_id",
                        column: x => x.requested_vehicle_type_id,
                        principalTable: "fleet_vehicle_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_requests_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_requests_users_requester_user_id",
                        column: x => x.requester_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_requests_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fleet_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fleet_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    driver_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    assignment_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    assignment_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    replaced_assignment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fleet_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_fleet_assignments_fleet_assignments_replaced_assignment_id",
                        column: x => x.replaced_assignment_id,
                        principalTable: "fleet_assignments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_assignments_fleet_requests_fleet_request_id",
                        column: x => x.fleet_request_id,
                        principalTable: "fleet_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_assignments_fleet_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "fleet_vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_assignments_users_assigned_by_user_id",
                        column: x => x.assigned_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_assignments_users_driver_user_id",
                        column: x => x.driver_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fleet_cancellation_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fleet_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fleet_cancellation_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_fleet_cancellation_requests_fleet_requests_fleet_request_id",
                        column: x => x.fleet_request_id,
                        principalTable: "fleet_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_cancellation_requests_users_requested_by_user_id",
                        column: x => x.requested_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fleet_request_passengers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fleet_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    position_or_organization = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    passenger_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_requester = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fleet_request_passengers", x => x.id);
                    table.ForeignKey(
                        name: "FK_fleet_request_passengers_fleet_requests_fleet_request_id",
                        column: x => x.fleet_request_id,
                        principalTable: "fleet_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_fleet_request_passengers_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fleet_request_status_histories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fleet_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    to_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    return_target = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fleet_request_status_histories", x => x.id);
                    table.ForeignKey(
                        name: "FK_fleet_request_status_histories_fleet_requests_fleet_request~",
                        column: x => x.fleet_request_id,
                        principalTable: "fleet_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_fleet_request_status_histories_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fleet_assignments_assigned_by_user_id",
                table: "fleet_assignments",
                column: "assigned_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_assignments_driver_user_id_is_active",
                table: "fleet_assignments",
                columns: new[] { "driver_user_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_fleet_assignments_fleet_request_id",
                table: "fleet_assignments",
                column: "fleet_request_id",
                unique: true,
                filter: "is_active = true");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_assignments_replaced_assignment_id",
                table: "fleet_assignments",
                column: "replaced_assignment_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_assignments_vehicle_id_is_active",
                table: "fleet_assignments",
                columns: new[] { "vehicle_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_fleet_cancellation_requests_fleet_request_id_created_at",
                table: "fleet_cancellation_requests",
                columns: new[] { "fleet_request_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_fleet_cancellation_requests_requested_by_user_id",
                table: "fleet_cancellation_requests",
                column: "requested_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_request_passengers_fleet_request_id_sort_order",
                table: "fleet_request_passengers",
                columns: new[] { "fleet_request_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_fleet_request_passengers_user_id",
                table: "fleet_request_passengers",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_requests_created_by_user_id",
                table: "fleet_requests",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_requests_requested_vehicle_type_id",
                table: "fleet_requests",
                column: "requested_vehicle_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_requests_requester_department_id",
                table: "fleet_requests",
                column: "requester_department_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_requests_requester_user_id_status",
                table: "fleet_requests",
                columns: new[] { "requester_user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_fleet_requests_request_no",
                table: "fleet_requests",
                column: "request_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fleet_requests_status_departure_at",
                table: "fleet_requests",
                columns: new[] { "status", "departure_at" });

            migrationBuilder.CreateIndex(
                name: "IX_fleet_requests_updated_by_user_id",
                table: "fleet_requests",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_request_status_histories_actor_user_id",
                table: "fleet_request_status_histories",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_request_status_histories_fleet_request_id_created_at",
                table: "fleet_request_status_histories",
                columns: new[] { "fleet_request_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fleet_assignments");

            migrationBuilder.DropTable(
                name: "fleet_cancellation_requests");

            migrationBuilder.DropTable(
                name: "fleet_request_passengers");

            migrationBuilder.DropTable(
                name: "fleet_request_status_histories");

            migrationBuilder.DropTable(
                name: "fleet_requests");
        }
    }
}
