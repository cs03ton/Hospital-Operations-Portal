START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818074425_AddFleetFeedbackManagementM3') THEN
    INSERT INTO role_permissions (role_id, permission_id)
    SELECT r.id, p.id
    FROM roles r
    CROSS JOIN permissions p
    WHERE r.is_active = TRUE
      AND r.name IN ('Admin', 'SuperAdmin', 'Director')
      AND p.code = 'FleetFeedback.ViewManagement'
      AND p.is_active = TRUE
    ON CONFLICT (role_id, permission_id) DO NOTHING;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818074425_AddFleetFeedbackManagementM3') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260818074425_AddFleetFeedbackManagementM3', '9.0.15');
    END IF;
END $EF$;
COMMIT;

