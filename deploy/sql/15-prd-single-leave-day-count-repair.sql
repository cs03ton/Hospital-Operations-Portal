-- Superseded for LV-202609-018: use 17-prd-lv-202609-018-final-repair.sql.
-- DBeaver: if the connection currently reports 25P02, run ROLLBACK separately first.
-- Execute this WHOLE file as a SQL Script with Stop on error.
-- On any error, run ROLLBACK in this same connection; no change is committed.
BEGIN ISOLATION LEVEL SERIALIZABLE;
SET LOCAL search_path = public, pg_temp;
SET LOCAL lock_timeout = '10s';
SET LOCAL statement_timeout = '120s';

CREATE TEMP TABLE one_leave_target ON COMMIT DROP AS
SELECT l.*, t.use_fiscal_year,
       CASE WHEN t.use_fiscal_year AND EXTRACT(MONTH FROM l.start_date) >= 10
            THEN EXTRACT(YEAR FROM l.start_date)::int + 1
            ELSE EXTRACT(YEAR FROM l.start_date)::int END AS balance_year,
       to_jsonb(l) - 'total_days' AS other_request_fields
FROM leave_requests l
JOIN leave_types t ON t.id = l.leave_type_id
WHERE l.id = '01a0b2c5-06d4-75f4-bf0f-f869ba1b5183'::uuid;

CREATE TEMP TABLE one_leave_balance ON COMMIT DROP AS
SELECT b.id, b.used_days, b.pending_days,
       to_jsonb(b) - 'used_days' - 'pending_days' - 'updated_at' AS other_balance_fields,
       (SELECT COALESCE(SUM(r.total_days), 0) FROM leave_requests r
        WHERE r.user_id = x.user_id AND r.leave_type_id = x.leave_type_id
          AND r.status = 'Approved'
          AND r.start_date >= CASE WHEN x.use_fiscal_year THEN make_date(x.balance_year - 1, 10, 1)
                                    ELSE make_date(x.balance_year, 1, 1) END
          AND r.start_date < CASE WHEN x.use_fiscal_year THEN make_date(x.balance_year, 10, 1)
                                   ELSE make_date(x.balance_year + 1, 1, 1) END) AS source_used_days,
       (SELECT COALESCE(SUM(r.total_days), 0) FROM leave_requests r
        WHERE r.user_id = x.user_id AND r.leave_type_id = x.leave_type_id
          AND r.status = 'Pending'
          AND r.start_date >= CASE WHEN x.use_fiscal_year THEN make_date(x.balance_year - 1, 10, 1)
                                    ELSE make_date(x.balance_year, 1, 1) END
          AND r.start_date < CASE WHEN x.use_fiscal_year THEN make_date(x.balance_year, 10, 1)
                                   ELSE make_date(x.balance_year + 1, 1, 1) END) AS source_pending_days
FROM one_leave_target x
JOIN leave_balances b ON b.user_id = x.user_id
    AND b.leave_type_id = x.leave_type_id AND b.year = x.balance_year;

-- Preview: the single request, its related balance, and any active holidays.
SELECT id, request_number, user_id, leave_type_id, balance_year, status,
       start_date, end_date, duration_type, total_days AS old_days, 2::numeric AS new_days
FROM one_leave_target;
SELECT id, used_days, pending_days, source_used_days, source_pending_days
FROM one_leave_balance;
SELECT h.holiday_date, h.name FROM leave_holidays h
WHERE h.is_active AND h.holiday_date BETWEEN DATE '2026-09-24' AND DATE '2026-09-25';

DO $precheck$
DECLARE r record;
BEGIN
    IF (SELECT count(*) FROM one_leave_target) <> 1 THEN
        RAISE EXCEPTION 'Leave request ID not found; no changes made';
    END IF;
    SELECT * INTO r FROM one_leave_target;
    IF r.start_date <> DATE '2026-09-24' OR r.end_date <> DATE '2026-09-25'
       OR r.duration_type <> 'FULL_DAY' OR r.total_days <> 1
       OR r.status NOT IN ('Draft', 'Pending', 'Approved', 'Rejected', 'Cancelled', 'ReturnedForRevision') THEN
        RAISE EXCEPTION 'Request date, duration, old day count, or status differs from expected; no changes made';
    END IF;
    IF EXTRACT(ISODOW FROM r.start_date) > 5 OR EXTRACT(ISODOW FROM r.end_date) > 5
       OR EXISTS (SELECT 1 FROM leave_holidays h
                  WHERE h.is_active AND h.holiday_date BETWEEN r.start_date AND r.end_date) THEN
        RAISE EXCEPTION 'The two dates are not both working days; no changes made';
    END IF;
    IF EXISTS (SELECT 1 FROM leave_cancellation_requests c WHERE c.original_leave_request_id = r.id)
       OR EXISTS (SELECT 1 FROM leave_balance_transactions x WHERE x.reference_id = r.id) THEN
        RAISE EXCEPTION 'Cancellation or balance transaction refers to this request; review manually';
    END IF;
    IF r.status IN ('Pending', 'Approved') AND
       ((SELECT count(*) FROM one_leave_balance) <> 1 OR EXISTS (
           SELECT 1 FROM one_leave_balance b
           WHERE b.used_days <> b.source_used_days
              OR b.pending_days <> b.source_pending_days
       )) THEN
        RAISE EXCEPTION 'Balance is missing or differs from leave request totals; no changes made';
    END IF;
END
$precheck$;

DO $update$
DECLARE request_rows integer; balance_rows integer; request_status text;
BEGIN
    SELECT status INTO request_status FROM one_leave_target;
    UPDATE leave_requests l SET total_days = 2
    FROM one_leave_target x
    WHERE l.id = x.id AND l.total_days = 1 AND l.start_date = x.start_date
      AND l.end_date = x.end_date AND l.status = x.status AND l.duration_type = x.duration_type;
    GET DIAGNOSTICS request_rows = ROW_COUNT;
    IF request_rows <> 1 THEN
        RAISE EXCEPTION 'Expected exactly one request update, got %; transaction rolled back', request_rows;
    END IF;

    IF request_status IN ('Pending', 'Approved') THEN
        UPDATE leave_balances b
        SET used_days = b.used_days + CASE WHEN request_status = 'Approved' THEN 1 ELSE 0 END,
            pending_days = b.pending_days + CASE WHEN request_status = 'Pending' THEN 1 ELSE 0 END,
            updated_at = now()
        FROM one_leave_balance x
        WHERE b.id = x.id AND b.used_days = x.used_days AND b.pending_days = x.pending_days;
        GET DIAGNOSTICS balance_rows = ROW_COUNT;
        IF balance_rows <> 1 THEN
            RAISE EXCEPTION 'Expected exactly one balance update, got %; transaction rolled back', balance_rows;
        END IF;
    END IF;
END
$update$;

DO $verify$
DECLARE r record;
BEGIN
    SELECT * INTO r FROM one_leave_target;
    IF NOT EXISTS (
        SELECT 1 FROM leave_requests l WHERE l.id = r.id AND l.total_days = 2
          AND to_jsonb(l) - 'total_days' = r.other_request_fields
    ) THEN
        RAISE EXCEPTION 'Request verification failed; transaction rolled back';
    END IF;
    IF r.status IN ('Pending', 'Approved') AND NOT EXISTS (
        SELECT 1 FROM one_leave_balance x JOIN leave_balances b ON b.id = x.id
        WHERE b.used_days = x.used_days + CASE WHEN r.status = 'Approved' THEN 1 ELSE 0 END
          AND b.pending_days = x.pending_days + CASE WHEN r.status = 'Pending' THEN 1 ELSE 0 END
          AND to_jsonb(b) - 'used_days' - 'pending_days' - 'updated_at' = x.other_balance_fields
    ) THEN
        RAISE EXCEPTION 'Balance verification failed; transaction rolled back';
    END IF;
END
$verify$;

SELECT l.id, l.request_number, l.status, l.start_date, l.end_date,
       x.total_days AS old_days, l.total_days AS new_days,
       b.used_days, b.pending_days
FROM one_leave_target x JOIN leave_requests l ON l.id = x.id
LEFT JOIN leave_balances b ON b.user_id = x.user_id AND b.leave_type_id = x.leave_type_id
    AND b.year = x.balance_year;
COMMIT;
