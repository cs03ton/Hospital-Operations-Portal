# HOP Vehicle To-Be

## Target Process

1. พนักงาน active สร้าง/แก้ draft และส่งคำขอ
2. Dispatcher ตรวจคำขอ ดู recommendation และเลือกรถ/คนขับเอง
3. Administration Reviewer review หรือ return/reject
4. Director approve/return/reject
5. เมื่อ approve ระบบ enqueue In-App และ LINE โดยไม่ผูกผลส่งกับ transaction หลัก
6. คนขับ acknowledge, start และ complete trip
7. ระบบอัปเดตเลขไมล์ด้วย transaction และบันทึก audit

## Design Principles

- Request, assignment, approval action และ trip record แยก lifecycle
- ทุก write endpoint ตรวจ permission, ownership, current state และ concurrency
- availability คำนวณจากช่วงเวลาทับซ้อน ไม่อาศัย status flag อย่างเดียว
- overlap ใช้เงื่อนไข half-open interval: `existing.start < requested.end && existing.end > requested.start`
- assignment replacement ปิดรายการเดิมและสร้างรายการใหม่ใน transaction
- recommendation อธิบายเหตุผลพร้อม/ไม่พร้อม และไม่มี side effect
- ข้อมูลแสดงภาษาไทย แต่ persisted code เป็นค่าคงที่ที่ version ได้
- Phase 2.0 แสดงข้อมูลช่วยหมุนเวียนแต่ไม่คำนวณ fairness score และไม่ทำ compatibility matrix เต็มรูปแบบ
- Emergency Ambulance Workflow ถูกกันเป็น extension boundary สำหรับ Phase 2.1

## Scope Boundary

MVP ไม่ auto-assign, ไม่ทำ automatic escalation สำหรับงานเลยเวลา, ไม่ hard-delete transaction, ไม่ import โดย commit ทันที และไม่สร้าง employee master ซ้ำ
