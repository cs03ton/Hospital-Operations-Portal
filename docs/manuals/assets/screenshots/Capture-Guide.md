# HOP Phase 1 Capture Guide

เอกสารนี้กำหนดวิธี capture screenshot สำหรับคู่มือ HOP Phase 1 ให้ตรงกับสิทธิ์จริงของแต่ละ role และปลอดภัยต่อการเผยแพร่ภายในโรงพยาบาล

## Standard Environment

| รายการ | มาตรฐาน |
|---|---|
| Browser | Google Chrome |
| Zoom | 100% |
| Resolution | 1920 x 1080 |
| Theme | Light Mode |
| Sidebar | Expanded |
| Format | PNG |
| Data | Test data only |

## Test User Preparation

ใช้บัญชีทดสอบหรือ seed account เท่านั้น ห้ามใช้บัญชีบุคลากรจริง

Role mapping ปัจจุบัน:

| Catalog Role | System Role | Example Seed User |
|---|---|---|
| user | Staff | `staff01` |
| head | DepartmentHead | `head01` |
| director | Director | `director01` |
| hr | LeaveAdmin | ต้องสร้าง/กำหนด test account ที่มี role LeaveAdmin |
| superadmin | SuperAdmin | bootstrap admin หรือ test superadmin เท่านั้น |

หมายเหตุ: seed ที่พบใน `DevelopmentDataSeeder.cs` มี `staff01`, `staff02`, `head01`, `director01`, `admin_support` และ bootstrap admin ตาม config แต่ไม่พบ standard `hr.test` โดยตรง จึงควรสร้างเป็น seed/test account เท่านั้นก่อน capture HR

## Login By Role

1. เปิด Google Chrome
2. ไปที่ `/login`
3. กรอก username/password ของ test account
4. กด `เข้าสู่ระบบ`
5. ตรวจสอบว่า redirect ไป `/dashboard`
6. ตรวจสอบ role/เมนูที่เห็นก่อน capture

ห้าม capture หลังกรอก password ขณะที่ password field ยังแสดงอยู่ในภาพ

## Run Backend and Frontend

ตัวอย่างการรัน backend:

```powershell
dotnet run --project backend\Hop.Api\Hop.Api.csproj --urls http://localhost:5000
```

ตัวอย่างการรัน frontend:

```powershell
npm run dev -- --host 127.0.0.1 --port 5173
```

ตั้งค่า API base URL ให้ตรงกับ environment ของทีม QA ก่อนรันจริง

## Browser Setup

- ตั้ง Chrome zoom เป็น `100%`
- ตั้ง viewport/resolution เป็น `1920 x 1080`
- ใช้ Light Mode
- ขยาย sidebar ให้เป็น expanded
- ปิด browser notification
- ปิด password manager popup และ extension popup
- รอให้ข้อมูลโหลดเสร็จ ไม่มี loading/skeleton ค้าง

## Sensitive Data Handling

- ใช้ข้อมูลทดสอบเท่านั้น
- ห้ามใช้ข้อมูลผู้ป่วยหรือข้อมูลจริง
- ซ่อนชื่อบุคลากรจริง เบอร์โทรศัพท์ อีเมล LINE user id และข้อมูลส่วนบุคคล
- ห้ามแสดง password, token, secret, cookie หรือ connection string
- ถ้า audit log แสดง IP/user agent จริง ให้ mask ก่อนใช้ในคู่มือ

## Manual Capture

1. เปิด route ตาม `Screenshot-Catalog.md`
2. ตรวจสอบว่า role เข้าหน้าได้ตามสิทธิ์จริง
3. ตั้งหน้าให้แสดง state ที่ต้องการ เช่น Pending, Approved, Rejected หรือ Cancelled
4. Capture เป็น PNG
5. บันทึกในโฟลเดอร์ module ที่กำหนด เช่น `leave/leave-create.png`
6. อัปเดต `capture-status.csv`
7. ส่ง reviewer ตรวจความถูกต้อง

## Playwright Capture

1. สร้าง config จากตัวอย่าง:

```powershell
Copy-Item tests\\screenshots\\config\\screenshot-users.example.json tests\\screenshots\\config\\screenshot-users.json
```

2. แก้ `screenshot-users.json` ให้ใช้ test account เท่านั้น
3. รัน frontend/backend ให้พร้อม
4. รันจาก project root:

```powershell
npm run docs:screenshot
```

Playwright script จะ:

- ใช้ viewport `1920 x 1080`
- ปิด animation ด้วย CSS เท่าที่ทำได้
- login ทีละ role
- capture หน้า dashboard และ route ที่ role เข้าถึงได้จริงจาก catalog
- capture denied route บางรายการเพื่อยืนยัน guard
- บันทึกไฟล์ลง `docs/manuals/assets/screenshots/<module>/`

## Seed Required Screenshots

รายการที่ต้องมีข้อมูลเฉพาะ เช่น leave id, user id, department id หรือ role id ให้ mark เป็น `Seed Required` จนกว่าจะมีข้อมูลทดสอบที่ปลอดภัย

ตัวอย่าง:

- `leave-detail-approved.png`
- `approval-detail-head.png`
- `user-management/edit-user.png`
- `user-management/role-detail.png`

## Review Screenshot

Reviewer ต้องตรวจ:

- ภาพอ่านภาษาไทยชัดเจน
- route และ role ถูกต้อง
- menu visibility ตรงกับสิทธิ์จริง
- ไม่มีข้อมูลจริงหรือ credential
- ไม่มี popup จาก browser/extension
- ชื่อไฟล์ตรงกับ catalog
- screenshot ไม่ crop จนเสียบริบท

## Update Manual After Capture

1. เปลี่ยน `capture-status.csv` เป็น `Captured`
2. ใส่วันที่ capture และ reviewer
3. อัปเดต `Screenshot-Catalog.md` ถ้ามี route หรือ naming เปลี่ยน
4. นำ path ภาพไปแทน placeholder ใน Markdown manual
5. รัน docs build/check ตาม framework เดิมก่อนเผยแพร่



