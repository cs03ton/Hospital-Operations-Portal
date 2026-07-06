# HOP Phase 1 Screenshot Assets

โฟลเดอร์นี้ใช้เก็บ screenshot สำหรับคู่มือ Hospital Operations Portal (HOP) Phase 1 โดยอ้างอิงสิทธิ์จริงจาก frontend routes/menu guards และ backend role/permission seed

## Folder Structure

```text
docs/manuals/assets/screenshots/
├── common/
├── login/
├── dashboard/
├── leave/
├── approval/
├── user-management/
├── hr/
├── director/
└── superadmin/
```

## Key Files

- `Role-Permission-Matrix.md` สรุป role, route, menu visibility, backend permission, API endpoint, access และ evidence
- `Screenshot-Catalog.md` รายการ screenshot ที่ต้อง capture ตาม role จริง
- `Capture-Guide.md` วิธีเตรียมข้อมูลและ capture แบบ manual/Playwright
- `capture-status.csv` ไฟล์ติดตามสถานะ capture/review
- `Findings.md` สรุปประเด็น QA/security/documentation ที่พบ

## Screenshot Standard

- Browser: Google Chrome
- Resolution: `1920 x 1080`
- Zoom: `100%`
- Theme: Light Mode
- Sidebar: Expanded
- Format: PNG
- Data: test data only

## Security and Privacy Standard

- ห้ามใช้ข้อมูลผู้ป่วยหรือข้อมูลจริง
- ห้าม capture password, token, secret, cookie หรือ credential
- ซ่อนข้อมูลส่วนบุคคลก่อนนำภาพเข้าเอกสาร
- ปิด browser notification, password manager popup และ extension popup
- ใช้บัญชีทดสอบเท่านั้น เช่น `staff01`, `head01`, `director01` หรือบัญชี seed/test account ที่กำหนดโดยทีม QA
- ห้าม commit `tests/screenshots/config/screenshot-users.json` ที่มี password จริง

## Naming Convention

ใช้ตัวพิมพ์เล็ก คั่นคำด้วย hyphen และลงท้าย `.png`

ตัวอย่าง:

- `login/login-01-page.png`
- `dashboard/dashboard-user.png`
- `dashboard/dashboard-head.png`
- `dashboard/dashboard-director.png`
- `dashboard/dashboard-hr.png`
- `dashboard/dashboard-superadmin.png`
- `leave/leave-create.png`
- `leave/leave-detail-pending.png`
- `approval/approval-pending-list.png`
- `hr/leave-balance.png`
- `user-management/user-list.png`
- `superadmin/audit-log.png`

## Capture Status

อัปเดต `capture-status.csv` ทุกครั้งหลัง capture หรือ review โดยใช้ค่า:

- `Pending`
- `Captured`
- `Skipped`
- `Blocked`

Capture method ที่ใช้ใน catalog:

- `Manual`
- `Playwright`
- `Seed Required`

## Playwright Automation

Automation project อยู่ที่ `tests/screenshots/`

ก่อนรัน ให้สร้าง config จาก example:

```powershell
Copy-Item tests\\screenshots\\config\\screenshot-users.example.json tests\\screenshots\\config\\screenshot-users.json
```

จากนั้นแก้ `screenshot-users.json` ให้เป็น test account เท่านั้น แล้วรันจาก project root:

```powershell
npm run docs:screenshot
```

ถ้า route ต้องใช้ข้อมูลเฉพาะ เช่น leave id, user id, department id หรือ role id ให้เตรียม seed/test data ก่อน แล้วอัปเดต catalog เป็น `Captured` หลังตรวจภาพแล้ว




