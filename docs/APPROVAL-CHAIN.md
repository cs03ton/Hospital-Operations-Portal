# Approval Chain

Phase 2.1 adds configurable multi-step approval chains for leave requests.

## Tables

- `approval_chains`
- `approval_chain_steps`
- `leave_approvals`

## Matching Rules

The backend selects the most specific active chain by:

1. Matching `department_id` or allowing all departments.
2. Matching `leave_type_id` or allowing all leave types.
3. Matching `minimum_days <= leave_request.total_days`.
4. Prioritizing department-specific, leave-type-specific, then highest minimum days.

If no chain is configured, the system falls back to the default approver lookup.

## Step Rules

Each step can define:

- Approver role
- Approver user
- Required permission code
- Step order
- Active status

The approver must have the configured `required_permission_code`. Invalid steps are not used when building the approval plan.

## APIs

- `GET /api/approval-chains`
- `GET /api/approval-chains/{id}`
- `POST /api/approval-chains`
- `PUT /api/approval-chains/{id}`
- `DELETE /api/approval-chains/{id}`
- `GET /api/approval-chains/{id}/steps`
- `POST /api/approval-chains/{id}/steps`
- `PUT /api/approval-chain-steps/{id}`
- `DELETE /api/approval-chain-steps/{id}`

## Frontend

- `/admin/approval-chains`
- `/admin/approval-chains/create`
- `/admin/approval-chains/{id}/edit`

## Audit Events

- `ApprovalChain.Create`
- `ApprovalChain.Edit`
- `ApprovalChain.Delete`
- `ApprovalChain.StepCreate`
- `ApprovalChain.StepEdit`
- `ApprovalChain.StepDelete`
