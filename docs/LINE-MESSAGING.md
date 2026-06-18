# LINE Messaging

## Configuration

```text
Line__Enabled=true
Line__ChannelAccessToken=<LINE channel access token>
Line__ChannelSecret=<LINE channel secret>
```

Aliases in `.env`:

```text
LINE_ENABLED=true
LINE_ACCESS_TOKEN=<LINE channel access token>
LINE_CHANNEL_SECRET=<LINE channel secret>
```

## Behavior

- Leave submit, approve, reject, and cancel create delivery records.
- If LINE is disabled, delivery status is `Disabled`.
- If token or user `line_user_id` is missing, delivery status is `Failed`.
- If LINE API succeeds, delivery status is `Sent`.
- If LINE API fails, delivery status is `Failed` with `next_retry_at`.

## Delivery Table

```text
line_delivery_logs
```

Important fields:

- `leave_request_id`
- `recipient_user_id`
- `event_name`
- `status`
- `payload`
- `response_detail`
- `attempt_count`
- `next_retry_at`
- `sent_at`

## Test

1. Set `Line__Enabled=true`.
2. Set `Line__ChannelAccessToken`.
3. Add `line_user_id` to the target user.
4. Submit or approve a leave request.
5. Inspect `line_delivery_logs`.
