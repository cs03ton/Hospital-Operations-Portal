# Approval Delegation and Escalation

## Tables

- `approval_delegations`
- `approval_escalation_rules`

## Delegation APIs

```text
GET /api/approval-delegations
POST /api/approval-delegations
PUT /api/approval-delegations/{id}
DELETE /api/approval-delegations/{id}
```

## Escalation Rule APIs

```text
GET /api/approval-escalation-rules
POST /api/approval-escalation-rules
PUT /api/approval-escalation-rules/{id}
DELETE /api/approval-escalation-rules/{id}
POST /api/approval-escalation-rules/run
```

## Permissions

- `ApprovalDelegation.View`
- `ApprovalDelegation.Create`
- `ApprovalDelegation.Edit`
- `ApprovalDelegation.Delete`
- `ApprovalDelegation.Manage`

## Audit Events

- `ApprovalDelegation.Create`
- `ApprovalDelegation.Update`
- `ApprovalDelegation.Delete`
- `ApprovalEscalationRule.Create`
- `ApprovalEscalationRule.Update`
- `ApprovalEscalationRule.Delete`
- `ApprovalEscalation.Run`
- `Approval.Escalated`

## Worker Environment

```text
APPROVAL_ESCALATION_ENABLED=false
APPROVAL_ESCALATION_INTERVAL_MINUTES=30
```

ASP.NET keys:

```text
ApprovalEscalation__Enabled=false
ApprovalEscalation__IntervalMinutes=30
```
