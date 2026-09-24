using Hop.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Hop.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260924130000_AddMeetingRoomAttendees")]
public sealed class AddMeetingRoomAttendees : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "meeting_room_booking_attendees",
            columns: table => new
            {
                booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                is_booker = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_meeting_room_booking_attendees", x => new { x.booking_id, x.user_id });
                table.ForeignKey("fk_meeting_room_booking_attendees_booking", x => x.booking_id, "meeting_room_bookings", "id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("fk_meeting_room_booking_attendees_user", x => x.user_id, "users", "id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex("ix_meeting_room_booking_attendees_user_id", "meeting_room_booking_attendees", "user_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("meeting_room_booking_attendees");
}
