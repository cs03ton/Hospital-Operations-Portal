-- HOP PRD employee import generated from the supplied 61-row roster.
-- DBeaver: execute as a SQL script with Stop on error; the final SELECT reports the result.
-- Username = employee code. New accounts only: password 1234, bcrypt cost 12, Staff role.
-- Temporary employees in this roster are mapped to TEMPORARY_EMPLOYEE_DAILY as confirmed.
-- Existing accounts are skipped; their profile, roles, and password are never overwritten.
-- Blank source status is treated as active. Canonical PRD departments must already exist.
BEGIN;
SET LOCAL search_path = public, pg_temp;
SET LOCAL lock_timeout = '10s';
SET LOCAL statement_timeout = '120s';

DO $precheck$
BEGIN
    IF to_regclass('public.users') IS NULL OR to_regclass('public.departments') IS NULL
       OR to_regclass('public.user_roles') IS NULL OR to_regclass('public.roles') IS NULL THEN
        RAISE EXCEPTION 'Required HOP tables are missing';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'pgcrypto') THEN
        RAISE EXCEPTION 'pgcrypto is required; run the PRD master-data setup first';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM roles WHERE name = 'Staff' AND is_active) THEN
        RAISE EXCEPTION 'Active Staff role is required';
    END IF;
END
$precheck$;

CREATE TEMP TABLE employee_seed_source (
    employee_code text PRIMARY KEY, fullname text NOT NULL, position text NOT NULL,
    department_name text NOT NULL, gender text NOT NULL,
    employment_type text NOT NULL, employment_start_date date NOT NULL
) ON COMMIT DROP;

INSERT INTO employee_seed_source VALUES
    ('nm64002', 'นายชัยภัทร กะจันทร์', 'นายแพทย์', 'กลุ่มงานการแพทย์', 'Male', 'CIVIL_SERVANT', DATE '2021-06-01'),
    ('nm68002', 'นางสาวชนินาถ แพทย์สมาน', 'นายแพทย์', 'กลุ่มงานการแพทย์', 'Female', 'CIVIL_SERVANT', DATE '2025-06-01'),
    ('nm67002', 'นางสาวกนกรัตน์ มงคล', 'นายแพทย์', 'กลุ่มงานการแพทย์', 'Female', 'CIVIL_SERVANT', DATE '2024-05-13'),
    ('nm44001', 'นายเทอดศักดิ์ อุตศรี', 'ทันตแพทย์', 'กลุ่มงานทันตกรรม', 'Male', 'CIVIL_SERVANT', DATE '2001-04-02'),
    ('nm61001', 'นางสาวบุษยามาส ทรายแก่น', 'ทันตแพทย์', 'กลุ่มงานทันตกรรม', 'Female', 'CIVIL_SERVANT', DATE '2018-05-17'),
    ('nm42001', 'นางพาวรรณตรี ปิจนำ', 'เจ้าพนักงานทันตสาธารณสุข', 'กลุ่มงานทันตกรรม', 'Female', 'CIVIL_SERVANT', DATE '1999-04-01'),
    ('nm30001', 'นางนิศานาถ สารเถื่อนแก้ว', 'พยาบาลวิชาชีพ', 'กลุ่มงานการพยาบาล', 'Female', 'CIVIL_SERVANT', DATE '1987-04-01'),
    ('nm38001', 'นางปาริชาติ จักธร', 'พยาบาลวิชาชีพ', 'กลุ่มงานการพยาบาล', 'Female', 'CIVIL_SERVANT', DATE '1995-04-03'),
    ('nm35002', 'นางลำดวน น้อยอินต๊ะ', 'พยาบาลวิชาชีพ', 'กลุ่มงานการพยาบาล', 'Female', 'CIVIL_SERVANT', DATE '1992-04-01'),
    ('nm57001', 'นางศุภลักษณ์ กองคำ', 'พยาบาลวิชาชีพ', 'กลุ่มงานการพยาบาล', 'Female', 'CIVIL_SERVANT', DATE '2014-05-03'),
    ('nm56001', 'นางสาวเจนจิรา สุภาแก้ว', 'พยาบาลวิชาชีพ', 'กลุ่มงานการพยาบาล', 'Female', 'CIVIL_SERVANT', DATE '2013-04-03'),
    ('nm42002', 'นางสุนทรี ติอิน', 'พยาบาลวิชาชีพ', 'กลุ่มงานการพยาบาล', 'Female', 'CIVIL_SERVANT', DATE '1999-04-01'),
    ('nm44002', 'นางบุษบา ยศหล้า', 'พยาบาลวิชาชีพ', 'กลุ่มงานการพยาบาล', 'Female', 'CIVIL_SERVANT', DATE '2001-04-02'),
    ('nm52003', 'นางกันยากร งึ้มนันใจ', 'พยาบาลวิชาชีพ', 'กลุ่มงานการพยาบาล', 'Female', 'CIVIL_SERVANT', DATE '2009-06-15'),
    ('nm52004', 'นางจีระนันท์ บัตริยะ', 'พยาบาลวิชาชีพ', 'กลุ่มงานการพยาบาล', 'Female', 'CIVIL_SERVANT', DATE '2009-10-01'),
    ('nm48001', 'นางกรรณิการ์ อนัญญาวงศ์', 'พยาบาลวิชาชีพ', 'กลุ่มงานการพยาบาล', 'Female', 'CIVIL_SERVANT', DATE '2005-04-01'),
    ('nm41001', 'นางศรีแพร เปี่ยมทวีศักดิ์', 'พยาบาลวิชาชีพ', 'กลุ่มงานการพยาบาล', 'Female', 'CIVIL_SERVANT', DATE '1998-04-01'),
    ('nm41002', 'นางหทัยกาญจน์ ยินดีผล', 'พยาบาลวิชาชีพ', 'กลุ่มงานการพยาบาล', 'Female', 'CIVIL_SERVANT', DATE '1998-04-01'),
    ('nm47001', 'นางสาววาสนา พึ่งเมือง', 'พยาบาลวิชาชีพ', 'กลุ่มงานการพยาบาล', 'Female', 'CIVIL_SERVANT', DATE '2004-04-01'),
    ('nm41003', 'นางดวงนภา อินต๊ะเขื่อน', 'พยาบาลวิชาชีพ', 'กลุ่มงานการพยาบาล', 'Female', 'CIVIL_SERVANT', DATE '1998-04-01'),
    ('nm59003', 'นางสาวกิตติยา กิตติพิบูลศักดิ์', 'พยาบาลวิชาชีพ', 'กลุ่มงานการพยาบาล', 'Female', 'CIVIL_SERVANT', DATE '2016-06-01'),
    ('nm62002', 'นางสาวปียาภรณ์ เรืองรินทร์', 'พยาบาลวิชาชีพ', 'กลุ่มงานการพยาบาล', 'Female', 'CIVIL_SERVANT', DATE '2019-06-04'),
    ('nm53002', 'นางสาวฐานิยา งามทรง', 'พยาบาลวิชาชีพ', 'กลุ่มงานการพยาบาล', 'Female', 'CIVIL_SERVANT', DATE '2010-04-01'),
    ('nm48004', 'นางสุภร น้อยอินต๊ะ', 'พนักงานช่วยเหลือคนไข้', 'กลุ่มงานการพยาบาล', 'Female', 'MOPH_EMPLOYEE', DATE '2005-06-17'),
    ('nm60001', 'นายอนุสรณ์ อยู่เย็น', 'แพทย์แผนไทย', 'กลุ่มงานการแพทย์แผนไทยและการแพทย์ทางเลือก', 'Male', 'CIVIL_SERVANT', DATE '2017-02-20'),
    ('nm57002', 'นางสาวณัฐรดา มาชมภู', 'นักรังสีการแพทย์', 'กลุ่มงานรังสีวิทยา', 'Female', 'CIVIL_SERVANT', DATE '2014-07-01'),
    ('nm59001', 'นางสาวชนิดา นิลอุบล', 'นักกายภาพบำบัด', 'กลุ่มงานเวชกรรมฟื้นฟู', 'Female', 'CIVIL_SERVANT', DATE '2016-05-02'),
    ('nm69001', 'นายสุทธินันนท์ กันชนะ', 'พนักงานบริการ', 'กลุ่มงานบริหารทั่วไป', 'Male', 'TEMPORARY_EMPLOYEE_DAILY', DATE '2026-02-02'),
    ('nm45003', 'นางสาวนงนุช คำป๊อก', 'ผู้ช่วยทันตแพทย์', 'กลุ่มงานทันตกรรม', 'Female', 'MOPH_EMPLOYEE', DATE '2002-10-01'),
    ('nm64009', 'นางสาวชรินทร์รัตน์ ง้วนกันทะ', 'ผู้ช่วยทันตแพทย์', 'กลุ่มงานทันตกรรม', 'Female', 'MOPH_EMPLOYEE', DATE '2021-12-01'),
    ('nm63005', 'นายสุพจน์ ปินตาเทพ', 'พนักงานบริการ', 'กลุ่มงานรังสีวิทยา', 'Male', 'MOPH_EMPLOYEE', DATE '2020-10-02'),
    ('nm54001', 'นางศุภรัศม์ สารเถื่อนแก้ว', 'พนักงานช่วยการพยาบาล', 'กลุ่มงานการแพทย์แผนไทยและการแพทย์ทางเลือก', 'Female', 'MOPH_EMPLOYEE', DATE '2011-06-01'),
    ('nm62003', 'นางนิตยา รินสิริ', 'ผู้ช่วยนักกายภาพบำบัด', 'กลุ่มงานเวชกรรมฟื้นฟู', 'Female', 'MOPH_EMPLOYEE', DATE '2019-11-01'),
    ('nm40001', 'นางฉัตรนภา บุญดวง', 'พนักงานช่วยเหลือคนไข้', 'กลุ่มงานการพยาบาล', 'Female', 'MOPH_EMPLOYEE', DATE '1997-10-01'),
    ('nm40002', 'นางสาวพนารัตน์ น้อยอินต๊ะ', 'พนักงานช่วยเหลือคนไข้', 'กลุ่มงานการพยาบาล', 'Female', 'MOPH_EMPLOYEE', DATE '1997-10-01'),
    ('nm55003', 'นางสาวประภานิช ต๊ะนนท์', 'พนักงานช่วยเหลือคนไข้', 'กลุ่มงานการพยาบาล', 'Female', 'MOPH_EMPLOYEE', DATE '2012-05-04'),
    ('nm45001', 'นางนันท์ชญาน์ ต้นกัน', 'พนักงานช่วยเหลือคนไข้', 'กลุ่มงานการพยาบาล', 'Female', 'MOPH_EMPLOYEE', DATE '2002-06-01'),
    ('nm49001', 'นางศรีแอ๊ด ขัดใจ', 'พนักงานช่วยเหลือคนไข้', 'กลุ่มงานการพยาบาล', 'Female', 'MOPH_EMPLOYEE', DATE '2006-10-05'),
    ('nm43001', 'นางณัฐติยา สารเถื่อนแก้ว', 'พนักงานช่วยเหลือคนไข้', 'กลุ่มงานการพยาบาล', 'Female', 'MOPH_EMPLOYEE', DATE '2000-04-05'),
    ('nm55004', 'นางภัทราภา ตันอิ่น', 'พนักงานช่วยเหลือคนไข้', 'กลุ่มงานการพยาบาล', 'Female', 'MOPH_EMPLOYEE', DATE '2012-11-16'),
    ('nm60002', 'นางกิ่งแก้ว พรมฆ้อง', 'พนักงานช่วยเหลือคนไข้', 'กลุ่มงานการพยาบาล', 'Female', 'MOPH_EMPLOYEE', DATE '2017-07-03'),
    ('nm50001', 'นางเกศินี แก้วกุลสี', 'พนักงานช่วยเหลือคนไข้', 'กลุ่มงานการพยาบาล', 'Female', 'MOPH_EMPLOYEE', DATE '2007-07-02'),
    ('nm53001', 'นางรัตนพรรณ นิลบุญเรือง', 'พนักงานช่วยเหลือคนไข้', 'กลุ่มงานการพยาบาล', 'Female', 'MOPH_EMPLOYEE', DATE '2010-02-03'),
    ('nm66001', 'นางสายชล น้อยอินต๊ะ', 'พนักงานช่วยเหลือคนไข้', 'กลุ่มงานการพยาบาล', 'Female', 'MOPH_EMPLOYEE', DATE '2023-01-03'),
    ('nm65003', 'นายศุภกิจ โนวังหาร', 'พยาบาลวิชาชีพ', 'กลุ่มงานการพยาบาล', 'Male', 'MOPH_EMPLOYEE', DATE '2022-04-01'),
    ('nm66005', 'นางสาวฉัตรชนก พรมแสนปัง', 'พยาบาลวิชาชีพ', 'กลุ่มงานการพยาบาล', 'Female', 'MOPH_EMPLOYEE', DATE '2023-04-03'),
    ('nm68004', 'นางสาวกัลยา เตชะพิมพ์', 'เจ้าพนักงานสาธารณสุข', 'กลุ่มงานการพยาบาล', 'Female', 'MOPH_EMPLOYEE', DATE '2025-09-02'),
    ('nm69004', 'นางสาวนันทิกานต์ ยานะน้อง', 'พยาบาลวิชาชีพ', 'กลุ่มงานการพยาบาล', 'Female', 'TEMPORARY_EMPLOYEE_DAILY', DATE '2026-04-01'),
    ('nm69008', 'นางสาวรัชดาพร ยศวงค์', 'พนักงานทำความสะอาด', 'กลุ่มงานบริหารทั่วไป', 'Female', 'TEMPORARY_EMPLOYEE_DAILY', DATE '2026-07-01'),
    ('nm47002', 'นางวินัส ปาโน', 'พนักงานบริการ', 'กลุ่มงานบริหารทั่วไป', 'Female', 'TEMPORARY_EMPLOYEE_DAILY', DATE '2004-07-01'),
    ('nm63006', 'นางสาวเปรมสุดา คำอ้ายด้วง', 'พนักงานประกอบอาหาร', 'กลุ่มงานบริหารทั่วไป', 'Female', 'TEMPORARY_EMPLOYEE_DAILY', DATE '2020-11-01'),
    ('nm64004', 'นายเอกนรินทร์ เรืองรินทร์', 'พนักงานเกษตรขั้นพื้นฐาน', 'กลุ่มงานบริหารทั่วไป', 'Male', 'TEMPORARY_EMPLOYEE_DAILY', DATE '2021-10-01'),
    ('nm64005', 'นายไชยนาม ธิเขียว', 'ผู้ช่วยช่างทั่วไป', 'กลุ่มงานบริหารทั่วไป', 'Male', 'TEMPORARY_EMPLOYEE_DAILY', DATE '2021-10-01'),
    ('nm64006', 'นางสร้อยเพชร ต๊ะเสน', 'พนักงานซักฟอก', 'กลุ่มงานการพยาบาล', 'Female', 'TEMPORARY_EMPLOYEE_DAILY', DATE '2021-10-01'),
    ('nm64007', 'นางสาวสุพรรณี ขันตัน', 'พนักงานบริการ', 'กลุ่มงานบริหารทั่วไป', 'Female', 'TEMPORARY_EMPLOYEE_DAILY', DATE '2021-10-01'),
    ('nm64001', 'นางณัฐรุจา เข็มทอง', 'พนักงานซักฟอก', 'กลุ่มงานการพยาบาล', 'Female', 'TEMPORARY_EMPLOYEE_DAILY', DATE '2021-03-01'),
    ('nm64008', 'นางสาวออนจิตา ขันคำมาละ', 'พนักงานบริการ', 'กลุ่มงานบริหารทั่วไป', 'Female', 'TEMPORARY_EMPLOYEE_DAILY', DATE '2021-10-01'),
    ('nm66003', 'นางสาวเสาวลักษณ์ ติสระ', 'พนักงานช่วยเหลือคนไข้', 'กลุ่มงานการพยาบาล', 'Female', 'TEMPORARY_EMPLOYEE_DAILY', DATE '2023-03-01'),
    ('nm66004', 'นางณิชา วิละปิง', 'พนักงานประกอบอาหาร', 'กลุ่มงานบริหารทั่วไป', 'Female', 'TEMPORARY_EMPLOYEE_DAILY', DATE '2023-03-01'),
    ('nm69005', 'นายธนกฤต ขัติวงศ์', 'พนักงานรักษาความปลอดภัย', 'กลุ่มงานบริหารทั่วไป', 'Male', 'TEMPORARY_EMPLOYEE_DAILY', DATE '2026-04-01'),
    ('nm69006', 'นายกล้าณรงค์ แก้วกุลสี', 'พนักงานรักษาความปลอดภัย', 'กลุ่มงานบริหารทั่วไป', 'Male', 'TEMPORARY_EMPLOYEE_DAILY', DATE '2026-04-01');

CREATE TEMP TABLE employee_seed_departments (name text PRIMARY KEY, id uuid NOT NULL) ON COMMIT DROP;
INSERT INTO employee_seed_departments VALUES
        ('กลุ่มงานการพยาบาล', '019f7dd2-dbb1-75b5-90e8-924cb0c0b46c'::uuid),
        ('กลุ่มงานการแพทย์', '019f7dd2-7da1-7c54-bef7-7249c63ea426'::uuid),
        ('กลุ่มงานการแพทย์แผนไทยและการแพทย์ทางเลือก', '019f7dd4-63a4-77cd-bd1a-3d4b25ad5de0'::uuid),
        ('กลุ่มงานทันตกรรม', '019f7dd3-d665-7ecc-a7f5-8041545d7fce'::uuid),
        ('กลุ่มงานบริหารทั่วไป', '019f7dd2-adb8-7033-ab3d-de5b4d9e3d3b'::uuid),
        ('กลุ่มงานรังสีวิทยา', '019f7dd3-084f-70bf-a917-dba4b45336f5'::uuid),
        ('กลุ่มงานเวชกรรมฟื้นฟู', '019f7dd4-3706-7339-9d3c-dec502a33655'::uuid);

DO $validate$
BEGIN
    IF (SELECT count(*) FROM employee_seed_source) <> 61 THEN
        RAISE EXCEPTION 'Expected 61 employee records';
    END IF;
    IF EXISTS (
        SELECT 1 FROM employee_seed_departments m
        LEFT JOIN departments d ON d.id = m.id AND d.name = m.name AND d.is_active
        WHERE d.id IS NULL
    ) OR EXISTS (
        SELECT 1 FROM employee_seed_source s
        LEFT JOIN employee_seed_departments m ON m.name = s.department_name
        WHERE m.id IS NULL
    ) THEN
        RAISE EXCEPTION 'Canonical department ID/name missing or inactive; no changes made';
    END IF;
    IF EXISTS (
        SELECT 1 FROM employee_seed_source s JOIN users u
          ON lower(u.username) = lower(s.employee_code)
         AND (u.employee_code IS NULL OR lower(u.employee_code) <> lower(s.employee_code))
    ) THEN
        RAISE EXCEPTION 'Username collision with a different employee code; no changes made';
    END IF;
    IF EXISTS (
        SELECT 1 FROM employee_seed_source s JOIN users u
          ON lower(u.employee_code) = lower(s.employee_code)
         AND lower(u.username) <> lower(s.employee_code)
    ) THEN
        RAISE EXCEPTION 'Existing employee code has a different username; review manually';
    END IF;
END
$validate$;

CREATE TEMP TABLE employee_seed_inserted (id uuid PRIMARY KEY, employee_code text NOT NULL) ON COMMIT DROP;
WITH new_users AS (
    INSERT INTO users (id, employee_code, fullname, username, password_hash,
                       position, gender, employment_type, employment_start_date,
                       department_id, is_active, created_at)
    SELECT gen_random_uuid(), s.employee_code, s.fullname, s.employee_code,
           crypt('1234', gen_salt('bf', 12)), s.position, s.gender,
           s.employment_type, s.employment_start_date, d.id, TRUE, now()
    FROM employee_seed_source s
    JOIN employee_seed_departments d ON d.name = s.department_name
    WHERE NOT EXISTS (SELECT 1 FROM users u WHERE lower(u.employee_code) = lower(s.employee_code))
    RETURNING id, employee_code
)
INSERT INTO employee_seed_inserted SELECT id, employee_code FROM new_users;

INSERT INTO user_roles (user_id, role_id)
SELECT i.id, r.id FROM employee_seed_inserted i CROSS JOIN roles r
WHERE r.name = 'Staff' AND r.is_active
ON CONFLICT (user_id, role_id) DO NOTHING;

SELECT (SELECT count(*) FROM employee_seed_source) AS source_count,
       (SELECT count(*) FROM employee_seed_inserted) AS inserted_count,
       (SELECT count(*) FROM employee_seed_source) - (SELECT count(*) FROM employee_seed_inserted) AS existing_skipped_count,
       (SELECT count(*) FROM user_roles ur JOIN employee_seed_inserted i ON i.id = ur.user_id
        JOIN roles r ON r.id = ur.role_id WHERE r.name = 'Staff') AS new_staff_grants;
COMMIT;