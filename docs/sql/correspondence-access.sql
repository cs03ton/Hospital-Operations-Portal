-- Run manually in DBeaver against the intended database after review.
-- No schema migration and no user assignments. Safe to run repeatedly.
BEGIN;
INSERT INTO permissions (id, code, name, group_name, action, is_active, created_at)
VALUES (gen_random_uuid(), 'Correspondence.Use', 'ใช้งานสารบรรณ', 'Correspondence', 'Use', true, now())
ON CONFLICT (code) DO UPDATE SET is_active = true, updated_at = now();

INSERT INTO roles (id, name, description, is_system_role, is_active, created_at)
VALUES (gen_random_uuid(), 'ผู้ใช้งานสารบรรณ', 'สร้างบันทึกข้อความและดาวน์โหลด DOCX/PDF; SuperAdmin เป็นผู้มอบสิทธิ์', true, true, now())
ON CONFLICT (name) DO UPDATE SET is_system_role = true, is_active = true, updated_at = now();

INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id FROM roles r CROSS JOIN permissions p
WHERE r.name IN ('ผู้ใช้งานสารบรรณ', 'Admin', 'SuperAdmin') AND p.code = 'Correspondence.Use'
ON CONFLICT (role_id, permission_id) DO NOTHING;
COMMIT;

-- Verify definitions only; this script never inserts into user_roles.
SELECT r.name AS role_name, p.code AS permission_code
FROM roles r JOIN role_permissions rp ON rp.role_id = r.id
JOIN permissions p ON p.id = rp.permission_id
WHERE p.code = 'Correspondence.Use' ORDER BY r.name;
