using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFleetLineGroupDeliveryQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "line_group_delivery_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_id = table.Column<Guid>(type: "uuid", nullable: false),
                    canonical_event_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    source_event_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    deduplication_key = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    message_text = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    available_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_line_group_delivery_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_line_group_delivery_logs_line_group_destinations_destinatio~",
                        column: x => x.destination_id,
                        principalTable: "line_group_destinations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_line_group_delivery_logs_deduplication_key",
                table: "line_group_delivery_logs",
                column: "deduplication_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_line_group_delivery_logs_destination_id",
                table: "line_group_delivery_logs",
                column: "destination_id");

            migrationBuilder.CreateIndex(
                name: "IX_line_group_delivery_logs_event_id_destination_id_canonical_~",
                table: "line_group_delivery_logs",
                columns: new[] { "event_id", "destination_id", "canonical_event_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_line_group_delivery_logs_status_available_at",
                table: "line_group_delivery_logs",
                columns: new[] { "status", "available_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "line_group_delivery_logs");
        }
    }
}
