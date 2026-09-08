START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818062212_AddFleetTripParticipantFeedbackM1') THEN
    CREATE TABLE fleet_trip_participants (
        id uuid NOT NULL,
        trip_id uuid NOT NULL,
        user_id uuid,
        is_requester boolean NOT NULL,
        participant_type character varying(20) NOT NULL,
        is_actual_participant boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        created_by_user_id uuid NOT NULL,
        CONSTRAINT "PK_fleet_trip_participants" PRIMARY KEY (id),
        CONSTRAINT ck_fleet_trip_participants_type CHECK (participant_type IN ('EMPLOYEE','EXTERNAL')),
        CONSTRAINT "FK_fleet_trip_participants_fleet_trip_records_trip_id" FOREIGN KEY (trip_id) REFERENCES fleet_trip_records (id) ON DELETE CASCADE,
        CONSTRAINT "FK_fleet_trip_participants_users_created_by_user_id" FOREIGN KEY (created_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_trip_participants_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818062212_AddFleetTripParticipantFeedbackM1') THEN
    CREATE INDEX "IX_fleet_trip_participants_created_by_user_id" ON fleet_trip_participants (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818062212_AddFleetTripParticipantFeedbackM1') THEN
    CREATE UNIQUE INDEX "IX_fleet_trip_participants_trip_id_user_id" ON fleet_trip_participants (trip_id, user_id) WHERE user_id IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818062212_AddFleetTripParticipantFeedbackM1') THEN
    CREATE INDEX "IX_fleet_trip_participants_user_id_is_actual_participant" ON fleet_trip_participants (user_id, is_actual_participant);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818062212_AddFleetTripParticipantFeedbackM1') THEN
    INSERT INTO permissions (id, code, name, group_name, action, is_active, created_at)
    VALUES
      ('019fd9a0-0000-7000-8000-000000000001', 'FleetFeedback.Create', 'ส่ง Feedback การเดินทาง', 'FleetFeedback', 'Create', TRUE, NOW()),
      ('019fd9a0-0000-7000-8000-000000000002', 'FleetFeedback.ViewOwn', 'ดู Feedback การเดินทางของตนเอง', 'FleetFeedback', 'ViewOwn', TRUE, NOW()),
      ('019fd9a0-0000-7000-8000-000000000003', 'FleetFeedback.ViewManagement', 'ดูรายงาน Feedback สำหรับผู้บริหาร', 'FleetFeedback', 'ViewManagement', TRUE, NOW()),
      ('019fd9a0-0000-7000-8000-000000000004', 'FleetFeedback.ViewIdentity', 'ดูตัวตนผู้ให้ Feedback', 'FleetFeedback', 'ViewIdentity', TRUE, NOW()),
      ('019fd9a0-0000-7000-8000-000000000005', 'FleetFeedback.Manage', 'จัดการ Feedback การเดินทาง', 'FleetFeedback', 'Manage', TRUE, NOW())
    ON CONFLICT (code) DO NOTHING;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818062212_AddFleetTripParticipantFeedbackM1') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260818062212_AddFleetTripParticipantFeedbackM1', '9.0.15');
    END IF;
END $EF$;
COMMIT;

