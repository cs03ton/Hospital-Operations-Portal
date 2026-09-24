-- DBeaver: run the whole file with Stop on error. No LINE message is sent by this SQL.
-- If this connection previously failed inside a transaction, run ROLLBACK first.
-- Reuses the existing group named กลุ่มงานยานพาหนะ; does not create a new destination.
BEGIN;
LOCK TABLE line_group_destinations, line_group_event_subscriptions IN SHARE ROW EXCLUSIVE MODE;

DO $$
DECLARE
    v_group_id uuid;
    v_status text;
    v_confirmed_at timestamptz;
    v_other_groups integer;
BEGIN
    IF (SELECT count(*) FROM line_group_destinations WHERE display_name = 'กลุ่มงานยานพาหนะ') <> 1 THEN
        RAISE EXCEPTION 'Expected exactly one กลุ่มงานยานพาหนะ group; no changes made';
    END IF;

    SELECT id, status, confirmed_at INTO v_group_id, v_status, v_confirmed_at
    FROM line_group_destinations
    WHERE display_name = 'กลุ่มงานยานพาหนะ' AND module IN ('FLEET', 'CENTRAL')
    FOR UPDATE;

    IF v_group_id IS NULL OR v_status <> 'Active' OR v_confirmed_at IS NULL THEN
        RAISE EXCEPTION 'Fleet group is not an active confirmed FLEET/CENTRAL destination; no changes made';
    END IF;

    SELECT count(DISTINCT d.id) INTO v_other_groups
    FROM line_group_destinations d
    JOIN line_group_event_subscriptions s ON s.destination_id = d.id
    WHERE d.id <> v_group_id AND d.status = 'Active' AND d.confirmed_at IS NOT NULL
      AND d.module NOT LIKE 'REPAIR_%' AND s.event_type LIKE 'Fleet.%' AND s.is_enabled;
    IF v_other_groups > 0 THEN
        RAISE EXCEPTION '% other active groups subscribe to Fleet events; inspect them before migration', v_other_groups;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM roles WHERE name = 'Admin' AND is_active)
       OR NOT EXISTS (SELECT 1 FROM roles WHERE name = 'SuperAdmin' AND is_active) THEN
        RAISE EXCEPTION 'Active Admin/SuperAdmin roles are required; no changes made';
    END IF;
END $$;

SELECT d.id, d.display_name, d.module, d.status, d.confirmed_at,
       d.delivery_provider, d.endpoint_url IS NOT NULL AS has_endpoint,
       d.client_secret_protected IS NOT NULL AS has_secret,
       count(s.id) FILTER (WHERE s.event_type LIKE 'Fleet.%' AND s.is_enabled) AS enabled_fleet_events
FROM line_group_destinations d
LEFT JOIN line_group_event_subscriptions s ON s.destination_id = d.id
WHERE d.display_name = 'กลุ่มงานยานพาหนะ'
GROUP BY d.id;

INSERT INTO permissions (id, code, name, group_name, action, is_active, created_at)
VALUES
  (gen_random_uuid(), 'LineGroup.View', 'ดูกลุ่มแจ้งเตือนส่วนกลาง', 'LineGroup', 'View', true, now()),
  (gen_random_uuid(), 'LineGroup.Manage', 'จัดการกลุ่มและเหตุการณ์แจ้งเตือนส่วนกลาง', 'LineGroup', 'Manage', true, now())
ON CONFLICT (code) DO UPDATE SET is_active = true;

INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id FROM roles r CROSS JOIN permissions p
WHERE r.name IN ('Admin', 'SuperAdmin') AND r.is_active
  AND p.code IN ('LineGroup.View', 'LineGroup.Manage') AND p.is_active
ON CONFLICT DO NOTHING;

UPDATE line_group_destinations
SET module = 'CENTRAL', concurrency_token = gen_random_uuid()
WHERE display_name = 'กลุ่มงานยานพาหนะ' AND module = 'FLEET';

INSERT INTO line_group_event_subscriptions (id, destination_id, event_type, is_enabled, created_at)
SELECT gen_random_uuid(), d.id, e.event_type, true, now()
FROM line_group_destinations d
CROSS JOIN (VALUES
  ('Fleet.RequestSubmitted'), ('Fleet.AssignmentCreated'), ('Fleet.AdminReviewed'),
  ('Fleet.Returned'), ('Fleet.DirectorApproved'), ('Fleet.Rejected'),
  ('Fleet.Cancelled'), ('Fleet.AssignmentChanged'), ('Fleet.DriverAcknowledged'),
  ('Fleet.TripCompleted'), ('Fleet.TripOverdue')
) AS e(event_type)
WHERE d.display_name = 'กลุ่มงานยานพาหนะ' AND d.module = 'CENTRAL'
ON CONFLICT (destination_id, event_type) DO NOTHING;

DO $$
BEGIN
    IF (SELECT count(*) FROM line_group_destinations
        WHERE display_name = 'กลุ่มงานยานพาหนะ' AND module = 'CENTRAL' AND status = 'Active' AND confirmed_at IS NOT NULL) <> 1 THEN
        RAISE EXCEPTION 'Fleet central destination postcheck failed';
    END IF;
    IF NOT EXISTS (
        SELECT 1 FROM line_group_destinations d
        JOIN line_group_event_subscriptions s ON s.destination_id = d.id
        WHERE d.display_name = 'กลุ่มงานยานพาหนะ'
          AND s.event_type LIKE 'Fleet.%' AND s.is_enabled
    ) THEN
        RAISE EXCEPTION 'No enabled Fleet event subscription on the central destination';
    END IF;
END $$;

COMMIT;

SELECT d.id, d.display_name, d.module, d.status, s.event_type, s.is_enabled
FROM line_group_destinations d
JOIN line_group_event_subscriptions s ON s.destination_id = d.id
WHERE d.display_name = 'กลุ่มงานยานพาหนะ' AND s.event_type LIKE 'Fleet.%'
ORDER BY s.event_type;
