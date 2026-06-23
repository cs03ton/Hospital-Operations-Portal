# Approval Override

Override เป็น flow แยกจากการอนุมัติปกติ ใช้เฉพาะกรณี support หรือเหตุฉุกเฉิน เช่น approval chain ตั้งผิด ผู้อนุมัติไม่พร้อมใช้งาน หรือมีเหตุจำเป็นในการปิดคำขอ

## Permissions

- `LeaveApproval.Override`

## APIs

- `POST /api/leave-requests/{id}/override-approve`
- `POST /api/leave-requests/{id}/override-reject`

Request:

```json
{
  "reason": "ระบุเหตุผล"
}
```

## Rules

- ต้องมีเหตุผล
- เจ้าของคำขอ override คำขอของตนเองไม่ได้
- ใช้ได้เฉพาะคำขอที่สถานะ `Pending`
- บันทึก original current approver
- บันทึกผู้ override, เหตุผล, เวลา, IP address และ user agent
- บันทึก audit event:
  - `LeaveApproval.OverrideApproved`
  - `LeaveApproval.OverrideRejected`

## Database

บันทึกที่ `approval_override_logs`
