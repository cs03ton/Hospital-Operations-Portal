using Hop.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Hop.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260924120000_AddMeetingRoomPhotos")]
public sealed class AddMeetingRoomPhotos : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("photo_path", "meeting_rooms", type: "character varying(1000)", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<string>("photo_content_type", "meeting_rooms", type: "character varying(100)", maxLength: 100, nullable: true);
        migrationBuilder.AddColumn<DateTime>("photo_updated_at", "meeting_rooms", type: "timestamp with time zone", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("photo_path", "meeting_rooms");
        migrationBuilder.DropColumn("photo_content_type", "meeting_rooms");
        migrationBuilder.DropColumn("photo_updated_at", "meeting_rooms");
    }
}
