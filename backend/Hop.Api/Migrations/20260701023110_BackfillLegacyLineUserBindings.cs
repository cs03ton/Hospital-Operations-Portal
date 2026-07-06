using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hop.Api.Migrations
{
    /// <inheritdoc />
    public partial class BackfillLegacyLineUserBindings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO line_user_bindings (
                    id,
                    line_user_id,
                    user_id,
                    status,
                    bound_at,
                    created_at,
                    updated_at
                )
                SELECT
                    md5('line-binding:' || ranked.line_user_id)::uuid,
                    ranked.line_user_id,
                    ranked.id,
                    'Bound',
                    COALESCE(ranked.updated_at, ranked.created_at, NOW()),
                    COALESCE(ranked.created_at, NOW()),
                    NOW()
                FROM (
                    SELECT
                        u.id,
                        btrim(u.line_user_id) AS line_user_id,
                        u.created_at,
                        u.updated_at,
                        ROW_NUMBER() OVER (
                            PARTITION BY btrim(u.line_user_id)
                            ORDER BY COALESCE(u.updated_at, u.created_at, NOW()) DESC, u.id
                        ) AS row_number
                    FROM users u
                    WHERE u.line_user_id IS NOT NULL
                        AND btrim(u.line_user_id) <> ''
                ) ranked
                WHERE ranked.row_number = 1
                    AND NOT EXISTS (
                        SELECT 1
                        FROM line_user_bindings existing
                        WHERE existing.line_user_id = ranked.line_user_id
                    )
                    AND NOT EXISTS (
                        SELECT 1
                        FROM line_user_bindings existing
                        WHERE existing.user_id = ranked.id
                            AND existing.status = 'Bound'
                    );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM line_user_bindings
                WHERE id IN (
                    SELECT md5('line-binding:' || btrim(u.line_user_id))::uuid
                    FROM users u
                    WHERE u.line_user_id IS NOT NULL
                        AND btrim(u.line_user_id) <> ''
                )
                    AND last_event_type IS NULL
                    AND display_name IS NULL
                    AND picture_url IS NULL;
                """);
        }
    }
}
