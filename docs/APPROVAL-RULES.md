# Approval Rules

Approval Rule คือกฎการอนุมัติวันลาที่ผูกกับผู้ใช้งานแต่ละคนโดยตรง

ระบบยัง reuse ตารางเดิม `approval_chains` และ `approval_chain_steps` เพื่อลดความเสี่ยงด้าน migration แต่ UI จะแสดงคำว่า `กฎการอนุมัติวันลา`

## Concept

```text
User
↓
Approval Rule
↓
Approval Rule Steps
↓
Approver แต่ละขั้น
```

ผู้ขอลาไม่สามารถเลือก rule เองตอนยื่นลา ระบบจะอ่าน rule จากข้อมูลผู้ใช้งานเท่านั้น

## Data Model

- `users.leave_approval_rule_id` ผูก user กับ approval rule เริ่มต้น
- `approval_chains` เก็บ approval rule
- `approval_chain_steps` เก็บลำดับผู้อนุมัติ
- 1 user ใช้ได้ 1 approval rule
- 1 approval rule ใช้ร่วมกันได้หลาย user

## Submit Behavior

เมื่อ user กดส่งคำขอลา:

1. ระบบตรวจ `users.leave_approval_rule_id`
2. ถ้าไม่มี rule ระบบปฏิเสธ submit พร้อมข้อความภาษาไทย
3. ระบบโหลด active steps จาก rule นั้น
4. ระบบสร้าง `leave_approvals` ตาม step
5. ถ้าพบ self approval ระบบบล็อก หรือใช้ Director fallback ตาม configuration เดิม

## Example Rules

| Rule | ใช้กับ | Steps |
| --- | --- | --- |
| `IT-STAFF` | `staff01`, `staff02` | `head01` → `director01` |
| `IT-HEAD` | `head01`, `admin_support` | `director01` |
| `DIRECTOR` | `director01` | `admin_support` หรือ fallback approver |

## Preview

Admin สามารถกด `ทดสอบกฎการอนุมัติ` ได้จาก:

- หน้า `จัดการผู้ใช้`
- หน้า `กฎการอนุมัติวันลา`

Preview จะแสดง:

- ผู้ใช้งาน
- rule ที่ใช้
- ผู้อนุมัติแต่ละขั้น
- warning ถ้า self approval
- warning ถ้า approver ไม่มี permission
- warning ถ้า rule inactive หรือไม่มี step

## Production Notes

- ต้องกำหนด approval rule ให้ user ก่อนเปิดให้ส่งคำขอลาจริง
- ห้ามผูก rule ที่ทำให้ผู้ขอลาอนุมัติตัวเอง
- Director leave ควรใช้ rule ที่มี fallback/deputy approver
- Backend enforcement ยังยึด `current_approver_id = currentUserId` สำหรับ queue อนุมัติ
