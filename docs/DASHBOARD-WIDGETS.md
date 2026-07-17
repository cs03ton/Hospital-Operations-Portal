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
| My Leave Requests | Personal leave request summary with draft, pending, returned-for-revision, approved, rejected, cancelled, latest requests, and approval/rejection metrics |
| Leave Cancellation Summary | Leave cancellation request summary with total, pending, approved, rejected, returned, restored days, latest requests, and approval performance |
| Head Leave Request Groups | Department Head split view for own pending requests and department requests |
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

Admin, SuperAdmin, and Director can still see leave request and leave cancellation summary widgets when the backend grants dashboard/report visibility. These widgets are for tracking and monitoring only. They do not grant leave creation or normal approval privileges by themselves.

Returned-for-revision requests count as the requester's own request total and as the separate “ตีกลับรอแก้ไข” bucket. They do not count as pending approval for approver queues.

## Leave Cancellation Summary

`Leave Cancellation Summary` is displayed separately from normal leave requests so cancellation workflow metrics do not inflate ordinary leave-request KPI.

| Metric | Meaning |
|---|---|
| ทั้งหมด | All leave cancellation requests in the user's permitted scope |
| รออนุมัติ | Cancellation requests waiting for an approval step |
| อนุมัติแล้ว | Cancellation requests approved through all steps |
| ไม่อนุมัติ | Cancellation requests rejected by an approver |
| ตีกลับ | Cancellation requests returned for revision |
| คืนวันลาแล้ว | Total leave days restored after final approval |

The widget should be placed next to or immediately after `My Leave Requests` in role layouts so users read normal leave requests first, then leave cancellation requests.

## Department Head Request Groups

`Head Leave Request Groups` uses backend grouped data from `GET /api/dashboard/summary`.

| Group | Data Rule | Empty State | Navigation |
|---|---|---|---|
| คำขอลาของฉันที่รออนุมัติ | `userId = currentUserId` and `status = Pending` | ไม่มีคำขอลาของคุณที่กำลังรออนุมัติ | `/leave?scope=mine&status=pending` |
| คำขอลาของหน่วยงาน | Same department, exclude current user, statuses `Pending`, `ReturnedForRevision`, `Approved`, `Rejected`, `Cancelled` | ยังไม่มีคำขอลาของหน่วยงาน | `/leave?scope=department` |

The department group must not be built by frontend filtering alone. Backend scope and department visibility are enforced before data is returned.

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
