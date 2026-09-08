# HOP Fleet Participant Feedback Business Rules

- Feedback เป็น optional และไม่เปลี่ยน Fleet workflow/status
- Trip เป็น `COMPLETED` ได้โดยไม่ต้องมี Feedback
- ใช้ `Fleet:FeedbackWindowDays` เป็นระยะเวลาเปิดรับ ค่าเริ่มต้น 7 วัน
- เปิดรับตั้งแต่ `ActualEndAt` ถึง `ActualEndAt + FeedbackWindowDays` โดยรวมเวลาปลายเขต
- ผู้ส่งต้องเป็น actual `EMPLOYEE` participant และต้องไม่ใช่ driver ของ Trip
- Requester ที่ไม่ได้เดินทางจริงไม่มีสิทธิ์ส่ง Feedback
- External participant ยังส่ง Feedback ผ่าน HOP ไม่ได้ใน phase นี้
- Derived status คือ `AVAILABLE`, `SUBMITTED`, `EXPIRED`, `NOT_ELIGIBLE`; ห้ามเพิ่ม `WAITING_FOR_FEEDBACK` ใน Request status
- M1 ยังไม่มี feedback record ดังนั้น duplicate/submitted check จะเพิ่มพร้อม `fleet_trip_feedbacks` ใน M2
- Feedback content และ identity ห้ามส่งเข้า LINE Group
