# Dashboard Architecture

Hospital Operations Portal uses a role-based dashboard architecture.

## Principles

- Dashboard is not one fixed screen for every user.
- Dashboard page is a thin container.
- Widgets are reusable.
- Role layouts are arrays of widget IDs.
- Future drag-and-drop can persist widget IDs and grid positions without rewriting widget logic.

## Frontend Structure

```text
DashboardPage
  -> RoleBasedDashboard
      -> roleLayouts
      -> widgetRegistry
      -> DashboardPanel
      -> metric/status/trend/placeholder widgets
```

Main files:

- `frontend/src/pages/DashboardPage.tsx`
- `frontend/src/pages/ExecutiveDashboardPage.tsx`
- `frontend/src/pages/LeaveAnalyticsPage.tsx`
- `frontend/src/components/dashboard/RoleBasedDashboard.tsx`
- `frontend/src/config/dashboardModules.ts`

## Backend Enforcement

Dashboard API still requires backend permission:

```text
Dashboard.View
```

Executive Dashboard uses a separate backend endpoint and permission:

```text
GET /api/dashboard/executive
Dashboard.Executive.View
LeaveDashboard.ViewExecutiveSummary
```

The frontend role layout is UX only. Sensitive data is also gated by the backend summary API:

- Staff receives only personal leave and notification metrics.
- Department Head and Director receive team/approval metrics only when their permissions allow it.
- Admin receives operational administration metrics.
- SuperAdmin receives additional security, queue, database, and health metrics.
- Executive Dashboard is available only to Director, Admin, SuperAdmin, or users explicitly granted the executive dashboard permission.

Unauthorized dashboard fields are returned as safe defaults such as `0` or `Restricted`.

## Role Layout Ordering

Dashboard content is ordered from the user's own work to wider operational or executive views.

| Role | Layout Direction |
|---|---|
| Staff | Personal leave balance, personal leave requests, leave cancellation requests, and calendar |
| Department Head | Personal leave requests first, then team/department work and approval queue |
| Director | Personal leave and cancellation summaries first, then executive comparison/trend widgets, with hospital-wide summary lower on the page |
| Admin/SuperAdmin | Personal/support leave summaries first, then admin control-center, health, LINE, backup, audit, and security widgets |

Leave cancellation requests are displayed as a separate widget from normal leave requests. This prevents cancellation workflow counts and restored days from being mixed into normal leave request KPI.

## Dashboard Hub Modules

`/dashboard` is the dashboard portal. Each module card is configured in `dashboardModules.ts` with:

- module key
- route
- status
- required permissions
- metric label
- order

The active Phase 1.5 executive module routes to `/dashboard/executive` and replaces the previous planned placeholder.

## Phase 1.5 Leave Analytics

Leave Analytics is a report/dashboard hybrid for deep leave analysis:

```text
Route: /reports/leave-analytics
API:   GET /api/reports/leave-analytics
Export: GET /api/reports/leave-analytics/export-excel
Permission: LeaveAnalytics.View or ReportManagement.View
```

It is intentionally separate from `/dashboard/executive` so changes to analytics filters and charts do not affect Executive Dashboard behavior.

## Report vs Analytics Navigation

- `/reports/leaves` is the operational leave report for list review, export, and cancellation report follow-up.
- `/reports/leave-analytics` is the analytical dashboard for trends, department comparison, heatmap, and executive insight.
- Director, Admin, and SuperAdmin can access these views through role policy or explicit permissions. Staff and Department Head require explicit permissions.

## Extending Dashboard

To add a widget:

1. Add a widget definition to `widgetRegistry`.
2. Add the widget ID to the target role layout.
3. Add backend metric fields only when the widget needs server data.

No new module should be exposed from dashboard unless Phase scope allows it.
