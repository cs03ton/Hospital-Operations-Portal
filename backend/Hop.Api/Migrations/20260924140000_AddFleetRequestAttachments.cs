using Hop.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Hop.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260924140000_AddFleetRequestAttachments")]
public sealed class AddFleetRequestAttachments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE TABLE fleet_request_attachments (
            id uuid PRIMARY KEY,
            fleet_request_id uuid NOT NULL REFERENCES fleet_requests(id) ON DELETE RESTRICT,
            uploaded_by_user_id uuid NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
            original_file_name varchar(260) NOT NULL,
            stored_path varchar(1000) NOT NULL,
            content_type varchar(100) NOT NULL,
            file_size bigint NOT NULL,
            created_at timestamptz NOT NULL,
            is_deleted boolean NOT NULL DEFAULT false,
            deleted_at timestamptz NULL,
            deleted_by_user_id uuid NULL REFERENCES users(id) ON DELETE RESTRICT,
            CONSTRAINT ck_fleet_request_attachment_size CHECK (file_size > 0 AND file_size <= 5242880)
        );
        CREATE INDEX ix_fleet_request_attachments_request_active ON fleet_request_attachments(fleet_request_id, is_deleted);
        CREATE INDEX ix_fleet_request_attachments_uploaded_by_user_id ON fleet_request_attachments(uploaded_by_user_id);
        CREATE INDEX ix_fleet_request_attachments_deleted_by_user_id ON fleet_request_attachments(deleted_by_user_id);
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("DROP TABLE fleet_request_attachments;");
}
