# Test Users

เอกสารนี้ใช้สำหรับ Development / QA / Pilot rehearsal เท่านั้น ห้ามใช้บัญชีทดสอบเหล่านี้เป็นบัญชี production จริง

## Default Password

Development default password:

```text
Nm@12345
```

> หลังนำระบบขึ้นใช้งานจริง ต้องเปลี่ยนรหัสผ่านหรือปิดการสร้างบัญชีทดสอบด้วย configuration

## Standard IT Department

หน่วยงานมาตรฐานสำหรับทดสอบ:

```text
Information Technology
```

## Standard IT Users

| Username | ชื่อ | Role | Department | Purpose |
| --- | --- | --- | --- | --- |
| admin_support | ผู้ดูแลระบบ | Admin | Information Technology | ผู้ดูแลระบบและงาน support ระบบลา |
| staff01 | เจ้าหน้าที่ 01 | Staff | Information Technology | ผู้ยื่นคำขอลา |
| staff02 | เจ้าหน้าที่ 02 | Staff | Information Technology | ผู้ยื่นคำขอลาเพิ่มเติม |
| head01 | หัวหน้าหน่วยงาน | DepartmentHead | Information Technology | ผู้อนุมัติขั้นที่ 1 |
| director01 | ผู้อำนวยการ | Director | Information Technology | ผู้อนุมัติขั้นสุดท้าย |

## Approval Chain

สายอนุมัติมาตรฐาน:

```text
Staff requester
↓
head01 (หัวหน้าหน่วยงาน)
↓
director01 (ผู้อำนวยการ)
```

Approval chain name:

```text
IT Standard Leave Approval Chain
```

## Retired Development Users

Seeder จะ disable user ทดสอบเก่าต่อไปนี้ถ้าพบในฐานข้อมูล เพื่อรักษาประวัติ leave request, audit log และ approval history:

```text
qa_notify_approver1_25690620205022
qa_notify_approver2_25690620205022
qa_notify_approver3_25690620205022
qa_notify_requester_25690620205022
qa_notify_unrelated_25690620205022
manager.it01
staff.it01
staff.other01
head.it01
```

## Seed Configuration

บัญชี IT มาตรฐานถูกสร้างใน non-production environment โดยค่าเริ่มต้น และสามารถควบคุมได้ด้วย:

```text
Seed__CreateStandardItUsers=true
```

หรือ environment variable:

```text
SEED_CREATE_STANDARD_IT_USERS=true
```

ใน production ควรตั้งค่าเป็น `false` เว้นแต่กำลังทำ bootstrap/pilot rehearsal ที่ควบคุมได้เท่านั้น
