# Responsive UI Audit

## Coverage

ตรวจ Phase 1, Phase 1.5 และ Fleet Phase 2 ด้วย role Staff, DepartmentHead, Director, Admin และ SuperAdmin ที่ viewport 375x812, 430x932, 768x1024, 1366x768 และ 1920x1080

## Findings and fixes

| Area | Finding | Resolution |
| --- | --- | --- |
| Mobile header | ข้อมูลชื่อเต็มถูกซ่อนตาม breakpoint และเคยทำให้เข้าใจว่าเมนูผู้ใช้หาย | ใช้ avatar button พร้อม accessible label และมีเมนูข้อมูลส่วนตัว เปลี่ยนรหัสผ่าน และออกจากระบบ |
| Fleet tables | Fleet shell บังคับ `overflow: hidden` ทำให้คอลัมน์ท้ายถูกตัด | เปลี่ยนเป็น horizontal scrolling แบบ touch |
| Management grids | ตารางผู้ใช้ หน่วยงาน บทบาท และสิทธิ์กว้างเกิน mobile | แสดง card list บน mobile โดยคงรายละเอียดและ action ทุกคอลัมน์ |
| Announcement, notification and fleet request tables | action อยู่คอลัมน์ท้ายและดูเหมือนหายเมื่อจอแคบ | ทำ action column แบบ sticky และคง horizontal scroll |
| Shared table cards | ผู้ใช้ไม่ทราบว่าตารางเลื่อนด้านข้างได้ | เพิ่มข้อความและไอคอนบอกการเลื่อนบน mobile |
| Toolbars | action ด้านขวาอาจแคบบน mobile | action ของ shared table card ขยายเต็มความกว้างและ wrap ตาม breakpoint |

## Automated verification

รัน `npm run e2e:responsive` โดยกำหนด `RESPONSIVE_QA_<ROLE>_USERNAME` และ `RESPONSIVE_QA_<ROLE>_PASSWORD` สำหรับบัญชี QA แต่ละ role สคริปต์ตรวจ route/permission matrix, page-level overflow, ตำแหน่งปุ่ม และ mobile user menu โดยไม่แก้ไขข้อมูล

การทดสอบ workflow ที่มี create/edit/approve/reject/delete ต้องใช้ UAT dataset แยก และไม่ควรรันกับ production
