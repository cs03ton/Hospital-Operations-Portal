# Dashboard Hub Manual Test Checklist

เอกสารนี้ใช้ตรวจสอบการปรับ Dashboard Hub ของ Hospital Operations Portal (HOP)

## Routes

- [ ] เข้า `/dashboard` แล้วเห็นหน้า Dashboard Hub
- [ ] Card “ระบบลา” แสดงสถานะ Active และปุ่ม “เปิด Dashboard”
- [ ] กด Card “ระบบลา” แล้วเข้า `/dashboard/leave`
- [ ] หน้า `/dashboard/leave` แสดงข้อมูล Dashboard ระบบลาเดิมครบ
- [ ] หน้า `/dashboard/leave` มีปุ่ม “กลับไป Dashboard Hub”
- [ ] หน้า `/dashboard/leave` แสดงปุ่ม “สร้างคำขอลา” เฉพาะผู้มีสิทธิ์ `LeaveRequest.Create`
- [ ] หน้า `/dashboard/leave` แสดงปุ่ม “งานรออนุมัติของฉัน” เฉพาะผู้มีสิทธิ์ `LeaveRequest.ViewPendingApproval`

## Permission

- [ ] Staff เห็น Dashboard Hub และ Card ระบบลา
- [ ] DepartmentHead เห็น Dashboard Hub และ Card ระบบลา
- [ ] Director เห็น Dashboard Hub, Card ระบบลา และเห็น Executive Dashboard เฉพาะเมื่อมี permission ที่กำหนด
- [ ] Admin เห็นเฉพาะ Dashboard ที่มี permission ของตนเอง
- [ ] SuperAdmin เห็นทุก Dashboard Card รวมถึง Coming soon/Planned
- [ ] User ที่ไม่มีสิทธิ์เข้า `/dashboard/vehicle` โดยตรง ต้องถูกส่งไปหน้า Access Denied

## Coming Soon

- [ ] Card ระบบจองรถ/ยืมรถ แสดง Badge “Coming soon” เมื่อผู้ใช้มีสิทธิ์เห็น
- [ ] Card ระบบแจ้งซ่อม แสดง Badge “Coming soon” เมื่อผู้ใช้มีสิทธิ์เห็น
- [ ] Card Inventory แสดง Badge “Coming soon” เมื่อผู้ใช้มีสิทธิ์เห็น
- [ ] Card Executive Dashboard แสดง Badge “Planned” ถ้ายังไม่ active
- [ ] ปุ่มของ Card ที่ยังไม่ active กดไม่ได้

## Sidebar / Breadcrumb

- [ ] Sidebar เมนู Dashboard ชี้ไปที่ `/dashboard`
- [ ] Sidebar เมนูแดชบอร์ดการลา ชี้ไปที่ `/dashboard/leave`
- [ ] เมื่ออยู่ `/dashboard` active เฉพาะ Dashboard Hub
- [ ] เมื่ออยู่ `/dashboard/leave` active เฉพาะแดชบอร์ดการลา
- [ ] Breadcrumb `/dashboard/leave` แสดง `Dashboard > ระบบลา`

## Responsive

- [ ] Desktop แสดง Card Grid หลายคอลัมน์
- [ ] Tablet แสดง Card Grid 2 คอลัมน์
- [ ] Mobile แสดง Card 1 คอลัมน์ และปุ่มไม่ล้นจอ

## Screenshot ที่ควร Capture ใหม่

- [ ] `/dashboard` สำหรับ Staff
- [ ] `/dashboard` สำหรับ SuperAdmin
- [ ] `/dashboard/leave`
- [ ] `/dashboard/vehicle` สำหรับ SuperAdmin
- [ ] หน้า Access Denied เมื่อเข้า route ที่ไม่มีสิทธิ์

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
