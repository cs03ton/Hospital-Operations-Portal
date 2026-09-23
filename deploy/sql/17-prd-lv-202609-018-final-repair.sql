-- DBeaver: run ROLLBACK separately first if this connection reported 25P02.
-- Execute this WHOLE file as a SQL Script with Stop on error.
-- If any statement fails, run ROLLBACK in this connection; nothing was committed.
BEGIN ISOLATION LEVEL SERIALIZABLE;
SET LOCAL search_path = public, pg_temp;
SET LOCAL lock_timeout = '10s';
SET LOCAL statement_timeout = '120s';

CREATE TEMP TABLE lv018_target ON COMMIT DROP AS
SELECT l.*, t.use_fiscal_year,
       to_jsonb(l) - 'start_date' - 'end_date' - 'total_days' AS other_request_fields
FROM leave_requests l JOIN leave_types t ON t.id = l.leave_type_id
WHERE l.id = '01a0b2c5-06d4-75f4-bf0f-f869ba1b5183'::uuid;

CREATE TEMP TABLE lv018_related ON COMMIT DROP AS
SELECT l.id, l.request_number, l.start_date, l.end_date, l.status, l.total_days,
       CASE WHEN t.use_fiscal_year AND EXTRACT(MONTH FROM l.start_date) >= 10
            THEN EXTRACT(YEAR FROM l.start_date)::int + 1
            ELSE EXTRACT(YEAR FROM l.start_date)::int END AS balance_year
FROM lv018_target x JOIN leave_requests l
  ON l.user_id = x.user_id AND l.leave_type_id = x.leave_type_id
JOIN leave_types t ON t.id = l.leave_type_id
WHERE (CASE WHEN t.use_fiscal_year AND EXTRACT(MONTH FROM l.start_date) >= 10
            THEN EXTRACT(YEAR FROM l.start_date)::int + 1
            ELSE EXTRACT(YEAR FROM l.start_date)::int END) IN (2026, 2569);

CREATE TEMP TABLE lv018_balances ON COMMIT DROP AS
SELECT b.id, b.year, b.used_days, b.pending_days,
       to_jsonb(b) - 'used_days' - 'pending_days' - 'updated_at' AS other_balance_fields,
       (SELECT COALESCE(SUM(r.total_days), 0) FROM lv018_related r
        WHERE r.balance_year = b.year AND r.status = 'Approved') AS source_used_days,
       (SELECT COALESCE(SUM(r.total_days), 0) FROM lv018_related r
        WHERE r.balance_year = b.year AND r.status = 'Pending') AS source_pending_days
FROM lv018_target x JOIN leave_balances b
  ON b.user_id = x.user_id AND b.leave_type_id = x.leave_type_id
WHERE b.year IN (2026, 2569);

-- Preview the exact rows checked below, before the first UPDATE.
SELECT id, request_number, start_date, end_date, status, total_days, balance_year
FROM lv018_related ORDER BY start_date, request_number;
SELECT year, used_days, pending_days, source_used_days, source_pending_days
FROM lv018_balances ORDER BY year;
SELECT holiday_date, name FROM leave_holidays
WHERE is_active AND holiday_date BETWEEN DATE '2026-09-24' AND DATE '2026-09-25';

DO $precheck$
DECLARE target record;
BEGIN
    IF (SELECT count(*) FROM lv018_target) <> 1 THEN
        RAISE EXCEPTION 'LV-202609-018 not found; no changes made';
    END IF;
    SELECT * INTO target FROM lv018_target;
    IF target.request_number IS DISTINCT FROM 'LV-202609-018'
       OR target.user_id <> '019f8cc8-2bb3-7aae-8f3b-0ccb5d2e8953'::uuid
       OR target.leave_type_id <> '019ede95-6417-7ef8-9803-b78982453d63'::uuid
       OR target.use_fiscal_year IS DISTINCT FROM TRUE
       OR target.start_date <> DATE '2569-09-24'
       OR target.end_date <> DATE '2569-09-25'
       OR target.duration_type IS DISTINCT FROM 'FULL_DAY'
       OR target.total_days <> 1 OR target.status IS DISTINCT FROM 'Pending' THEN
        RAISE EXCEPTION 'Target request differs from confirmed data; no changes made';
    END IF;
    IF (SELECT count(*) FROM lv018_related) <> 3
       OR NOT EXISTS (SELECT 1 FROM lv018_related r WHERE r.request_number = 'LV-202608-007'
                      AND r.start_date = DATE '2026-08-18' AND r.end_date = DATE '2026-08-18'
                      AND r.status = 'Approved' AND r.total_days = 1 AND r.balance_year = 2026)
       OR NOT EXISTS (SELECT 1 FROM lv018_related r WHERE r.request_number = 'LV-202609-006'
                      AND r.start_date = DATE '2026-09-18' AND r.end_date = DATE '2026-09-18'
                      AND r.status = 'Approved' AND r.total_days = 1 AND r.balance_year = 2026)
       OR NOT EXISTS (SELECT 1 FROM lv018_related r WHERE r.id = target.id
                      AND r.status = 'Pending' AND r.total_days = 1 AND r.balance_year = 2569) THEN
        RAISE EXCEPTION 'The three related requests differ from the supplied result; no changes made';
    END IF;
    IF EXTRACT(ISODOW FROM DATE '2026-09-24') > 5
       OR EXTRACT(ISODOW FROM DATE '2026-09-25') > 5
       OR EXISTS (SELECT 1 FROM leave_holidays h WHERE h.is_active
                  AND h.holiday_date BETWEEN DATE '2026-09-24' AND DATE '2026-09-25') THEN
        RAISE EXCEPTION 'Corrected dates are not both working days; no changes made';
    END IF;
    IF EXISTS (SELECT 1 FROM leave_cancellation_requests c WHERE c.original_leave_request_id = target.id)
       OR EXISTS (SELECT 1 FROM leave_balance_transactions t WHERE t.reference_id = target.id) THEN
        RAISE EXCEPTION 'Cancellation or balance transaction refers to target; review manually';
    END IF;
    IF (SELECT count(*) FROM lv018_balances) <> 2
       OR NOT EXISTS (SELECT 1 FROM lv018_balances b
                      WHERE b.year = 2026 AND b.used_days = 1 AND b.pending_days = 0
                        AND b.source_used_days = 2 AND b.source_pending_days = 0)
       OR NOT EXISTS (SELECT 1 FROM lv018_balances b
                      WHERE b.year = 2569 AND b.used_days = 1 AND b.pending_days = 1
                        AND b.source_used_days = 0 AND b.source_pending_days = 1) THEN
        RAISE EXCEPTION 'Balance rows or source totals differ from supplied values; no changes made';
    END IF;
END
$precheck$;

DO $update$
DECLARE changed_rows integer;
BEGIN
    UPDATE leave_requests l
    SET start_date = DATE '2026-09-24', end_date = DATE '2026-09-25', total_days = 2
    FROM lv018_target x
    WHERE l.id = x.id AND l.request_number = x.request_number
      AND l.start_date = x.start_date AND l.end_date = x.end_date
      AND l.status = x.status AND l.total_days = x.total_days;
    GET DIAGNOSTICS changed_rows = ROW_COUNT;
    IF changed_rows <> 1 THEN
        RAISE EXCEPTION 'Expected one request update, got %; transaction rolled back', changed_rows;
    END IF;

    UPDATE leave_balances b SET used_days = 2, pending_days = 2, updated_at = now()
    FROM lv018_balances x
    WHERE b.id = x.id AND x.year = 2026
      AND b.used_days = x.used_days AND b.pending_days = x.pending_days;
    GET DIAGNOSTICS changed_rows = ROW_COUNT;
    IF changed_rows <> 1 THEN
        RAISE EXCEPTION 'Expected one 2026 balance update, got %; transaction rolled back', changed_rows;
    END IF;

    UPDATE leave_balances b SET used_days = 0, pending_days = 0, updated_at = now()
    FROM lv018_balances x
    WHERE b.id = x.id AND x.year = 2569
      AND b.used_days = x.used_days AND b.pending_days = x.pending_days;
    GET DIAGNOSTICS changed_rows = ROW_COUNT;
    IF changed_rows <> 1 THEN
        RAISE EXCEPTION 'Expected one 2569 balance update, got %; transaction rolled back', changed_rows;
    END IF;
END
$update$;

DO $verify$
DECLARE target record;
BEGIN
    SELECT * INTO target FROM lv018_target;
    IF NOT EXISTS (
        SELECT 1 FROM leave_requests l WHERE l.id = target.id
          AND l.start_date = DATE '2026-09-24' AND l.end_date = DATE '2026-09-25'
          AND l.total_days = 2
          AND to_jsonb(l) - 'start_date' - 'end_date' - 'total_days' = target.other_request_fields
    ) OR EXISTS (
        SELECT 1 FROM lv018_balances x JOIN leave_balances b ON b.id = x.id
        WHERE b.used_days <> CASE WHEN x.year = 2026 THEN 2 ELSE 0 END
           OR b.pending_days <> CASE WHEN x.year = 2026 THEN 2 ELSE 0 END
           OR to_jsonb(b) - 'used_days' - 'pending_days' - 'updated_at' <> x.other_balance_fields
    ) OR EXISTS (
        SELECT 1 FROM lv018_balances x
        WHERE (SELECT COALESCE(SUM(l.total_days), 0) FROM leave_requests l
               WHERE l.user_id = target.user_id AND l.leave_type_id = target.leave_type_id
                 AND l.status = 'Approved'
                 AND (CASE WHEN target.use_fiscal_year AND EXTRACT(MONTH FROM l.start_date) >= 10
                           THEN EXTRACT(YEAR FROM l.start_date)::int + 1
                           ELSE EXTRACT(YEAR FROM l.start_date)::int END) = x.year)
              <> CASE WHEN x.year = 2026 THEN 2 ELSE 0 END
           OR (SELECT COALESCE(SUM(l.total_days), 0) FROM leave_requests l
               WHERE l.user_id = target.user_id AND l.leave_type_id = target.leave_type_id
                 AND l.status = 'Pending'
                 AND (CASE WHEN target.use_fiscal_year AND EXTRACT(MONTH FROM l.start_date) >= 10
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
