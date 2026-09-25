-- HOP / DBeaver: grant read access to general manuals to every active role.
-- Deploy the Backend change that separates general and administrator manuals as well.
-- Run this whole file with Stop on error. If this connection previously had 25P02,
-- run ROLLBACK in the same connection before starting.
-- This grants only Documentation.View; it never assigns roles to users.

BEGIN;

DO $precheck$
BEGIN
    IF to_regclass('public.roles') IS NULL
       OR to_regclass('public.permissions') IS NULL
       OR to_regclass('public.role_permissions') IS NULL
       OR to_regclass('public.users') IS NULL
       OR to_regclass('public.user_roles') IS NULL THEN
        RAISE EXCEPTION 'Required HOP role/permission tables are missing';
    END IF;
    IF (SELECT count(*) FROM permissions WHERE code = 'Documentation.View' AND is_active) <> 1 THEN
        RAISE EXCEPTION 'Exactly one active Documentation.View permission is required';
    END IF;
END $precheck$;

INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM roles AS r
CROSS JOIN permissions AS p
WHERE r.is_active
  AND p.code = 'Documentation.View'
  AND p.is_active
ON CONFLICT (role_id, permission_id) DO NOTHING;

DO $postcheck$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM roles AS r
        CROSS JOIN permissions AS p
        WHERE r.is_active AND p.code = 'Documentation.View' AND p.is_active
          AND NOT EXISTS (
              SELECT 1 FROM role_permissions AS rp
              WHERE rp.role_id = r.id AND rp.permission_id = p.id
          )
    ) THEN
        RAISE EXCEPTION 'Some active roles still lack Documentation.View; transaction rolled back';
    END IF;
END $postcheck$;

COMMIT;

-- These are read-only checks after commit.
SELECT r.name AS role_name,
       r.is_active,
       EXISTS (
           SELECT 1 FROM role_permissions AS rp
           JOIN permissions AS p ON p.id = rp.permission_id
           WHERE rp.role_id = r.id AND p.code = 'Documentation.View' AND p.is_active
       ) AS can_view_general_manuals
FROM roles AS r
ORDER BY r.is_active DESC, r.name;

-- Active accounts with no active role need separate identity/role review.
-- This script intentionally does not assign them a role.
SELECT u.id, u.username
FROM users AS u
WHERE u.is_active
  AND NOT EXISTS (
      SELECT 1 FROM user_roles AS ur
      JOIN roles AS r ON r.id = ur.role_id AND r.is_active
      WHERE ur.user_id = u.id
  )
ORDER BY u.username;
