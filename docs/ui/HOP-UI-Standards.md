# HOP UI Standards

ระบบลาในโค้ดปัจจุบันเป็น UI reference หลักของ HOP และใช้ MUI theme/token กลาง ห้ามกำหนดสี สถานะ ระยะห่าง หรือปุ่มแบบเฉพาะโมดูลเมื่อของกลางรองรับ

## Page anatomy

- ใช้ `PageHeader` สำหรับชื่อหน้าและคำอธิบาย โดยวาง primary action ด้านขวาบนและ stack แนวตั้งบนมือถือ
- list page เรียง header, summary (เมื่อมี), filter card, result card, responsive table/card และ pagination
- detail page มี back navigation, request identity, shared status badge, information sections, timeline และ action area ตามสิทธิ์
- form pageแบ่ง `FormSection`, แสดง validation ใต้ field และป้องกัน submit ซ้ำ

## Interaction states

ทุก data surface ต้องมี loading, empty, filtered-empty, error/retry และ permission-denied state ที่แยกกัน ไม่แสดง enum ภาษาอังกฤษต่อผู้ใช้ และ action ทำลายข้อมูลต้องยืนยันพร้อมเหตุผลตาม business rule

## Responsive and accessibility

ใช้ breakpoint จาก theme, touch target อย่างน้อย 44px, label ผูก input, icon button มี aria-label, focus มองเห็นได้ และ table ที่อ่านไม่ได้บนมือถือให้เปลี่ยนเป็น card list แทนการบีบคอลัมน์
