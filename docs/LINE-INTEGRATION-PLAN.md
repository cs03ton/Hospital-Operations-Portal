# LINE Integration Plan

Phase 2 prepares LINE Messaging integration without sending real messages.

Phase 2.1 adds delivery audit structure and retry design for the real sender worker in a later phase.

## Created Foundation

- `ILineMessagingService`
- `LineMessagingService`
- `LeaveNotificationMessage`
- `line_delivery_logs`

## Current Behavior

The service writes delivery preparation records to `line_delivery_logs`.

It does not call LINE Messaging API yet.

If `Line:Enabled` is false, records are saved with status `Disabled`.
If enabled in a future phase, records can be queued with `Queued` and `next_retry_at`.

## Retry Policy Design

1. Store message payload and event name.
2. Queue records with status `Queued`.
3. Worker sends messages to LINE Messaging API.
4. On transient failure, increment `attempt_count` and set `next_retry_at`.
5. Use exponential backoff for retry intervals.
6. Stop retrying after a configured maximum attempt count and mark `Failed`.

## Delivery Audit Design

Each delivery attempt should record:

- Leave request
- Recipient user
- Event name
- Payload
- Attempt count
- Status
- LINE API response detail
- Sent timestamp
- Next retry timestamp

## Future Work

1. Add secure LINE channel configuration.
2. Add message templates.
3. Map hospital users to LINE user IDs.
4. Send notifications on leave submit, approve, reject, and cancel.
5. Add background sender worker.
6. Add retry and delivery audit execution.
