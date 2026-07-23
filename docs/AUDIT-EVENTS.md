# Audit Events

## Phase 1 Core Events

| Event | Description |
| --- | --- |
| `Auth.LoginSuccess` | ผู้ใช้เข้าสู่ระบบสำเร็จ |
| `Auth.LoginFailed` | เข้าสู่ระบบไม่สำเร็จ |
| `Auth.LoginLocked` | บัญชีหรือ username ถูก rate limit/lock ชั่วคราวจากการ login ผิดหลายครั้ง |
| `PermissionDenied` | ผู้ใช้พยายามเรียก endpoint หรือ action ที่ไม่มีสิทธิ์ |
| `LeaveRequest.Created` | สร้างคำขอลา |
| `LeaveRequest.Submitted` | ส่งคำขอเข้าสู่ workflow อนุมัติ |
| `LeaveRequest.Approved` | อนุมัติคำขอลาใน current step |
| `LeaveRequest.Rejected` | ไม่อนุมัติคำขอลาใน current step |
| `LeaveRequest.Cancelled` | ผู้ขอยกเลิกคำขอลา |
| `LeaveRequest.PdfGenerated` | Generate/Download PDF ใบลา |
| `LeaveAttachment.Upload` | แนบไฟล์กับคำขอลาสำเร็จ |
| `LeaveAttachment.UploadFailed` | แนบไฟล์ไม่สำเร็จ |
| `UserProfile.Updated` | ผู้ใช้อัปเดตข้อมูลส่วนตัว |
| `UserProfile.ImageUploaded` | ผู้ใช้อัปโหลดรูปโปรไฟล์ |
| `AuditLog.Export` | Export audit log เป็น CSV |
| `AuditLog.ExportExcel` | Export audit log เป็น Excel |
| `AuditLog.ExportPdf` | Export audit log เป็น PDF |
| `AuditLog.RetentionRun` | รัน retention เพื่อลบ audit log ที่เกินอายุ |
| `Announcement.Created` | สร้างประกาศ |
| `Announcement.Updated` | แก้ไขประกาศ |
| `Announcement.Published` | เผยแพร่ประกาศ |
| `Announcement.Unpublished` | ยกเลิกการเผยแพร่ประกาศ |
| `Announcement.Archived` | จัดเก็บประกาศ |
| `Announcement.Cancelled` | ยกเลิกประกาศ |
| `Announcement.DeletedDraft` | ลบประกาศแบบร่าง |
| `Announcement.Duplicated` | คัดลอกประกาศ |
| `Announcement.Read` | ผู้ใช้เปิดอ่านรายละเอียดประกาศ |
| `Announcement.Acknowledged` | ผู้ใช้กดรับทราบประกาศ |
| `Announcement.NotificationPreviewed` | ผู้ดูแลตรวจ preview จำนวนผู้รับแจ้งเตือนก่อนเผยแพร่ |
| `Announcement.NotificationDispatched` | ระบบสร้าง Notification Bell หรือ LINE queue สำหรับประกาศสำเร็จ |
| `Announcement.NotificationSkipped` | ประกาศถูกเผยแพร่โดยไม่เลือกช่องทางแจ้งเตือนเพิ่มเติม |
| `Announcement.NotificationDispatchFailed` | ระบบสร้างรายการแจ้งเตือนประกาศไม่สำเร็จ |
| `Announcement.ScheduledPublished` | ระบบเผยแพร่ประกาศที่ตั้งเวลาไว้โดยอัตโนมัติ |

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
| `Line.TestMessageSent` | ส่งข้อความทดสอบ LINE สำเร็จ |
| `Line.TestMessageFailed` | ส่งข้อความทดสอบ LINE ไม่สำเร็จ หรือ config ไม่ครบ |

ทุก event ต้องมี actor ถ้ามี, target resource, result และ detail เพียงพอสำหรับ audit ย้อนหลัง

## Coverage Note

ควรตรวจ event ข้างต้นใน Phase 1 pilot test report หลังทำ manual workflow จริงอย่างน้อย 1 รอบ: create, submit, approve, reject, cancel, PDF download, attachment upload, login success/fail, permission denied, export และ retention run
