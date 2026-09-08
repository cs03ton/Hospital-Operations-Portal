# Fleet Rollout and Monitoring Runbook

## Controlled rollout

1. Stage 0: verify `Disabled`, zero production-role Fleet mappings and no visible Fleet menu.
2. Stage 1: use `UATOnly`; allowlist Fleet Admin and Dispatcher test identities/roles.
3. Stage 2: add Administration Reviewer and Director.
4. Stage 3: add test Drivers.
5. Stage 4: add Requesters from approved pilot departments through explicit identities/roles.
6. Stage 5: expand pilot only after workflow, notification and rollback evidence is signed.
7. Stage 6: change to `Enabled` only under a separate production change request.

This implementation does not change a production setting or assign a production role.

## UAT checklist

- Confirm rollout status and allowlist on `/admin/fleet-rollout`.
- Confirm a non-allowlisted permission-bearing user is denied in `UATOnly`.
- Confirm Fleet menu is hidden in `Disabled` and for non-allowlisted users.
- Confirm every Fleet API still returns 403 without its specific permission.
- Review `/fleet/health`, severity filters, stuck requests and channel-specific delivery failures.
- Confirm audit event `Fleet.RolloutModeChanged` includes actor, old/new mode, reason and correlation ID.
- Run requester-to-trip completion, replacement, cancellation, delegation and notification retry scenarios.
- Confirm Leave regression tests and normal Leave UI/API behavior.

## Notification failure response

1. Identify Outbox Message ID, event type, channel and retry count from Fleet Health.
2. Check In-App and LINE independently; do not resend a processed In-App delivery.
3. Correct provider/configuration failure without exposing LINE tokens in tickets or logs.
4. Observe retry until processed or failed threshold.
5. For persistent failure, retain outbox/delivery records and escalate with correlation ID only.

## Stuck workflow response

1. Filter issues by `Warning` then `Critical`.
2. Open the Fleet request and inspect current assignment/history/audit.
3. Confirm the responsible permission group has active users and valid delegation.
4. Do not modify state directly in SQL. Use supported return/replacement/cancellation actions.

## Emergency rollback

Set rollout mode to `Disabled`, record incident reason, confirm all operational Fleet endpoints deny access, preserve outbox/audit data, and roll back application binaries. Do not revert Leave migrations or delete Fleet operational history.
