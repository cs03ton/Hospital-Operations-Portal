# Dashboard Metrics

Dashboard summary endpoint:

```text
GET /api/dashboard/summary
```

## Metrics

- `totalUsers`
- `totalDepartments`
- `pendingApprovals`
- `staffOnLeaveToday`
- `staffOnLeaveThisWeek`
- `staffOnLeaveThisMonth`
- `myCoreLeaveBalances`
- `myPendingRequests`
- `departmentRequests`

## Data Sources

- `leave_requests`
- `leave_balances`
- `leave_types`
- `users`
- `departments`

## Rules

- Pending approvals count only requests assigned to the current user as current approver.
- Staff on leave counts distinct users with approved leave overlapping the selected period.
- Leave balance is not shown as one combined total because each leave type cannot be used interchangeably.
- `myCoreLeaveBalances` returns core leave type balances separately: vacation, personal leave, and sick leave.
- `myPendingRequests` returns the current user's own pending leave requests.
- `departmentRequests` returns same-department leave requests for users with department visibility and excludes the current user's own requests.
- Missing data returns `0`.
- Phase 1 frontend displays role-based dashboard cards.

## Department Head Grouped Requests

| Field | Rule |
|---|---|
| `myPendingRequests` | `UserId = currentUserId` and `Status = Pending` |
| `departmentRequests` | Same department, exclude current user, include `Pending`, `ReturnedForRevision`, `Approved`, `Rejected`, `Cancelled` |

Dashboard navigation uses:

```text
/leave?scope=mine&status=pending
/leave?scope=department
```

The list endpoint enforces the scope server-side.
