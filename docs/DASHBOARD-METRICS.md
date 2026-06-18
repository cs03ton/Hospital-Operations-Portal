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
- `myRemainingLeaveDays`

## Data Sources

- `leave_requests`
- `leave_balances`

## Rules

- Pending approvals count only requests assigned to the current user as current approver.
- Staff on leave counts distinct users with approved leave overlapping the selected period.
- Remaining leave days sums current-year leave balances for the current user.
- Missing data returns `0`.
- Phase 1 frontend displays only leave-related dashboard cards.
