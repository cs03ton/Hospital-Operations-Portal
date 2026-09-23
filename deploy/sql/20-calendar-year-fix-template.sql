-- Guarded repair template. Populate only rows reviewed from script 19.
-- Never run against PRD without a reviewed candidate list and backup.
ROLLBACK;
BEGIN;
CREATE TEMP TABLE approved_calendar_repairs (
  table_name text NOT NULL, column_name text NOT NULL, primary_key_column text NOT NULL,
  primary_key_value text NOT NULL, expected_old_value text NOT NULL, approved_new_value text NOT NULL
) ON COMMIT DROP;

-- Example only; copy reviewed values and remove the comment markers:
-- INSERT INTO approved_calendar_repairs VALUES
-- ('leave_requests','start_date','id','<uuid>','2569-09-24','2026-09-24');

DO $fix$
DECLARE r record; changed integer;
BEGIN
  IF NOT EXISTS (SELECT 1 FROM approved_calendar_repairs) THEN
    RAISE EXCEPTION 'No reviewed repairs supplied; no changes made';
  END IF;
  FOR r IN SELECT * FROM approved_calendar_repairs LOOP
    EXECUTE format('UPDATE %I SET %I=$1::date WHERE %I::text=$2 AND %I::text=$3',
      r.table_name,r.column_name,r.primary_key_column,r.column_name)
      USING r.approved_new_value,r.primary_key_value,r.expected_old_value;
    GET DIAGNOSTICS changed = ROW_COUNT;
    IF changed <> 1 THEN RAISE EXCEPTION 'Expected one row for %.%, key %; transaction rolled back',r.table_name,r.column_name,r.primary_key_value; END IF;
  END LOOP;
END $fix$;

SELECT * FROM approved_calendar_repairs ORDER BY table_name,primary_key_value,column_name;
-- Replace with COMMIT only after reviewing every result and related business totals.
ROLLBACK;
