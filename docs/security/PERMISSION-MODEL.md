# Leave Permission Model

เอกสารนี้อธิบาย permission model สำหรับ Leave Management ระยะ Phase 1 โดยแยกสิทธิ์การดูข้อมูล สิทธิ์อนุมัติ และสิทธิ์ดูแลระบบออกจากกัน

## Permission Matrix

| Permission | คำอธิบาย | ใช้กับ |
| --- | --- | --- |
| `LeaveRequest.ViewOwn` | ดูคำขอลาของตนเองเท่านั้น | Staff, DepartmentHead, Director |
| `LeaveRequest.ViewPendingApproval` | ดูเฉพาะคำขอที่ถึงคิวตนเองอนุมัติ | DepartmentHead, Director |
| `LeaveRequest.ViewDepartment` | ดูคำขอลาในหน่วยงานของตนเอง | LeaveAdmin, Admin |
| `LeaveRequest.ViewAll` | ดูคำขอลาทั้งหมด | SuperAdmin |
| `LeaveRequest.Create` | สร้างและส่งคำขอลาของตนเอง | Staff |
| `LeaveRequest.EditOwn` | แก้ไขคำขอของตนเองที่ยังแก้ไขได้ | Staff |
| `LeaveRequest.CancelOwn` | ยกเลิกคำขอของตนเอง | Staff |
| `LeaveApproval.ApproveCurrentStep` | อนุมัติหรือไม่อนุมัติเฉพาะ current step ของตนเอง | DepartmentHead, Director |
| `LeaveApproval.Delegate` | จัดการการมอบหมายผู้อนุมัติ | SuperAdmin |
| `LeaveApproval.Override` | สิทธิ์ override สำหรับกรณีพิเศษ | SuperAdmin |
| `LeaveAdmin.ManageTypes` | จัดการประเภทการลา | LeaveAdmin, Admin, SuperAdmin |
| `LeaveAdmin.ManageBalances` | จัดการยอดวันลาและ adjustment | LeaveAdmin, Admin, SuperAdmin |
| `LeaveAdmin.ManageHolidays` | จัดการวันหยุดราชการ | LeaveAdmin, Admin, SuperAdmin |
| `LeaveAdmin.ManageApprovalChains` | จัดการสายอนุมัติวันลา | LeaveAdmin, Admin, SuperAdmin |

## Role Mapping

> **HR ใน Phase 1:** ไม่ seed role `HR` แยกใน runtime ปัจจุบัน งาน HR ด้านระบบลาให้ map เป็น role `LeaveAdmin` โดยตรง ส่วน `Admin` ใช้สำหรับผู้ดูแลระบบที่ได้รับสิทธิ์ support เพิ่มเติมเท่านั้น

| Role | Permissions |
| --- | --- |
| Staff | `LeaveRequest.ViewOwn`, `LeaveRequest.Create`, `LeaveRequest.EditOwn`, `LeaveRequest.CancelOwn` |
| DepartmentHead | `LeaveRequest.ViewOwn`, `LeaveRequest.ViewPendingApproval`, `LeaveApproval.ApproveCurrentStep` |
| Director | `LeaveRequest.ViewOwn`, `LeaveRequest.ViewPendingApproval`, `LeaveApproval.ApproveCurrentStep` |
| LeaveAdmin | `LeaveRequest.ViewDepartment`, `LeaveAdmin.ManageTypes`, `LeaveAdmin.ManageBalances`, `LeaveAdmin.ManageHolidays`, `LeaveAdmin.ManageApprovalChains` |
| SuperAdmin | `LeaveRequest.ViewAll`, `LeaveApproval.Override`, `LeaveApproval.Delegate`, และ admin permissions ทั้งหมด |

`Admin` ยังถูก seed ให้มี admin permissions หลักเพื่อ backward compatibility กับ deployment เดิม แต่ role ที่ใช้แทน HR operator สำหรับ Phase 1 คือ `LeaveAdmin`

## Visibility Rules

Backend เป็น source of truth สำหรับ visibility:

| Permission | Rule |
| --- | --- |
| `LeaveRequest.ViewOwn` | `leave_request.user_id == current_user_id` |
| `LeaveRequest.ViewPendingApproval` | `leave_request.current_approver_id == current_user_id` |
| `LeaveRequest.ViewDepartment` | `leave_request.user.department_id == current_user.department_id` |
| `LeaveRequest.ViewAll` | เห็นทุกคำขอ |

Frontend guard ใช้เพื่อ UX เท่านั้น ไม่ถือเป็น security boundary

## Approval Rules

- ผู้อนุมัติต้องมี `LeaveApproval.ApproveCurrentStep`
- อนุมัติได้เฉพาะคำขอที่ `current_approver_id` เป็นตนเอง
- ระบบบล็อก self-approval และบันทึก audit event `SelfApprovalBlocked`
- กรณี Director ลาเอง ระบบรองรับ fallback approver ผ่าน configuration และบันทึก `DirectorLeaveFallbackApplied`
- ยังไม่เปิด override flow ใน Phase นี้

## Migration Strategy

- เพิ่ม granular permissions ผ่าน `DevelopmentDataSeeder`
- ไม่ลบ legacy permissions เช่น `LeaveManagement.View`, `LeaveManagement.Approve`, `LeaveManagement.Manage` ทันที เพื่อให้ข้อมูลเดิมไม่เสียหาย
- Endpoint ระบบลาหลักเปลี่ยนมา enforce granular permissions แล้ว
- Role เดิมยัง login ได้ แต่ควร review role permissions ก่อน pilot
