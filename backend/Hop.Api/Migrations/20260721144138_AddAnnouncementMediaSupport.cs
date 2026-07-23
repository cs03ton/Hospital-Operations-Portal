using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAnnouncementMediaSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "created_by_user_id",
                table: "announcement_files",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "announcement_images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    announcement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    stored_file_name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    relative_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    large_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    medium_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    thumbnail_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    mime_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_cover = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_announcement_images", x => x.id);
                    table.ForeignKey(
                        name: "FK_announcement_images_announcements_announcement_id",
                        column: x => x.announcement_id,
                        principalTable: "announcements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_announcement_images_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_announcement_images_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_announcement_files_created_by_user_id",
                table: "announcement_files",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_announcement_images_announcement_id",
                table: "announcement_images",
                column: "announcement_id",
                unique: true,
                filter: "is_cover = true");

            migrationBuilder.CreateIndex(
                name: "IX_announcement_images_announcement_id_display_order",
                table: "announcement_images",
                columns: new[] { "announcement_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_announcement_images_announcement_id_is_cover",
                table: "announcement_images",
                columns: new[] { "announcement_id", "is_cover" });

            migrationBuilder.CreateIndex(
                name: "IX_announcement_images_created_by_user_id",
                table: "announcement_images",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_announcement_images_updated_by_user_id",
                table: "announcement_images",
                column: "updated_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_announcement_files_users_created_by_user_id",
                table: "announcement_files",
                column: "created_by_user_id",
                principalTable: "users",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_announcement_files_users_created_by_user_id",
                table: "announcement_files");

            migrationBuilder.DropTable(
                name: "announcement_images");

            migrationBuilder.DropIndex(
                name: "IX_announcement_files_created_by_user_id",
                table: "announcement_files");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                table: "announcement_files");
        }
    }
}
