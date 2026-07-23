# Leave Balance Ledger

## ตารางที่มีอยู่

ระบบมี `leave_balance_transactions` สำหรับบันทึก movement บางประเภทแล้ว

Transaction type ปัจจุบัน:

| Type | ความหมาย |
| --- | --- |
| `EntitlementGranted` | ตั้งต้นสิทธิ์ลาจาก policy |
| `LeaveCancellationRestore` | คืนวันลาหลังคำขอยกเลิกใบลาอนุมัติครบ |

## Cached Balance

ตาราง `leave_balances` ยังเป็น cached balance หลักที่ระบบใช้แสดงผลและตรวจยอด:

```text
entitled_days
carried_over_days
adjusted_days
used_days
pending_days
```

## Direction

ค่าบวก:

- EntitlementGranted
- LeaveCancellationRestore
- Manual positive adjustment
- CarryForward

ค่าลบ:

- LeaveApprovedDeduction
- Expired
- Manual negative adjustment

## Gap

ledger ยังไม่ครอบคลุมทุก movement เช่น approve deduction และ pending reserve แบบเต็มรูปแบบ จึงควรมี reconciliation tool ก่อนใช้ ledger เป็น source of truth ทั้งหมด

