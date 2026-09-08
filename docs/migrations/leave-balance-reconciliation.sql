-- HOP leave balance cache reconciliation (PostgreSQL / DBeaver)
-- Run the preview first. The UPDATE is idempotent and does not alter leave requests.

WITH actual AS (
    SELECT lr.user_id,
           lr.leave_type_id,
           CASE WHEN lt.use_fiscal_year
                THEN EXTRACT(YEAR FROM lr.start_date)::int + CASE WHEN EXTRACT(MONTH FROM lr.start_date) >= 10 THEN 1 ELSE 0 END
                ELSE EXTRACT(YEAR FROM lr.start_date)::int
           END AS balance_year,
           SUM(CASE WHEN lr.status = 'Approved' THEN lr.total_days ELSE 0 END) AS used_days,
           SUM(CASE WHEN lr.status = 'Pending' THEN lr.total_days ELSE 0 END) AS pending_days
    FROM leave_requests lr
    JOIN leave_types lt ON lt.id = lr.leave_type_id
    WHERE lr.status IN ('Approved', 'Pending')
    GROUP BY lr.user_id, lr.leave_type_id, balance_year
)
SELECT lb.id, lb.user_id, lb.leave_type_id, lb.year,
       lb.used_days AS cached_used_days, COALESCE(a.used_days, 0) AS actual_used_days,
       lb.pending_days AS cached_pending_days, COALESCE(a.pending_days, 0) AS actual_pending_days
FROM leave_balances lb
LEFT JOIN actual a ON a.user_id = lb.user_id
                  AND a.leave_type_id = lb.leave_type_id
                  AND a.balance_year = lb.year
WHERE lb.used_days IS DISTINCT FROM COALESCE(a.used_days, 0)
   OR lb.pending_days IS DISTINCT FROM COALESCE(a.pending_days, 0)
ORDER BY lb.year DESC, lb.user_id, lb.leave_type_id;

-- Execute this block only after reviewing the preview above.
BEGIN;
WITH actual AS (
    SELECT lr.user_id,
           lr.leave_type_id,
           CASE WHEN lt.use_fiscal_year
                THEN EXTRACT(YEAR FROM lr.start_date)::int + CASE WHEN EXTRACT(MONTH FROM lr.start_date) >= 10 THEN 1 ELSE 0 END
                ELSE EXTRACT(YEAR FROM lr.start_date)::int
           END AS balance_year,
           SUM(CASE WHEN lr.status = 'Approved' THEN lr.total_days ELSE 0 END) AS used_days,
           SUM(CASE WHEN lr.status = 'Pending' THEN lr.total_days ELSE 0 END) AS pending_days
    FROM leave_requests lr
    JOIN leave_types lt ON lt.id = lr.leave_type_id
    WHERE lr.status IN ('Approved', 'Pending')
    GROUP BY lr.user_id, lr.leave_type_id, balance_year
), expected AS (
    SELECT lb.id, COALESCE(a.used_days, 0) AS used_days, COALESCE(a.pending_days, 0) AS pending_days
    FROM leave_balances lb
    LEFT JOIN actual a ON a.user_id = lb.user_id
                      AND a.leave_type_id = lb.leave_type_id
                      AND a.balance_year = lb.year
)
UPDATE leave_balances lb
SET used_days = expected.used_days,
    pending_days = expected.pending_days,
    updated_at = CURRENT_TIMESTAMP
FROM expected
WHERE lb.id = expected.id
  AND (lb.used_days IS DISTINCT FROM expected.used_days
       OR lb.pending_days IS DISTINCT FROM expected.pending_days);
COMMIT;
