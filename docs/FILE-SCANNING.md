# File Scanning

## Purpose

Leave attachment uploads now pass through a scanning interface before being saved.

## Interface

```text
IFileScanningService
```

Current implementation:

```text
PlaceholderFileScanningService
```

## Environment

```text
FILE_SCAN_ENABLED=false
FILE_SCAN_PROVIDER=Placeholder
```

ASP.NET configuration keys:

```text
FileScan__Enabled=false
FileScan__Provider=Placeholder
```

## Audit Events

- `LeaveAttachment.ScanPassed`
- `LeaveAttachment.ScanFailed`

## Future ClamAV

Replace `PlaceholderFileScanningService` with a ClamAV implementation behind `IFileScanningService`.
