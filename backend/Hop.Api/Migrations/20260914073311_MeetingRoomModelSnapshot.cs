using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class MeetingRoomModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "meeting_rooms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    location = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    concurrency_token = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meeting_rooms", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "meeting_room_bookings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    booker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    purpose = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    start_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    attendee_count = table.Column<int>(type: "integer", nullable: false),
                    meeting_link = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    additional_request = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    cancellation_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    cancelled_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    concurrency_token = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meeting_room_bookings", x => x.id);
                    table.ForeignKey(
                        name: "FK_meeting_room_bookings_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_meeting_room_bookings_meeting_rooms_room_id",
                        column: x => x.room_id,
                        principalTable: "meeting_rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_meeting_room_bookings_users_booker_id",
                        column: x => x.booker_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_meeting_room_bookings_users_cancelled_by_id",
                        column: x => x.cancelled_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "meeting_room_attachments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uploaded_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    stored_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    content_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meeting_room_attachments", x => x.id);
                    table.ForeignKey(
                        name: "FK_meeting_room_attachments_meeting_room_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "meeting_room_bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_meeting_room_attachments_users_uploaded_by_id",
                        column: x => x.uploaded_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "meeting_room_booking_histories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    from_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    to_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    detail = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meeting_room_booking_histories", x => x.id);
                    table.ForeignKey(
                        name: "FK_meeting_room_booking_histories_meeting_room_bookings_bookin~",
                        column: x => x.booking_id,
                        principalTable: "meeting_room_bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_meeting_room_booking_histories_users_actor_id",
                        column: x => x.actor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_meeting_room_attachments_booking_id_created_at",
                table: "meeting_room_attachments",
                columns: new[] { "booking_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_meeting_room_attachments_uploaded_by_id",
                table: "meeting_room_attachments",
                column: "uploaded_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_meeting_room_booking_histories_actor_id",
                table: "meeting_room_booking_histories",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "IX_meeting_room_booking_histories_booking_id_created_at",
                table: "meeting_room_booking_histories",
                columns: new[] { "booking_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_meeting_room_bookings_booker_id_start_at",
                table: "meeting_room_bookings",
                columns: new[] { "booker_id", "start_at" });

            migrationBuilder.CreateIndex(
                name: "IX_meeting_room_bookings_cancelled_by_id",
                table: "meeting_room_bookings",
                column: "cancelled_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_meeting_room_bookings_department_id",
                table: "meeting_room_bookings",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_meeting_room_bookings_number",
                table: "meeting_room_bookings",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_meeting_room_bookings_room_id_start_at_end_at",
                table: "meeting_room_bookings",
                columns: new[] { "room_id", "start_at", "end_at" });

            migrationBuilder.CreateIndex(
                name: "IX_meeting_rooms_code",
                table: "meeting_rooms",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "meeting_room_attachments");

            migrationBuilder.DropTable(
                name: "meeting_room_booking_histories");

            migrationBuilder.DropTable(
                name: "meeting_room_bookings");

            migrationBuilder.DropTable(
                name: "meeting_rooms");
        }
    }
}
