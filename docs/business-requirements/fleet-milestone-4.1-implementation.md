# Fleet Phase 2.0 Milestone 4.1

Milestone 4.1 adds KPI reporting, vehicle maintenance, and a read-only operational calendar. All Fleet endpoints remain protected by backend permissions and the Milestone 4.0 rollout gate. No Fleet permission is assigned to a production role by this change.

## KPI definitions

All dates are interpreted in `Asia/Bangkok`, converted to UTC, and queried as `[start,end)`. Thai fiscal year starts 1 October. Rates return zero when their denominator is zero.

| KPI | Numerator | Denominator |
| --- | --- | --- |
| Approval | completed requests | completed + rejected |
| Rejection | rejected requests | completed + rejected |
| Cancellation | cancelled requests | all requests |
| Trip completion | completed requests | assignments |
| Driver acceptance | driver accepts | driver accepts + declines |
| Notification success | sent deliveries per channel | all deliveries per channel |

Vehicle utilization is intended as non-overlapping trip hours divided by available hours after unavailability and blocking maintenance. Current API exposes all utilization fields; further production calibration against operational data is listed as a remaining risk.

## CSV security

Exports require `FleetReport.Export`, enforce configured range/row limits, write an audit record containing only report type and filters, emit UTF-8 with BOM, quote every field, and prefix values beginning with `=`, `+`, `-`, `@`, tab, or carriage return. License and document numbers are excluded or masked.

## Maintenance administration

Schedules support date, mileage, either-condition, one-time, and recurring plans. Lifecycle is `ACTIVE → IN_PROGRESS → COMPLETED`; cancellation preserves history. Completion validates concurrency, non-negative cost, and vehicle mileage. A lower mileage requires an explicit override reason and privileged operation. Recurrence creates one successor during completion.

Maintenance attachments are scanned and content-validated before storage under `fleet/maintenance/yyyy/MM/{recordId}`; only PDF/JPG/JPEG/PNG metadata is stored in PostgreSQL. Deletion is metadata soft-delete.

Availability computes maintenance and required-document safety blocks directly. It does not create duplicate vehicle-unavailability rows. In-progress work always blocks; overdue types block when configured; expired required documents block when configured. Emergency workflow may not bypass this safety decision in Milestone 4.2.

## Notifications and operations

Maintenance lifecycle events use the durable Fleet outbox. Recipients are resolved centrally from Fleet maintenance/dispatcher permissions and the vehicle responsible user. Delivery idempotency remains enforced by the unique delivery idempotency key.

Operators should alert on failed outbox messages and overdue maintenance. Disable Fleet using the Milestone 4.0 rollout control before incident mitigation. Do not delete maintenance history.

## Calendar

`GET /api/fleet/calendar` requires an explicit bounded range and returns normalized, non-sensitive DTOs. Requests, maintenance due dates, and document expiry are projected in the database. Document expiry is all-day in Bangkok. The calendar is presentation only and is never an availability source of truth.

## Permissions and roles

Added definitions: `FleetMaintenance.View`, `Manage`, `Complete`, `Cancel`, `OverrideMileage`, `ManageTypes`, `ManageDocuments`, `UploadAttachment`, and `FleetCalendar.View`. Dashboard uses `FleetDashboard.View`, `FleetReport.View`, and `FleetReport.Export`. Production role mappings remain empty.

## Migration and rollback

The migration `AddFleetKpiMaintenanceCalendar` is additive: five new tables, foreign keys, checks, and indexes; no Leave object is changed. Generate the deployment artifact with `dotnet ef migrations script --idempotent`. Apply only after backup and Fleet-disabled verification. For rollback, disable Fleet first and roll application code back. Do not run the destructive down migration in production when maintenance data exists; preserve/export data and use a forward corrective migration.

## UAT checklist

- Verify Bangkok date boundaries and Thai fiscal year.
- Export Thai CSV and formula-like values in Excel.
- Create, edit, start, complete, cancel, and concurrently edit maintenance.
- Verify overdue/expired-document availability blocks.
- Verify attachment rejection and scanning.
- Verify In-App/LINE deliveries and retry behavior.
- Verify calendar month/agenda, filters, links, and mobile layout.
- Verify rollout disabled users and unassigned production roles still have no Fleet access.

## Deferred to Milestone 4.2

Compatibility Matrix, Emergency Transport Workflow, and Mobile Driver UX are not implemented and no partial schema for them is included.
