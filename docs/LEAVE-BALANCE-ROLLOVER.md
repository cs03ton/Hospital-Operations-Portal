# Production-Ready Leave Balance Rollover

ระบบยกยอดวันลาใช้ `LeavePolicyService` เป็น source of truth สำหรับ entitlement, carry over cap, เงื่อนไขประเภทบุคลากร และ policy ของแต่ละประเภทลา

## Fiscal Year

Backend และ database เก็บปีงบประมาณเป็น ค.ศ. เท่านั้น เช่น `2026`, `2027`

ถ้า API ได้ปี พ.ศ. เช่น `2569` ระบบจะ reject ด้วยข้อความ:

```text
ปีงบประมาณไม่ถูกต้อง ระบบต้องใช้ปี ค.ศ. ภายใน backend
```

Frontend แสดงปีเป็น พ.ศ. เพื่อผู้ใช้ แต่แปลงเป็น ค.ศ. ก่อนส่ง API

## Supported Modes

ใช้ API ชุดเดียวกันทั้งแบบ batch และรายบุคคล

- Batch: ส่ง filter เช่น department, employment type, leave type
- Individual: ส่ง `userId` และ/หรือ `leaveTypeId`

## Preview / Dry Run

Preview ไม่สร้างหรือแก้ไข balance แต่คำนวณผลลัพธ์ให้ตรวจสอบก่อนยืนยัน

```http
POST /api/leave-balances/rollover/preview
Content-Type: application/json

{
  "fromFiscalYear": 2026,
  "toFiscalYear": 2027,
  "departmentId": null,
  "employmentType": null,
  "leaveTypeId": null,
  "userId": null
}
```

Preview item แสดง:

- ผู้ใช้งาน, หน่วยงาน, ประเภทบุคลากร
- ประเภทลา
- ปีต้นทาง/ปลายทาง
- entitlement, carried over, adjusted, used, pending
- `endYearRemaining`
- `carryOverCap`
- `carryOverDays`
- `forfeitedDays`
- `newEntitlementDays`
- `newAvailableDays`
- action: `Created`, `Updated`, `Skipped`, `NoChange`, `Blocked`
- reason และ warnings

## Confirm

Confirm ต้องระบุเหตุผลเสมอ และ backend จะ rerun preview ก่อน write เพื่อป้องกันข้อมูลเปลี่ยนระหว่างตรวจสอบกับยืนยัน

```http
POST /api/leave-balances/rollover/confirm
Content-Type: application/json

{
  "fromFiscalYear": 2026,
  "toFiscalYear": 2027,
  "departmentId": null,
  "employmentType": null,
  "leaveTypeId": null,
  "userId": null,
  "reason": "ยกยอดวันลาประจำปีงบประมาณ 2570"
}
```

Confirm ทำงานใน transaction และ idempotent:

- ถ้าปีปลายทางยังไม่มี balance: สร้าง balance ใหม่
- ถ้าปีปลายทางมี balance แล้วและ policy อนุญาต: อัปเดตเฉพาะ `carriedOverDays`
- ถ้าไม่ควรเปลี่ยน: action เป็น `NoChange` หรือ `Skipped`
- ไม่สร้าง duplicate balance

## Formula

```text
endYearRemaining =
  entitlementDays + carriedOverDays + adjustedDays - usedDays - pendingDays

carryOverDays =
  min(max(endYearRemaining, 0), carryOverCap)

forfeitedDays =
  max(endYearRemaining - carryOverCap, 0)

newAvailableDays =
  newEntitlementDays + carryOverDays
```

`pendingDays` ถูกนำมาคิดเสมอ ส่วน rejected/cancelled ไม่ถูกนับเป็น pending

## Carry Over Cap

Cap มาจาก `LeavePolicyService` ไม่ hardcode ใน controller

ตัวอย่าง policy เริ่มต้น:

| ประเภทบุคลากร | Cap ลาพักผ่อน |
|---|---:|
| ข้าราชการ อายุงานน้อยกว่า 10 ปี | 20 |
| ข้าราชการ อายุงานครบ 10 ปีขึ้นไป | 30 |
| พนักงานราชการ | 15 |
| พนักงานกระทรวงสาธารณสุข | 15 |
| ลูกจ้างชั่วคราว | 0 |

## Snapshot / Run Log

Confirm สร้างข้อมูลติดตาม:

- `leave_balance_rollover_runs`
- `leave_balance_snapshots`

snapshot เก็บยอดต้นทางก่อน write เพื่อ audit และตรวจสอบย้อนหลัง

## Export

```http
POST /api/leave-balances/rollover/export-preview
```

ส่งออก CSV ของ preview ปัจจุบัน

## Permission

ผู้ใช้ต้องมีอย่างน้อยหนึ่ง permission:

- `LeaveBalance.Rollover`
- `LeaveAdmin.ManageBalances`

## Audit Events

- `LeaveBalance.RolloverPreviewed`
- `LeaveBalance.RolloverStarted`
- `LeaveBalance.RolloverCompleted`
- `LeaveBalance.RolloverFailed`
- `LeaveBalance.Adjusted`

## Legacy Endpoints

endpoint รายคนเดิมยังคงไว้เพื่อ backward compatibility แต่ UI ใหม่ใช้ preview/confirm batch API โดยส่ง `userId` สำหรับรายบุคคล
