# Monitoring

## Security Summary API

```text
GET /api/monitoring/security-summary?hours=24
```

Permission:

```text
SystemSettings.View
```

The endpoint summarizes security and operations signals from audit logs and LINE delivery logs.

Fields:

- `scanFailures`
- `failedUploads`
- `loginLockouts`
- `permissionDenied`
- `refreshTokenReuse`
- `auditExports`
- `csrfFailures`
- `failedLineDeliveries`

## Structured Logs

The backend writes structured logs for:

- Audit events: `Audit event recorded`
- CSRF validation failures
- Permission denied
- Leave attachment upload failures
- ClamAV unavailable or infected responses
- LINE delivery failures
- Login lockout through the audit event `Auth.LoginLocked`
- Refresh token reuse through the audit event `Auth.RefreshTokenReuseDetected`
- Audit export through the audit event `AuditLog.Export`

## Audit Events To Monitor

```text
LeaveAttachment.ScanFailed
LeaveAttachment.UploadFailed
Auth.LoginLocked
Authorization.Denied
Auth.RefreshTokenReuseDetected
Security.CsrfValidationFailed
AuditLog.Export
```

## LINE Delivery Monitoring

Failed LINE sends are stored in:

```text
line_delivery_logs
```

Watch rows where:

```text
status = 'Failed'
```

## Suggested Alert Rules

Initial rules for production:

- `scanFailures > 0` in 15 minutes: investigate file upload source.
- `failedUploads > 10` in 15 minutes: inspect file validation and user behavior.
- `loginLockouts > 5` in 15 minutes: possible brute force attempt.
- `permissionDenied > 20` in 15 minutes: possible role misconfiguration or probing.
- `refreshTokenReuse > 0` in 24 hours: revoke sessions and investigate account compromise.
- `csrfFailures > 0` in 15 minutes when cookie mode is enabled: check frontend origin and possible CSRF attempt.
- `failedLineDeliveries > 0` for more than 30 minutes: inspect LINE token/configuration and retry worker.
- `auditExports > 5` in 24 hours: review data export activity.

## Future Work

- Export metrics to Prometheus/OpenTelemetry.
- Add dashboard widgets for security summary.
- Add alerts in the production monitoring platform.
- Correlate audit logs with request id and user agent.
