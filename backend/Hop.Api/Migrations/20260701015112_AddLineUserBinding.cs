using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLineUserBinding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "line_pairing_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_line_pairing_codes", x => x.id);
                    table.ForeignKey(
                        name: "FK_line_pairing_codes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "line_user_bindings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_user_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    picture_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    last_event_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    last_event_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    bound_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    unbound_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_line_user_bindings", x => x.id);
                    table.ForeignKey(
                        name: "FK_line_user_bindings_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_line_pairing_codes_code",
                table: "line_pairing_codes",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_line_pairing_codes_expires_at",
                table: "line_pairing_codes",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_line_pairing_codes_user_id_status",
                table: "line_pairing_codes",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_line_user_bindings_line_user_id",
                table: "line_user_bindings",
                column: "line_user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_line_user_bindings_status",
                table: "line_user_bindings",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_line_user_bindings_user_id",
                table: "line_user_bindings",
                column: "user_id",
                unique: true,
                filter: "\"user_id\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "line_pairing_codes");

            migrationBuilder.DropTable(
                name: "line_user_bindings");
        }
    }
}
