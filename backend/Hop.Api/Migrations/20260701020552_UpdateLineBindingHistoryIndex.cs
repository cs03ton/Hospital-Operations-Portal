using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLineBindingHistoryIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_line_user_bindings_user_id",
                table: "line_user_bindings");

            migrationBuilder.CreateIndex(
                name: "IX_line_user_bindings_user_id",
                table: "line_user_bindings",
                column: "user_id",
                unique: true,
                filter: "\"user_id\" IS NOT NULL AND \"status\" = 'Bound'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_line_user_bindings_user_id",
                table: "line_user_bindings");

            migrationBuilder.CreateIndex(
                name: "IX_line_user_bindings_user_id",
                table: "line_user_bindings",
                column: "user_id",
                unique: true,
                filter: "\"user_id\" IS NOT NULL");
        }
    }
}
