using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLineConnectTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "line_connect_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    short_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_ip = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    line_user_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_line_connect_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_line_connect_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_line_connect_tokens_expires_at",
                table: "line_connect_tokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_line_connect_tokens_short_code",
                table: "line_connect_tokens",
                column: "short_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_line_connect_tokens_token",
                table: "line_connect_tokens",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_line_connect_tokens_user_id_status",
                table: "line_connect_tokens",
                columns: new[] { "user_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "line_connect_tokens");
        }
    }
}
