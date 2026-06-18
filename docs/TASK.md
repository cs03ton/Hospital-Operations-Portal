อ่านไฟล์ต่อไปนี้ก่อนเริ่มงาน

* docs/SETUP-PROJECT.md
* docs/PROJECT_SUMMARY.md
* docs/DEVELOPMENT_ROADMAP.md
* docs/LEAVE-DESIGN.md
* docs/LEAVE-MODULE.md
* docs/APPROVAL-WORKFLOW.md
* docs/APPROVAL-CHAIN.md
* docs/PERMISSION-POLICY.md
* docs/LINE-INTEGRATION-PLAN.md
* docs/TASK.md

จากนั้นดำเนินการตามงานใน docs/TASK.md

เป้าหมายของงานนี้คือทำ Leave Management Module ให้พร้อมใช้งานจริงในโรงพยาบาล

ขอบเขตงาน

1. Leave Form Generation
2. PDF Export
3. Approval Workflow UI
4. Approval History
5. LINE Messaging Integration
6. Dashboard Integration
7. Leave Calendar
8. Leave Balance Dashboard

---

Leave Form Requirements

สร้างแบบฟอร์มลาในรูปแบบ A4

รองรับ

* ลาพักผ่อน
* ลาป่วย
* ลากิจ
* ลาคลอด
* ประเภทลาอื่น

ข้อมูลที่ต้องแสดง

* ชื่อผู้ขอลา
* ตำแหน่ง
* หน่วยงาน
* ประเภทลา
* วันที่ลา
* จำนวนวัน
* เหตุผล
* ลายเซ็นผู้ขอลา
* สถานะการอนุมัติ
* ผู้อนุมัติแต่ละขั้น

สร้าง

* HTML Template
* PDF Template

---

PDF Requirements

Create endpoint

GET /api/leave-requests/{id}/pdf

Requirements

* Generate PDF from leave request
* รองรับภาษาไทย
* แสดงโลโก้โรงพยาบาล
* แสดงชื่อโรงพยาบาลจาก configuration
* รองรับดาวน์โหลด PDF

---

Approval Workflow UI

Create pages

* /leave/pending-approvals
* /leave/my-requests

Features

* Approve
* Reject
* Approval Comment
* Approval History
* Current Approval Step
* Approval Timeline

---

Leave Calendar

Create page

* /leave/calendar

Features

* Monthly View
* Department Filter
* Leave Type Filter
* Color By Leave Type
* Show Approved Leave
* Show Pending Leave

UI Language

* ภาษาไทยทั้งหมด

---

Dashboard Integration

Update dashboard

Add cards

* จำนวนคำขอลารออนุมัติ
* จำนวนผู้ลาวันนี้
* จำนวนผู้ลาสัปดาห์นี้
* จำนวนผู้ลาเดือนนี้
* วันลาคงเหลือของผู้ใช้

---

LINE Messaging Integration

Implement actual LINE Messaging API

Requirements

* ส่งแจ้งเตือนเมื่อ submit leave
* ส่งแจ้งเตือนเมื่อ approve
* ส่งแจ้งเตือนเมื่อ reject
* ส่งแจ้งเตือนเมื่อ cancel

Create

* ILineMessagingService
* LineMessagingService

Use configuration

* LINE_CHANNEL_SECRET
* LINE_ACCESS_TOKEN

Implement

* Retry Policy
* Delivery Audit
* Error Logging

Create database table

line_delivery_logs

Fields

* id
* request_id
* recipient
* status
* response_code
* response_message
* sent_at

---

Notification Templates

Create templates

* Leave Submitted
* Leave Approved
* Leave Rejected
* Leave Cancelled

All notification messages must be Thai language.

---

Audit Requirements

Log events

* Leave Created
* Leave Submitted
* Leave Approved
* Leave Rejected
* Leave Cancelled
* Attachment Uploaded
* PDF Generated
* LINE Sent

---

Frontend Requirements

UI must use Thai language.

Use

* Material UI
* React Query
* React Hook Form

Keep healthcare theme.

Use hospital logo from:

frontend/src/assets/logo

---

Documentation Requirements

Create

* docs/LEAVE-PDF.md
* docs/LINE-MESSAGING.md
* docs/LEAVE-CALENDAR.md

Update

* docs/LEAVE-MODULE.md
* docs/DEVELOPMENT_ROADMAP.md
* README.md

---

Completion Criteria

The task is complete when:

* User can submit leave request
* Approval workflow works
* PDF can be generated
* Leave calendar works
* Dashboard shows leave data
* LINE notification works
* Audit log records leave actions
* Documentation is updated

เมื่อเสร็จแล้วให้สรุป

1. ไฟล์ที่สร้างใหม่
2. ไฟล์ที่แก้ไข
3. Database migration ที่เพิ่ม
4. API endpoint ที่เพิ่ม
5. หน้า Frontend ที่เพิ่ม
6. LINE integration ที่เพิ่ม
7. Audit events ที่เพิ่ม
8. คำสั่งสำหรับทดสอบ PDF
9. คำสั่งสำหรับทดสอบ LINE
10. งานถัดไปที่แนะนำ
