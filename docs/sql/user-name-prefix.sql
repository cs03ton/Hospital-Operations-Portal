-- PostgreSQL / DBeaver: select the HOP database, then Execute SQL Script (Alt+X).
-- Installs only AddUserNamePrefix; does not change any existing names.
-- Requires an existing HOP database with public.users and EF migration history.
BEGIN;
SET LOCAL lock_timeout = '10s';

-- Serialize repeated executions of this installation script.
SELECT pg_advisory_xact_lock(20261106, 30602);

DO $migration$
DECLARE
    col record;
BEGIN
    IF to_regclass('public.users') IS NULL
       OR to_regclass('public."__EFMigrationsHistory"') IS NULL THEN
        RAISE EXCEPTION 'Select an existing HOP database: public.users and public.__EFMigrationsHistory are required.';
    END IF;

    ALTER TABLE public.users ADD COLUMN IF NOT EXISTS name_prefix varchar(100) NULL;

    SELECT data_type, character_maximum_length, is_nullable, column_default,
           is_generated, is_identity
      INTO col
      FROM information_schema.columns
     WHERE table_schema = 'public' AND table_name = 'users'
       AND column_name = 'name_prefix';

    IF col.data_type IS DISTINCT FROM 'character varying'
       OR col.character_maximum_length IS DISTINCT FROM 100
       OR col.is_nullable IS DISTINCT FROM 'YES'
       OR col.column_default IS NOT NULL
       OR col.is_generated IS DISTINCT FROM 'NEVER'
       OR col.is_identity IS DISTINCT FROM 'NO' THEN
        RAISE EXCEPTION 'public.users.name_prefix must be nullable varchar(100), without default, identity or generated expression. No changes committed.';
    END IF;

    INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261106030602_AddUserNamePrefix', '9.0.15')
    ON CONFLICT ("MigrationId") DO NOTHING;
END
$migration$;

COMMIT;

-- Verify installation (read only).
SELECT column_name, data_type, character_maximum_length, is_nullable, column_default
FROM information_schema.columns
WHERE table_schema = 'public' AND table_name = 'users' AND column_name = 'name_prefix';

SELECT "MigrationId", "ProductVersion"
FROM public."__EFMigrationsHistory"
WHERE "MigrationId" = '20261106030602_AddUserNamePrefix';

-- Optional read example; does not update user data:
-- PostgreSQL ADD COLUMN appends physically; use this projection to display
-- name_prefix between employee_code and fullname in DBeaver's result grid.
-- SELECT id, employee_code, name_prefix, fullname, username
-- FROM public.users ORDER BY fullname LIMIT 50;
