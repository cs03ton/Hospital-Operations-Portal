# Executive Dashboard

Phase 1.5 Executive Dashboard provides a management-level overview for Hospital Operations Portal.

## Route

```text
/dashboard/executive
```

## Backend API

```http
GET /api/dashboard/executive
```

Required permission:

- `Dashboard.Executive.View`
- or `LeaveDashboard.ViewExecutiveSummary`

Default allowed roles from development seed:

- Director
- Admin
- SuperAdmin

Staff and Department Head do not receive this permission by default.

## KPI Definitions

| KPI | Calculation |
|---|---|
| บุคลากรทั้งหมด | active users only |
| มาปฏิบัติงานวันนี้ | active users - unique users on approved leave today |
| ลาวันนี้ | unique users with approved leave covering today |
| รออนุมัติ | leave requests with status `Pending` |
| คิวผู้อำนวยการ | pending approval rows assigned to the current user |
| อนุมัติวันนี้ | leave requests with status `Approved` and updated today |
| ไม่อนุมัติวันนี้ | leave requests with status `Rejected` and updated today |
| Leave Rate | onLeaveToday / totalActiveUsers * 100 |
| Approval SLA | average hours from submittedAt to final updatedAt for approved/rejected requests in the last 90 days |

## Today Summary

The summary section shows:

- total unique staff on leave today
- sick leave today
- personal leave today
- vacation leave today
- pending approvals
- approved today
- rejected today
- top department by unique staff on leave today

Cancelled and rejected leave requests are not counted as on-leave.

## Analytics

Monthly Leave Trend:

- last 12 months
- approved requests only
- core leave types: sick, personal, vacation
- uses `totalDays`, so half-day leave contributes `0.5`

Leave By Department:

- current month
- approved requests only
- top 10 departments

Leave By Type:

- current month
- core leave types first

Yearly Summary:

- current fiscal year
- fiscal year starts 1 October and ends 30 September
- frontend displays fiscal year as Buddhist Era

## System Health

Executive Dashboard includes safe health status for:

- API
- Database
- Storage
- LINE
- Disk
- Backup
- Version
- Environment

The API must not return tokens, secrets, connection strings, or stack traces.

## Limitations

- Monthly trend groups leave by request start month.
- Approval SLA depends on `submittedAt` and `updatedAt` being populated.
- Backup status depends on configured backup folder visibility from the API process.
