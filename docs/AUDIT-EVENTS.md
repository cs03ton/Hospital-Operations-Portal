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

ทุก event ต้องมี actor ถ้ามี, target resource, result และ detail เพียงพอสำหรับ audit ย้อนหลัง
