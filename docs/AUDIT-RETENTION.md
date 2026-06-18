# Audit Retention

Phase 2 adds audit export and retention execution.

## Environment

```text
AUDIT_LOG_RETENTION_DAYS=365
```

## APIs

- `GET /api/audit-logs/export`
- `POST /api/audit-logs/retention/run`

## Audit Events

- `AuditLog.Export`
- `AuditLog.RetentionRun`

## Frontend

- `/admin/audit-logs/export`

Export currently returns CSV.
