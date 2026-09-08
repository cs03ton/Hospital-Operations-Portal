# HOP Responsive Pattern

- header/action: column ที่ `xs`, row ตั้งแต่ `sm`
- filter: 1 คอลัมน์ที่ `xs`, 2 ที่ `sm`, ตามความเหมาะสมที่ `md`
- table: horizontal scroll เป็น fallback; รายการ operational/driver ใช้ responsive card เมื่อข้อมูลสำคัญอ่านยาก
- action group: full-width บนมือถือ, sticky bottom เฉพาะ workflow ที่ต้องใช้งานภาคสนาม
- pagination toolbar wrap ได้และมี min-width ปลอดภัย
- ทดสอบอย่างน้อย 360x800, tablet และ desktop พร้อมยืนยันว่าไม่มี page-level horizontal overflow
