# Leave PDF Export

## Endpoint

```text
GET /api/leave-requests/{id}/pdf
```

## Behavior

- Requires one of:
  - `LeaveRequest.ViewOwn`
  - `LeaveRequest.ViewPendingApproval`
  - `LeaveRequest.ViewDepartment`
  - `LeaveRequest.ViewAll`
- Uses the same access rule as leave request detail:
  - request owner can download
- approver can download only when visibility rules allow access
- admin/support users can download by permission.
- Generates an A4 PDF from leave form template mapping when available.
- Falls back to built-in A4 layout if template config is missing.
- Uses real leave request data.
- Shows hospital name from backend configuration:

```text
Hospital__Name
```

- Loads the hospital logo from:

```text
assets/logo/hospital-logo.png
frontend/src/assets/logo/hospital-logo.png
```

## Template

Template config is read from:

```text
storage/templates/leave/leave_form_template.json
```

Optional override:

```text
LeavePdf__TemplateConfigPath=/absolute/path/to/leave_form_template.json
```

See:

```text
docs/LEAVE-FORM-TEMPLATE.md
```

## Field Mapping

Supported fields include:

- request number
- requester name
- employee code
- position / role
- department
- phone number
- leave contact address
- leave type
- start date
- end date
- total days
- half-day duration type
- reason
- submitted date
- approval steps
- approval status
- approval action date
- approval remark

## Audit Event

```text
LeaveRequest.PdfGenerated
```

## Test

```powershell
curl.exe -L "http://localhost:5000/api/leave-requests/<leave-request-id>/pdf" `
  -H "Authorization: Bearer <token>" `
  -o leave-request.pdf
```
