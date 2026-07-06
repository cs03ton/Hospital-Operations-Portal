# HOP Documentation Framework

ระบบเอกสารนี้เป็น Docs as Code สำหรับคู่มือ Hospital Operations Portal (HOP) ของโรงพยาบาลนาหมื่น โดยใช้ Markdown ใน `docs/manuals/phase1/` เป็น source หลัก และ build ออกเป็น PDF, DOCX และ HTML ได้จาก PowerShell scripts

## โครงสร้าง

```text
docs/manuals/
├── phase1/                 # Markdown source ของคู่มือ Phase 1
├── assets/                 # รูป โลโก้ ไอคอน screenshot diagram illustration
├── templates/              # metadata, LaTeX template, DOCX reference
├── pdf/                    # PDF output
├── docx/                   # DOCX output
├── html/                   # HTML output
└── scripts/                # build และตรวจสอบเอกสาร
```

## คำสั่งหลัก

ติดตั้งหรือแนะนำติดตั้งเครื่องมือ:

```powershell
.\docs\manuals\scripts\install-tools-windows.ps1
```

ตรวจสอบ dependency และ link เบื้องต้น:

```powershell
.\docs\manuals\scripts\check-docs.ps1
```

Build ทั้งหมด:

```powershell
.\docs\manuals\scripts\build-all.ps1
```

Build แยกประเภท:

```powershell
.\docs\manuals\scripts\build-pdf.ps1
.\docs\manuals\scripts\build-docx.ps1
.\docs\manuals\scripts\build-html.ps1
```

## Output

- `docs/manuals/pdf/HOP-Phase1-User-Manual.pdf`
- `docs/manuals/docx/HOP-Phase1-User-Manual.docx`
- `docs/manuals/html/index.html`
- ถ้ามี `11-Quick-Start-One-Page.md` จะสร้าง Quick Start PDF/DOCX แยกให้ด้วย

## Dependency

ต้องมี:

- Pandoc
- LaTeX engine สำหรับ PDF เช่น MiKTeX พร้อม `xelatex`

แนะนำให้มี:

- `wkhtmltopdf` หรือ `weasyprint` สำหรับ HTML/PDF fallback ในอนาคต
- Node.js
- Mermaid CLI (`mmdc`) ถ้ามี diagram แบบ Mermaid
- Google Chrome หรือ Microsoft Edge สำหรับการตรวจ HTML ด้วย browser

## หมายเหตุเรื่องภาษาไทย

PDF ใช้ `xelatex` และ template `docs/manuals/templates/hop-template.tex` โดยตั้งค่า font เป็น `TH Sarabun New` และ fallback เป็น `TH SarabunPSK` ถ้าเครื่องไม่มี font เหล่านี้ LaTeX จะ fallback เป็น `Tahoma` ซึ่งอ่านภาษาไทยได้แต่รูปแบบอาจไม่ตรงตามมาตรฐานเอกสารราชการเท่า TH Sarabun

DOCX ใช้ `docs/manuals/templates/hop-reference.docx` เป็น reference style เพื่อควบคุม Heading, Body และ Table ใน Microsoft Word

## แนวทางเพิ่ม Phase ถัดไป

สร้างโฟลเดอร์ใหม่ เช่น `docs/manuals/phase2/` แล้ววาง Markdown เรียงตามชื่อไฟล์ จากนั้นสามารถปรับ script ให้รับ parameter `-Phase phase2` ได้ต่อยอดจากโครงสร้างเดิมโดยไม่ต้องเปลี่ยน source Markdown ของ Phase 1
