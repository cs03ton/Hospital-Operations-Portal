-- HOP: apply 20260924150000_AddCentralLineGroupRepairTeam in DBeaver.
-- Fixes HTTP 500 on GET /api/admin/line-groups when the deployed API expects
-- repair_team_code / repair_team_assigned_at but PRD has not applied this migration.
-- Run the whole script on the PRD database with Stop on error. If the connection
-- has a previous 25P02 error, run ROLLBACK first. This does not assign teams,
-- enable destinations, change credentials, or send LINE messages.

BEGIN;

DO $precheck$
BEGIN
    IF to_regclass('public."__EFMigrationsHistory"') IS NULL
       OR to_regclass('public.line_group_destinations') IS NULL THEN
        RAISE EXCEPTION 'HOP migration history or line_group_destinations is missing';
    END IF;
    IF NOT EXISTS (
        SELECT 1 FROM "__EFMigrationsHistory"
        WHERE "MigrationId" = '20260924140000_AddFleetRequestAttachments'
    ) THEN
        RAISE EXCEPTION 'Apply HOP migrations through 20260924140000 first';
    END IF;
    IF EXISTS (
        SELECT 1 FROM "__EFMigrationsHistory"
        WHERE "MigrationId" = '20260924150000_AddCentralLineGroupRepairTeam'
    ) THEN
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'line_group_destinations'
              AND column_name = 'repair_team_code'
        ) OR NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'line_group_destinations'
              AND column_name = 'repair_team_assigned_at'
        ) THEN
            RAISE EXCEPTION 'Migration history says applied, but repair team columns are missing';
        END IF;
    ELSIF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'line_group_destinations'
          AND column_name IN ('repair_team_code', 'repair_team_assigned_at')
    ) THEN
        RAISE EXCEPTION 'Repair team columns exist without migration history; inspect partial/manual migration';
    END IF;
END $precheck$;

DO $migration$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM "__EFMigrationsHistory"
        WHERE "MigrationId" = '20260924150000_AddCentralLineGroupRepairTeam'
    ) THEN
        ALTER TABLE line_group_destinations ADD COLUMN repair_team_code varchar(20);
        ALTER TABLE line_group_destinations ADD COLUMN repair_team_assigned_at timestamptz;
        ALTER TABLE line_group_destinations ADD CONSTRAINT ck_line_group_repair_team
            CHECK ((repair_team_code IS NULL AND repair_team_assigned_at IS NULL)
                OR (repair_team_code IN ('IT', 'GENERAL') AND repair_team_assigned_at IS NOT NULL));
        CREATE INDEX ix_line_group_destinations_repair_team_code
            ON line_group_destinations(repair_team_code) WHERE repair_team_code IS NOT NULL;
        INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
        VALUES ('20260924150000_AddCentralLineGroupRepairTeam', '9.0.15');
    END IF;
END $migration$;

DO $postcheck$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM "__EFMigrationsHistory"
        WHERE "MigrationId" = '20260924150000_AddCentralLineGroupRepairTeam'
    ) OR NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'line_group_destinations'
          AND column_name = 'repair_team_code'
    ) OR NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'line_group_destinations'
          AND column_name = 'repair_team_assigned_at'
    ) THEN
        RAISE EXCEPTION 'Central line group migration verification failed';
    END IF;
END $postcheck$;

COMMIT;

SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20260924150000_AddCentralLineGroupRepairTeam';

SELECT column_name, data_type
FROM information_schema.columns
WHERE table_schema = 'public' AND table_name = 'line_group_destinations'
  AND column_name IN ('repair_team_code', 'repair_team_assigned_at')
ORDER BY column_name;
