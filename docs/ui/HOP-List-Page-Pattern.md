# HOP List Page Pattern

มาตรฐาน list page อ้างอิง `LeaveManagementPage` และ shared `ManagementDataGrid`:

1. `PageHeader` พร้อม primary action
2. filter card; เมื่อ filter เปลี่ยนให้ reset `page=1`
3. query key ต้องรวม page, pageSize, filter และ sort
4. table desktop และ card mobileเมื่อข้อมูลหลายคอลัมน์
5. `ListPagination` ใช้เลขหน้าแบบ one-based ที่ boundary ของ component

URL state ใช้ `page`, `pageSize`, `search`, `status`, `sortBy`, `sortDirection` และ filter เฉพาะ domain โดยละค่าที่ว่าง ค่า default คือ page 1/pageSize 20 การกลับจาก detail ต้องใช้ URL เดิมหรือ `returnUrl` ที่ผ่านการ validate

ข้อความ empty ต้องแยก “ยังไม่มีข้อมูล” จาก “ไม่พบข้อมูลตามตัวกรอง” และ error ต้องมี retry action
