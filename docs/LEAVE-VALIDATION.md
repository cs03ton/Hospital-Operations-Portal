# Leave Validation

Phase 2.1 moves leave day validation into backend services.

## Services

- `ILeaveCalendarService`
- `ILeaveValidationService`
- `IApprovalChainService`

## Rules

- End date must not be before start date.
- Leave requests cannot overlap existing `Pending` or `Approved` requests.
- Leave days are calculated from working days only.
- Saturday and Sunday are excluded.
- Active records in `leave_holidays` are excluded.
- Half-day leave is supported when start date and end date are the same and requested total days is `0.5`.
- Submit validates remaining leave balance before changing status to `Pending`.
- Leave types that require attachments cannot be submitted until at least one attachment exists.

## Thai Validation Messages

Backend returns clear Thai messages for user-facing leave validation, including:

- วันที่สิ้นสุดต้องไม่น้อยกว่าวันที่เริ่มลา
- ช่วงวันที่เลือกไม่มีวันทำการ กรุณาเลือกวันที่ลาใหม่
- ไม่สามารถขอลาซ้ำหรือทับซ้อนกับคำขอที่รออนุมัติหรืออนุมัติแล้ว
- ยอดวันลาคงเหลือไม่พอ
- ประเภทการลานี้ต้องแนบไฟล์ประกอบก่อนส่งคำขอ

## Related Tables

- `leave_requests`
- `leave_balances`
- `leave_holidays`
- `leave_attachments`
