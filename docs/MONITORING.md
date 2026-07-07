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

## Queue / Worker Health

Admin Health Dashboard แสดง Queue / Worker Status จาก `GET /api/admin/health`

Fields ที่ควร monitor:

- `queue.pendingLineDeliveries`
- `queue.failedLineDeliveries`
- `queue.pendingRetries`
- `queue.lineRetryEnabled`
- `queue.approvalEscalationEnabled`
- `queue.lastLineSuccessAt`
- `queue.lastLineFailureAt`

Queue health จะเป็น `Warning` เมื่อมี failed LINE deliveries หรือ pending queue สูงผิดปกติ

## Correlation ID

ทุก request รองรับ header:

```text
X-Correlation-ID
```

ถ้ามีการส่ง header นี้ผ่าน Nginx backend จะใช้เป็น `ReferenceId` ใน error response และ log scope เพื่อให้ผู้ดูแลระบบ trace ปัญหาจาก frontend, Nginx และ backend ได้ง่ายขึ้น

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
- `queue.failedLineDeliveries > 0`: ตรวจ LINE token, LINE user binding และ retry worker.
- `queue.pendingRetries > 50`: ตรวจว่า LINE retry worker เปิดใช้งานและไม่มี rate limit จาก LINE API.

## Future Work

- Export metrics to Prometheus/OpenTelemetry.
- Add dashboard widgets for security summary.
- Add alerts in the production monitoring platform.
- Correlate audit logs with user agent and central log aggregator.
