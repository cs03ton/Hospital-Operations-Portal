# LINE Approval UI

Route:

```text
/line/leave-approval/{leaveRequestId}?action=approve
/line/leave-approval/{leaveRequestId}?action=reject
```

Security rules:

- ต้อง login ก่อนเสมอ
- ถ้ายังไม่ login จะ redirect ไปหน้า login พร้อม return URL
- หลัง login จะกลับมายัง action เดิม
- หน้าเว็บไม่ auto approve/reject
- backend endpoint เดิมยังเป็น source of truth:
  - `POST /api/leave-requests/{id}/approve`
  - `POST /api/leave-requests/{id}/reject`
- requester approve ตัวเองไม่ได้
- user ที่ไม่ใช่ current approver จะถูกปฏิเสธ
- reject ต้องกรอกเหตุผลใน UI

Audit/delivery events:

- `LeaveApprovalActionOpened`
- `LeaveApprovedFromLineUi`
- `LeaveRejectedFromLineUi`

