using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFleetKpiMaintenanceCalendar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fleet_maintenance_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_date_based = table.Column<bool>(type: "boolean", nullable: false),
                    is_mileage_based = table.Column<bool>(type: "boolean", nullable: false),
                    blocks_availability_when_overdue = table.Column<bool>(type: "boolean", nullable: false),
                    default_reminder_days = table.Column<int>(type: "integer", nullable: true),
                    default_reminder_mileage = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    concurrency_token = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fleet_maintenance_types", x => x.id);
                    table.CheckConstraint("ck_fleet_maintenance_types_basis", "is_date_based OR is_mileage_based");
                    table.CheckConstraint("ck_fleet_maintenance_types_reminders", "default_reminder_days IS NULL OR default_reminder_days >= 0 AND (default_reminder_mileage IS NULL OR default_reminder_mileage >= 0)");
                });

            migrationBuilder.CreateTable(
                name: "fleet_vehicle_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    document_number = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    issued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    provider = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    concurrency_token = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fleet_vehicle_documents", x => x.id);
                    table.CheckConstraint("ck_fleet_vehicle_document_dates", "issued_at IS NULL OR expires_at IS NULL OR expires_at >= issued_at");
                    table.ForeignKey(
                        name: "FK_fleet_vehicle_documents_fleet_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "fleet_vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fleet_vehicle_maintenance_schedules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    maintenance_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    due_mileage = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    reminder_days = table.Column<int>(type: "integer", nullable: true),
                    reminder_mileage = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    recurrence_days = table.Column<int>(type: "integer", nullable: true),
                    recurrence_mileage = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    last_completed_record_id = table.Column<Guid>(type: "uuid", nullable: true),
                    concurrency_token = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fleet_vehicle_maintenance_schedules", x => x.id);
                    table.CheckConstraint("ck_fleet_maintenance_schedule_values", "(due_mileage IS NULL OR due_mileage >= 0) AND (reminder_days IS NULL OR reminder_days >= 0) AND (reminder_mileage IS NULL OR reminder_mileage >= 0) AND (recurrence_days IS NULL OR recurrence_days > 0) AND (recurrence_mileage IS NULL OR recurrence_mileage > 0)");
                    table.ForeignKey(
                        name: "FK_fleet_vehicle_maintenance_schedules_fleet_maintenance_types~",
                        column: x => x.maintenance_type_id,
                        principalTable: "fleet_maintenance_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_vehicle_maintenance_schedules_fleet_vehicles_vehicle_~",
                        column: x => x.vehicle_id,
                        principalTable: "fleet_vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fleet_vehicle_maintenance_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    maintenance_schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    maintenance_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_mileage = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    cost = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    vendor = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    invoice_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    result = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    performed_by = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    next_due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    next_due_mileage = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    is_cancelled = table.Column<bool>(type: "boolean", nullable: false),
                    cancellation_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    concurrency_token = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fleet_vehicle_maintenance_records", x => x.id);
                    table.CheckConstraint("ck_fleet_maintenance_record_cost", "cost IS NULL OR cost >= 0");
                    table.CheckConstraint("ck_fleet_maintenance_record_dates", "completed_at IS NULL OR completed_at >= started_at");
                    table.CheckConstraint("ck_fleet_maintenance_record_mileage", "completed_mileage IS NULL OR completed_mileage >= 0");
                    table.ForeignKey(
                        name: "FK_fleet_vehicle_maintenance_records_fleet_vehicle_maintenance~",
                        column: x => x.maintenance_schedule_id,
                        principalTable: "fleet_vehicle_maintenance_schedules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fleet_maintenance_attachments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    maintenance_record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    stored_file_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    file_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fleet_maintenance_attachments", x => x.id);
                    table.ForeignKey(
                        name: "FK_fleet_maintenance_attachments_fleet_vehicle_maintenance_rec~",
                        column: x => x.maintenance_record_id,
                        principalTable: "fleet_vehicle_maintenance_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fleet_maintenance_attachments_maintenance_record_id_is_dele~",
                table: "fleet_maintenance_attachments",
                columns: new[] { "maintenance_record_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_fleet_maintenance_types_code",
                table: "fleet_maintenance_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fleet_maintenance_types_is_active_sort_order",
                table: "fleet_maintenance_types",
                columns: new[] { "is_active", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_fleet_vehicle_documents_expires_at",
                table: "fleet_vehicle_documents",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_vehicle_documents_vehicle_id_is_active_document_type",
                table: "fleet_vehicle_documents",
                columns: new[] { "vehicle_id", "is_active", "document_type" });

            migrationBuilder.CreateIndex(
                name: "IX_fleet_vehicle_maintenance_records_maintenance_schedule_id",
                table: "fleet_vehicle_maintenance_records",
                column: "maintenance_schedule_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_vehicle_maintenance_records_vehicle_id_started_at",
                table: "fleet_vehicle_maintenance_records",
                columns: new[] { "vehicle_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "IX_fleet_vehicle_maintenance_schedules_due_date",
                table: "fleet_vehicle_maintenance_schedules",
                column: "due_date");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_vehicle_maintenance_schedules_due_mileage",
                table: "fleet_vehicle_maintenance_schedules",
                column: "due_mileage");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_vehicle_maintenance_schedules_maintenance_type_id",
                table: "fleet_vehicle_maintenance_schedules",
                column: "maintenance_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_vehicle_maintenance_schedules_vehicle_id_is_active_st~",
                table: "fleet_vehicle_maintenance_schedules",
                columns: new[] { "vehicle_id", "is_active", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fleet_maintenance_attachments");

            migrationBuilder.DropTable(
                name: "fleet_vehicle_documents");

            migrationBuilder.DropTable(
                name: "fleet_vehicle_maintenance_records");

            migrationBuilder.DropTable(
                name: "fleet_vehicle_maintenance_schedules");

            migrationBuilder.DropTable(
                name: "fleet_maintenance_types");
        }
    }
}
