-- Selected announcement LINE groups. Apply before deploying the new Backend.
-- Requires migrations through 20261014015735_AddFleetReferPatientName.
-- Existing announcements remain opted out. No messages are queued by this script.
-- Migration history guards make a completed rerun safe.
START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022083439_AddAnnouncementLineGroups') THEN
    ALTER TABLE announcements ADD notify_via_line_group boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022083439_AddAnnouncementLineGroups') THEN
    CREATE TABLE announcement_line_group_targets (
        announcement_id uuid NOT NULL,
        destination_id uuid NOT NULL,
        CONSTRAINT "PK_announcement_line_group_targets" PRIMARY KEY (announcement_id, destination_id),
        CONSTRAINT "FK_announcement_line_group_targets_announcements_announcement_~" FOREIGN KEY (announcement_id) REFERENCES announcements (id) ON DELETE CASCADE,
        CONSTRAINT "FK_announcement_line_group_targets_line_group_destinations_des~" FOREIGN KEY (destination_id) REFERENCES line_group_destinations (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022083439_AddAnnouncementLineGroups') THEN
    CREATE INDEX "IX_announcement_line_group_targets_destination_id" ON announcement_line_group_targets (destination_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022083439_AddAnnouncementLineGroups') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261022083439_AddAnnouncementLineGroups', '9.0.15');
    END IF;
END $EF$;
COMMIT;

