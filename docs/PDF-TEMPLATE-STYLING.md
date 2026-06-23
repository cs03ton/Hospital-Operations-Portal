# PDF Template Styling

เอกสารนี้อธิบายวิธีตั้งค่ารูปแบบตัวอักษรของเอกสาร PDF โดยไม่ต้องแก้ source code

## Template Config

ไฟล์หลัก:

```text
storage/templates/leave/leave_form_template.json
```

หรือกำหนด path เองด้วย:

```text
LeavePdf__TemplateConfigPath=/absolute/path/to/leave_form_template.json
```

## Document Template Settings

กำหนดค่า default ทั้งเอกสารด้วย `documentSettings`

```json
{
  "documentSettings": {
    "fontFamily": "TH Sarabun New",
    "fontSize": 16,
    "lineHeight": 1.2
  }
}
```

Default ที่ระบบใช้เมื่อไม่กำหนดค่า:

| Setting | Default |
|---|---|
| Font Family | `TH Sarabun New` |
| Font Size | `16` pt |
| Line Height | `1.2` |

## Field Override

แต่ละ field สามารถ override ค่า font ได้:

```json
{
  "key": "requesterName",
  "label": "ชื่อผู้ขอลา",
  "x": 70,
  "y": 674,
  "fontSize": 16
}
```

ตัวอย่าง:

```json
{
  "key": "reason",
  "label": "เหตุผล",
  "x": 70,
  "y": 532,
  "fontSize": 14
}
```

## Static Text Override

หัวเอกสารหรือข้อความคงที่สามารถกำหนดขนาดเฉพาะได้:

```json
{
  "text": "แบบฟอร์มใบลา",
  "x": 50,
  "y": 790,
  "fontSize": 18
}
```

## Approval Rows Styling

แถวข้อมูลการอนุมัติรองรับ:

- `fontFamily`
- `fontSize`
- `lineHeight`
- `rowHeight`

ถ้าไม่กำหนด `rowHeight` ระบบจะคำนวณจาก:

```text
fontSize * lineHeight
```

ตัวอย่าง:

```json
{
  "approvalRows": {
    "x": 70,
    "startY": 374,
    "fontSize": 14,
    "lineHeight": 1.2,
    "maxRows": 10,
    "format": "{{stepOrder}}. {{stepName}} | {{approverName}} | {{status}} | {{actionAt}} | {{remark}}"
  }
}
```

## Notes

- Source code ไม่ต้องแก้เมื่อเปลี่ยน font size หรือ line height
- ถ้า field ไม่กำหนดค่า จะใช้ `documentSettings`
- PDF writer ปัจจุบันใช้ font resource name จาก `fontFamily` ใน template config
- ควรใช้ font ภาษาไทย เช่น `TH Sarabun New` เพื่อให้เอกสารอ่านง่าย
