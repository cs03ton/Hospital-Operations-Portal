# HOP Vehicle Workflow

| Current | Action | Permission | Next |
|---|---|---|---|
| DRAFT | Submit | FleetRequest.Submit | PENDING_DISPATCH |
| PENDING_DISPATCH | Assign and forward | FleetDispatch.Assign | PENDING_ADMIN_REVIEW |
| PENDING_DISPATCH | Return | FleetDispatch.Return | RETURNED |
| PENDING_ADMIN_REVIEW | Forward | FleetAdminReview.Approve | PENDING_DIRECTOR_APPROVAL |
| PENDING_ADMIN_REVIEW | Return | FleetAdminReview.Return | RETURNED |
| PENDING_ADMIN_REVIEW | Reject | FleetAdminReview.Reject | REJECTED |
| PENDING_DIRECTOR_APPROVAL | Approve | FleetDirector.Approve | APPROVED |
| PENDING_DIRECTOR_APPROVAL | Return | FleetDirector.Return | RETURNED |
| PENDING_DIRECTOR_APPROVAL | Reject | FleetDirector.Reject | REJECTED |
| APPROVED | Acknowledge | FleetDriver.Acknowledge | DRIVER_ACKNOWLEDGED |
| DRIVER_ACKNOWLEDGED | Start | FleetDriver.Start | IN_PROGRESS |
| IN_PROGRESS | Complete | FleetDriver.Complete | COMPLETED |
| DRAFT ถึง PENDING_DIRECTOR_APPROVAL | Requester cancel | FleetRequest.Cancel | CANCELLED |
| APPROVED หรือ DRIVER_ACKNOWLEDGED | Dispatcher/Admin cancel | FleetDispatch.Reassign หรือ permission ที่กำหนดสำหรับ cancel | CANCELLED |
| RETURNED | Save revision | FleetRequest.EditOwn หรือ FleetDispatch.Assign | DRAFT/PENDING_DISPATCH ตาม return target |

ทุก action ใช้ transaction, current-state predicate, concurrency token, actor/time, audit และ outbox/queue notification หลัง commit. Approval history เดิมไม่ถูกแก้เมื่อ reassign หลังอนุมัติ

การ return ต้องระบุ `ReturnTarget`: `REQUESTER` สำหรับข้อมูลคำขอ และ `DISPATCHER` สำหรับข้อมูล assignment. Fleet Delegation ใช้ `ApprovalDelegation` แกนกลางโดย query `Scope=FLEET` และ permission ของ stage ที่ตรงกัน พร้อมตรวจ Fleet permission ของผู้รับมอบหมาย ณ เวลาดำเนินการ
