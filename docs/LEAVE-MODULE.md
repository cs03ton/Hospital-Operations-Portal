# Leave Module

Phase 2 starts the Leave Management API and Thai frontend workflow.

## Backend APIs

- `GET /api/leave-types`
- `POST /api/leave-types`
- `PUT /api/leave-types/{id}`
- `DELETE /api/leave-types/{id}`
- `GET /api/leave-requests`
- `GET /api/leave-requests/{id}`
- `GET /api/leave-requests/{id}/pdf`
- `POST /api/leave-requests`
- `PUT /api/leave-requests/{id}`
- `POST /api/leave-requests/{id}/submit`
- `POST /api/leave-requests/{id}/cancel`
- `POST /api/leave-requests/{id}/approve`
- `POST /api/leave-requests/{id}/reject`
- `GET /api/leave-requests/{id}/attachments`
- `POST /api/leave-requests/{id}/attachments`
- `GET /api/leave-attachments/{id}/download`
- `DELETE /api/leave-attachments/{id}`
- `GET /api/leave-balances/me`
- `GET /api/leave-balances/user/{userId}`
- `GET /api/approval-chains`
- `POST /api/approval-chains`
- `PUT /api/approval-chains/{id}`
- `DELETE /api/approval-chains/{id}`
- `GET /api/approval-chains/{id}/steps`
- `POST /api/approval-chains/{id}/steps`
- `PUT /api/approval-chain-steps/{id}`
- `DELETE /api/approval-chain-steps/{id}`
- `GET /api/leave-balance-adjustments`
- `POST /api/leave-balance-adjustments`
- `GET /api/leave-holidays`
- `POST /api/leave-holidays`
- `PUT /api/leave-holidays/{id}`
- `DELETE /api/leave-holidays/{id}`

## Frontend Routes

- `/leave`
- `/leave/create`
- `/leave/{id}`
- `/leave/types`
- `/leave/balances`
- `/admin/approval-chains`
- `/admin/approval-chains/create`
- `/admin/approval-chains/{id}/edit`
- `/admin/leave-balances/adjustments`
- `/admin/leave-holidays`

## Status Values

- Draft
- Pending
- Approved
- Rejected
- Cancelled

## Audit Events

- `LeaveType.Create`
- `LeaveType.Update`
- `LeaveType.Delete`
- `LeaveRequest.Create`
- `LeaveRequest.Update`
- `LeaveRequest.Submit`
- `LeaveRequest.Cancel`
- `LeaveRequest.PdfGenerated`
- `LeaveRequest.Approved`
- `LeaveRequest.Rejected`
- `LeaveAttachment.Upload`
- `LeaveAttachment.Download`
- `LeaveAttachment.Delete`
- `ApprovalChain.Create`
- `ApprovalChain.Edit`
- `ApprovalChain.Delete`
- `ApprovalChain.StepCreate`
- `ApprovalChain.StepEdit`
- `ApprovalChain.StepDelete`
- `LeaveBalance.Adjust`
- `LeaveHoliday.Create`
- `LeaveHoliday.Edit`
- `LeaveHoliday.Delete`
