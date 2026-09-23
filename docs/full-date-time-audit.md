# Full Date/Time Audit — HOP

วันที่ตรวจ: 23 กันยายน 2026

## ขอบเขตและมาตรฐาน

ตรวจ source, DTO, controller, service, entity/schema snapshot, UI, รายงาน และ integration ของ Authentication/Admin, พนักงาน, ประกาศ, การลา, Fleet, ห้องประชุม, แจ้งซ่อม, Notification/LINE, Audit, Backup/Restore และ Dashboard

| ชนิดข้อมูล | ค่าภายใน/API/DB | การแสดงผล |
| --- | --- | --- |
| DateOnly | `YYYY-MM-DD` ค.ศ. ไม่มี timezone | วัน/เดือน/พ.ศ. |
| Date+Time เหตุการณ์ | ISO 8601 พร้อม offset; backend/DB เป็น UTC | พ.ศ. และเวลา `Asia/Bangkok` |
| System timestamp | UTC | แปลงเฉพาะตอนแสดงต่อผู้ใช้ |
| ปีปฏิทิน | ค.ศ. 1900–2100; boundary รับ พ.ศ. 2443–2643 เฉพาะ field ที่ประกาศ | พ.ศ. |
| ปีงบประมาณ | ค่า business เดิมของระบบ (ค.ศ. ใน schema ปัจจุบัน) | แปลงเพื่อแสดง พ.ศ. แบบ explicit |

## ผลตรวจและการแก้ไข

| ระดับ | จุด | สาเหตุ/ผลกระทบ | การแก้ |
| --- | --- | --- | --- |
| Critical | JSON year converter | เคยตีความ `Year` และ `FiscalYear` ทุกแห่งเป็นปีปฏิทิน ทำให้ค่า business ถูกหัก 543 ได้ | จำกัด JSON conversion เฉพาะ field ปีปฏิทินที่ยืนยัน (`manufactureYear`); query calendar filters ใช้ allowlist แยก |
| Critical | Leave DateOnly | client ที่ส่ง 25xx บันทึกปี พ.ศ. และทำให้คำนวณวัน/ยอดผิด | normalize ก่อน validation ทั้ง JSON/query; ตรวจ leap day หลังแปลง; service ปฏิเสธปีนอกช่วง |
| High | Date range filters | ค่า `to` เวลา 00:00 ตัดข้อมูลวันสุดท้าย และขึ้นกับ timezone เครื่อง | Audit/Fleet emergency review รับ DateOnly และใช้ `[start-of-day, next-day)` ตาม Bangkok |
| High | Dashboard month/year | `new Date().getFullYear/getMonth` ใช้ timezone เครื่อง | ใช้ Bangkok clock ก่อนสร้าง query key และ API filter |
| High | datetime-local | browser ตีความตาม timezone เครื่อง | ใช้ `AppDateTimeInput`; local form หมายถึงเวลาโรงพยาบาลและส่ง UTC |
| High | Leave day count | form เคยเริ่มที่ 1 วันและอาจไม่ตรงวันทำงาน/วันหยุด | ใช้ policy preview จาก API และกฎ `LeaveCalendarService` |
| Medium | การแสดงวันที่กระจายหลายแห่ง | locale/calendar บางหน้าขึ้นกับเครื่องหรือแสดง ค.ศ. | เพิ่ม formatter กลาง frontend/backend; รายงาน PDF/Excel/LINE ที่แก้ใช้ พ.ศ. และ Bangkok |
| Medium | `DateTime.Today`/local time | วันปัจจุบันของ server อาจไม่ใช่วันไทย | งานที่แก้ใช้ `HospitalTime.Today` และ UTC conversion กลาง |
| Medium | Excel | วันที่อาจถูกเขียนเป็นข้อความจนคำนวณต่อไม่ได้ | เก็บ Excel serial แบบ Gregorian และกำหนดรูปแบบแสดง Buddhist calendar |

## จุดใช้งานหลักที่เปลี่ยน

- Frontend: `dateFormat.ts`, `AppDatePicker`, `AppDateTimeInput`, หน้าลา/ยอดลา/ปฏิทิน, Fleet calendar/request/delegation/emergency/maintenance/capability/report, Meeting Room, Announcement, Profile, Dashboard และหน้าผู้ดูแลที่เกี่ยวข้อง
- Backend boundary: `CalendarDateInput`, `CalendarInstantInput`, `CalendarYearInput`, `CalendarInputConfiguration`
- Backend business/display: `HospitalTime`, `ThaiDateDisplay`, leave validation/normalizer/PDF/report/calendar/balance/analytics, Fleet health/emergency filters, notifications และ exports
- Database: schema ใช้ `date` และ `timestamp with time zone` เป็นหลัก; integer year fields ต้องแยกความหมาย calendar/fiscal/manufacture ก่อนแก้ข้อมูล

## ฐานข้อมูลและ SQL

- `18-calendar-year-audit.sql`: audit ระบบลาและยอดที่เกี่ยวข้อง
- `19-calendar-year-detection.sql`: ตรวจ date/timestamp ปี 2443–2643 ทั่ว schema พร้อม PK, ค่าปัจจุบัน, ค่าที่เสนอ และ inventory ของ integer/text year/date fields
- `20-calendar-year-fix-template.sql`: template ซ่อมแบบ allowlist พร้อม expected-old-value guard; ค่าเริ่มต้นลงท้าย `ROLLBACK`
- `21-calendar-year-rollback-template.sql`: compensating rollback โดยต้องใช้หลักฐานค่าก่อนแก้

SQL ทั้งหมดไม่ได้เชื่อมต่อหรือแก้ PRD จากงานนี้ การแก้วันที่ลาเก่าต้องตรวจจำนวนวัน สถานะ cancellation และ `leave_balances` ร่วมกันก่อนอนุมัติรายการซ่อม

## การตรวจสอบ

- Backend: 423 passed, 15 skipped (ชุดที่ skip ต้อง PostgreSQL integration environment)
- Frontend: 148 passed
- ครอบคลุม input ค.ศ./พ.ศ., leap day, วันทำงาน/วันหยุด/ครึ่งวัน, fiscal boundary, UTC/Bangkok payload, mobile leave details และ report serialization
- `git diff --check` ผ่าน

## ความเสี่ยงที่เหลือ

1. ยังไม่ได้รัน detection/fix SQL กับ snapshot PostgreSQL ของ PRD จึงยังไม่มี affected count จริง
2. PostgreSQL integration tests ถูก skip เมื่อไม่มี test database; ต้องรันใน staging ก่อนใช้ SQL
3. ข้อความ/เลขเอกสารที่มีเลข 25xx ไม่ควรถูกแปลงอัตโนมัติ ต้องพิจารณาจากความหมาย field
4. ไฟล์ Excel ควรเปิดตรวจด้วย Microsoft Excel จริงอีกครั้ง เพราะ renderer อื่นอาจตีความ extended locale format ต่างกัน
5. งานภายนอกที่ส่ง ISO/CSV ยังคงใช้ ค.ศ. ตามสัญญา; การนำเข้าไฟล์ legacy ต้องผ่านตัวตรวจของแต่ละ importer ก่อนบันทึก
6. ยังมีโค้ดแสดงผลเดิมบางจุดที่บวก 543 โดยตรง แม้ไม่แตะค่าที่ส่งหรือจัดเก็บ ควรทยอยย้ายเข้าตัวจัดรูปแบบกลางเมื่อแก้หน้าดังกล่าวเพื่อลดภาระดูแล
