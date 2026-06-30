# Role-Based Notifications

## Staff

Staff notifications focus on their own leave requests:

- ส่งคำขอลาสำเร็จ
- คำขอรออนุมัติ
- คำขออนุมัติแล้ว
- คำขอไม่อนุมัติ
- คำขอถูกส่งกลับแก้ไข
- คำขอถูกยกเลิก
- วันลาคงเหลือน้อย
- PDF ใบลาพร้อมดาวน์โหลด

## Department Head

Department Head notifications focus on team actions:

- มีคำขอใหม่รออนุมัติ
- ลูกทีมยกเลิกคำขอ
- ลูกทีมลาวันนี้
- ลูกทีมลาพรุ่งนี้
- ลูกทีมวันลาคงเหลือน้อย
- คำขอใกล้ครบ SLA

## Director

Director notifications focus on final approval and executive overview:

- คำขอขั้นสุดท้ายรออนุมัติ
- คำขอค้างเกิน SLA
- จำนวนผู้ลาวันนี้
- หน่วยงานที่มีผู้ลาจำนวนมาก
- Executive Summary

## Admin / SuperAdmin

Admin and SuperAdmin notifications focus on system readiness and operations:

- Approval Rule ไม่สมบูรณ์
- Leave Balance ยังไม่ Roll Over
- Holiday ปีใหม่ยังไม่ตั้ง
- Import Holiday สำเร็จ/ล้มเหลว
- LINE Delivery Failed
- Notification Queue Failed
- Backup Failed
- Background Job Failed
- Audit Warning
- Login Failed หลายครั้ง
- Permission Denied ผิดปกติ
- Database Health
- System Health

## Badge Rule

Badge count includes only:

- `notificationType = ActionRequired`
- `unread = true`

Information notifications do not increase the badge after being read.
