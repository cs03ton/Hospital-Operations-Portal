-- Read-only DBeaver audit. Run before 14-prd-leave-day-count-repair.sql.
-- The service counts Monday-Friday, excluding active leave_holidays.
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
