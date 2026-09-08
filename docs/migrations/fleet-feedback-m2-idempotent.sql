START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818071449_AddFleetTripFeedbackM2') THEN
    CREATE TABLE fleet_trip_feedbacks (
        id uuid NOT NULL,
        trip_id uuid NOT NULL,
        fleet_request_id uuid NOT NULL,
        vehicle_assignment_id uuid NOT NULL,
        vehicle_id uuid NOT NULL,
        driver_user_id uuid NOT NULL,
        submitted_by_user_id uuid NOT NULL,
        punctuality_rating integer NOT NULL,
        safety_rating integer NOT NULL,
        service_rating integer NOT NULL,
        overall_rating integer NOT NULL,
        vehicle_condition_rating integer NOT NULL,
        vehicle_cleanliness_rating integer NOT NULL,
        has_incident boolean NOT NULL,
        incident_category character varying(40),
        comment character varying(2000),
        submitted_at timestamp with time zone NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        updated_by_user_id uuid,
        concurrency_token uuid NOT NULL,
        CONSTRAINT "PK_fleet_trip_feedbacks" PRIMARY KEY (id),
        CONSTRAINT ck_fleet_trip_feedback_incident CHECK ((has_incident = FALSE AND incident_category IS NULL) OR (has_incident = TRUE AND incident_category IS NOT NULL AND comment IS NOT NULL)),
        CONSTRAINT ck_fleet_trip_feedback_incident_category CHECK (incident_category IS NULL OR incident_category IN ('DRIVING','PUNCTUALITY','SERVICE','VEHICLE_CONDITION','CLEANLINESS','OTHER')),
        CONSTRAINT ck_fleet_trip_feedback_ratings CHECK (punctuality_rating BETWEEN 1 AND 5 AND safety_rating BETWEEN 1 AND 5 AND service_rating BETWEEN 1 AND 5 AND overall_rating BETWEEN 1 AND 5 AND vehicle_condition_rating BETWEEN 1 AND 5 AND vehicle_cleanliness_rating BETWEEN 1 AND 5),
        CONSTRAINT "FK_fleet_trip_feedbacks_fleet_assignments_vehicle_assignment_id" FOREIGN KEY (vehicle_assignment_id) REFERENCES fleet_assignments (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_trip_feedbacks_fleet_requests_fleet_request_id" FOREIGN KEY (fleet_request_id) REFERENCES fleet_requests (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_trip_feedbacks_fleet_trip_records_trip_id" FOREIGN KEY (trip_id) REFERENCES fleet_trip_records (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_trip_feedbacks_fleet_vehicles_vehicle_id" FOREIGN KEY (vehicle_id) REFERENCES fleet_vehicles (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_trip_feedbacks_users_driver_user_id" FOREIGN KEY (driver_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_trip_feedbacks_users_submitted_by_user_id" FOREIGN KEY (submitted_by_user_id) REFERENCES users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_fleet_trip_feedbacks_users_updated_by_user_id" FOREIGN KEY (updated_by_user_id) REFERENCES users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818071449_AddFleetTripFeedbackM2') THEN
    CREATE INDEX "IX_fleet_trip_feedbacks_driver_user_id_submitted_at" ON fleet_trip_feedbacks (driver_user_id, submitted_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818071449_AddFleetTripFeedbackM2') THEN
    CREATE INDEX "IX_fleet_trip_feedbacks_fleet_request_id" ON fleet_trip_feedbacks (fleet_request_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818071449_AddFleetTripFeedbackM2') THEN
    CREATE INDEX "IX_fleet_trip_feedbacks_submitted_by_user_id_submitted_at" ON fleet_trip_feedbacks (submitted_by_user_id, submitted_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818071449_AddFleetTripFeedbackM2') THEN
    CREATE UNIQUE INDEX "IX_fleet_trip_feedbacks_trip_id_submitted_by_user_id" ON fleet_trip_feedbacks (trip_id, submitted_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818071449_AddFleetTripFeedbackM2') THEN
    CREATE INDEX "IX_fleet_trip_feedbacks_updated_by_user_id" ON fleet_trip_feedbacks (updated_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818071449_AddFleetTripFeedbackM2') THEN
    CREATE INDEX "IX_fleet_trip_feedbacks_vehicle_assignment_id" ON fleet_trip_feedbacks (vehicle_assignment_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818071449_AddFleetTripFeedbackM2') THEN
    CREATE INDEX "IX_fleet_trip_feedbacks_vehicle_id_submitted_at" ON fleet_trip_feedbacks (vehicle_id, submitted_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818071449_AddFleetTripFeedbackM2') THEN
    INSERT INTO role_permissions (role_id, permission_id)
    SELECT r.id, p.id
    FROM roles r
    CROSS JOIN permissions p
    WHERE r.is_active = TRUE
      AND p.is_active = TRUE
      AND p.code IN ('FleetFeedback.Create', 'FleetFeedback.ViewOwn')
    ON CONFLICT (role_id, permission_id) DO NOTHING;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818071449_AddFleetTripFeedbackM2') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260818071449_AddFleetTripFeedbackM2', '9.0.15');
    END IF;
END $EF$;
COMMIT;

