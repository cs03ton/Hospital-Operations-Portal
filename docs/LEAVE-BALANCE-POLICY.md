# Leave Balance Policy

ระบบ HOP ใช้นโยบาย **Reserve on Submit** สำหรับ Phase 1

## Policy

เมื่อผู้ใช้กดส่งคำขอลา:

1. Backend คำนวณจำนวนวันลาจริงจากวันที่และ `duration_type`
2. Backend ตรวจยอดวันลาคงเหลือของ `userId + leaveTypeId + year`
3. ถ้ายอดเพียงพอ ระบบเพิ่มค่าใน `leave_balances.pending_days`
4. ถ้าคำขอถูกอนุมัติครบ ระบบย้ายยอดจาก `pending_days` ไป `used_days`
5. ถ้าคำขอถูกไม่อนุมัติหรือยกเลิก ระบบคืนยอดจาก `pending_days`

## Formula

```text
availableDays = entitledDays - usedDays - pendingDays
```

คำขอจะส่งได้เมื่อ:

```text
availableDays >= requestedDays
```

## Why Reserve On Submit

การ reserve ยอดตั้งแต่ submit ป้องกันกรณีเจ้าหน้าที่ส่งหลายคำขอพร้อมกันจนเกินสิทธิ์วันลาจริง

ตัวอย่าง:

| รายการ | จำนวนวัน |
| --- | ---: |
| สิทธิ์คงเหลือก่อนรออนุมัติ | 5 |
| คำขอรออนุมัติ | 3 |
| เหลือใช้ได้ | 2 |
| คำขอใหม่ | 3 |
| ผลลัพธ์ | ไม่อนุญาตให้ส่ง |

## Leave Type Without Quota

ถ้า `leave_types.requires_balance = false` ระบบจะข้าม balance validation สำหรับประเภทลานั้น

