using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class FleetProductionHardeningUatReadiness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "abort_reason",
                table: "fleet_trip_records",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "aborted_at",
                table: "fleet_trip_records",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "aborted_by_user_id",
                table: "fleet_trip_records",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_aborted",
                table: "fleet_trip_records",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_token",
                table: "fleet_assignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("""
                UPDATE fleet_assignments
                SET concurrency_token = md5(id::text || created_at::text)::uuid
                WHERE concurrency_token = '00000000-0000-0000-0000-000000000000';

                CREATE OR REPLACE FUNCTION protect_fleet_assignment_immutable_fields()
                RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF NEW.fleet_request_id IS DISTINCT FROM OLD.fleet_request_id
                       OR NEW.vehicle_id IS DISTINCT FROM OLD.vehicle_id
                       OR NEW.driver_user_id IS DISTINCT FROM OLD.driver_user_id
                       OR NEW.assigned_by_user_id IS DISTINCT FROM OLD.assigned_by_user_id
                       OR NEW.assigned_at IS DISTINCT FROM OLD.assigned_at
                       OR NEW.assignment_reason IS DISTINCT FROM OLD.assignment_reason
                       OR NEW.replaced_assignment_id IS DISTINCT FROM OLD.replaced_assignment_id
                       OR NEW.created_at IS DISTINCT FROM OLD.created_at THEN
                        RAISE EXCEPTION 'Fleet assignment business fields are immutable';
                    END IF;
                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER trg_fleet_assignments_immutable
                BEFORE UPDATE ON fleet_assignments
                FOR EACH ROW EXECUTE FUNCTION protect_fleet_assignment_immutable_fields();
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_fleet_trip_abort_details",
                table: "fleet_trip_records",
                sql: "NOT is_aborted OR (aborted_at IS NOT NULL AND aborted_by_user_id IS NOT NULL AND length(trim(abort_reason)) > 0)");

            migrationBuilder.CreateIndex(
                name: "IX_fleet_trip_records_aborted_by_user_id",
                table: "fleet_trip_records",
                column: "aborted_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_fleet_trip_records_users_aborted_by_user_id",
                table: "fleet_trip_records",
                column: "aborted_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_fleet_assignments_immutable ON fleet_assignments; DROP FUNCTION IF EXISTS protect_fleet_assignment_immutable_fields();");

            migrationBuilder.DropCheckConstraint(
                name: "ck_fleet_trip_abort_details",
                table: "fleet_trip_records");
            migrationBuilder.DropForeignKey(
                name: "FK_fleet_trip_records_users_aborted_by_user_id",
                table: "fleet_trip_records");

            migrationBuilder.DropIndex(
                name: "IX_fleet_trip_records_aborted_by_user_id",
                table: "fleet_trip_records");

            migrationBuilder.DropColumn(
                name: "abort_reason",
                table: "fleet_trip_records");

            migrationBuilder.DropColumn(
                name: "aborted_at",
                table: "fleet_trip_records");

            migrationBuilder.DropColumn(
                name: "aborted_by_user_id",
                table: "fleet_trip_records");

            migrationBuilder.DropColumn(
                name: "is_aborted",
                table: "fleet_trip_records");

            migrationBuilder.DropColumn(
                name: "concurrency_token",
                table: "fleet_assignments");
        }
    }
}
