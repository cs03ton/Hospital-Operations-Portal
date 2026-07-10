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

## Extending Dashboard

To add a widget:

1. Add a widget definition to `widgetRegistry`.
2. Add the widget ID to the target role layout.
3. Add backend metric fields only when the widget needs server data.

No new module should be exposed from dashboard unless Phase scope allows it.
