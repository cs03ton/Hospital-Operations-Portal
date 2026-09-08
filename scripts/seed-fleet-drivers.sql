\set ON_ERROR_STOP on

BEGIN;

CREATE EXTENSION IF NOT EXISTS pgcrypto;

INSERT INTO roles (id, name, description, is_system_role, is_active, created_at)
SELECT gen_random_uuid(), 'FleetDriver', 'พนักงานขับรถ: เข้าถึงเฉพาะงานขับรถที่ได้รับมอบหมาย', TRUE, TRUE, NOW()
WHERE NOT EXISTS (SELECT 1 FROM roles WHERE name = 'FleetDriver');

WITH driver_permissions(code) AS (
    VALUES
        ('FleetDriver.ViewOwn'),
        ('FleetDriver.ViewOwnJobs'),
        ('FleetDriver.Acknowledge'),
        ('FleetDriver.AcceptJob'),
        ('FleetDriver.DeclineJob'),
        ('FleetDriver.Start'),
        ('FleetDriver.StartTrip'),
        ('FleetDriver.Complete'),
        ('FleetDriver.CompleteTrip'),
        ('FleetTrip.Start'),
        ('FleetTrip.Complete'),
        ('FleetTrip.UploadAttachment'),
        ('FleetCalendar.View')
)
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM roles r
JOIN driver_permissions dp ON TRUE
JOIN permissions p ON p.code = dp.code AND p.is_active
WHERE r.name = 'FleetDriver'
ON CONFLICT (role_id, permission_id) DO NOTHING;

WITH source(employee_code, fullname, license_expiry_date, restriction_note) AS (
    VALUES
        ('nm48003', 'นายอภิสิทธิ์ สารเถื่อนแก้ว', DATE '2029-05-04', 'เวรเช้าและเวร * เท่านั้น'),
        ('nm63004', 'นายวินัย ทับเกลี้ยง',       DATE '2029-03-04', NULL),
        ('nm63003', 'นายฐิตพงศ์ ต๊ะทา',          DATE '2028-01-07', NULL),
        ('nm66006', 'นายภราดร ธิเขียว',           DATE '2029-02-02', NULL),
        ('nm68005', 'นายจิตติวัฒน์ สารเถื่อนแก้ว', DATE '2028-04-24', NULL)
)
INSERT INTO users (
    id, employee_code, fullname, username, password_hash, position, gender,
    department_id, is_active, created_at, updated_at
)
SELECT
    gen_random_uuid(), s.employee_code, s.fullname, s.employee_code,
    crypt(encode(gen_random_bytes(32), 'hex'), gen_salt('bf', 12)),
    'พนักงานขับรถ', 'MALE', NULL, TRUE, NOW(), NOW()
FROM source s
ON CONFLICT (employee_code) DO UPDATE SET
    fullname = EXCLUDED.fullname,
    position = COALESCE(users.position, EXCLUDED.position),
    is_active = TRUE,
    updated_at = NOW();

WITH target_users AS (
    SELECT id FROM users
    WHERE lower(employee_code) IN ('nm48003', 'nm63004', 'nm63003', 'nm66006', 'nm68005')
), target_roles AS (
    SELECT id FROM roles WHERE name IN ('Staff', 'FleetDriver') AND is_active
)
INSERT INTO user_roles (user_id, role_id)
SELECT u.id, r.id FROM target_users u CROSS JOIN target_roles r
ON CONFLICT (user_id, role_id) DO NOTHING;

WITH source(employee_code, license_expiry_date, restriction_note) AS (
    VALUES
        ('nm48003', DATE '2029-05-04', 'เวรเช้าและเวร * เท่านั้น'),
        ('nm63004', DATE '2029-03-04', NULL),
        ('nm63003', DATE '2028-01-07', NULL),
        ('nm66006', DATE '2029-02-02', NULL),
        ('nm68005', DATE '2028-04-24', NULL)
), resolved AS (
    SELECT u.id AS user_id, s.license_expiry_date, s.restriction_note
    FROM source s
    JOIN users u ON lower(u.employee_code) = s.employee_code
)
INSERT INTO fleet_driver_profiles (
    id, user_id, license_number, license_type, license_expiry_date,
    can_drive_sedan, can_drive_pickup, can_drive_van, can_drive_ambulance,
    can_drive_other, driver_status, is_active, note, created_at, updated_at
)
SELECT
    gen_random_uuid(), r.user_id, '', 'ขับรถทุกประเภทชนิดที่ 2', r.license_expiry_date,
    TRUE, TRUE, TRUE, TRUE, FALSE, 'AVAILABLE', TRUE,
    concat_ws(E'\n',
        'ได้รับอนุญาตให้ขับรถ: รถพยาบาล/รถยนต์ส่วนบุคคล',
        CASE WHEN r.restriction_note IS NOT NULL THEN 'ข้อจำกัด: ' || r.restriction_note END,
        'เลขที่ใบขับขี่: ไม่ได้ระบุในข้อมูลต้นทาง'
    ),
    NOW(), NOW()
FROM resolved r
ON CONFLICT (user_id) DO UPDATE SET
    license_type = EXCLUDED.license_type,
    license_expiry_date = EXCLUDED.license_expiry_date,
    can_drive_sedan = EXCLUDED.can_drive_sedan,
    can_drive_pickup = EXCLUDED.can_drive_pickup,
    can_drive_van = EXCLUDED.can_drive_van,
    can_drive_ambulance = EXCLUDED.can_drive_ambulance,
    can_drive_other = EXCLUDED.can_drive_other,
    driver_status = EXCLUDED.driver_status,
    is_active = EXCLUDED.is_active,
    note = EXCLUDED.note,
    updated_at = NOW();

COMMIT;

SELECT
    u.employee_code, u.fullname, d.name AS department,
    fp.license_type, fp.license_expiry_date, fp.driver_status, fp.is_active
FROM users u
JOIN fleet_driver_profiles fp ON fp.user_id = u.id
LEFT JOIN departments d ON d.id = u.department_id
WHERE lower(u.employee_code) IN ('nm48003', 'nm63004', 'nm63003', 'nm66006', 'nm68005')
ORDER BY u.employee_code;
