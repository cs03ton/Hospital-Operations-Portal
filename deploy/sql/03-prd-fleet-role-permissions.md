# เติมสิทธิ์ Fleet ให้บัญชี PRD เดิม

ไฟล์ `03-prd-fleet-role-permissions.sql` เพิ่ม permission ที่ขาดและผูกกับ role เดิม ไม่สร้างบัญชีหรือเปลี่ยน `user_roles` และไม่ต้องรัน master-data ทั้งไฟล์เพื่อเติมสิทธิ์

## วิธีรันใน DBeaver

1. สำรองฐานข้อมูลและเลือก connection/database PRD ที่ถูกต้อง
2. เปิด SQL Editor ใหม่ ตรวจว่าไม่มี transaction ค้าง หากมี transaction ที่ error ให้รัน `ROLLBACK;` ก่อน
3. เปิดไฟล์ SQL และใช้ Execute SQL Script ทั้งไฟล์ ตั้ง Error handling เป็น Stop และอย่าเปิดการ commit ทีละ statement; ไฟล์มี `BEGIN`/`COMMIT` เอง
4. หาก pre-check แจ้ง missing/inactive role หรือ permission ให้รัน `ROLLBACK;` และตรวจรายการที่รายงาน ห้ามข้าม pre-check; script ไม่เปิด role หรือ permission ที่ปิดไว้กลับให้อัตโนมัติ
5. ผล `missing_permission` ต้องไม่มีแถว และผล operations แสดงจำนวนเพิ่ม/ถอนจริง การรันซ้ำต้องเป็นศูนย์ทั้งหมด
6. ให้ผู้ใช้ออกจากระบบแล้วเข้าใหม่ ตรวจ Admin จัดการ Fleet, Staff สร้างคำขอ และคนขับดูงานของตน

## Matrix

| Role | จำนวนสิทธิ์ที่เติมให้ครบ | ขอบเขต |
|---|---:|---|
| Admin / SuperAdmin | 80 ต่อ role | Fleet administration ทั้งหมดตาม constants ปัจจุบัน |
| Staff | 11 | Requester, capability/compatibility, feedback ตนเอง, Calendar |
| DepartmentHead | 12 | Staff และดูระดับหน่วยงาน |
| Director | 18 | Requester, ViewAll, Director approval, Dashboard/Reports, feedback management |
| FleetAdminReviewer | 15 | Requester และ review, Dashboard |
| พนักงานขับรถ | 16 | งานและทริปของตน แนบไฟล์ feedback และ Calendar |
| LeaveAdmin | 2 | Feedback Create/ViewOwn ตาม migration เดิม |

คง custom grants และ custom roles ทั้งหมด ยกเว้นถอน `FleetRequest.ViewAll` ของ DepartmentHead ตาม corrective migration เดิม จำนวนจริงอาจมากกว่าตารางเมื่อมี custom grants หรือผู้ใช้ถือหลาย role; สิทธิ์ของผู้ใช้เป็นผลรวมของ role ที่เปิดใช้งาน

ใช้ `permissions.group_name` ตาม schema จริง รายการ permission อ้างอิง `FleetPermissions.cs` ทั้ง 80 ค่า ชื่อ permission ใหม่ใช้ code เพื่อให้อ่านตรงกับ API โดยไม่เขียนทับชื่อเดิม ส่วน mapping อ้างอิง seeder, PRD master-data และ Fleet corrective/feedback migrations

## ตรวจ Fleet rollout แยกต่างหาก

```sql
SELECT mode, uat_user_ids, uat_role_codes, created_at
FROM public.fleet_rollout_settings
ORDER BY created_at DESC
LIMIT 1;
```

แถวล่าสุดในฐานข้อมูล override environment ถ้าไม่มีแถวจึงใช้ `Fleet__RolloutMode` (default `Disabled`), `Fleet__UatUserIds__0` และ `Fleet__UatRoleCodes__0` ตาม configuration

- `Enabled`: เปิดใช้งานตาม permission
- `UATOnly`: ผู้ใช้ต้องอยู่ใน allowlist ด้วย
- `Disabled`: โมดูลยังถูกปิด แม้มี permission แล้ว

SQL seed ไม่เปลี่ยน rollout และไม่เพิ่ม driver profile; คนขับที่ยังไม่มี profile หรืองานมอบหมายอาจเห็นรายการว่างหลังได้สิทธิ์

## ผลทดสอบ Local

ทดสอบบน PostgreSQL 16 ในฐานจำลองแยกจากฐานแอป ใช้ตาราง RBAC ขั้นต่ำพร้อม unique/FK constraints: เติม mapping 235 คู่, คง custom role, ถอน ViewAll ของ DepartmentHead, รอบที่สองเพิ่ม/ถอน 0 และกรณี inactive permission หยุดก่อนเขียนข้อมูล ไม่ได้รันบน PRD
