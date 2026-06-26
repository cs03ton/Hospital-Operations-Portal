# การยกยอดวันลารายคน

ระบบรองรับการยกยอดวันลาแบบรายคนสำหรับปีงบประมาณ โดยผู้ดูแลระบบต้องตรวจสอบ preview ก่อนยืนยันเสมอ

## เงื่อนไข

ทำได้เฉพาะประเภทลาที่ตั้งค่า:

```text
allowCarryOver = true
```

ถ้าประเภทลาไม่รองรับการยกยอด ระบบจะแจ้ง:

```text
ประเภทลานี้ไม่รองรับการยกยอด
```

## ขั้นตอนการใช้งาน

1. เข้าเมนู `ระบบลา`
2. เลือก `วันลาคงเหลือ`
3. กรองปีงบประมาณ/หน่วยงาน/ผู้ใช้งานตามต้องการ
4. กดปุ่ม `ยกยอดรายคน` ในแถวที่ต้องการ
5. ตรวจสอบ Dialog `ตรวจสอบการยกยอดวันลา`
6. ระบุเหตุผล
7. กด `ยืนยันการยกยอด`

## Preview

Preview แสดงข้อมูล:

- ผู้ใช้งาน
- ประเภทลา
- ปีงบประมาณต้นทาง
- ปีงบประมาณปลายทาง
- สิทธิ์ประจำปีเดิม
- ยอดยกมาปีเดิม
- ปรับปรุง
- ใช้ไปแล้ว
- รออนุมัติ
- คงเหลือปลายปี
- ยกยอดได้
- ยอดถูกตัดออกเพราะเกิน limit
- สิทธิ์ประจำปีใหม่
- ยอดรวมปีใหม่หลังยกยอด

## Formula

```text
endYearRemaining =
  entitlementDays + carriedOverDays + adjustedDays - usedDays - pendingDays

carryOverDays =
  min(max(endYearRemaining, 0), carryOverMaxDays)

forfeitedDays =
  max(endYearRemaining - carryOverMaxDays, 0)

newAvailableDays =
  newEntitlementDays + carryOverDays
```

## Existing Target Balance

ถ้าปีงบประมาณปลายทางมี balance อยู่แล้ว ระบบจะแสดง warning และไม่ overwrite อัตโนมัติ

ผู้ดูแลระบบเลือกได้เฉพาะ:

- ยกเลิก
- อัปเดตเฉพาะยอด `carriedOverDays`

## Audit Events

ระบบบันทึก audit event:

- `LeaveBalance.RolloverPreviewed`
- `LeaveBalance.RolloverConfirmed`
- `LeaveBalance.RolloverUpdatedExistingBalance`
- `LeaveBalance.RolloverBlocked`
- `LeaveBalance.Adjusted`

## API

Preview:

```http
POST /api/leave-balances/{id}/rollover-preview
```

Confirm:

```http
POST /api/leave-balances/{id}/rollover-confirm
Content-Type: application/json

{
  "toFiscalYear": 2027,
  "newEntitlementDays": 10,
  "reason": "ยกยอดวันลาปีงบประมาณ 2570",
  "updateExistingCarriedOverOnly": false
}
```

ปรับยอด:

```http
POST /api/leave-balances/{id}/adjust
Content-Type: application/json

{
  "adjustmentDays": 1,
  "reason": "ปรับยอดตามคำสั่งผู้ดูแลระบบ"
}
```
