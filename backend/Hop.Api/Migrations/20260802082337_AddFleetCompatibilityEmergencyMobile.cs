using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFleetCompatibilityEmergencyMobile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "completion_idempotency_key",
                table: "fleet_trip_records",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "start_idempotency_key",
                table: "fleet_trip_records",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "emergency_declared_at",
                table: "fleet_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "emergency_declared_by_user_id",
                table: "fleet_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "emergency_policy_code",
                table: "fleet_requests",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "emergency_reason",
                table: "fleet_requests",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "incident_location",
                table: "fleet_requests",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "priority",
                table: "fleet_requests",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "NORMAL");

            migrationBuilder.AddColumn<DateTime>(
                name: "reported_at",
                table: "fleet_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "reported_by_user_id",
                table: "fleet_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "requested_departure_at",
                table: "fleet_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "requires_post_review",
                table: "fleet_requests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "fleet_capabilities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    data_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_required_safety_capability = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_fleet_capabilities", x => x.id);
                    table.CheckConstraint("ck_fleet_capability_type", "data_type IN ('BOOLEAN','NUMBER','TEXT','ENUM')");
                });

            migrationBuilder.Sql("""
                INSERT INTO fleet_capabilities
                    (id, code, name, description, category, data_type, unit, is_required_safety_capability, is_active, sort_order, concurrency_token, created_at, created_by_user_id)
                SELECT gen_random_uuid(), seed.code, seed.name, seed.description, seed.category, seed.data_type, seed.unit, seed.is_safety, true, seed.sort_order, gen_random_uuid(), NOW(), NULL
                FROM (VALUES
                    ('PassengerTransport','Passenger transport','รองรับการขนส่งผู้โดยสาร','TRANSPORT','BOOLEAN',NULL,false,10),
                    ('PassengerCapacity','Passenger capacity','จำนวนผู้โดยสารที่รองรับ','CAPACITY','NUMBER','person',true,20),
                    ('Wheelchair','Wheelchair','รองรับรถเข็น','ACCESSIBILITY','BOOLEAN',NULL,true,30),
                    ('PatientTransport','Patient transport','รองรับการขนส่งผู้ป่วยภายใต้ Fleet policy','SAFETY','BOOLEAN',NULL,true,40),
                    ('MedicalEquipment','Medical equipment','รองรับอุปกรณ์การแพทย์','SAFETY','BOOLEAN',NULL,true,50),
                    ('Cargo','Cargo','รองรับสัมภาระ','CARGO','BOOLEAN',NULL,false,60),
                    ('HeavyCargo','Heavy cargo','รองรับสัมภาระหนัก','CARGO','BOOLEAN',NULL,true,70),
                    ('MaximumCargoWeight','Maximum cargo weight','น้ำหนักสัมภาระสูงสุด','CARGO','NUMBER','kg',true,80),
                    ('VIP','VIP','รองรับภารกิจ VIP','SERVICE','BOOLEAN',NULL,false,90),
                    ('LongDistance','Long distance','รองรับเส้นทางไกล','ROUTE','BOOLEAN',NULL,false,100),
                    ('MountainRoute','Mountain route','รองรับเส้นทางภูเขา','ROUTE','BOOLEAN',NULL,true,110),
                    ('RouteType','Route type','ประเภทเส้นทางที่รองรับ','ROUTE','ENUM',NULL,false,120),
                    ('Overnight','Overnight','รองรับภารกิจค้างคืน','ROUTE','BOOLEAN',NULL,false,130),
                    ('AirConditioning','Air conditioning','มีเครื่องปรับอากาศ','COMFORT','BOOLEAN',NULL,false,140),
                    ('EmergencySupport','Emergency support','รองรับ Emergency Transport ตาม Fleet policy','SAFETY','BOOLEAN',NULL,true,150)
                ) AS seed(code,name,description,category,data_type,unit,is_safety,sort_order)
                WHERE NOT EXISTS (SELECT 1 FROM fleet_capabilities existing WHERE existing.code = seed.code);

                INSERT INTO permissions (id, code, name, group_name, action, is_active, created_at)
                SELECT gen_random_uuid(), item.code, item.name, item.group_name, item.action, true, NOW()
                FROM (VALUES
                    ('FleetCapability.View','ดู Capability Fleet','FleetCapability','View'),
                    ('FleetCapability.Manage','จัดการ Capability Fleet','FleetCapability','Manage'),
                    ('FleetVehicleCapability.Manage','กำหนด Capability ให้รถ','FleetVehicleCapability','Manage'),
                    ('FleetRequestCapability.ManageOwn','กำหนด Capability ในคำขอตนเอง','FleetRequestCapability','ManageOwn'),
                    ('FleetCompatibility.View','ดูผล Compatibility','FleetCompatibility','View'),
                    ('FleetCompatibility.Override','Override Compatibility ที่ไม่ใช่ Safety','FleetCompatibility','Override'),
                    ('FleetEmergency.Create','สร้างคำขอ Fleet Emergency','FleetEmergency','Create'),
                    ('FleetEmergency.ViewOwn','ดูคำขอ Fleet Emergency ของตน','FleetEmergency','ViewOwn'),
                    ('FleetEmergency.ViewQueue','ดูคิว Fleet Emergency','FleetEmergency','ViewQueue'),
                    ('FleetEmergency.Dispatch','จัดรถ Fleet Emergency','FleetEmergency','Dispatch'),
                    ('FleetEmergency.BypassApproval','ข้าม approval ตามนโยบาย Fleet Emergency','FleetEmergency','BypassApproval'),
                    ('FleetEmergency.Review','ทบทวน Fleet Emergency หลังเหตุการณ์','FleetEmergency','Review'),
                    ('FleetEmergency.ViewAudit','ดู Audit Fleet Emergency','FleetEmergency','ViewAudit'),
                    ('FleetDriver.ViewJobs','ดูรายการงานขับรถบนมือถือ','FleetDriver','ViewJobs'),
                    ('FleetDriver.AcceptJob','ตอบรับงานขับรถ','FleetDriver','AcceptJob'),
                    ('FleetDriver.DeclineJob','ปฏิเสธงานขับรถ','FleetDriver','DeclineJob'),
                    ('FleetTrip.Start','เริ่ม Trip','FleetTrip','Start'),
                    ('FleetTrip.Complete','จบ Trip','FleetTrip','Complete'),
                    ('FleetTrip.UploadAttachment','อัปโหลดไฟล์ Trip','FleetTrip','UploadAttachment')
                ) AS item(code,name,group_name,action)
                WHERE NOT EXISTS (SELECT 1 FROM permissions p WHERE p.code=item.code);
                """);

            migrationBuilder.CreateTable(
                name: "fleet_compatibility_overrides",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fleet_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    mismatch_capability_ids = table.Column<string>(type: "jsonb", nullable: false),
                    reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    concurrency_token = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fleet_compatibility_overrides", x => x.id);
                    table.CheckConstraint("ck_fleet_compatibility_override_mismatches", "jsonb_typeof(mismatch_capability_ids) = 'array' AND jsonb_array_length(mismatch_capability_ids) > 0");
                    table.ForeignKey(
                        name: "FK_fleet_compatibility_overrides_fleet_assignments_assignment_~",
                        column: x => x.assignment_id,
                        principalTable: "fleet_assignments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_compatibility_overrides_fleet_requests_fleet_request_~",
                        column: x => x.fleet_request_id,
                        principalTable: "fleet_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_compatibility_overrides_fleet_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "fleet_vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_compatibility_overrides_users_approved_by_user_id",
                        column: x => x.approved_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fleet_emergency_post_reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fleet_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    outcome = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    was_bypass_appropriate = table.Column<bool>(type: "boolean", nullable: false),
                    response_time_assessment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    safety_issues = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    follow_up_actions = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    concurrency_token = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fleet_emergency_post_reviews", x => x.id);
                    table.CheckConstraint("ck_fleet_emergency_review_outcome", "outcome IN ('ACCEPTABLE','NEEDS_IMPROVEMENT','POLICY_VIOLATION')");
                    table.ForeignKey(
                        name: "FK_fleet_emergency_post_reviews_fleet_requests_fleet_request_id",
                        column: x => x.fleet_request_id,
                        principalTable: "fleet_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_emergency_post_reviews_users_reviewed_by_user_id",
                        column: x => x.reviewed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fleet_trip_attachments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    stored_file_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    file_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fleet_trip_attachments", x => x.id);
                    table.CheckConstraint("ck_fleet_trip_attachment_size", "file_size > 0");
                    table.ForeignKey(
                        name: "FK_fleet_trip_attachments_fleet_trip_records_trip_id",
                        column: x => x.trip_id,
                        principalTable: "fleet_trip_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_trip_attachments_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fleet_request_required_capabilities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fleet_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    capability_id = table.Column<Guid>(type: "uuid", nullable: false),
                    @operator = table.Column<string>(name: "operator", type: "character varying(40)", maxLength: 40, nullable: false),
                    required_boolean_value = table.Column<bool>(type: "boolean", nullable: true),
                    required_numeric_value = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    required_text_value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    required_enum_value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_mandatory = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fleet_request_required_capabilities", x => x.id);
                    table.CheckConstraint("ck_fleet_request_capability_operator", "operator IN ('EQUALS','GREATER_THAN_OR_EQUAL','LESS_THAN_OR_EQUAL','CONTAINS','IN')");
                    table.CheckConstraint("ck_fleet_request_capability_value", "num_nonnulls(required_boolean_value,required_numeric_value,required_text_value,required_enum_value)=1");
                    table.ForeignKey(
                        name: "FK_fleet_request_required_capabilities_fleet_capabilities_capa~",
                        column: x => x.capability_id,
                        principalTable: "fleet_capabilities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_request_required_capabilities_fleet_requests_fleet_re~",
                        column: x => x.fleet_request_id,
                        principalTable: "fleet_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fleet_vehicle_capabilities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    capability_id = table.Column<Guid>(type: "uuid", nullable: false),
                    boolean_value = table.Column<bool>(type: "boolean", nullable: true),
                    numeric_value = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    text_value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    enum_value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    effective_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    effective_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    concurrency_token = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fleet_vehicle_capabilities", x => x.id);
                    table.CheckConstraint("ck_fleet_vehicle_capability_dates", "effective_to IS NULL OR effective_to > effective_from");
                    table.CheckConstraint("ck_fleet_vehicle_capability_value", "num_nonnulls(boolean_value,numeric_value,text_value,enum_value)=1");
                    table.ForeignKey(
                        name: "FK_fleet_vehicle_capabilities_fleet_capabilities_capability_id",
                        column: x => x.capability_id,
                        principalTable: "fleet_capabilities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_vehicle_capabilities_fleet_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "fleet_vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fleet_trip_records_completion_idempotency_key",
                table: "fleet_trip_records",
                column: "completion_idempotency_key",
                unique: true,
                filter: "completion_idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_trip_records_start_idempotency_key",
                table: "fleet_trip_records",
                column: "start_idempotency_key",
                unique: true,
                filter: "start_idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_requests_priority_status_submitted_at",
                table: "fleet_requests",
                columns: new[] { "priority", "status", "submitted_at" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_fleet_requests_emergency_reason",
                table: "fleet_requests",
                sql: "priority <> 'EMERGENCY' OR emergency_reason IS NOT NULL AND reported_by_user_id IS NOT NULL AND reported_at IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_fleet_requests_priority",
                table: "fleet_requests",
                sql: "priority IN ('NORMAL','URGENT','EMERGENCY')");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_capabilities_code",
                table: "fleet_capabilities",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fleet_capabilities_is_active_sort_order",
                table: "fleet_capabilities",
                columns: new[] { "is_active", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_fleet_compatibility_overrides_approved_by_user_id",
                table: "fleet_compatibility_overrides",
                column: "approved_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_compatibility_overrides_assignment_id",
                table: "fleet_compatibility_overrides",
                column: "assignment_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_compatibility_overrides_fleet_request_id_vehicle_id_c~",
                table: "fleet_compatibility_overrides",
                columns: new[] { "fleet_request_id", "vehicle_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_fleet_compatibility_overrides_vehicle_id",
                table: "fleet_compatibility_overrides",
                column: "vehicle_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_emergency_post_reviews_fleet_request_id",
                table: "fleet_emergency_post_reviews",
                column: "fleet_request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fleet_emergency_post_reviews_reviewed_by_user_id",
                table: "fleet_emergency_post_reviews",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_request_required_capabilities_capability_id",
                table: "fleet_request_required_capabilities",
                column: "capability_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_request_required_capabilities_fleet_request_id_capabi~",
                table: "fleet_request_required_capabilities",
                columns: new[] { "fleet_request_id", "capability_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fleet_trip_attachments_created_by_user_id",
                table: "fleet_trip_attachments",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_trip_attachments_idempotency_key",
                table: "fleet_trip_attachments",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fleet_trip_attachments_trip_id",
                table: "fleet_trip_attachments",
                column: "trip_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_vehicle_capabilities_capability_id",
                table: "fleet_vehicle_capabilities",
                column: "capability_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_vehicle_capabilities_vehicle_id_capability_id",
                table: "fleet_vehicle_capabilities",
                columns: new[] { "vehicle_id", "capability_id" },
                unique: true,
                filter: "is_active = true AND effective_to IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM permissions p
                WHERE p.code IN ('FleetCapability.View','FleetCapability.Manage','FleetVehicleCapability.Manage','FleetRequestCapability.ManageOwn','FleetCompatibility.View','FleetCompatibility.Override','FleetEmergency.Create','FleetEmergency.ViewOwn','FleetEmergency.ViewQueue','FleetEmergency.Dispatch','FleetEmergency.BypassApproval','FleetEmergency.Review','FleetEmergency.ViewAudit','FleetDriver.ViewJobs','FleetDriver.AcceptJob','FleetDriver.DeclineJob','FleetTrip.Start','FleetTrip.Complete','FleetTrip.UploadAttachment')
                  AND NOT EXISTS (SELECT 1 FROM role_permissions rp WHERE rp.permission_id=p.id);
                """);
            migrationBuilder.DropTable(
                name: "fleet_compatibility_overrides");

            migrationBuilder.DropTable(
                name: "fleet_emergency_post_reviews");

            migrationBuilder.DropTable(
                name: "fleet_request_required_capabilities");

            migrationBuilder.DropTable(
                name: "fleet_trip_attachments");

            migrationBuilder.DropTable(
                name: "fleet_vehicle_capabilities");

            migrationBuilder.DropTable(
                name: "fleet_capabilities");

            migrationBuilder.DropIndex(
                name: "IX_fleet_trip_records_completion_idempotency_key",
                table: "fleet_trip_records");

            migrationBuilder.DropIndex(
                name: "IX_fleet_trip_records_start_idempotency_key",
                table: "fleet_trip_records");

            migrationBuilder.DropIndex(
                name: "IX_fleet_requests_priority_status_submitted_at",
                table: "fleet_requests");

            migrationBuilder.DropCheckConstraint(
                name: "ck_fleet_requests_emergency_reason",
                table: "fleet_requests");

            migrationBuilder.DropCheckConstraint(
                name: "ck_fleet_requests_priority",
                table: "fleet_requests");

            migrationBuilder.DropColumn(
                name: "completion_idempotency_key",
                table: "fleet_trip_records");

            migrationBuilder.DropColumn(
                name: "start_idempotency_key",
                table: "fleet_trip_records");

            migrationBuilder.DropColumn(
                name: "emergency_declared_at",
                table: "fleet_requests");

            migrationBuilder.DropColumn(
                name: "emergency_declared_by_user_id",
                table: "fleet_requests");

            migrationBuilder.DropColumn(
                name: "emergency_policy_code",
                table: "fleet_requests");

            migrationBuilder.DropColumn(
                name: "emergency_reason",
                table: "fleet_requests");

            migrationBuilder.DropColumn(
                name: "incident_location",
                table: "fleet_requests");

            migrationBuilder.DropColumn(
                name: "priority",
                table: "fleet_requests");

            migrationBuilder.DropColumn(
                name: "reported_at",
                table: "fleet_requests");

            migrationBuilder.DropColumn(
                name: "reported_by_user_id",
                table: "fleet_requests");

            migrationBuilder.DropColumn(
                name: "requested_departure_at",
                table: "fleet_requests");

            migrationBuilder.DropColumn(
                name: "requires_post_review",
                table: "fleet_requests");
        }
    }
}
