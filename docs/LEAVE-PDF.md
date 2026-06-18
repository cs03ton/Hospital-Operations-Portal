# Leave PDF Export

## Endpoint

```text
GET /api/leave-requests/{id}/pdf
```

## Behavior

- Requires `LeaveManagement.View`.
- Uses the same access rule as leave request detail:
  - request owner can download
  - users with `LeaveManagement.Approve` can access relevant approval data
- Generates an A4 PDF.
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
