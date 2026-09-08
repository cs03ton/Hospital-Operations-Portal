using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLineGroupCustomEndpointConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "client_id",
                table: "line_group_destinations",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "client_secret_protected",
                table: "line_group_destinations",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "delivery_provider",
                table: "line_group_destinations",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "LINE_MESSAGING_API");

            migrationBuilder.AddColumn<string>(
                name: "endpoint_url",
                table: "line_group_destinations",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "client_id",
                table: "line_group_destinations");

            migrationBuilder.DropColumn(
                name: "client_secret_protected",
                table: "line_group_destinations");

            migrationBuilder.DropColumn(
                name: "delivery_provider",
                table: "line_group_destinations");

            migrationBuilder.DropColumn(
                name: "endpoint_url",
                table: "line_group_destinations");
        }
    }
}
