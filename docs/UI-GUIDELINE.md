# HOP UI Guideline

## Design Direction

HOP ใช้แนวทาง Premium Healthcare, Modern Hospital, Cozy Minimal โดยคง Deep Green เป็น primary และใช้ Warm Gold เป็น accent เฉพาะจุดสำคัญเท่านั้น

## Tokens

Source of truth:

```text
frontend/src/theme/palette.ts
frontend/src/theme/tokens.ts
frontend/src/theme/components.ts
```

### Colors

| Token | Value |
|---|---|
| primary | `#1F5E4F` |
| primary-hover | `#144338` |
| primary-soft | `#3C7A69` |
| secondary | `#8B6B4A` |
| accent | `#C8A96B` |
| accent-soft | `#D8BC82` |
| background | `#FAF8F2` |
| surface | `#FFFFFF` |
| border | `#E4E0D7` |
| text-primary | `#2C2C2C` |
| text-secondary | `#667085` |
| success | `#4CAF50` |
| warning | `#C8A96B` |
| danger | `#D9534F` |
| info | `#0284C7` |

### Spacing

ใช้ scale: `4, 8, 12, 16, 20, 24, 32, 40, 48`

### Radius

| Token | Usage |
|---|---|
| small | input, compact chip |
| medium | button, small panel |
| large | card/dialog |
| full | chip/avatar badge |

### Shadow

- subtle: hover/soft section
- card: dashboard card
- dialog: modal/dialog

## Cards

- Card ทั่วไปใช้พื้นขาว, border gray, radius 16
- ใช้ gold top border เฉพาะ card สำคัญหรือ dashboard panel
- ห้ามใส่ gold border รอบ card ทุกใบ
- Card grid ต้องใช้ `minmax(0, 1fr)` และ `alignItems: stretch`

## Tables

- Header ต้อง align ตามข้อมูล
- Action column อยู่ขวาสุด
- Long text ต้อง wrap หรือ truncate พร้อม tooltip
- Mobile/tablet ต้องมี horizontal scroll หรือ card layout
- Empty state ต้องใช้ข้อความไทยชัดเจน

## Filters

- ใช้ `FilterToolbar` เมื่อมีหลาย filter
- Filter ต้อง wrap บน tablet/mobile
- มีปุ่ม `ค้นหา` และ `ล้างตัวกรอง`
- หน้าที่ต้อง deep link ควร sync query string

## Status Badge

ใช้ `StatusBadge` หรือ mapping จาก `frontend/src/utils/statusLabels.ts`

ตัวอย่าง:

```tsx
<StatusBadge domain="leave" status={request.status} />
<StatusBadge domain="diagnostics" status={service.status} />
```

## Toast

ใช้ Notification service กลางเท่านั้น:

- success: งานสำเร็จ
- error: งานล้มเหลว ใช้ข้อความ sanitized
- warning: ควรตรวจสอบ
- info: แจ้งข้อมูลทั่วไป

## Dialog

Dialog ต้องมี:

- title ชัดเจน
- consequence ชัดเจน
- reason เมื่อเป็น approve/reject/restore/retention
- loading state
- prevent double submit
- danger style สำหรับ destructive action

ควรใช้ `ActionDialog` สำหรับ dialog ยืนยันแบบทั่วไป และใช้ `ConfirmDeleteDialog` สำหรับการลบ/ปิดใช้งานที่ต้องแสดงข้อมูลอ้างอิง

## Thai Wording Standard

| English | Thai |
|---|---|
| Draft | แบบร่าง |
| Pending | รออนุมัติ |
| InApproval | อยู่ระหว่างอนุมัติ |
| ReturnedForRevision | ตีกลับรอแก้ไข |
| Approved | อนุมัติแล้ว |
| Rejected | ไม่อนุมัติ |
| Cancelled | ยกเลิก |
| CancelledAfterApproval | ยกเลิกหลังอนุมัติ |

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
