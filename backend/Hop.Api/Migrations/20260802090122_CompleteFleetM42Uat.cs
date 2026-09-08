using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class CompleteFleetM42Uat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "fleet_trip_attachments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by_user_id",
                table: "fleet_trip_attachments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "fleet_trip_attachments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "approval_bypass_allowed_snapshot",
                table: "fleet_requests",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "dispatch_target_minutes_snapshot",
                table: "fleet_requests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "driver_ack_target_minutes_snapshot",
                table: "fleet_requests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "emergency_policy_id",
                table: "fleet_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "post_review_required_snapshot",
                table: "fleet_requests",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "response_target_minutes_snapshot",
                table: "fleet_requests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "enum_options_json",
                table: "fleet_capabilities",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "maximum_numeric_value",
                table: "fleet_capabilities",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "minimum_numeric_value",
                table: "fleet_capabilities",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "fleet_emergency_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    response_target_minutes = table.Column<int>(type: "integer", nullable: false),
                    dispatch_target_minutes = table.Column<int>(type: "integer", nullable: false),
                    driver_ack_target_minutes = table.Column<int>(type: "integer", nullable: false),
                    approval_bypass_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    post_review_required = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    effective_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    effective_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    concurrency_token = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fleet_emergency_policies", x => x.id);
                    table.CheckConstraint("ck_fleet_emergency_policy_dates", "effective_to IS NULL OR effective_to > effective_from");
                    table.CheckConstraint("ck_fleet_emergency_policy_priority", "priority IN ('URGENT','EMERGENCY')");
                    table.CheckConstraint("ck_fleet_emergency_policy_targets", "response_target_minutes > 0 AND dispatch_target_minutes > 0 AND driver_ack_target_minutes > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_fleet_requests_emergency_policy_id",
                table: "fleet_requests",
                column: "emergency_policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_emergency_policies_code",
                table: "fleet_emergency_policies",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fleet_emergency_policies_priority_is_active_effective_from",
                table: "fleet_emergency_policies",
                columns: new[] { "priority", "is_active", "effective_from" });

            migrationBuilder.AddForeignKey(
                name: "FK_fleet_requests_fleet_emergency_policies_emergency_policy_id",
                table: "fleet_requests",
                column: "emergency_policy_id",
                principalTable: "fleet_emergency_policies",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                INSERT INTO fleet_emergency_policies
                    (id,code,name,priority,response_target_minutes,dispatch_target_minutes,driver_ack_target_minutes,approval_bypass_allowed,post_review_required,is_active,effective_from,concurrency_token,created_at,created_by_user_id)
                SELECT gen_random_uuid(),'INTERNAL_EMERGENCY_DEFAULT','Internal emergency transport default','EMERGENCY',15,15,15,true,true,true,TIMESTAMPTZ '2026-01-01 00:00:00+00',gen_random_uuid(),CURRENT_TIMESTAMP,NULL
                WHERE NOT EXISTS (SELECT 1 FROM fleet_emergency_policies WHERE code='INTERNAL_EMERGENCY_DEFAULT');

                UPDATE fleet_requests SET
                    emergency_policy_code=COALESCE(NULLIF(emergency_policy_code,''),'CONFIG_FALLBACK'),
                    response_target_minutes_snapshot=COALESCE(response_target_minutes_snapshot,15),
                    dispatch_target_minutes_snapshot=COALESCE(dispatch_target_minutes_snapshot,15),
                    driver_ack_target_minutes_snapshot=COALESCE(driver_ack_target_minutes_snapshot,15),
                    approval_bypass_allowed_snapshot=COALESCE(approval_bypass_allowed_snapshot,true),
                    post_review_required_snapshot=COALESCE(post_review_required_snapshot,requires_post_review)
                WHERE priority='EMERGENCY';

                INSERT INTO permissions (id,code,name,group_name,action,is_active,created_at)
                SELECT gen_random_uuid(),v.code,v.name,'FleetEmergencyPolicy',v.action,true,CURRENT_TIMESTAMP
                FROM (VALUES
                    ('FleetEmergencyPolicy.View','ดูนโยบาย Fleet Emergency','View'),
                    ('FleetEmergencyPolicy.Manage','จัดการนโยบาย Fleet Emergency','Manage')
                ) v(code,name,action)
                WHERE NOT EXISTS (SELECT 1 FROM permissions p WHERE p.code=v.code);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM permissions p WHERE p.code IN ('FleetEmergencyPolicy.View','FleetEmergencyPolicy.Manage')
                AND NOT EXISTS (SELECT 1 FROM role_permissions rp WHERE rp.permission_id=p.id);
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_fleet_requests_fleet_emergency_policies_emergency_policy_id",
                table: "fleet_requests");

            migrationBuilder.DropTable(
                name: "fleet_emergency_policies");

            migrationBuilder.DropIndex(
                name: "IX_fleet_requests_emergency_policy_id",
                table: "fleet_requests");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "fleet_trip_attachments");

            migrationBuilder.DropColumn(
                name: "deleted_by_user_id",
                table: "fleet_trip_attachments");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "fleet_trip_attachments");

            migrationBuilder.DropColumn(
                name: "approval_bypass_allowed_snapshot",
                table: "fleet_requests");

            migrationBuilder.DropColumn(
                name: "dispatch_target_minutes_snapshot",
                table: "fleet_requests");

            migrationBuilder.DropColumn(
                name: "driver_ack_target_minutes_snapshot",
                table: "fleet_requests");

            migrationBuilder.DropColumn(
                name: "emergency_policy_id",
                table: "fleet_requests");

            migrationBuilder.DropColumn(
                name: "post_review_required_snapshot",
                table: "fleet_requests");

            migrationBuilder.DropColumn(
                name: "response_target_minutes_snapshot",
                table: "fleet_requests");

            migrationBuilder.DropColumn(
                name: "enum_options_json",
                table: "fleet_capabilities");

            migrationBuilder.DropColumn(
                name: "maximum_numeric_value",
                table: "fleet_capabilities");

            migrationBuilder.DropColumn(
                name: "minimum_numeric_value",
                table: "fleet_capabilities");
        }
    }
}
