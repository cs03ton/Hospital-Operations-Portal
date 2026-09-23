-- Superseded for LV-202609-018: use 17-prd-lv-202609-018-final-repair.sql.
-- DBeaver: if this connection previously reported 25P02, run ROLLBACK separately first.
-- Execute this WHOLE file as a SQL Script with Stop on error.
-- On any error, run ROLLBACK in the same connection. Nothing below commits early.
BEGIN ISOLATION LEVEL SERIALIZABLE;
SET LOCAL search_path = public, pg_temp;
SET LOCAL lock_timeout = '10s';
SET LOCAL statement_timeout = '120s';

CREATE TEMP TABLE lv018_request ON COMMIT DROP AS
SELECT l.*, t.use_fiscal_year,
       to_jsonb(l) - 'start_date' - 'end_date' - 'total_days' AS other_request_fields
FROM leave_requests l JOIN leave_types t ON t.id = l.leave_type_id
WHERE l.id = '01a0b2c5-06d4-75f4-bf0f-f869ba1b5183'::uuid;

CREATE TEMP TABLE lv018_balances ON COMMIT DROP AS
SELECT b.id, b.user_id, b.leave_type_id, b.year, b.used_days, b.pending_days,
       to_jsonb(b) - 'pending_days' - 'updated_at' AS other_balance_fields,
       (SELECT COALESCE(SUM(r.total_days), 0) FROM leave_requests r
        WHERE r.user_id = b.user_id AND r.leave_type_id = b.leave_type_id
          AND r.status = 'Approved'
          AND (CASE WHEN x.use_fiscal_year AND EXTRACT(MONTH FROM r.start_date) >= 10
                    THEN EXTRACT(YEAR FROM r.start_date)::int + 1
                    ELSE EXTRACT(YEAR FROM r.start_date)::int END) = b.year) AS source_used_days,
       (SELECT COALESCE(SUM(r.total_days), 0) FROM leave_requests r
        WHERE r.user_id = b.user_id AND r.leave_type_id = b.leave_type_id
          AND r.status = 'Pending'
          AND (CASE WHEN x.use_fiscal_year AND EXTRACT(MONTH FROM r.start_date) >= 10
                    THEN EXTRACT(YEAR FROM r.start_date)::int + 1
                    ELSE EXTRACT(YEAR FROM r.start_date)::int END) = b.year) AS source_pending_days
FROM lv018_request x JOIN leave_balances b
  ON b.user_id = x.user_id AND b.leave_type_id = x.leave_type_id
WHERE b.year IN (2026, 2569);

-- Preview the only request and the two balance rows before the first UPDATE.
SELECT id, request_number, user_id, leave_type_id, use_fiscal_year,
       start_date, end_date, duration_type, total_days, status
FROM lv018_request;
SELECT year, used_days, pending_days, source_used_days, source_pending_days
FROM lv018_balances ORDER BY year;
SELECT h.holiday_date, h.name FROM leave_holidays h
WHERE h.is_active AND h.holiday_date BETWEEN DATE '2026-09-24' AND DATE '2026-09-25';

DO $precheck$
DECLARE r record;
BEGIN
    IF (SELECT count(*) FROM lv018_request) <> 1 THEN
        RAISE EXCEPTION 'LV-202609-018 request is missing; no changes made';
    END IF;
    SELECT * INTO r FROM lv018_request;
    IF r.request_number IS DISTINCT FROM 'LV-202609-018'
       OR r.user_id <> '019f8cc8-2bb3-7aae-8f3b-0ccb5d2e8953'::uuid
       OR r.leave_type_id <> '019ede95-6417-7ef8-9803-b78982453d63'::uuid
       OR r.use_fiscal_year IS DISTINCT FROM TRUE
       OR r.start_date <> DATE '2569-09-24' OR r.end_date <> DATE '2569-09-25'
       OR r.duration_type IS DISTINCT FROM 'FULL_DAY'
       OR r.total_days <> 1 OR r.status IS DISTINCT FROM 'Pending' THEN
        RAISE EXCEPTION 'Request differs from confirmed ID, code, dates, type, days, or status; no changes made';
    END IF;
    IF EXTRACT(ISODOW FROM DATE '2026-09-24') > 5
       OR EXTRACT(ISODOW FROM DATE '2026-09-25') > 5
       OR EXISTS (SELECT 1 FROM leave_holidays h WHERE h.is_active
                  AND h.holiday_date BETWEEN DATE '2026-09-24' AND DATE '2026-09-25') THEN
        RAISE EXCEPTION 'A corrected date is not a working day; no changes made';
    END IF;
    IF EXISTS (SELECT 1 FROM leave_cancellation_requests c WHERE c.original_leave_request_id = r.id)
       OR EXISTS (SELECT 1 FROM leave_balance_transactions t WHERE t.reference_id = r.id) THEN
        RAISE EXCEPTION 'A cancellation or balance transaction refers to this request; review manually';
    END IF;
    IF (SELECT count(*) FROM lv018_balances) <> 2
       OR NOT EXISTS (SELECT 1 FROM lv018_balances b
                      WHERE b.year = 2026 AND b.used_days = 1 AND b.pending_days = 0)
       OR NOT EXISTS (SELECT 1 FROM lv018_balances b
                      WHERE b.year = 2569 AND b.used_days = 1 AND b.pending_days = 1)
       OR EXISTS (SELECT 1 FROM lv018_balances b
                  WHERE b.used_days <> b.source_used_days
                     OR b.pending_days <> b.source_pending_days) THEN
        RAISE EXCEPTION 'Balance rows differ from supplied values or request totals; no changes made';
    END IF;
END
$precheck$;

DO $update$
DECLARE changed_rows integer;
BEGIN
    UPDATE leave_requests l
    SET start_date = DATE '2026-09-24', end_date = DATE '2026-09-25', total_days = 2
    FROM lv018_request x
    WHERE l.id = x.id AND l.request_number = x.request_number
      AND l.start_date = x.start_date AND l.end_date = x.end_date
      AND l.total_days = x.total_days AND l.status = x.status;
    GET DIAGNOSTICS changed_rows = ROW_COUNT;
    IF changed_rows <> 1 THEN
        RAISE EXCEPTION 'Expected one request update, got %; transaction rolled back', changed_rows;
    END IF;

    UPDATE leave_balances b SET pending_days = 0, updated_at = now()
    FROM lv018_balances x
    WHERE b.id = x.id AND x.year = 2569
      AND b.used_days = x.used_days AND b.pending_days = x.pending_days;
    GET DIAGNOSTICS changed_rows = ROW_COUNT;
    IF changed_rows <> 1 THEN
        RAISE EXCEPTION 'Expected one 2569 balance update, got %; transaction rolled back', changed_rows;
    END IF;

    UPDATE leave_balances b SET pending_days = 2, updated_at = now()
    FROM lv018_balances x
    WHERE b.id = x.id AND x.year = 2026
      AND b.used_days = x.used_days AND b.pending_days = x.pending_days;
    GET DIAGNOSTICS changed_rows = ROW_COUNT;
    IF changed_rows <> 1 THEN
        RAISE EXCEPTION 'Expected one 2026 balance update, got %; transaction rolled back', changed_rows;
    END IF;
END
$update$;

DO $verify$
DECLARE r record;
BEGIN
    SELECT * INTO r FROM lv018_request;
    IF NOT EXISTS (
        SELECT 1 FROM leave_requests l WHERE l.id = r.id
          AND l.start_date = DATE '2026-09-24' AND l.end_date = DATE '2026-09-25'
          AND l.total_days = 2
          AND to_jsonb(l) - 'start_date' - 'end_date' - 'total_days' = r.other_request_fields
    ) OR EXISTS (
        SELECT 1 FROM lv018_balances x JOIN leave_balances b ON b.id = x.id
        WHERE b.pending_days <> CASE WHEN x.year = 2026 THEN 2 ELSE 0 END
           OR to_jsonb(b) - 'pending_days' - 'updated_at' <> x.other_balance_fields
    ) OR EXISTS (
        SELECT 1 FROM lv018_balances x
        WHERE (SELECT COALESCE(SUM(l.total_days), 0) FROM leave_requests l
               WHERE l.user_id = x.user_id AND l.leave_type_id = x.leave_type_id
                 AND l.status = 'Pending'
                 AND (CASE WHEN r.use_fiscal_year AND EXTRACT(MONTH FROM l.start_date) >= 10
                           THEN EXTRACT(YEAR FROM l.start_date)::int + 1
                           ELSE EXTRACT(YEAR FROM l.start_date)::int END) = x.year)
              <> CASE WHEN x.year = 2026 THEN 2 ELSE 0 END
    ) THEN
        RAISE EXCEPTION 'Post-update verification failed; transaction rolled back';
    END IF;
END
$verify$;

SELECT l.id, l.request_number, l.start_date, l.end_date, l.total_days, l.status,
       b.year, b.used_days, b.pending_days
FROM leave_requests l JOIN leave_balances b
  ON b.user_id = l.user_id AND b.leave_type_id = l.leave_type_id
WHERE l.id = '01a0b2c5-06d4-75f4-bf0f-f869ba1b5183'::uuid
  AND b.year IN (2026, 2569)
ORDER BY b.year;
COMMIT;
