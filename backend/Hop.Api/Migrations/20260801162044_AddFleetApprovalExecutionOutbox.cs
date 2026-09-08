using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFleetApprovalExecutionOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_token",
                table: "fleet_cancellation_requests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "review_reason",
                table: "fleet_cancellation_requests",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "reviewed_at",
                table: "fleet_cancellation_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "reviewed_by_user_id",
                table: "fleet_cancellation_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "correlation_id",
                table: "audit_logs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "delegation_id",
                table: "audit_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "delegator_user_id",
                table: "audit_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "effective_actor_user_id",
                table: "audit_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "new_value",
                table: "audit_logs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "old_value",
                table: "audit_logs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reason",
                table: "audit_logs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_agent",
                table: "audit_logs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_token",
                table: "approval_delegations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "end_at",
                table: "approval_delegations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "required_permission_code",
                table: "approval_delegations",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "scope",
                table: "approval_delegations",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "start_at",
                table: "approval_delegations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE approval_delegations
                SET scope = 'LEAVE',
                    required_permission_code = 'LeaveApproval.ApproveCurrentStep',
                    start_at = start_date::timestamp AT TIME ZONE 'Asia/Bangkok',
                    end_at = (end_date + 1)::timestamp AT TIME ZONE 'Asia/Bangkok',
                    concurrency_token = md5(id::text || created_at::text)::uuid
                WHERE scope IS NULL;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_approval_delegations_scope",
                table: "approval_delegations",
                sql: "scope IS NULL OR scope IN ('LEAVE', 'FLEET')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_approval_delegations_interval",
                table: "approval_delegations",
                sql: "start_at IS NULL OR end_at IS NULL OR end_at > start_at");

            migrationBuilder.CreateTable(
                name: "domain_events",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    scope = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    aggregate_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_domain_events", x => x.event_id);
                });

            migrationBuilder.CreateTable(
                name: "fleet_trip_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fleet_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    driver_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actual_start_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    actual_end_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    start_mileage = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    end_mileage = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    fuel_amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    fuel_cost = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    trip_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    completion_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    completed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    override_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    concurrency_token = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fleet_trip_records", x => x.id);
                    table.CheckConstraint("ck_fleet_trip_mileage", "start_mileage IS NULL OR start_mileage >= 0 AND (end_mileage IS NULL OR end_mileage >= start_mileage)");
                    table.CheckConstraint("ck_fleet_trip_time", "actual_start_at IS NULL OR actual_end_at IS NULL OR actual_end_at >= actual_start_at");
                    table.ForeignKey(
                        name: "FK_fleet_trip_records_fleet_assignments_assignment_id",
                        column: x => x.assignment_id,
                        principalTable: "fleet_assignments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_trip_records_fleet_requests_fleet_request_id",
                        column: x => x.fleet_request_id,
                        principalTable: "fleet_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_trip_records_users_completed_by_user_id",
                        column: x => x.completed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_trip_records_users_driver_user_id",
                        column: x => x.driver_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    scope = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    available_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    concurrency_token = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_outbox_messages_domain_events_event_id",
                        column: x => x.event_id,
                        principalTable: "domain_events",
                        principalColumn: "event_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notification_deliveries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outbox_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_deliveries", x => x.id);
                    table.ForeignKey(
                        name: "FK_notification_deliveries_outbox_messages_outbox_message_id",
                        column: x => x.outbox_message_id,
                        principalTable: "outbox_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_notification_deliveries_users_recipient_user_id",
                        column: x => x.recipient_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fleet_cancellation_requests_reviewed_by_user_id",
                table: "fleet_cancellation_requests",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_approval_delegations_approver_user_id_scope_required_permis~",
                table: "approval_delegations",
                columns: new[] { "approver_user_id", "scope", "required_permission_code", "start_at", "end_at" });

            migrationBuilder.CreateIndex(
                name: "IX_domain_events_scope_event_type_occurred_at",
                table: "domain_events",
                columns: new[] { "scope", "event_type", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_fleet_trip_records_assignment_id",
                table: "fleet_trip_records",
                column: "assignment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fleet_trip_records_completed_by_user_id",
                table: "fleet_trip_records",
                column: "completed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_trip_records_driver_user_id",
                table: "fleet_trip_records",
                column: "driver_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_trip_records_fleet_request_id",
                table: "fleet_trip_records",
                column: "fleet_request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_deliveries_idempotency_key",
                table: "notification_deliveries",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_deliveries_outbox_message_id",
                table: "notification_deliveries",
                column: "outbox_message_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_deliveries_recipient_user_id",
                table: "notification_deliveries",
                column: "recipient_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_deliveries_status_created_at",
                table: "notification_deliveries",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_event_id",
                table: "outbox_messages",
                column: "event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_status_available_at",
                table: "outbox_messages",
                columns: new[] { "status", "available_at" });

            migrationBuilder.AddForeignKey(
                name: "FK_fleet_cancellation_requests_users_reviewed_by_user_id",
                table: "fleet_cancellation_requests",
                column: "reviewed_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_fleet_cancellation_requests_users_reviewed_by_user_id",
                table: "fleet_cancellation_requests");

            migrationBuilder.DropTable(
                name: "fleet_trip_records");

            migrationBuilder.DropTable(
                name: "notification_deliveries");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "domain_events");

            migrationBuilder.DropIndex(
                name: "IX_fleet_cancellation_requests_reviewed_by_user_id",
                table: "fleet_cancellation_requests");

            migrationBuilder.DropIndex(
                name: "IX_approval_delegations_approver_user_id_scope_required_permis~",
                table: "approval_delegations");

            migrationBuilder.DropCheckConstraint(name: "ck_approval_delegations_scope", table: "approval_delegations");
            migrationBuilder.DropCheckConstraint(name: "ck_approval_delegations_interval", table: "approval_delegations");

            migrationBuilder.DropColumn(
                name: "concurrency_token",
                table: "fleet_cancellation_requests");

            migrationBuilder.DropColumn(
                name: "review_reason",
                table: "fleet_cancellation_requests");

            migrationBuilder.DropColumn(
                name: "reviewed_at",
                table: "fleet_cancellation_requests");

            migrationBuilder.DropColumn(
                name: "reviewed_by_user_id",
                table: "fleet_cancellation_requests");

            migrationBuilder.DropColumn(
                name: "correlation_id",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "delegation_id",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "delegator_user_id",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "effective_actor_user_id",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "new_value",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "old_value",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "reason",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "user_agent",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "concurrency_token",
                table: "approval_delegations");

            migrationBuilder.DropColumn(
                name: "end_at",
                table: "approval_delegations");

            migrationBuilder.DropColumn(
                name: "required_permission_code",
                table: "approval_delegations");

            migrationBuilder.DropColumn(
                name: "scope",
                table: "approval_delegations");

            migrationBuilder.DropColumn(
                name: "start_at",
                table: "approval_delegations");
        }
    }
}
