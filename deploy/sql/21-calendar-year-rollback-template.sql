-- Compensating rollback template for a previously reviewed repair batch.
ROLLBACK;
BEGIN;
CREATE TEMP TABLE approved_calendar_rollbacks (
  table_name text NOT NULL, column_name text NOT NULL, primary_key_column text NOT NULL,
  primary_key_value text NOT NULL, expected_current_value text NOT NULL, restore_value text NOT NULL
) ON COMMIT DROP;
-- Populate from the exact pre-change evidence retained from script 20.
DO $rollback$
DECLARE r record; changed integer;
BEGIN
  IF NOT EXISTS (SELECT 1 FROM approved_calendar_rollbacks) THEN RAISE EXCEPTION 'No rollback rows supplied'; END IF;
  FOR r IN SELECT * FROM approved_calendar_rollbacks LOOP
    EXECUTE format('UPDATE %I SET %I=$1::date WHERE %I::text=$2 AND %I::text=$3',
      r.table_name,r.column_name,r.primary_key_column,r.column_name)
      USING r.restore_value,r.primary_key_value,r.expected_current_value;
    GET DIAGNOSTICS changed = ROW_COUNT;
    IF changed <> 1 THEN RAISE EXCEPTION 'Rollback precondition failed for %.%, key %',r.table_name,r.column_name,r.primary_key_value; END IF;
  END LOOP;
END $rollback$;
-- Replace with COMMIT only after verifying the restored values and dependent totals.
ROLLBACK;
