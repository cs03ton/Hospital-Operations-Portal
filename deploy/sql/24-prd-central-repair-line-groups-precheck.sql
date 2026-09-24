-- Read-only precheck for the central repair notification cutover.
-- Run after migration 20260924150000_AddCentralLineGroupRepairTeam.
-- Set repair_team_code via the central LINE Groups page; do not infer it from names.
SELECT d.id, d.display_name, d.module, d.status, d.repair_team_code,
       d.delivery_provider, d.confirmed_at, d.repair_team_assigned_at,
       count(s.id) FILTER (WHERE s.event_type LIKE 'Repair.%' AND s.is_enabled) AS enabled_repair_events,
       CASE WHEN d.status = 'Active' AND d.confirmed_at IS NOT NULL
                 AND d.repair_team_code IN ('IT', 'GENERAL') AND d.repair_team_assigned_at IS NOT NULL
                 AND count(s.id) FILTER (WHERE s.event_type LIKE 'Repair.%' AND s.is_enabled) > 0
            THEN 'READY' ELSE 'NOT_READY' END AS repair_delivery_readiness
FROM line_group_destinations d
LEFT JOIN line_group_event_subscriptions s ON s.destination_id = d.id
WHERE d.module NOT LIKE 'REPAIR_%'
GROUP BY d.id, d.display_name, d.module, d.status, d.repair_team_code,
         d.delivery_provider, d.confirmed_at, d.repair_team_assigned_at
ORDER BY d.display_name;

SELECT team_code, status, count(*) AS legacy_dispatch_count
FROM repair_dispatches
GROUP BY team_code, status
ORDER BY team_code, status;
