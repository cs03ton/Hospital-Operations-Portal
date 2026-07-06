# HOP Screenshot Automation

Playwright project นี้ใช้ capture screenshot จากระบบ Hospital Operations Portal (HOP) ที่กำลังรันอยู่จริงเท่านั้น เพื่อใช้ประกอบคู่มือใน `docs/manuals/phase1/`

## Rules

- ห้ามสร้าง mockup
- ห้ามใช้ AI-generated image
- ห้ามใช้ placeholder image
- ใช้ test account และ test data เท่านั้น
- ห้าม commit password จริง

## Setup

```powershell
Copy-Item tests\screenshots\config\screenshot-users.example.json tests\screenshots\config\screenshot-users.json
```

แก้ `tests/screenshots/config/screenshot-users.json` ให้ใช้ test-only credentials และ seed ids ที่ปลอดภัย

## Run

จาก project root:

```powershell
npm run docs:screenshot
```

## Output

- PNG screenshots: `docs/manuals/assets/screenshots/`
- Index: `docs/manuals/assets/screenshots/Screenshot-Index.md`
- Capture report: `docs/manuals/assets/screenshots/Capture-Report.md`
- Manual mapping: `docs/manuals/assets/screenshots/Manual-Screenshot-Mapping.md`

หลัง capture เสร็จ global teardown จะอัปเดต Markdown ใน `docs/manuals/phase1/` โดยแทน placeholder `[ใส่รูปภาพ: ...]` เฉพาะรายการที่มีไฟล์ภาพจริงแล้วเท่านั้น
