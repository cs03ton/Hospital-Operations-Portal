using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFleetRequestStatusDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fleet_status_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    domain = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    thai_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fleet_status_definitions", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "fleet_status_definitions",
                columns: new[] { "id", "code", "created_at", "description", "domain", "is_active", "sort_order", "thai_name", "updated_at" },
                values: new object[,]
                {
                    { new Guid("019fd100-0000-7000-8000-000000000001"), "DRAFT", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, "REQUEST", true, 10, "แบบร่าง", null },
                    { new Guid("019fd100-0000-7000-8000-000000000002"), "PENDING_DISPATCH", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, "REQUEST", true, 20, "รอจัดรถและคนขับ", null },
                    { new Guid("019fd100-0000-7000-8000-000000000003"), "PENDING_ADMIN_REVIEW", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, "REQUEST", true, 30, "รอหัวหน้าฝ่ายบริหารตรวจสอบ", null },
                    { new Guid("019fd100-0000-7000-8000-000000000004"), "PENDING_DIRECTOR", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, "REQUEST", true, 40, "รอผู้อำนวยการอนุมัติ", null },
                    { new Guid("019fd100-0000-7000-8000-000000000005"), "APPROVED", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, "REQUEST", true, 50, "อนุมัติแล้ว", null },
                    { new Guid("019fd100-0000-7000-8000-000000000006"), "PENDING_DRIVER_ACK", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, "REQUEST", true, 60, "รอคนขับรับทราบ", null },
                    { new Guid("019fd100-0000-7000-8000-000000000007"), "READY", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, "REQUEST", true, 70, "พร้อมเดินทาง", null },
                    { new Guid("019fd100-0000-7000-8000-000000000008"), "IN_PROGRESS", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, "REQUEST", true, 80, "กำลังปฏิบัติงาน", null },
                    { new Guid("019fd100-0000-7000-8000-000000000009"), "COMPLETED", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, "REQUEST", true, 90, "เสร็จสิ้น", null },
                    { new Guid("019fd100-0000-7000-8000-000000000010"), "CANCELLATION_PENDING", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, "REQUEST", true, 100, "รอพิจารณายกเลิก", null },
                    { new Guid("019fd100-0000-7000-8000-000000000011"), "RETURNED", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, "REQUEST", true, 110, "ส่งกลับแก้ไข", null },
                    { new Guid("019fd100-0000-7000-8000-000000000012"), "REJECTED", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, "REQUEST", true, 120, "ไม่รับคำขอ", null },
                    { new Guid("019fd100-0000-7000-8000-000000000013"), "CANCELLED", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, "REQUEST", true, 130, "ยกเลิก", null },
                    { new Guid("019fd100-0000-7000-8000-000000000014"), "ABORTED", new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, "REQUEST", true, 140, "ยุติการเดินทาง", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_fleet_status_definitions_domain_code",
                table: "fleet_status_definitions",
                columns: new[] { "domain", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fleet_status_definitions_domain_is_active_sort_order",
                table: "fleet_status_definitions",
                columns: new[] { "domain", "is_active", "sort_order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fleet_status_definitions");
        }
    }
}
