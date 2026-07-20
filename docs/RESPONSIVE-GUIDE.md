# HOP Responsive Guide

## Breakpoints

ต้องตรวจอย่างน้อย:

- Mobile: 375px
- Tablet: 768px
- Small desktop: 1024px
- Desktop: 1440px

## Layout Rules

| Viewport | Rule |
|---|---|
| Desktop | max width 1440px, section gap 24px |
| Tablet | grid 3 columns ลดเหลือ 2 columns |
| Mobile | 1 column, primary button full width เมื่อเหมาะสม |

## Tables

- ถ้าคอลัมน์มากกว่า 5 ให้ใช้ horizontal scroll
- Action column ต้องไม่ตกขอบ
- Row text ต้อง wrap หรือ truncate
- ใช้ `DataTableCard` เป็นค่าเริ่มต้นสำหรับ table ใหม่

## Forms

- Mobile: field เต็มความกว้าง
- Tablet: 2 columns ได้เฉพาะข้อมูลไม่ยาว
- Submit action อยู่ท้าย form และ disable ระหว่าง submit

## Known Responsive Risks

| Page | Risk | Action |
|---|---|---|
| Diagnostics Center | log viewer ยาว | ใช้ scroll container |
| Backup Center | restore/retention tables | ควรใช้ horizontal scroll |
| Audit Log | filter จำนวนมาก | ใช้ FilterToolbar และ wrap |
| Leave Reports | export buttons/table | ควรจัด action group ให้ไม่ชนขอบ |
| LINE Settings | payload preview | ต้อง truncate/wrap |

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
