# Selected announcement LINE groups

Apply `20261022083439_AddAnnouncementLineGroups.sql` before deploying the updated Backend. The database must already have migrations through `20261014015735_AddFleetReferPatientName`. Run against the intended database using your normal backup and deployment procedure. This development task does not apply the SQL to production.

The script adds `announcements.notify_via_line_group` (default `false`) and `announcement_line_group_targets`. It does not select recipients, enqueue messages, or change group event subscriptions. Completed runs are guarded by EF migration history.

Verify after applying:

```sql
SELECT "MigrationId" FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20261022083439_AddAnnouncementLineGroups';

SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_schema = 'public' AND table_name = 'announcements'
  AND column_name = 'notify_via_line_group';

SELECT to_regclass('public.announcement_line_group_targets');
```

Editors need the existing `Announcement.Notification.SendLine` permission. Only active central `CUSTOM_ENDPOINT` destinations are offered; legacy repair groups are excluded. Group deliveries use the existing central LINE worker and its enable/retry configuration. Select groups in an announcement, save, preview the recipients, then publish. Saving edits alone does not send; republishing queues only destinations without a previous queue entry for that announcement.

The PostgreSQL concurrency/migration integration test requires an isolated test database via `HOP_E2E_CONNECTION_STRING` (database name must contain `qa`, `test`, `ci`, or `uat`). It recreates that database; never point it at an operational database.
