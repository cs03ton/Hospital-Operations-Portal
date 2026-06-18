# Approval Workflow

Phase 2 implements a first approval workflow for leave requests.

Phase 2.1 adds configurable multi-step approval chains.

## Flow

1. User creates a draft leave request.
2. User submits the draft.
3. System changes status to `Pending`.
4. System validates working days, overlapping leave, required attachments, and remaining balance.
5. System selects an approval chain by department, leave type, and minimum days.
6. System creates one `leave_approvals` record per approval step.
7. Current approver approves or rejects the pending step.
8. If approved and another step is waiting, the next step becomes pending.
9. If the last step is approved, the leave request becomes `Approved`.
10. If any step is rejected, the leave request becomes `Rejected`.

## Balance Updates

- Submit adds request days to `pending_days`.
- Cancel removes pending days.
- Approve removes pending days and adds used days.
- Reject removes pending days.

## Reliability Additions

- Approval delegation can replace an approver during approval plan creation when the delegate has the required permission.
- Approval escalation rules can move overdue pending approval steps to another configured user or role.
- LINE delivery writes delivery logs and the retry worker can retry `Queued` or `Failed` deliveries when enabled.
