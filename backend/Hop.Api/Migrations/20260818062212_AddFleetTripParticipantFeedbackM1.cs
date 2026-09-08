using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFleetTripParticipantFeedbackM1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fleet_trip_participants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_requester = table.Column<bool>(type: "boolean", nullable: false),
                    participant_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_actual_participant = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fleet_trip_participants", x => x.id);
                    table.CheckConstraint("ck_fleet_trip_participants_type", "participant_type IN ('EMPLOYEE','EXTERNAL')");
                    table.ForeignKey(
                        name: "FK_fleet_trip_participants_fleet_trip_records_trip_id",
                        column: x => x.trip_id,
                        principalTable: "fleet_trip_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_fleet_trip_participants_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fleet_trip_participants_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fleet_trip_participants_created_by_user_id",
                table: "fleet_trip_participants",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_trip_participants_trip_id_user_id",
                table: "fleet_trip_participants",
                columns: new[] { "trip_id", "user_id" },
                unique: true,
                filter: "user_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_trip_participants_user_id_is_actual_participant",
                table: "fleet_trip_participants",
                columns: new[] { "user_id", "is_actual_participant" });

            migrationBuilder.Sql("""
                INSERT INTO permissions (id, code, name, group_name, action, is_active, created_at)
                VALUES
                  ('019fd9a0-0000-7000-8000-000000000001', 'FleetFeedback.Create', 'ส่ง Feedback การเดินทาง', 'FleetFeedback', 'Create', TRUE, NOW()),
                  ('019fd9a0-0000-7000-8000-000000000002', 'FleetFeedback.ViewOwn', 'ดู Feedback การเดินทางของตนเอง', 'FleetFeedback', 'ViewOwn', TRUE, NOW()),
                  ('019fd9a0-0000-7000-8000-000000000003', 'FleetFeedback.ViewManagement', 'ดูรายงาน Feedback สำหรับผู้บริหาร', 'FleetFeedback', 'ViewManagement', TRUE, NOW()),
                  ('019fd9a0-0000-7000-8000-000000000004', 'FleetFeedback.ViewIdentity', 'ดูตัวตนผู้ให้ Feedback', 'FleetFeedback', 'ViewIdentity', TRUE, NOW()),
                  ('019fd9a0-0000-7000-8000-000000000005', 'FleetFeedback.Manage', 'จัดการ Feedback การเดินทาง', 'FleetFeedback', 'Manage', TRUE, NOW())
                ON CONFLICT (code) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fleet_trip_participants");

            migrationBuilder.Sql("""
                DELETE FROM permissions
                WHERE code IN (
                  'FleetFeedback.Create',
                  'FleetFeedback.ViewOwn',
                  'FleetFeedback.ViewManagement',
                  'FleetFeedback.ViewIdentity',
                  'FleetFeedback.Manage'
                );
                """);
        }
    }
}
