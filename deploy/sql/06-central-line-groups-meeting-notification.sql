BEGIN;

INSERT INTO permissions (id, code, name, group_name, action, is_active, created_at)
VALUES
  (gen_random_uuid(), 'LineGroup.View', 'ดูกลุ่มแจ้งเตือนส่วนกลาง', 'LineGroup', 'View', TRUE, NOW()),
  (gen_random_uuid(), 'LineGroup.Manage', 'จัดการกลุ่มและเหตุการณ์แจ้งเตือนส่วนกลาง', 'LineGroup', 'Manage', TRUE, NOW())
ON CONFLICT (code) DO UPDATE SET name = EXCLUDED.name, group_name = EXCLUDED.group_name,
  action = EXCLUDED.action, is_active = TRUE;

INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id FROM roles r CROSS JOIN permissions p
WHERE r.name IN ('Admin', 'SuperAdmin') AND r.is_active
  AND p.code IN ('LineGroup.View', 'LineGroup.Manage') AND p.is_active
ON CONFLICT DO NOTHING;

INSERT INTO line_group_event_subscriptions (id, destination_id, event_type, is_enabled, created_at)
SELECT gen_random_uuid(), d.id, e.event_type,
  CASE WHEN d.module IN ('REPAIR_IT', 'REPAIR_GENERAL') THEN e.event_type LIKE 'Repair.%' ELSE FALSE END,
  NOW()
FROM line_group_destinations d
CROSS JOIN (VALUES
  ('Repair.Submitted'), ('Repair.Resubmitted'), ('Repair.Reopened'), ('Repair.Started'),
  ('Repair.Resumed'), ('Repair.Solved'), ('Repair.Closed'), ('MeetingRoom.BookingCreated')
) AS e(event_type)
ON CONFLICT (destination_id, event_type) DO NOTHING;

COMMIT;

SELECT d.display_name, d.status, s.event_type, s.is_enabled
FROM line_group_destinations d
JOIN line_group_event_subscriptions s ON s.destination_id = d.id
WHERE s.event_type LIKE 'MeetingRoom.%' OR s.event_type LIKE 'Repair.%'
ORDER BY d.display_name, s.event_type;
