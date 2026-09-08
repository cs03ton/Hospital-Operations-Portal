using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFleetLineGroupRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "line_group_destinations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_group_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    module = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    attention_required = table.Column<bool>(type: "boolean", nullable: false),
                    attention_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    first_detected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_detected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    confirmed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    disabled_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    disabled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    concurrency_token = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_line_group_destinations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "line_webhook_inbox",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    webhook_event_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    event_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    source_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    source_group_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    available_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_line_webhook_inbox", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "line_group_event_subscriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_line_group_event_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_line_group_event_subscriptions_line_group_destinations_dest~",
                        column: x => x.destination_id,
                        principalTable: "line_group_destinations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_line_group_destinations_line_group_id",
                table: "line_group_destinations",
                column: "line_group_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_line_group_destinations_module_status",
                table: "line_group_destinations",
                columns: new[] { "module", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_line_group_event_subscriptions_destination_id_event_type",
                table: "line_group_event_subscriptions",
                columns: new[] { "destination_id", "event_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_line_webhook_inbox_status_available_at",
                table: "line_webhook_inbox",
                columns: new[] { "status", "available_at" });

            migrationBuilder.CreateIndex(
                name: "IX_line_webhook_inbox_webhook_event_id",
                table: "line_webhook_inbox",
                column: "webhook_event_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "line_group_event_subscriptions");

            migrationBuilder.DropTable(
                name: "line_webhook_inbox");

            migrationBuilder.DropTable(
                name: "line_group_destinations");
        }
    }
}
