# Dashboard Widgets

Dashboard widgets are reusable UI blocks.

## Widget Types

| Widget Type | Purpose |
|---|---|
| Metric Widget | Shows a number and optional link |
| Status Widget | Shows health/status text |
| Trend Widget | Shows simple metric bars |
| Placeholder Widget | Reserves a future-ready area with Thai empty state |
| Summary Widget | Uses existing reusable components such as leave summary |

## Current Widgets

| Widget | Used For |
|---|---|
| Welcome | Role-specific greeting |
| Leave Balance | My remaining leave days |
| My Leave Requests | Personal leave request summary |
| Pending Approval | Current user's pending approval queue |
| Pending Approval Overview | Admin/SuperAdmin operational count of leave requests still pending |
| Team Calendar | Link to leave calendar |
| Leave Statistics | Today/week/month leave metrics |
| Audit Log | Today's audit events |
| Notification Queue | Unread notification count |
| Holiday Management | Active holidays in current year |
| System Health | API and database status |
| Security Events | Failed login and permission denied |
| LINE Delivery | LINE failed/queued metrics |
| Version Information | Application version |

## Permission Behavior

Widgets can be present in a role layout without exposing unrestricted data. The backend summary endpoint calculates sensitive metrics only for roles/permissions that should see them. Admin and SuperAdmin use an overview widget, not the personal "งานรออนุมัติของฉัน" queue.

## UI Standard

- Earth tone theme
- Gold accent top border
- White card surface
- Skeleton loading
- Thai empty state
- Responsive grid

## Future Drag and Drop

The current layout uses widget IDs. A future user preference table can store:

```text
userId
role
widgetId
x
y
w
h
isVisible
```

This can be added without changing widget implementation.
