# Leave Reports

## APIs

```text
GET /api/reports/leaves
GET /api/reports/leaves/export-excel
GET /api/reports/leaves/export-pdf
```

## Permissions

- View: `ReportManagement.View`
- Export: `ReportManagement.Export`

## Filters

- `from`
- `to`
- `departmentId`
- `leaveTypeId`

## Reports

- Leave requests by date range
- Leave requests by department
- Leave balances
- Pending approval count

## Export Hardening

- Excel export returns HTML-compatible `.xls` and encodes all user-controlled text.
- Values starting with `=`, `+`, `-`, or `@` are prefixed before export to reduce spreadsheet formula injection risk.
- PDF export paginates leave request rows instead of truncating the report.

## Frontend

Route:

```text
/reports/leaves
```

## Audit Events

- `LeaveReport.ExportExcel`
- `LeaveReport.ExportPdf`
