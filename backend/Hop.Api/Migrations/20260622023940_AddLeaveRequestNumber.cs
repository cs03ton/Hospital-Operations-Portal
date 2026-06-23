using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveRequestNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "request_number",
                table: "leave_requests",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.Sql("""
                WITH numbered AS (
                    SELECT
                        id,
                        'LV-' || to_char(created_at, 'YYYYMM') || '-' ||
                        lpad(row_number() OVER (
                            PARTITION BY to_char(created_at, 'YYYYMM')
                            ORDER BY created_at, id
                        )::text, 3, '0') AS generated_request_number
                    FROM leave_requests
                    WHERE request_number IS NULL
                )
                UPDATE leave_requests AS request
                SET request_number = numbered.generated_request_number
                FROM numbered
                WHERE request.id = numbered.id;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_leave_requests_request_number",
                table: "leave_requests",
                column: "request_number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_leave_requests_request_number",
                table: "leave_requests");

            migrationBuilder.DropColumn(
                name: "request_number",
                table: "leave_requests");
        }
    }
}
