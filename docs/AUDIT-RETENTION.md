# Audit Retention

Phase 1 มี audit log, export และ retention execution แล้ว เพื่อช่วยตรวจสอบการใช้งานระบบก่อนขึ้น production

## Environment

```text
AUDIT_LOG_RETENTION_DAYS=365
AuditLog__RetentionDays=365
```

## APIs

- `GET /api/audit-logs/export`
- `GET /api/audit-logs/export/excel`
- `GET /api/audit-logs/export/pdf`
- `POST /api/audit-logs/retention/run`

## Audit Events

- `AuditLog.Export`
- `AuditLog.ExportExcel`
- `AuditLog.ExportPdf`
- `AuditLog.RetentionRun`

## Frontend

- `/admin/audit-logs/export`

## Operational Policy

1. ค่าเริ่มต้น retention คือ 365 วัน
2. การรัน retention ต้องทำโดยผู้มีสิทธิ์ `SystemSettings.Manage`
3. ก่อนเปิด production ต้องกำหนด owner และรอบการรัน เช่น รายเดือน
4. ควรทดสอบบน database สำเนาก่อนรันกับ production
5. Export รองรับ CSV, Excel และ PDF ตาม endpoint ปัจจุบัน
