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
- Uses QuestPDF generated form layout with section/table/grid structure.
- Falls back to built-in A4 generated layout if template config is missing.
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

Current standard template:

```text
Universal Leave Form v1.0
Document number: LV-{YYYYMM}-{RunningNo}
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
- position from user profile
- department
- phone number
- email from user profile
- leave contact address
- leave type government-style checkbox
- full day / half-day AM / half-day PM checkbox
- start date
- end date
- total days
- working days, official holidays, weekend days
- leave balance before request, used this request, pending days, balance after approval
- reason
- attachment checkbox summary and attachment count
- submitted date
- approval steps
- approval status
- approval action date
- approval remark
- head approver comment
- director approver comment
- final approval result
- generated at and application version

## Configuration

The PDF reads values from backend configuration:

```text
Hospital__Name
Branding__LogoPath
Application__Version
LeavePdf__TemplateConfigPath
LeavePdf__FontPath
LeavePdf__FontFamily
LeavePdf__FontSize
LeavePdf__LineHeight
```

If `LeavePdf__TemplateConfigPath` is not set, the default template path is used.

Thai font troubleshooting:

```text
docs/LEAVE-PDF-THAI-FONT.md
```

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
