# LINE Integration Plan

Phase 2 prepared LINE Messaging integration. Phase 2.2 adds immediate push delivery attempts when LINE is enabled.

Phase 2.1 adds delivery audit structure and retry design for the real sender worker in a later phase.

## Created Foundation

- `ILineMessagingService`
- `LineMessagingService`
- `LeaveNotificationMessage`
- `line_delivery_logs`

## Current Behavior

The service writes delivery records to `line_delivery_logs`.

If `Line:Enabled` is false, records are saved with status `Disabled`.
If enabled, the service calls LINE Messaging API push endpoint immediately.
Successful delivery is saved as `Sent`.
Missing configuration, missing user `line_user_id`, API failure, or network failure is saved as `Failed` with `next_retry_at`.

## Retry Policy Design

1. Store message payload and event name.
2. Queue records with status `Queued`.
3. Worker sends failed messages to LINE Messaging API.
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
2. Add richer message templates.
3. Map all hospital users to LINE user IDs.
4. Add background retry worker.
5. Add retry management UI.
