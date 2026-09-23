-- Run the read-only 13-prd-leave-day-count-preview.sql first.
-- Run this WHOLE file in DBeaver as SQL Script with Stop on error.
-- If any assertion fails, run ROLLBACK in the same connection. Do not rerun
-- 12-prd-leave-buddhist-year-repair.sql to correct day counts.
BEGIN ISOLATION LEVEL SERIALIZABLE;
SET LOCAL search_path = public, pg_temp;
SET LOCAL lock_timeout = '10s';
SET LOCAL statement_timeout = '120s';

CREATE TEMP TABLE leave_day_repair_candidates ON COMMIT DROP AS
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
       c.total_days AS old_days, c.expected_days AS new_days, c.active_holidays,
       to_jsonb(c) - 'total_days' - 'expected_days' - 'business_days' - 'active_holidays' - 'use_fiscal_year' - 'balance_year' AS request_snapshot
FROM calculated c
WHERE c.total_days <> c.expected_days;

-- Preview every change before the first UPDATE.
SELECT id, request_number, start_date, end_date, duration_type, status,
       old_days, new_days, active_holidays
FROM leave_day_repair_candidates ORDER BY start_date, request_number, id;
SELECT l.id, l.request_number, l.start_date, l.end_date, l.total_days,
       c.new_days AS calculated_days, c.active_holidays
FROM leave_requests l LEFT JOIN leave_day_repair_candidates c ON c.id = l.id
WHERE l.start_date = DATE '2026-09-24' AND l.end_date = DATE '2026-09-25';

CREATE TEMP TABLE leave_day_repair_balance_keys ON COMMIT DROP AS
SELECT DISTINCT user_id, leave_type_id, balance_year AS year
FROM leave_day_repair_candidates WHERE status IN ('Pending', 'Approved');

CREATE TEMP TABLE leave_day_repair_balances ON COMMIT DROP AS
WITH usage AS (
    SELECT k.user_id, k.leave_type_id, k.year,
           COALESCE(SUM(l.total_days) FILTER (WHERE l.status = 'Approved'), 0) AS source_used_days,
           COALESCE(SUM(l.total_days) FILTER (WHERE l.status = 'Pending'), 0) AS source_pending_days,
           COALESCE(SUM(COALESCE(c.new_days, l.total_days)) FILTER (WHERE l.status = 'Approved'), 0) AS new_used_days,
           COALESCE(SUM(COALESCE(c.new_days, l.total_days)) FILTER (WHERE l.status = 'Pending'), 0) AS new_pending_days
    FROM leave_day_repair_balance_keys k
    LEFT JOIN leave_requests l ON l.user_id = k.user_id AND l.leave_type_id = k.leave_type_id
        AND l.status IN ('Approved', 'Pending')
        AND (SELECT CASE WHEN t.use_fiscal_year AND EXTRACT(MONTH FROM l.start_date) >= 10
                         THEN EXTRACT(YEAR FROM l.start_date)::int + 1
                         ELSE EXTRACT(YEAR FROM l.start_date)::int END
             FROM leave_types t WHERE t.id = l.leave_type_id) = k.year
    LEFT JOIN leave_day_repair_candidates c ON c.id = l.id
    GROUP BY k.user_id, k.leave_type_id, k.year
)
SELECT u.*, b.id AS balance_id, b.used_days AS old_used_days,
       b.pending_days AS old_pending_days,
       to_jsonb(b) - 'used_days' - 'pending_days' - 'updated_at' AS balance_snapshot
FROM usage u LEFT JOIN leave_balances b
  ON b.user_id = u.user_id AND b.leave_type_id = u.leave_type_id AND b.year = u.year;

SELECT * FROM leave_day_repair_balances ORDER BY user_id, leave_type_id, year;

DO $precheck$
BEGIN
    IF EXISTS (
        SELECT 1 FROM leave_requests l
        WHERE EXTRACT(YEAR FROM l.start_date) BETWEEN 2500 AND 2599
           OR EXTRACT(YEAR FROM l.end_date) BETWEEN 2500 AND 2599
    ) THEN
        RAISE EXCEPTION 'Some leave dates still use Buddhist years; repair dates first';
    END IF;
    IF EXISTS (
        SELECT 1 FROM leave_day_repair_candidates c
        WHERE c.duration_type NOT IN ('FULL_DAY', 'HALF_DAY_AM', 'HALF_DAY_PM')
           OR c.new_days <= 0
           OR c.status NOT IN ('Draft', 'Pending', 'Approved', 'Rejected', 'Cancelled', 'ReturnedForRevision')
           OR EXISTS (SELECT 1 FROM leave_cancellation_requests x WHERE x.original_leave_request_id = c.id)
           OR EXISTS (SELECT 1 FROM leave_balance_transactions x WHERE x.reference_id = c.id)
    ) THEN
        RAISE EXCEPTION 'A mismatched request needs manual review (duration, status, cancellation, or balance transaction); no changes made';
    END IF;
    IF EXISTS (
        SELECT 1 FROM leave_day_repair_balances b
        WHERE b.balance_id IS NULL OR b.old_used_days <> b.source_used_days
           OR b.old_pending_days <> b.source_pending_days
    ) THEN
        RAISE EXCEPTION 'A balance is missing or differs from current leave-request totals; no changes made';
    END IF;
END
$precheck$;

CREATE TEMP TABLE leave_day_repair_request_count ON COMMIT DROP AS SELECT count(*) AS total FROM leave_requests;
CREATE TEMP TABLE leave_day_repair_balance_count ON COMMIT DROP AS SELECT count(*) AS total FROM leave_balances;

UPDATE leave_requests l SET total_days = c.new_days
FROM leave_day_repair_candidates c
WHERE l.id = c.id AND l.total_days = c.old_days
  AND l.start_date = c.start_date AND l.end_date = c.end_date
  AND l.status = c.status AND l.duration_type = c.duration_type;

UPDATE leave_balances b
SET used_days = x.new_used_days, pending_days = x.new_pending_days, updated_at = now()
FROM leave_day_repair_balances x
WHERE b.id = x.balance_id AND b.used_days = x.old_used_days
  AND b.pending_days = x.old_pending_days;

DO $verify$
BEGIN
    IF (SELECT count(*) FROM leave_requests) <> (SELECT total FROM leave_day_repair_request_count)
       OR (SELECT count(*) FROM leave_balances) <> (SELECT total FROM leave_day_repair_balance_count)
       OR EXISTS (
           SELECT 1 FROM leave_day_repair_candidates c LEFT JOIN leave_requests l ON l.id = c.id
           WHERE l.id IS NULL OR l.total_days <> c.new_days
              OR to_jsonb(l) - 'total_days' <> c.request_snapshot
       )
       OR EXISTS (
           SELECT 1 FROM leave_day_repair_balances x LEFT JOIN leave_balances b ON b.id = x.balance_id
           WHERE b.id IS NULL OR b.used_days <> x.new_used_days OR b.pending_days <> x.new_pending_days
              OR to_jsonb(b) - 'used_days' - 'pending_days' - 'updated_at' <> x.balance_snapshot
       ) THEN
        RAISE EXCEPTION 'Post-update verification failed; transaction rolled back';
    END IF;
END
$verify$;

SELECT count(*) AS corrected_requests FROM leave_day_repair_candidates;
SELECT count(*) AS reconciled_balance_rows FROM leave_day_repair_balances;
COMMIT;
