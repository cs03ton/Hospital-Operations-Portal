# HOP Vehicle Permission Matrix

ใช้ convention เดิมของ HOP (`PascalCase.Resource.Action`) และไม่ผูก authorization กับชื่อ role

| Capability | Permission |
|---|---|
| ดู/สร้าง/แก้/ส่ง/ยกเลิก/คัดลอกคำขอตนเอง | `FleetRequest.ViewOwn`, `.Create`, `.EditOwn`, `.Submit`, `.Cancel`, `.Copy` |
| ดูระดับหน่วยงาน/ทั้งหมด | `FleetRequest.ViewDepartment`, `FleetRequest.ViewAll` |
| ดูคิว/จัด/เปลี่ยน/return | `FleetDispatch.View`, `.Assign`, `.Reassign`, `.Return` |
| Administration review | `FleetAdminReview.Approve`, `.Return`, `.Reject` |
| Director approval | `FleetDirector.Approve`, `.Return`, `.Reject` |
| Driver actions | `FleetDriver.ViewOwn`, `.Acknowledge`, `.Start`, `.Complete` |
| Master data | `FleetVehicle.Manage`, `FleetDriver.Manage`, `FleetSettings.Manage` |
| จัดการการมอบหมายผู้ review/อนุมัติของ Fleet | `FleetDelegation.Manage` |
| Reports | `FleetReport.View`, `FleetReport.Export` |
| Admin override | `FleetTrip.OverrideComplete` |

หลังผ่าน Milestone 4.2 UAT และได้รับอนุมัติ rollout เมื่อ 2026-08-04 ระบบ seed สิทธิ์ requester ให้ `Staff`, `DepartmentHead` และ `Director`; `Director` ได้สิทธิ์ Fleet director approval เพิ่ม และ `Admin`/`SuperAdmin` ได้สิทธิ์ Fleet administration. สิทธิ์ Dispatcher และ Driver ต้อง assign ผ่าน Role Management ตามหน้าที่จริง. Active-user check เป็น business rule เพิ่มจาก permission

`FleetDelegation.Manage` จัดการได้เฉพาะ row ที่ `Scope=FLEET` และไม่ใช้แทน `LeaveApproval.Delegate` หรือ `LeaveApprovalDelegation.Manage`. ผู้รับมอบหมายยังต้องมี `RequiredPermissionCode` ของ row นั้นจริง

## Milestone 3.1 production guard

ข้อห้าม automatic production mapping สิ้นสุดหลัง Milestone 4.2 UAT ผ่านและมี rollout approval เมื่อ 2026-08-04 โดยยังคง least privilege และไม่ infer สิทธิ์ Dispatcher/Driver จาก role อื่น.
