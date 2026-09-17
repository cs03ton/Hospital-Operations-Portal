-- Repair department_id after 10-prd-employee-seed.sql was committed with roster group names.
-- Run this WHOLE file in DBeaver as a SQL Script with Stop on error.
-- It changes only users.department_id for roster accounts still linked to the seven
-- departments created by that seed. It then removes those departments if unreferenced.
-- Any failed assertion aborts the transaction; issue ROLLBACK in the same connection.
BEGIN;
SET LOCAL search_path = public, pg_temp;
SET LOCAL lock_timeout = '10s';
SET LOCAL statement_timeout = '120s';

CREATE TEMP TABLE employee_dept_repair_map (
    old_name text PRIMARY KEY,
    target_name text NOT NULL,
    target_id uuid NOT NULL,
    employee_codes text[] NOT NULL
) ON COMMIT DROP;

INSERT INTO employee_dept_repair_map VALUES
    ('การพยาบาล', 'กลุ่มงานการพยาบาล', '019f7dd2-dbb1-75b5-90e8-924cb0c0b46c',
     ARRAY['nm30001','nm38001','nm35002','nm57001','nm56001','nm42002','nm44002','nm52003','nm52004','nm48001','nm41001','nm41002','nm47001','nm41003','nm59003','nm62002','nm53002','nm48004','nm40001','nm40002','nm55003','nm45001','nm49001','nm43001','nm55004','nm60002','nm50001','nm53001','nm66001','nm65003','nm66005','nm68004','nm69004','nm64006','nm64001','nm66003']),
    ('ทันตกรรม', 'กลุ่มงานทันตกรรม', '019f7dd3-d665-7ecc-a7f5-8041545d7fce',
     ARRAY['nm44001','nm61001','nm42001','nm45003','nm64009']),
    ('บริหารทั่วไป', 'กลุ่มงานบริหารทั่วไป', '019f7dd2-adb8-7033-ab3d-de5b4d9e3d3b',
     ARRAY['nm69001','nm69008','nm47002','nm63006','nm64004','nm64005','nm64007','nm64008','nm66004','nm69005','nm69006']),
    ('แพทย์แผนไทย', 'กลุ่มงานการแพทย์แผนไทยและการแพทย์ทางเลือก', '019f7dd4-63a4-77cd-bd1a-3d4b25ad5de0',
     ARRAY['nm60001','nm54001']),
    ('ฟื้นฟู', 'กลุ่มงานเวชกรรมฟื้นฟู', '019f7dd4-3706-7339-9d3c-dec502a33655',
     ARRAY['nm59001','nm62003']),
    ('รังสีการแพทย์', 'กลุ่มงานรังสีวิทยา', '019f7dd3-084f-70bf-a917-dba4b45336f5',
     ARRAY['nm57002','nm63005']),
    ('องค์กรแพทย์', 'กลุ่มงานการแพทย์', '019f7dd2-7da1-7c54-bef7-7249c63ea426',
     ARRAY['nm64002','nm68002','nm67002']);

CREATE TEMP TABLE employee_dept_repair_targets ON COMMIT DROP AS
SELECT m.old_name, m.target_name, m.target_id, old.id AS old_id,
       lower(codes.code) AS employee_code
FROM employee_dept_repair_map m
JOIN departments old ON old.name = m.old_name
    AND old.description = 'Imported from employee roster'
CROSS JOIN LATERAL unnest(m.employee_codes) AS codes(code);

-- Preview: current wrong assignments by source group. The sum may be below 61
-- because the original seed skipped pre-existing users.
SELECT t.old_name, t.target_name, t.target_id,
       count(u.id) AS roster_users_still_on_wrong_department
FROM employee_dept_repair_targets t
LEFT JOIN users u ON lower(u.employee_code) = t.employee_code AND u.department_id = t.old_id
GROUP BY t.old_name, t.target_name, t.target_id
ORDER BY t.old_name;

-- Preview: every account that this transaction would change.
SELECT u.employee_code, u.fullname, t.old_name AS current_department,
       t.target_name AS corrected_department
FROM employee_dept_repair_targets t
JOIN users u ON lower(u.employee_code) = t.employee_code AND u.department_id = t.old_id
ORDER BY t.old_name, u.employee_code;

-- Snapshot the affected accounts so the final check can prove that no other
-- user fields (including password, roles stored on users, and employment type) changed.
CREATE TEMP TABLE employee_dept_repair_before ON COMMIT DROP AS
SELECT u.id, to_jsonb(u) - 'department_id' - 'updated_at' AS other_fields
FROM users u JOIN employee_dept_repair_targets t
  ON lower(u.employee_code) = t.employee_code AND u.department_id = t.old_id;
CREATE TEMP TABLE employee_dept_repair_user_count ON COMMIT DROP AS
SELECT count(*) AS total FROM users;

DO $validate$
DECLARE fk record; reference_count bigint;
BEGIN
    IF (SELECT count(*) FROM employee_dept_repair_map) <> 7
       OR (SELECT count(DISTINCT old_name) FROM employee_dept_repair_targets) <> 7
       OR (SELECT count(*) FROM employee_dept_repair_targets) <> 61
       OR (SELECT count(DISTINCT employee_code) FROM employee_dept_repair_targets) <> 61 THEN
        RAISE EXCEPTION 'Expected seven seed-created departments and 61 unique roster codes; no changes made';
    END IF;
    IF EXISTS (
        SELECT 1 FROM employee_dept_repair_map m
        LEFT JOIN departments d ON d.id = m.target_id AND d.name = m.target_name AND d.is_active
        WHERE d.id IS NULL
    ) THEN
        RAISE EXCEPTION 'A canonical department ID/name is missing or inactive; no changes made';
    END IF;
    IF EXISTS (
        SELECT 1 FROM users u JOIN employee_dept_repair_targets t ON u.department_id = t.old_id
        WHERE lower(u.employee_code) <> t.employee_code
          AND NOT EXISTS (
              SELECT 1 FROM employee_dept_repair_targets own
              WHERE own.old_id = u.department_id AND own.employee_code = lower(u.employee_code)
          )
    ) THEN
        RAISE EXCEPTION 'An account outside the supplied roster uses a seed-created department; no changes made';
    END IF;
    -- All single-column foreign keys to departments except users are checked.
    -- If any workflow record refers to a wrong department, stop rather than guessing
    -- whether its historical department should be changed.
    FOR fk IN
        SELECT con.conrelid::regclass AS table_name, a.attname AS column_name
        FROM pg_constraint con
        JOIN pg_attribute a ON a.attrelid = con.conrelid AND a.attnum = con.conkey[1]
        WHERE con.contype = 'f' AND con.confrelid = 'public.departments'::regclass
          AND array_length(con.conkey, 1) = 1 AND con.conrelid <> 'public.users'::regclass
    LOOP
        EXECUTE format('SELECT count(*) FROM %s WHERE %I IN (SELECT DISTINCT old_id FROM employee_dept_repair_targets)',
                       fk.table_name, fk.column_name) INTO reference_count;
        IF reference_count > 0 THEN
            RAISE EXCEPTION 'Wrong department is referenced by %.% (% rows); no changes made',
                fk.table_name, fk.column_name, reference_count;
        END IF;
    END LOOP;
END
$validate$;

CREATE TEMP TABLE employee_dept_repair_changed (
    employee_code text PRIMARY KEY, user_id uuid NOT NULL,
    old_department_id uuid NOT NULL, new_department_id uuid NOT NULL
) ON COMMIT DROP;

WITH changed AS (
    UPDATE users u SET department_id = t.target_id, updated_at = now()
    FROM employee_dept_repair_targets t
    WHERE lower(u.employee_code) = t.employee_code AND u.department_id = t.old_id
    RETURNING u.employee_code, u.id, u.department_id
)
INSERT INTO employee_dept_repair_changed
SELECT c.employee_code, c.id, t.old_id, t.target_id
FROM changed c JOIN employee_dept_repair_targets t
  ON t.employee_code = lower(c.employee_code) AND t.target_id = c.department_id;

DO $verify$
BEGIN
    IF (SELECT count(*) FROM employee_dept_repair_changed)
       <> (SELECT count(*) FROM employee_dept_repair_before) THEN
        RAISE EXCEPTION 'Affected-user count changed unexpectedly; transaction rolled back';
    END IF;
    IF EXISTS (
        SELECT 1 FROM employee_dept_repair_before b JOIN users u ON u.id = b.id
        WHERE to_jsonb(u) - 'department_id' - 'updated_at' <> b.other_fields
    ) THEN
        RAISE EXCEPTION 'Other user fields changed unexpectedly; transaction rolled back';
    END IF;
    IF EXISTS (
        SELECT 1 FROM employee_dept_repair_changed c JOIN users u ON u.id = c.user_id
        WHERE u.department_id <> c.new_department_id
    ) OR EXISTS (
        SELECT 1 FROM employee_dept_repair_targets t JOIN users u
          ON lower(u.employee_code) = t.employee_code AND u.department_id = t.old_id
    ) THEN
        RAISE EXCEPTION 'Department repair verification failed; transaction rolled back';
    END IF;
    IF EXISTS (
        SELECT 1 FROM users u JOIN employee_dept_repair_targets t ON u.department_id = t.old_id
    ) THEN
        RAISE EXCEPTION 'Seed-created department still has user references; transaction rolled back';
    END IF;
END
$verify$;

-- These rows were created by the faulty seed; FK constraints prevent deletion if
-- any unexpected reference appeared since the validation above.
DELETE FROM departments d
USING (SELECT DISTINCT old_id FROM employee_dept_repair_targets) t
WHERE d.id = t.old_id AND d.description = 'Imported from employee roster';

DO $final$
BEGIN
    IF (SELECT count(*) FROM users) <> (SELECT total FROM employee_dept_repair_user_count) THEN
        RAISE EXCEPTION 'Total user count changed unexpectedly; transaction rolled back';
    END IF;
    IF EXISTS (SELECT 1 FROM departments d JOIN employee_dept_repair_map m ON d.name = m.old_name
               WHERE d.description = 'Imported from employee roster') THEN
        RAISE EXCEPTION 'Seed-created departments were not fully removed; transaction rolled back';
    END IF;
END
$final$;

SELECT count(*) AS corrected_users FROM employee_dept_repair_changed;
SELECT old_name, target_name, target_id,
       (SELECT count(*) FROM employee_dept_repair_changed c WHERE c.new_department_id = m.target_id) AS corrected_users
FROM employee_dept_repair_map m ORDER BY old_name;
COMMIT;
