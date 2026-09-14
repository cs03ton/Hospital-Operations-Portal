# Repair Management v1

## Deployment

Local preparation only; do not deploy or seed user assignments automatically.
Back up PRD before applying migration 20260911074410_AddRepairManagement.
Run deploy/sql/06-prd-repair-schema.sql then 07-prd-repair-master-data.sql as whole scripts in DBeaver with Stop on error.
Schema SQL expects migration 20260909090000_CorrectFleetRolePermissions already applied. Never fake migration history.
On SQL failure ROLLBACK the connection. Master data is additive and does not reactivate disabled roles/permissions or overwrite category mappings.
Deploy backend and frontend together after QA, then assign the two technician roles to approved accounts manually.

New configuration (backend only):

```dotenv
Repairs__NotificationsEnabled=false
Repairs__AllowedNotificationHosts__0=REPLACE_WITH_APPROVED_MORPROM_HOST
```

Use the existing Storage:RootPath, FileScan, Data Protection key persistence and LINE PublicAppUrl configuration.
Enable Repairs__NotificationsEnabled after the schema and both destinations are configured.
Apply the repair-image location from the matching Nginx template and run `nginx -t` before reloading.
Only `/api/repairs/{uuid}/images` accepts a 51 MiB multipart request; other routes retain the 20 MiB proxy limit.
The API limits each image to 10 MiB and each batch to five, independently of the proxy.
Keep each group's secret in protected settings, never SQL or source control. Endpoints must be HTTPS and match the host allowlist; use trusted provider hosts only.
The repair sender reuses the existing message client with a separate HTTP transport: 15-second timeout and redirects disabled. Fleet transport/subscriptions are unchanged.
Production seeding remains off. Restart does not create roles, people, or bindings.

## API

All routes require authentication and Repair permissions. Own/team/all filtering is enforced server-side.
GET /api/repairs/options, /summary, /?scope=mine|team&page=1&pageSize=20&status=&search=
GET /api/repairs/{id} includes allowed actions, events, rounds, waiting intervals, contributors and image identifiers.
POST /api/repairs creates a request and queues one dispatch event atomically.
POST /api/repairs/{id}/actions/{action} requires concurrencyToken and note where relevant.
Actions: start, wait, resume, return, priority, note, solve, accept, reject-solution, reopen, resubmit, cancel.
Solve requires solverId and optional contributorIds from active team members. Resubmit requires request fields.
GET /api/repairs/{id}/solvers lists active eligible team members for technicians only.
POST /api/repairs/{id}/images is multipart files + concurrencyToken; GET /api/repairs/images/{imageId} is protected.
Settings API: GET /api/repairs/settings, POST /settings/categories, PUT /settings/groups/{IT|GENERAL}.
Stale tokens return 409. Disallowed state transitions return 403. Out-of-scope detail and images return 404.

## Database / Delivery

repair_teams, repair_categories, repair_requests, repair_rounds, repair_events, repair_contributors,
repair_waiting_periods, repair_images and repair_dispatches are new additive tables.
Requests retain their team snapshot. Round numbers and request numbers have unique indexes.
State, events, Bell messages and dispatch entries are saved in one EF transaction.
Repair dispatches are separate from Fleet outbox processing. Destinations use REPAIR_IT / REPAIR_GENERAL modules.
Unique event/team keys and optimistic claims prevent concurrent worker duplication.
Custom endpoints do not promise exactly-once delivery: network ambiguity is marked Attention, and a crashed Sending job is left for operator review rather than automatically sent twice.
Known transient HTTP/application errors retry at most three delivery attempts. Missing configuration waits without consuming attempts.
Queued events for a former team are marked Superseded after a requester resubmits to another team.
No patient data, symptoms, location, names or photos are included in group messages.

## Acceptance

See [local verification results](qa/repair-v1-verification.md) for executed tests and remaining pilot checks.

Verify both team scopes, dual roles, active permissions, return/resubmit across categories, waiting intervals,
failed acceptance, repeated reopen, contributor attribution, stale writes, image access/limits and delivery retries.
Verify 375/430/768/1366/1920 widths and mobile dialogs. No SLA scores or asset/stock/purchasing/PM in v1.
Rolling back the schema drops repair history; restore from backup instead of running Down on a populated production database.
