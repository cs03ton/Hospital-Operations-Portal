# User Management

เอกสารนี้อธิบายการจัดการผู้ใช้งาน Phase 1 สำหรับ Hospital Operations Portal

## ข้อมูลสำคัญของผู้ใช้

- รหัสพนักงาน
- ชื่อ-สกุล
- ชื่อผู้ใช้
- หน่วยงาน
- บทบาท
- LINE User ID
- กฎการอนุมัติวันลา

## กฎการอนุมัติวันลา

ช่อง `กฎการอนุมัติวันลา` ใช้กำหนดเส้นทางอนุมัติเมื่อผู้ใช้งานส่งคำขอลา

ผู้ใช้งานไม่สามารถเลือก rule เองตอนยื่นลาได้ Admin ต้องกำหนด rule ให้ถูกต้องในหน้าเพิ่ม/แก้ไขผู้ใช้งาน

## ตัวอย่างการผูก Rule

| Username | Role | Approval Rule |
| --- | --- | --- |
| `staff01` | Staff | `IT-STAFF` |
| `staff02` | Staff | `IT-STAFF` |
| `head01` | DepartmentHead | `IT-HEAD` |
| `director01` | Director | `DIRECTOR` |
| `admin_support` | Admin | `IT-HEAD` |

## วิธีตรวจสอบ Rule

1. ไปที่ `จัดการระบบผู้ใช้` → `จัดการผู้ใช้`
2. ตรวจคอลัมน์ `กฎการอนุมัติวันลา`
3. กดปุ่ม `ทดสอบกฎการอนุมัติ`
4. ตรวจว่าผู้อนุมัติแต่ละขั้นถูกต้อง และไม่มี warning เรื่อง self approval

## ข้อควรระวัง

- ถ้า user ไม่มี approval rule จะไม่สามารถ submit leave request ได้
- ถ้า rule ถูกปิดใช้งานควรเปลี่ยน rule ให้ user ก่อนใช้งานจริง
- ผู้อนุมัติในแต่ละขั้นต้องมี permission `LeaveApproval.ApproveCurrentStep`
