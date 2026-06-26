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
- `frontend/src/components/dashboard/RoleBasedDashboard.tsx`

## Backend Enforcement

Dashboard API still requires backend permission:

```text
Dashboard.View
```

The frontend role layout is UX only. Sensitive data is also gated by the backend summary API:

- Staff receives only personal leave and notification metrics.
- Department Head and Director receive team/approval metrics only when their permissions allow it.
- Admin receives operational administration metrics.
- SuperAdmin receives additional security, queue, database, and health metrics.

Unauthorized dashboard fields are returned as safe defaults such as `0` or `Restricted`.

## Extending Dashboard

To add a widget:

1. Add a widget definition to `widgetRegistry`.
2. Add the widget ID to the target role layout.
3. Add backend metric fields only when the widget needs server data.

No new module should be exposed from dashboard unless Phase scope allows it.
