using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRepairManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "repair_teams",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    responsible_department = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_repair_teams", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "repair_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    team_code = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    concurrency_token = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_repair_categories", x => x.id);
                    table.ForeignKey(
                        name: "FK_repair_categories_repair_teams_team_code",
                        column: x => x.team_code,
                        principalTable: "repair_teams",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "repair_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    requester_id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_code = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    title = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    description = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    location = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    contact = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    priority = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    current_round = table.Column<int>(type: "integer", nullable: false),
                    has_started = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    concurrency_token = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_repair_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_repair_requests_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_repair_requests_repair_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "repair_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_repair_requests_repair_teams_team_code",
                        column: x => x.team_code,
                        principalTable: "repair_teams",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_repair_requests_users_requester_id",
                        column: x => x.requester_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "repair_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    round = table.Column<int>(type: "integer", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    from_status = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    to_status = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    note = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    solver_id = table.Column<Guid>(type: "uuid", nullable: true),
                    priority = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_repair_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_repair_events_repair_requests_request_id",
                        column: x => x.request_id,
                        principalTable: "repair_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_repair_events_users_actor_id",
                        column: x => x.actor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_repair_events_users_solver_id",
                        column: x => x.solver_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "repair_rounds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    accepted_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    acceptance_note = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_repair_rounds", x => x.id);
                    table.ForeignKey(
                        name: "FK_repair_rounds_repair_requests_request_id",
                        column: x => x.request_id,
                        principalTable: "repair_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_repair_rounds_users_accepted_by_id",
                        column: x => x.accepted_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "repair_waiting_periods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    round = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_repair_waiting_periods", x => x.id);
                    table.ForeignKey(
                        name: "FK_repair_waiting_periods_repair_requests_request_id",
                        column: x => x.request_id,
                        principalTable: "repair_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "repair_contributors",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_repair_contributors", x => new { x.event_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_repair_contributors_repair_events_event_id",
                        column: x => x.event_id,
                        principalTable: "repair_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_repair_contributors_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "repair_dispatches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_code = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    available_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_code = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    concurrency_token = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_repair_dispatches", x => x.id);
                    table.ForeignKey(
                        name: "FK_repair_dispatches_repair_events_event_id",
                        column: x => x.event_id,
                        principalTable: "repair_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_repair_dispatches_repair_requests_request_id",
                        column: x => x.request_id,
                        principalTable: "repair_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_repair_dispatches_repair_teams_team_code",
                        column: x => x.team_code,
                        principalTable: "repair_teams",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "repair_images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uploaded_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stored_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_repair_images", x => x.id);
                    table.ForeignKey(
                        name: "FK_repair_images_repair_events_event_id",
                        column: x => x.event_id,
                        principalTable: "repair_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_repair_images_repair_requests_request_id",
                        column: x => x.request_id,
                        principalTable: "repair_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_repair_images_users_uploaded_by_id",
                        column: x => x.uploaded_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_repair_categories_name",
                table: "repair_categories",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_repair_categories_team_code",
                table: "repair_categories",
                column: "team_code");

            migrationBuilder.CreateIndex(
                name: "IX_repair_contributors_user_id",
                table: "repair_contributors",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_repair_dispatches_event_id_team_code",
                table: "repair_dispatches",
                columns: new[] { "event_id", "team_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_repair_dispatches_request_id",
                table: "repair_dispatches",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "IX_repair_dispatches_status_available_at",
                table: "repair_dispatches",
                columns: new[] { "status", "available_at" });

            migrationBuilder.CreateIndex(
                name: "IX_repair_dispatches_team_code",
                table: "repair_dispatches",
                column: "team_code");

            migrationBuilder.CreateIndex(
                name: "IX_repair_events_actor_id",
                table: "repair_events",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "IX_repair_events_request_id_created_at",
                table: "repair_events",
                columns: new[] { "request_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_repair_events_solver_id",
                table: "repair_events",
                column: "solver_id");

            migrationBuilder.CreateIndex(
                name: "IX_repair_images_event_id",
                table: "repair_images",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "IX_repair_images_request_id",
                table: "repair_images",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "IX_repair_images_uploaded_by_id",
                table: "repair_images",
                column: "uploaded_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_repair_requests_category_id",
                table: "repair_requests",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_repair_requests_department_id",
                table: "repair_requests",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_repair_requests_number",
                table: "repair_requests",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_repair_requests_requester_id_created_at",
                table: "repair_requests",
                columns: new[] { "requester_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_repair_requests_team_code_status_created_at",
                table: "repair_requests",
                columns: new[] { "team_code", "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_repair_rounds_accepted_by_id",
                table: "repair_rounds",
                column: "accepted_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_repair_rounds_request_id_number",
                table: "repair_rounds",
                columns: new[] { "request_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_repair_waiting_periods_request_id",
                table: "repair_waiting_periods",
                column: "request_id");
            migrationBuilder.Sql(Hop.Api.Data.RepairSeed.Sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "repair_contributors");

            migrationBuilder.DropTable(
                name: "repair_dispatches");

            migrationBuilder.DropTable(
                name: "repair_images");

            migrationBuilder.DropTable(
                name: "repair_rounds");

            migrationBuilder.DropTable(
                name: "repair_waiting_periods");

            migrationBuilder.DropTable(
                name: "repair_events");

            migrationBuilder.DropTable(
                name: "repair_requests");

            migrationBuilder.DropTable(
                name: "repair_categories");

            migrationBuilder.DropTable(
                name: "repair_teams");
        }
    }
}
