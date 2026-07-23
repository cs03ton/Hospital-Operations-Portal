using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAnnouncementCenter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "announcement_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_announcement_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "announcements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    priority = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    published_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    publish_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_featured = table.Column<bool>(type: "boolean", nullable: false),
                    show_as_popup = table.Column<bool>(type: "boolean", nullable: false),
                    show_as_banner = table.Column<bool>(type: "boolean", nullable: false),
                    requires_acknowledgement = table.Column<bool>(type: "boolean", nullable: false),
                    cover_image_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    tags = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    view_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_announcements", x => x.id);
                    table.ForeignKey(
                        name: "FK_announcements_announcement_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "announcement_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_announcements_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_announcements_users_published_by_user_id",
                        column: x => x.published_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_announcements_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "announcement_files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    announcement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    content_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    file_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    file_role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_announcement_files", x => x.id);
                    table.ForeignKey(
                        name: "FK_announcement_files_announcements_announcement_id",
                        column: x => x.announcement_id,
                        principalTable: "announcements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "announcement_reads",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    announcement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    acknowledged_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_announcement_reads", x => x.id);
                    table.ForeignKey(
                        name: "FK_announcement_reads_announcements_announcement_id",
                        column: x => x.announcement_id,
                        principalTable: "announcements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_announcement_reads_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "announcement_targets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    announcement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    target_value = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_announcement_targets", x => x.id);
                    table.ForeignKey(
                        name: "FK_announcement_targets_announcements_announcement_id",
                        column: x => x.announcement_id,
                        principalTable: "announcements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_announcement_categories_is_active_display_order",
                table: "announcement_categories",
                columns: new[] { "is_active", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_announcement_categories_name",
                table: "announcement_categories",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_announcement_files_announcement_id_file_role",
                table: "announcement_files",
                columns: new[] { "announcement_id", "file_role" });

            migrationBuilder.CreateIndex(
                name: "IX_announcement_reads_announcement_id_user_id",
                table: "announcement_reads",
                columns: new[] { "announcement_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_announcement_reads_user_id_read_at",
                table: "announcement_reads",
                columns: new[] { "user_id", "read_at" });

            migrationBuilder.CreateIndex(
                name: "IX_announcements_category_id",
                table: "announcements",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_announcements_created_by_user_id",
                table: "announcements",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_announcements_is_featured_status",
                table: "announcements",
                columns: new[] { "is_featured", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_announcements_published_by_user_id",
                table: "announcements",
                column: "published_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_announcements_show_as_banner_status",
                table: "announcements",
                columns: new[] { "show_as_banner", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_announcements_show_as_popup_status",
                table: "announcements",
                columns: new[] { "show_as_popup", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_announcements_status_publish_at_expires_at",
                table: "announcements",
                columns: new[] { "status", "publish_at", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_announcements_updated_by_user_id",
                table: "announcements",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_announcement_targets_announcement_id_target_type_target_val~",
                table: "announcement_targets",
                columns: new[] { "announcement_id", "target_type", "target_value" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "announcement_files");

            migrationBuilder.DropTable(
                name: "announcement_reads");

            migrationBuilder.DropTable(
                name: "announcement_targets");

            migrationBuilder.DropTable(
                name: "announcements");

            migrationBuilder.DropTable(
                name: "announcement_categories");
        }
    }
}
