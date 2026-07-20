# HOP Accessibility Guide

## Baseline

HOP ต้องรองรับ keyboard navigation, screen reader และ contrast ที่อ่านง่ายสำหรับผู้ใช้งานโรงพยาบาล

## Checklist

- [ ] ปุ่ม icon-only มี `aria-label` หรือ tooltip
- [ ] Form field มี label ชัดเจน
- [ ] Error message อยู่ใกล้ field ที่ผิด
- [ ] Dialog focus อยู่ใน dialog
- [ ] ปุ่มหลักกดด้วย keyboard ได้
- [ ] สี status ไม่เป็นสื่อเดียว ต้องมีข้อความ label
- [ ] Table header ใช้ `TableHead`
- [ ] Toast/error มีข้อความไทยอ่านเข้าใจ
- [ ] Production ไม่แสดง stack trace

## Status Badge

ใช้ `StatusBadge` เพื่อให้ทุกสถานะมีข้อความ ไม่พึ่งสีเพียงอย่างเดียว

## Contrast

Gold accent ใช้กับ border/icon/accent เท่านั้น หลีกเลี่ยงการใช้เป็นพื้นหลังใหญ่กับข้อความเล็ก

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
