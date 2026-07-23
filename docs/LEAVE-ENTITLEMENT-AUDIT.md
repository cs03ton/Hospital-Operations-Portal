# Leave Entitlement Audit

อัปเดตล่าสุด: 21 กรกฎาคม 2026

## Executive Summary

ระบบ HOP มีข้อมูลพื้นฐานสำหรับคำนวณสิทธิ์ลาแล้ว ได้แก่ `users.employment_type`, `users.employment_start_date`, `leave_policy_rules`, `leave_balances` และ `leave_balance_transactions` แต่ก่อนการปรับปรุงรอบนี้ยังมีช่องว่างสำคัญ:

- การสร้างผู้ใช้ใหม่ยังไม่ตั้งต้น `leave_balances` อัตโนมัติ
- การแก้ไขประเภทพนักงานยังบันทึกทับ field เดิมโดยไม่มี preview/effective history
- บาง code path สร้าง balance อัตโนมัติด้วยปีปฏิทินและ default leave type แทน policy
- ledger มีอยู่แล้ว แต่ยังไม่ได้เป็น source of truth ทั้งระบบ ใช้เด่นชัดกับการคืนวันลาจากคำขอยกเลิกใบลา

รอบนี้แก้แบบ incremental โดยไม่เพิ่ม schema:

- เพิ่ม `LeaveEntitlementService.InitializeAsync`
- ผูกการ initialize กับ Create/Edit User เมื่อข้อมูลพนักงานครบ
- ใช้ policy ตามประเภทพนักงานและ fiscal year แทน default กลาง
- แก้ auto-created balance จาก leave request ให้ใช้ fiscal year และ policy entitlement
- เพิ่ม audit และ transaction `EntitlementGranted`

## 1. สถานะปัจจุบัน

| คำถาม | คำตอบ |
| --- | --- |
| สร้าง User ใหม่แล้วสร้างสิทธิ์ลาอัตโนมัติหรือไม่ | หลังปรับปรุง: สร้างให้เมื่อมี `employmentType`, `employmentStartDate`, active user และ policy rule ครบ |
| ใช้ field ใดตัดสิน | `User.EmploymentType`, `User.EmploymentStartDate`, `LeaveType.RequiresBalance`, `LeavePolicyRule` |
| ใช้ประเภทพนักงานหรือกลุ่มสิทธิ์ลาเป็นตัวหลัก | ปัจจุบันใช้ `EmploymentType` เป็นตัวหลัก ยังไม่มี LeavePolicy/LeaveGroup entity แยก |
| ใช้อายุงานหรือไม่ | ใช้ผ่าน `LeavePolicyService.ResolvePolicyEntitlement` และ minimum service validation |
| ใช้ปีปฏิทินหรือปีงบประมาณ | ระบบใช้ fiscal year 1 ต.ค. - 30 ก.ย. ผ่าน `FiscalYearHelper` |
| รองรับเริ่มงานกลางปีหรือไม่ | รองรับบางส่วนผ่าน first-year/probation/prorate fields ใน `leave_policy_rules` |
| รองรับ prorate หรือไม่ | รองรับ `ProrateIfServiceLessThanYear` ใน policy rule แต่ยังไม่มี breakdown persistence |
| เปลี่ยนประเภทพนักงานแล้วคำนวณใหม่หรือไม่ | ยังไม่ recalculate existing balances อัตโนมัติ รอบนี้ audit และ initialize เฉพาะ balance ที่ยังไม่มี |
| มีโอกาสทับยอดเดิมหรือไม่ | ลดความเสี่ยงแล้ว เพราะ initializer idempotent และ skip balance ที่มีอยู่ |
| มี transaction/audit รองรับหรือไม่ | เพิ่ม `EntitlementGranted` transaction และ audit `LeaveEntitlement.Initialized` |
| จุดเสี่ยง duplicate entitlement | การนำเข้าหรือ manual create balance; ป้องกันระดับ DB ด้วย unique index `user_id + leave_type_id + year` |
| จุดเสี่ยงวันลาติดลบ/คืนเกิน | ยังต้อง reconcile ledger กับ cached balance ใน phase ถัดไป |

## 2. Domain Model ที่พบ

| Domain | สถานะจริงในระบบ | หมายเหตุ |
| --- | --- | --- |
| Department | มี table `departments` และ `users.department_id` | ใช้กำหนดหน่วยงาน ไม่ควรใช้แทนประเภทพนักงาน |
| DepartmentType | ยังไม่พบ model/table แยก | ถ้ามีผลต่อ policy ต้องเพิ่มใน phase ถัดไป |
| EmploymentType | เป็น string บน `users.employment_type` | เป็น source หลักของ policy ปัจจุบัน |
| EmployeeCategory | ยังไม่พบ model/table แยก | ใช้แนวคิดเดียวกับ EmploymentType ในระบบปัจจุบัน |
| Position | `users.position` | ไม่ควรใช้คำนวณสิทธิ์ลา |
| LeavePolicy/LeaveGroup | ยังไม่มี entity แยก | ใช้ `leave_policy_rules` เป็น policy matrix |
| LeaveEntitlementProfile | ยังไม่มี entity แยก | ใช้ `leave_balances` เป็น cached entitlement/balance |

## 3. Code Path ที่ตรวจ

| Flow | ไฟล์ | ผลตรวจ |
| --- | --- | --- |
| Create user | `backend/Hop.Api/Controllers/UsersController.cs` | ก่อนแก้ไม่ initialize; หลังแก้เรียก `ILeaveEntitlementService` |
| Update user | `backend/Hop.Api/Controllers/UsersController.cs` | audit เมื่อ employment profile เปลี่ยน และ initialize missing balance |
| Policy calculation | `backend/Hop.Api/Services/LeavePolicyService.cs` | ใช้ employment type, start date, fiscal year และ policy rule |
| Balance validation | `backend/Hop.Api/Services/LeaveBalanceValidationService.cs` | backend enforce ก่อน submit |
| Leave request balance update | `backend/Hop.Api/Controllers/LeaveRequestsController.cs` | ก่อนแก้ใช้ปีปฏิทิน/default; หลังแก้ใช้ fiscal year/policy |
| Rollover | `backend/Hop.Api/Services/LeaveBalanceRolloverService.cs` | มี preview/confirm batch และ individual |
| Cancellation/refund | `backend/Hop.Api/Controllers/LeaveCancellationRequestsController.cs` | คืน used days และเขียน transaction `LeaveCancellationRestore` |
| Dashboard balance | `backend/Hop.Api/Controllers/LeaveBalancesController.cs`, `frontend/src/pages/LeaveBalancePage.tsx` | backend ส่ง policy preview note ถ้ายังไม่มี balance จริง |

## 4. Formula ที่ใช้จริง

```text
availableDays = entitledDays + carriedOverDays + adjustedDays - usedDays - pendingDays
```

Fiscal year:

```text
1 ต.ค. - 30 ก.ย.
ถ้าวันที่อยู่เดือน ต.ค.-ธ.ค. fiscalYear = calendarYear + 1
ถ้าวันที่อยู่เดือน ม.ค.-ก.ย. fiscalYear = calendarYear
```

## 5. Gap / Risk

| Priority | Gap | ผลกระทบ | Recommendation |
| --- | --- | --- | --- |
| P0 | ยังไม่มี employment history/effective date table | เปลี่ยนประเภทพนักงานย้อนหลังไม่ได้อย่างปลอดภัย | เพิ่ม `employee_employment_histories` |
| P0 | ยังไม่มี preview/apply workflow สำหรับ employment type change | อาจแก้ field แล้ว entitlement ไม่ถูก recalculation | เพิ่ม preview/apply API ก่อนอนุญาตเปลี่ยนใน UI |
| P1 | ledger ยังไม่ใช่ source of truth ทั้งหมด | cached balance อาจคลาดเคลื่อน | เพิ่ม reconciliation tool |
| P1 | ไม่มี policy version entity | เปลี่ยน policy ในอนาคตอาจกระทบอดีต | เพิ่ม `leave_policies` version/effective period |
| P1 | dashboard ยังไม่ได้แสดง calculation breakdown เต็ม | HR/ผู้ใช้ตรวจที่มาของสิทธิ์ยาก | เพิ่ม endpoint entitlement details |
| P2 | ยังไม่มี CSV/JSON dry-run repair report จาก service | ตรวจ production data ยังต้องใช้ SQL manual | เพิ่ม repair command แบบ dry-run |

## 6. Files Inspected

- `backend/Hop.Api/Models/User.cs`
- `backend/Hop.Api/Models/LeaveBalance.cs`
- `backend/Hop.Api/Models/LeaveBalanceTransaction.cs`
- `backend/Hop.Api/Models/LeavePolicyRule.cs`
- `backend/Hop.Api/Controllers/UsersController.cs`
- `backend/Hop.Api/Controllers/LeaveBalancesController.cs`
- `backend/Hop.Api/Controllers/LeaveRequestsController.cs`
- `backend/Hop.Api/Controllers/LeaveCancellationRequestsController.cs`
- `backend/Hop.Api/Services/LeavePolicyService.cs`
- `backend/Hop.Api/Services/LeaveBalanceValidationService.cs`
- `backend/Hop.Api/Services/LeaveBalanceRolloverService.cs`
- `backend/Hop.Api/Data/AppDbContext.cs`
- `backend/Hop.Api/Data/DevelopmentDataSeeder.cs`
- `backend/Hop.Api/Migrations/20260701042150_AddEmploymentTypeLeavePolicyRules.cs`
- `backend/Hop.Api/Migrations/20260717045456_AddLeaveBalanceTransactions.cs`
- `frontend/src/pages/LeaveBalancePage.tsx`
- `frontend/src/pages/UserFormPage.tsx`

