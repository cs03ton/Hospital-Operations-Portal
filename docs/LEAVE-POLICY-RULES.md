# กฎสิทธิ์วันลาตามประเภทพนักงาน

ระบบใช้ตาราง `leave_policy_rules` เพื่อกำหนดสิทธิ์วันลาตาม `employmentType + leaveTypeId`

## Policy Engine

Backend ใช้ `ILeavePolicyService` / `LeavePolicyService` เป็น source of truth สำหรับ:

- ค้นหา policy ตามผู้ใช้ ประเภทลา และปีงบประมาณ
- คำนวณ entitlement
- คำนวณ carry over และ cap ตอนยกยอดวันลา
- ตรวจอายุงานขั้นต่ำ
- คำนวณ `availableDays`
- รวม `usedDays`, `pendingDays`, `adjustedDays`, `carriedOverDays`
- คืน warning/note สำหรับสิทธิ์ที่ไม่ได้รับค่าจ้างหรือมีเงื่อนไขพิเศษ

Frontend ใช้ endpoint preview:

```http
POST /api/leave-requests/policy-preview
```

เพื่อแสดงสิทธิ์, ใช้ไปแล้ว, รออนุมัติ, คงเหลือ, warning และ error ก่อนบันทึกคำขอ

## Field สำคัญ

| Field | คำอธิบาย |
|---|---|
| `employmentType` | ประเภทพนักงาน |
| `leaveTypeId` | ประเภทการลา |
| `fiscalYear` | ปีงบประมาณ ถ้าเป็น `null` คือค่า default |
| `entitlementDays` | สิทธิ์วันลาประจำปี |
| `maxPaidDays` | จำนวนวันสูงสุดที่ได้รับค่าจ้าง |
| `allowCarryOver` | อนุญาตให้ยกยอดหรือไม่ |
| `carryOverMaxDays` | ยอดยกมาสูงสุด |
| `maxAccumulatedDays` | ยอดสะสมสูงสุด |
| `minServiceMonths` / `minServiceYears` | อายุงานขั้นต่ำ |
| `firstYearEntitlementDays` | สิทธิ์ปีแรก |
| `isPaid` | เป็นสิทธิ์ได้รับค่าจ้างหรือไม่ |
| `notes` | หมายเหตุจาก policy |

## Validation ตอนส่งคำขอลา

Backend ตรวจทุกครั้งตอนสร้าง/แก้ไข/ส่งคำขอ โดยเฉพาะ submit:

- ต้องมีประเภทพนักงาน
- ต้องมี policy สำหรับประเภทพนักงานและประเภทลานั้น
- ถ้ามีเงื่อนไขอายุงาน ต้องมีวันที่เริ่มปฏิบัติงานและอายุงานถึงเกณฑ์
- ถ้า leave type ใช้ balance ต้องตรวจยอดคงเหลือรวม pending days
- ไม่ส่งรูปแบบวันที่หรือ payload ที่เปลี่ยน API contract เดิม

## Default Policy ที่ seed

ระบบ seed default policy สำหรับ 5 ประเภทพนักงาน x 5 ประเภทลา รวม 25 รายการ

ตัวอย่าง:

- ลาพักผ่อน: ต้องมีอายุงานอย่างน้อย 6 เดือน
- ข้าราชการ: ลาพักผ่อน 10 วัน สะสมรวมสูงสุด 20 วันเมื่ออายุงานน้อยกว่า 10 ปี และ 30 วันเมื่อครบ 10 ปีขึ้นไป
- พนักงานราชการ/พกส.: ลาพักผ่อน 10 วัน สะสมรวมสูงสุด 15 วัน
- ลูกจ้างชั่วคราว: ลาพักผ่อน 10 วัน ไม่สะสม
- ลากิจส่วนตัว: ข้าราชการ 45 วัน ปีแรก 15 วัน, พนักงานราชการ 10 วัน, พกส. 15 วัน
- ลาป่วย: ข้าราชการ 60 วัน พร้อม note กรณี ผอ. พิจารณาได้รวมไม่เกิน 120 วัน, พนักงานราชการ 30 วัน, พกส. 45 วัน, ลูกจ้างชั่วคราว 15 วัน
- ลาคลอดบุตร: 90 วัน โดยบางประเภทได้รับค่าจ้างไม่เกิน 45 วันตาม policy
- ลาบวช: ข้าราชการต้องครบ 12 เดือน, พนักงานราชการ/พกส. ต้องครบ 4 ปี, ลูกจ้างชั่วคราวเป็นสิทธิ์ไม่รับค่าจ้าง

## Carry Over Policy

ระบบยกยอดวันลาเรียก `LeavePolicyService.CalculateCarryOverAsync()` เพื่อใช้ policy เดียวกับการ validate คำขอลา

| ประเภทบุคลากร | เงื่อนไข | Cap ยกยอดลาพักผ่อน |
|---|---|---:|
| ข้าราชการ | อายุงานน้อยกว่า 10 ปี | 20 |
| ข้าราชการ | อายุงานครบ 10 ปีขึ้นไป | 30 |
| พนักงานราชการ | ตาม policy เริ่มต้น | 15 |
| พนักงานกระทรวงสาธารณสุข | ตาม policy เริ่มต้น | 15 |
| ลูกจ้างชั่วคราวรายเดือน | ไม่สะสม | 0 |
| ลูกจ้างชั่วคราวรายวัน | ไม่สะสม | 0 |

ถ้า policy เฉพาะปีมี `carryOverMaxDays` หรือ `maxAccumulatedDays` ระบบจะใช้ค่าที่เข้มงวดกว่าเป็น cap สุดท้าย

Controller ห้าม hardcode cap โดยตรง เพื่อให้เปลี่ยนนโยบายได้จาก policy/seeder/migration โดยไม่ต้องแก้ business flow

## Leave Type Balance Flags

| Code | ใช้ยอดคงเหลือ | ใช้ปีงบประมาณ | เหตุผล |
|---|---|---|---|
| `VACATION_LEAVE` | ใช่ | ใช่ | เป็นสิทธิ์ประจำปีและมีการสะสมตาม policy |
| `PERSONAL_LEAVE` | ใช่ | ใช่ | เป็นสิทธิ์ประจำปี |
| `SICK_LEAVE` | ใช่ | ใช่ | เป็นสิทธิ์ประจำปีและอาจมีเงื่อนไขพิเศษ |
| `MATERNITY_LEAVE` | ไม่ | ใช่ | ตรวจด้วย policy/gender/จำนวนวัน ไม่กันยอด balance รายปี |
| `ORDINATION_LEAVE` | ไม่ | ไม่ | ตรวจด้วย policy/gender/อายุงาน ไม่กันยอด balance รายปี |

## Gender Eligibility Rules

Backend ตรวจสิทธิ์ตามเพศผ่าน `LeavePolicyService.ValidateGenderRequirement()` ทุกครั้งที่ validate คำขอลา ไม่อนุญาตให้พึ่ง frontend อย่างเดียว

| ประเภทลา | ผู้มีสิทธิ์ | ผู้ไม่มีสิทธิ์ | ข้อความแจ้งเตือน |
|---|---|---|---|
| `MATERNITY_LEAVE` ลาคลอดบุตร | `Female` | `Male`, `Unknown` | ประเภทการลาคลอดบุตร ใช้ได้เฉพาะบุคลากรเพศหญิง |
| `ORDINATION_LEAVE` ลาบวช | `Male` | `Female`, `Unknown` | ประเภทการลาบวช ใช้ได้เฉพาะบุคลากรเพศชาย |

Frontend หน้า Create Leave ใช้ policy preview เพื่อแสดงข้อความสีแดงและปิดปุ่ม submit เมื่อไม่ผ่านเงื่อนไข และมี config `VITE_HIDE_INELIGIBLE_LEAVE_TYPES` สำหรับซ่อนประเภทลาที่ user ไม่มีสิทธิ์ตามเพศ ถ้าไม่กำหนดจะเปิดไว้เป็นค่าเริ่มต้น

## Formula

```text
availableDays = entitlementDays + carriedOverDays + adjustedDays - usedDays - pendingDays
```

`pendingDays` ต้องนับเฉพาะคำขอที่ยังรออนุมัติ ส่วน `rejected` และ `cancelled` ไม่ถูกนับเป็น pending
