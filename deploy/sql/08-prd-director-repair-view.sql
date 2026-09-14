BEGIN;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM roles WHERE name = 'Director' AND is_active) THEN
        RAISE EXCEPTION 'Active Director role is required';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM permissions WHERE code = 'RepairManagement.ViewAll' AND is_active) THEN
        RAISE EXCEPTION 'Active RepairManagement.ViewAll permission is required';
    END IF;
END $$;

INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM roles r
CROSS JOIN permissions p
WHERE r.name = 'Director'
  AND r.is_active
  AND p.code = 'RepairManagement.ViewAll'
  AND p.is_active
ON CONFLICT DO NOTHING;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM role_permissions rp
        JOIN roles r ON r.id = rp.role_id
        JOIN permissions p ON p.id = rp.permission_id
        WHERE r.name = 'Director' AND p.code = 'RepairManagement.ViewAll'
    ) THEN
        RAISE EXCEPTION 'Director repair read permission was not created';
    END IF;
END $$;

COMMIT;
