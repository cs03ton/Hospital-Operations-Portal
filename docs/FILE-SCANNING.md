# File Scanning

## Purpose

Leave attachment uploads now pass through a scanning interface before being saved.

## Interface

```text
IFileScanningService
```

Implementations:

```text
PlaceholderFileScanningService
ClamAvFileScanningService
```

## Environment

```text
FILE_SCAN_ENABLED=false
FILE_SCAN_PROVIDER=Placeholder
FILE_SCAN_FAIL_CLOSED=true
CLAMAV_HOST=localhost
CLAMAV_PORT=3310
CLAMAV_TIMEOUT_MS=5000
```

ASP.NET configuration keys:

```text
FileScan__Enabled=false
FileScan__Provider=Placeholder
FileScan__FailClosed=true
ClamAv__Host=localhost
ClamAv__Port=3310
ClamAv__TimeoutMs=5000
```

Use ClamAV:

```text
FILE_SCAN_ENABLED=true
FILE_SCAN_PROVIDER=ClamAV
CLAMAV_HOST=clamav
CLAMAV_PORT=3310
FILE_SCAN_FAIL_CLOSED=true
```

Docker Compose includes a `clamav` service:

```text
image: clamav/clamav:1.4
port: 3310
volume: hop_clamav_data
```

Backend default in Docker points to:

```text
CLAMAV_HOST=clamav
CLAMAV_PORT=3310
```

## Behavior

- `Placeholder` passes files when scanning is disabled or provider is `Placeholder`.
- `ClamAV` uses the ClamAV `INSTREAM` protocol.
- If ClamAV is unavailable and `FILE_SCAN_FAIL_CLOSED=true`, upload is rejected.
- If ClamAV is unavailable and `FILE_SCAN_FAIL_CLOSED=false`, upload is allowed and the result message records fail-open behavior.
- The upload controller records audit events for scan passed and scan failed.
- ClamAV service health is checked by Docker Compose before backend startup.

## Audit Events

- `LeaveAttachment.ScanPassed`
- `LeaveAttachment.ScanFailed`

## Tests

Backend tests include mock TCP scanner coverage for clean, infected, and unavailable ClamAV behavior.
