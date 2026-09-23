-- DBeaver: execute as a script. Inspection only: no application rows are changed.
-- If the connection previously errored, execute ROLLBACK; separately first.
-- Results remain in temporary tables only until COMMIT below.
BEGIN ISOLATION LEVEL REPEATABLE READ;
SET LOCAL TIME ZONE 'Asia/Bangkok';
SET LOCAL statement_timeout = '60s';
CREATE TEMP TABLE hop_calendar_audit (
    table_name text, column_name text, row_id text, stored_value text
) ON COMMIT DROP;
DO $audit$
DECLARE c record;
BEGIN
    FOR c IN
        SELECT table_schema, table_name, column_name, data_type
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name IN (SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE')
          AND (data_type IN ('date', 'timestamp without time zone', 'timestamp with time zone')
               OR (data_type IN ('integer', 'smallint', 'bigint') AND
                   column_name IN ('year', 'fiscal_year', 'target_fiscal_year', 'from_fiscal_year', 'to_fiscal_year', 'manufacture_year')))
    LOOP
        IF c.data_type IN ('date', 'timestamp without time zone', 'timestamp with time zone') THEN
            EXECUTE format(
                'INSERT INTO hop_calendar_audit SELECT %L, %L, COALESCE(to_jsonb(t)->>''id'', t.ctid::text), t.%I::text
                 FROM %I.%I t WHERE t.%I IS NOT NULL AND
                 (NOT isfinite(t.%I) OR EXTRACT(YEAR FROM t.%I) NOT BETWEEN 1900 AND 2100)',
                c.table_name, c.column_name, c.column_name, c.table_schema, c.table_name,
                c.column_name, c.column_name, c.column_name);
        ELSE
            EXECUTE format(
                'INSERT INTO hop_calendar_audit SELECT %L, %L, COALESCE(to_jsonb(t)->>''id'', t.ctid::text), t.%I::text
                 FROM %I.%I t WHERE t.%I IS NOT NULL AND t.%I NOT BETWEEN 1900 AND 2100',
                c.table_name, c.column_name, c.column_name, c.table_schema, c.table_name, c.column_name, c.column_name);
        END IF;
    END LOOP;
END
$audit$;
SELECT table_name, column_name, count(*) AS suspect_rows FROM hop_calendar_audit GROUP BY 1, 2 ORDER BY 1, 2;
SELECT * FROM hop_calendar_audit ORDER BY table_name, column_name, row_id;

-- Verify the specific request, including a visible NOT_FOUND result if absent.
SELECT expected.id, r.request_number, r.start_date, r.end_date, r.total_days, r.status,
       CASE WHEN r.id IS NULL THEN 'NOT_FOUND'
            WHEN r.start_date = DATE '2026-09-24' AND r.end_date = DATE '2026-09-25' AND r.total_days = 2 THEN 'DATES_AND_COUNT_CORRECT'
            ELSE 'REVIEW_REQUIRED' END AS check_result
FROM (VALUES ('01a0b2c5-06d4-75f4-bf0f-f869ba1b5183'::uuid)) expected(id)
LEFT JOIN leave_requests r ON r.id = expected.id;

-- Reconcile cached totals against existing request values; discrepancies need review.
-- This does not assume that a previous date repair also repaired days or balances.
WITH totals AS (
    SELECT r.user_id, r.leave_type_id,
           EXTRACT(YEAR FROM r.start_date)::int + CASE WHEN t.use_fiscal_year AND EXTRACT(MONTH FROM r.start_date) >= 10 THEN 1 ELSE 0 END AS year,
           SUM(CASE WHEN r.status = 'Approved' THEN r.total_days ELSE 0 END) AS used_days,
           SUM(CASE WHEN r.status = 'Pending' THEN r.total_days ELSE 0 END) AS pending_days
    FROM leave_requests r JOIN leave_types t ON t.id = r.leave_type_id
    GROUP BY 1, 2, 3
)
SELECT COALESCE(b.user_id, t.user_id) AS user_id, COALESCE(b.leave_type_id, t.leave_type_id) AS leave_type_id,
       COALESCE(b.year, t.year) AS year, b.used_days AS stored_used, b.pending_days AS stored_pending,
       COALESCE(t.used_days, 0) AS request_used, COALESCE(t.pending_days, 0) AS request_pending,
       'REVIEW_LEDGER_AND_CANCELLATIONS_BEFORE_REPAIR' AS action
FROM leave_balances b FULL JOIN totals t USING (user_id, leave_type_id, year)
WHERE b.used_days IS DISTINCT FROM COALESCE(t.used_days, 0)
   OR b.pending_days IS DISTINCT FROM COALESCE(t.pending_days, 0)
ORDER BY 1, 2, 3;
-- Working-day differences (same database snapshot):
WITH eligible AS (
    SELECT l.*, t.use_fiscal_year,
           CASE WHEN t.use_fiscal_year AND EXTRACT(MONTH FROM l.start_date) >= 10
                THEN EXTRACT(YEAR FROM l.start_date)::int + 1
                ELSE EXTRACT(YEAR FROM l.start_date)::int END AS balance_year
    FROM leave_requests l JOIN leave_types t ON t.id = l.leave_type_id
    WHERE EXTRACT(YEAR FROM l.start_date) BETWEEN 1900 AND 2100
      AND EXTRACT(YEAR FROM l.end_date) BETWEEN 1900 AND 2100
      AND l.end_date >= l.start_date AND l.end_date - l.start_date <= 366
), calculated AS (
    SELECT e.*, d.business_days,
           CASE WHEN e.duration_type IN ('HALF_DAY_AM', 'HALF_DAY_PM')
                THEN CASE WHEN e.start_date = e.end_date AND d.business_days = 1 THEN 0.5::numeric ELSE 0::numeric END
                ELSE d.business_days::numeric END AS expected_days,
           d.active_holidays
    FROM eligible e
    CROSS JOIN LATERAL (
        SELECT count(*) FILTER (WHERE EXTRACT(ISODOW FROM day_value)::int <= 5 AND h.id IS NULL) AS business_days,
               string_agg(h.holiday_date::text || ' ' || h.name, ', ' ORDER BY h.holiday_date)
                   FILTER (WHERE h.id IS NOT NULL) AS active_holidays
        FROM generate_series(e.start_date::timestamp, e.end_date::timestamp, INTERVAL '1 day') AS days(day_value)
        LEFT JOIN leave_holidays h ON h.holiday_date = days.day_value::date AND h.is_active
    ) d
)
SELECT c.id, c.request_number, c.user_id, c.leave_type_id, c.balance_year,
       c.start_date, c.end_date, c.duration_type, c.status,
       c.total_days AS stored_days, c.expected_days, c.active_holidays,
       b.used_days AS cached_used_days, b.pending_days AS cached_pending_days,
       CASE WHEN EXISTS (SELECT 1 FROM leave_cancellation_requests x WHERE x.original_leave_request_id = c.id)
            THEN 'REVIEW_CANCELLATION'
            WHEN c.duration_type NOT IN ('FULL_DAY', 'HALF_DAY_AM', 'HALF_DAY_PM') OR c.expected_days <= 0
            THEN 'REVIEW_INVALID_DURATION'
            WHEN c.status NOT IN ('Draft', 'Pending', 'Approved', 'Rejected', 'Cancelled', 'ReturnedForRevision')
            THEN 'REVIEW_STATUS'
            ELSE 'CANDIDATE' END AS review_status
FROM calculated c
LEFT JOIN leave_balances b ON b.user_id = c.user_id AND b.leave_type_id = c.leave_type_id AND b.year = c.balance_year
WHERE c.total_days <> c.expected_days
ORDER BY c.start_date, c.request_number, c.id;

-- Inspect the reported 24-25 September leave and any active holiday on those days.
SELECT l.id, l.request_number, l.user_id, l.status, l.start_date, l.end_date,
       l.duration_type, l.total_days, h.holiday_date, h.name AS holiday_name, h.is_active
FROM leave_requests l
LEFT JOIN leave_holidays h ON h.holiday_date BETWEEN l.start_date AND l.end_date AND h.is_active
WHERE l.start_date = DATE '2026-09-24' AND l.end_date = DATE '2026-09-25'
ORDER BY l.id, h.holiday_date;

-- These are deliberately excluded from automatic correction.
SELECT l.id, l.request_number, l.start_date, l.end_date, l.total_days, l.status
FROM leave_requests l
WHERE EXTRACT(YEAR FROM l.start_date) NOT BETWEEN 1900 AND 2100
   OR EXTRACT(YEAR FROM l.end_date) NOT BETWEEN 1900 AND 2100
   OR l.end_date < l.start_date OR l.end_date - l.start_date > 366
ORDER BY l.start_date, l.id;

COMMIT;

-- All result sets above are diagnostic; they do not authorize an automatic repair.
-- Do not run any earlier repair scripts based solely on this audit.
