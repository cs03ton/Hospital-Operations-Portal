# Documentation Center

Documentation Center เป็นหน้าศูนย์คู่มือการใช้งานภายในระบบ HOP เพื่อให้ผู้ใช้แต่ละบทบาทเข้าถึงคู่มือที่เกี่ยวข้องจากหน้าเว็บโดยตรง

## Route

- `/docs` ศูนย์คู่มือ
- `/docs/:slug` รายละเอียดคู่มือ

## API

- `GET /api/docs`
- `GET /api/docs/{slug}`
- `PUT /api/docs/{slug}` เฉพาะ `Documentation.Manage`
- `GET /api/docs/{slug}/pdf`

## Permission

| Permission | ใช้สำหรับ |
|---|---|
| Documentation.View | ดูคู่มือทั่วไปตามบทบาท |
| Documentation.AdminView | ดูคู่มือผู้ดูแลระบบ |
| Documentation.Manage | เตรียมไว้สำหรับจัดการคู่มือในอนาคต |

## Markdown Source

ไฟล์อยู่ที่:

```text
docs/user-guide/
```

ไฟล์เริ่มต้น:

- `staff.md`
- `head.md`
- `director.md`
- `admin.md`
- `faq.md`
- `release-notes.md`

## Role Visibility

| คู่มือ | Staff | Head | Director | Admin/SuperAdmin |
|---|---:|---:|---:|---:|
| staff-guide | ✓ | ✓ | ✓ | ✓ |
| head-guide | - | ✓ | - | ✓ |
| director-guide | - | - | ✓ | ✓ |
| admin-guide | - | - | - | ✓ |
| faq | ✓ | ✓ | ✓ | ✓ |
| release-notes | - | - | - | ✓ |

## Security Rules

- Backend ใช้รายการ slug ที่กำหนดไว้ ไม่อ่าน path อิสระจาก user
- ป้องกัน path traversal เช่น `../../appsettings.json`
- ห้ามใส่ token, secret, password, connection string จริงใน Markdown
- Backend redact ค่า secret-like assignment เบื้องต้น
- Online editor จะปฏิเสธการบันทึกเมื่อพบ secret-like assignment เช่น `AccessToken=...`
- Frontend ไม่ render raw HTML แบบอันตราย

## Online Editor

ผู้ใช้ที่มี `Documentation.Manage` จะเห็นปุ่ม `แก้ไขคู่มือ` ในหน้า `/docs/:slug`

ข้อกำหนด:

- แก้ไขได้เฉพาะ slug ที่ระบบกำหนดไว้
- บันทึกกลับไปยัง Markdown file ใน `docs/user-guide`
- บันทึก audit event `Documentation.Updated`
- ห้ามใส่ secret จริงในเนื้อหา

## PDF Download

หน้า `/docs/:slug` มีปุ่ม `ดาวน์โหลด PDF`

Backend สร้าง PDF จาก Markdown ผ่าน QuestPDF และใช้ฟอนต์จาก config:

- `Documentation:PdfFontPath` หรือ `DOCUMENTATION_PDF_FONT_PATH`
- fallback ไปยัง `LeavePdf:FontPath` / `LEAVE_PDF_FONT_PATH`

## การเพิ่มคู่มือใหม่

1. สร้างไฟล์ Markdown ใน `docs/user-guide/`
2. เพิ่ม metadata slug/title/category/roles ใน `DocumentationService`
3. ตรวจ permission visibility
4. รัน `dotnet build`, `dotnet test`, `npm run build`
