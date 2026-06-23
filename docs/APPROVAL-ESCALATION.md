# Approval Escalation

Approval Escalation ใช้ตรวจคำขอที่ค้างอนุมัติเกินเวลาที่กำหนด

## Permission

- `LeaveApprovalEscalation.Manage`

## Rules

- กำหนดจำนวนชั่วโมงที่ค้างได้
- ตรวจคำขอที่ status เป็น `Pending`
- ไม่ auto-approve
- ถ้ามี escalation target จะย้าย current approver ไปยัง target
- ถ้าไม่มี target จะบันทึกและแจ้งเตือนฝั่ง support/admin เพื่อดำเนินการต่อ

## Audit Events

- `LeaveApproval.EscalationDetected`
- `LeaveApproval.EscalationNotified`

## Worker

มี `ApprovalEscalationWorker` และเปิด/ปิดได้ด้วย configuration:

- `ApprovalEscalation:Enabled`
- `ApprovalEscalation:IntervalMinutes`
