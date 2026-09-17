-- Run as a complete DBeaver SQL script with Stop on error. Inspect preview result sets.
-- On any error, issue ROLLBACK in the same connection. No workflow fields are changed.
BEGIN;
SET LOCAL search_path = public, pg_temp;
SET LOCAL lock_timeout = '10s';
SET LOCAL statement_timeout = '120s';

CREATE TEMP TABLE leave_year_repair_expected (id uuid PRIMARY KEY, original_date date NOT NULL) ON COMMIT DROP;
INSERT INTO leave_year_repair_expected VALUES
 ('01a09eea-d72e-7e6c-b766-928468f16a99', DATE '2569-09-21'),
 ('01a09eea-2095-73e7-afa8-08aa898d0c3a', DATE '2569-09-18'),
 ('01a08ea0-ac50-7c1b-873f-4a78658a9b96', DATE '2569-09-25'),
 ('01a08e8f-9c22-745d-ad9d-258cb2eb791a', DATE '2569-09-18');

-- Preview the four supplied IDs, including any that were already corrected.
SELECT e.id, l.request_number, l.start_date, l.end_date, l.status,
       CASE WHEN l.id IS NULL THEN 'MISSING'
            WHEN l.start_date = e.original_date AND l.end_date = e.original_date THEN 'NEEDS_REPAIR'
            WHEN l.start_date = (e.original_date - INTERVAL '543 years')::date
             AND l.end_date = (e.original_date - INTERVAL '543 years')::date THEN 'ALREADY_CORRECTED'
            ELSE 'DIFFERENT_DATA' END AS verification
FROM leave_year_repair_expected e LEFT JOIN leave_requests l ON l.id = e.id ORDER BY e.id;

-- Preview every affected request and count by status; both dates must use the same calendar.
CREATE TEMP TABLE leave_year_repair_candidates ON COMMIT DROP AS
SELECT l.id, l.start_date AS old_start_date, l.end_date AS old_end_date,
       make_date(EXTRACT(YEAR FROM l.start_date)::int - 543,
                 EXTRACT(MONTH FROM l.start_date)::int,
                 EXTRACT(DAY FROM l.start_date)::int) AS new_start_date,
       make_date(EXTRACT(YEAR FROM l.end_date)::int - 543,
                 EXTRACT(MONTH FROM l.end_date)::int,
                 EXTRACT(DAY FROM l.end_date)::int) AS new_end_date,
       to_jsonb(l) - 'start_date' - 'end_date' AS other_fields
FROM leave_requests l
WHERE (EXTRACT(YEAR FROM l.start_date) BETWEEN 2500 AND 2599)
   OR (EXTRACT(YEAR FROM l.end_date) BETWEEN 2500 AND 2599);

SELECT l.id, l.request_number, l.status, l.user_id,
       c.old_start_date, c.old_end_date, c.new_start_date, c.new_end_date
FROM leave_year_repair_candidates c JOIN leave_requests l ON l.id = c.id
ORDER BY l.request_number, l.id;
SELECT l.status, count(*) AS affected_requests
FROM leave_year_repair_candidates c JOIN leave_requests l ON l.id = c.id
GROUP BY l.status ORDER BY l.status;

CREATE TEMP TABLE leave_year_repair_total ON COMMIT DROP AS SELECT count(*) AS total FROM leave_requests;

DO $precheck$
BEGIN
    IF EXISTS (
        SELECT 1 FROM leave_year_repair_expected e LEFT JOIN leave_requests l ON l.id = e.id
        WHERE l.id IS NULL OR NOT (
            (l.start_date = e.original_date AND l.end_date = e.original_date)
            OR (l.start_date = (e.original_date - INTERVAL '543 years')::date
                AND l.end_date = (e.original_date - INTERVAL '543 years')::date)
        )
    ) THEN
        RAISE EXCEPTION 'One of the four supplied leave requests is missing or differs from the supplied dates; no changes made';
    END IF;
    IF EXISTS (
        SELECT 1 FROM leave_year_repair_candidates
        WHERE EXTRACT(YEAR FROM old_start_date) NOT BETWEEN 2500 AND 2599
           OR EXTRACT(YEAR FROM old_end_date) NOT BETWEEN 2500 AND 2599
           OR new_end_date < new_start_date
    ) THEN
        RAISE EXCEPTION 'Mixed calendars or reversed dates found; no changes made';
    END IF;
END
$precheck$;

UPDATE leave_requests l SET start_date = c.new_start_date, end_date = c.new_end_date
FROM leave_year_repair_candidates c
WHERE l.id = c.id AND l.start_date = c.old_start_date AND l.end_date = c.old_end_date;

DO $verify$
BEGIN
    IF (SELECT count(*) FROM leave_requests) <> (SELECT total FROM leave_year_repair_total)
       OR EXISTS (
           SELECT 1 FROM leave_year_repair_candidates c JOIN leave_requests l ON l.id = c.id
           WHERE l.start_date <> c.new_start_date OR l.end_date <> c.new_end_date
              OR to_jsonb(l) - 'start_date' - 'end_date' <> c.other_fields
       )
       OR EXISTS (
           SELECT 1 FROM leave_requests
           WHERE EXTRACT(YEAR FROM start_date) BETWEEN 2500 AND 2599
              OR EXTRACT(YEAR FROM end_date) BETWEEN 2500 AND 2599
       ) THEN
        RAISE EXCEPTION 'Leave date repair verification failed; transaction rolled back';
    END IF;
END
$verify$;

SELECT count(*) AS corrected_requests FROM leave_year_repair_candidates;
COMMIT;
