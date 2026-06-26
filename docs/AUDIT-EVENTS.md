# Audit Events

## Leave Support and Governance

| Event | Description |
| --- | --- |
| `SelfApprovalBlocked` | บล็อกการอนุมัติคำขอของตนเอง |
| `DirectorLeaveFallbackApplied` | ใช้ fallback approver สำหรับกรณี Director ลาเอง |
| `LeaveApproval.DelegationCreated` | สร้าง delegation |
| `LeaveApproval.DelegationUpdated` | แก้ไข delegation |
| `LeaveApproval.DelegationCancelled` | ยกเลิก delegation |
| `LeaveApproval.DelegationApplied` | ใช้ delegation ตอน resolve approver |
| `LeaveApproval.EscalationDetected` | ตรวจพบคำขอค้างอนุมัติ |
| `LeaveApproval.EscalationNotified` | แจ้งเตือนหรือส่งต่อ escalation |
| `LeaveApproval.OverrideApproved` | อนุมัติแทนผ่าน override flow |
| `LeaveApproval.OverrideRejected` | ไม่อนุมัติแทนผ่าน override flow |
| `LeaveBalance.Adjusted` | ปรับยอดวันลารายคนพร้อมเหตุผล |
| `LeaveBalance.RolloverPreviewed` | เปิด preview การยกยอดวันลารายคน |
| `LeaveBalance.RolloverConfirmed` | ยืนยันการยกยอดและสร้าง balance ปีงบประมาณใหม่ |
| `LeaveBalance.RolloverUpdatedExistingBalance` | ยืนยันการยกยอดโดยอัปเดตเฉพาะยอดยกมาของ balance ที่มีอยู่ |
| `LeaveBalance.RolloverBlocked` | บล็อกการยกยอดเพราะไม่ผ่านเงื่อนไข เช่น ประเภทลาไม่รองรับการยกยอด |

ทุก event ต้องมี actor ถ้ามี, target resource, result และ detail เพียงพอสำหรับ audit ย้อนหลัง
