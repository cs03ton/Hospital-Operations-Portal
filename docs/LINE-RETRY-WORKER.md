# LINE Retry Worker

## Purpose

Retries LINE delivery records that are `Failed` or still `Queued`.

## Environment

```text
LINE_RETRY_ENABLED=false
LINE_RETRY_MAX_ATTEMPTS=3
LINE_RETRY_INTERVAL_MINUTES=5
```

ASP.NET configuration keys:

```text
LineRetry__Enabled=false
LineRetry__MaxAttempts=3
LineRetry__IntervalMinutes=5
```

## Behavior

- Reads from `line_delivery_logs`.
- Processes records where `next_retry_at` is due.
- Stops retry after max attempts.
- Main leave workflow does not fail if LINE fails.

## Status Values

- `Queued`
- `Sent`
- `Failed`
- `Disabled`
