# Leave Analytics Dashboard

เอกสารนี้อธิบายหน้า Leave Analytics สำหรับวิเคราะห์ข้อมูลการลาเชิงลึกของ HOP Phase 1.5

## Route และ Permission

- Frontend: `/reports/leave-analytics`
- Backend: `GET /api/reports/leave-analytics`
- Export: `GET /api/reports/leave-analytics/export-excel`
- ผู้มีสิทธิ์: `Director`, `Admin`, `SuperAdmin` หรือผู้มี permission `LeaveAnalytics.View` / `ReportManagement.View`

## ตัวกรองข้อมูล

| Filter | ความหมาย | ค่าเริ่มต้น |
|---|---|---|
| ปีงบประมาณ | ใช้ช่วง 1 ต.ค. - 30 ก.ย. | ปีงบประมาณปัจจุบัน |
| ปี | ใช้ดูแบบปีปฏิทิน | ว่าง ใช้ปีงบประมาณ |
| เดือน | ใช้เจาะเฉพาะเดือน | ว่าง คือทั้งปี |
| หน่วยงาน | กรองตาม department | ทุกหน่วยงาน |
| ประเภทลา | กรองตาม leave type | ทุกประเภทลา |
| สถานะ | กรองตามสถานะคำขอ | Approved |
| เฉพาะประเภทหลัก | ลาป่วย, ลากิจส่วนตัว, ลาพักผ่อน | เปิด |

## KPI

- จำนวนรายการลา: จำนวนคำขอที่ตรงกับตัวกรอง
- บุคลากรที่ลา: นับ `userId` แบบไม่ซ้ำ
- จำนวนวันลารวม: รวม `totalDays`
- ลาป่วยรวม: รวมวันของ `SICK_LEAVE`
- ลากิจรวม: รวมวันของ `PERSONAL_LEAVE`
- ลาพักผ่อนรวม: รวมวันของ `VACATION_LEAVE`
- หน่วยงานที่ลามากที่สุด: หน่วยงานที่มีจำนวนวันลารวมสูงสุด
- ประเภทลาที่ใช้มากที่สุด: ประเภทลาที่มีจำนวนวันลารวมสูงสุด

## Charts

- Monthly Trend: แสดงจำนวนวันลาและจำนวนรายการตามเดือน
- Stacked Bar by Department: Top 10 หน่วยงาน แยก stack ตามประเภทลาหลัก
- Pie / Donut by Leave Type: สัดส่วนวันลาแยกตามประเภทลา
- Heatmap: ความหนาแน่นการลาในแต่ละวัน สีเข้มหมายถึงมีผู้ลามาก

## Data Rules

- ค่าเริ่มต้นนับเฉพาะ `Approved`
- `Rejected` และ `Cancelled` ไม่ถูกนับ เว้นแต่ผู้ใช้เลือก filter สถานะนั้นเอง
- วันลาครึ่งวันใช้ค่า `totalDays = 0.5`
- Backend เก็บปีแบบ ค.ศ. แต่ frontend แสดงปีเป็น พ.ศ.
- ปีงบประมาณใช้ 1 ต.ค. - 30 ก.ย.

## Export

ปุ่ม `Export Excel` ส่งออกข้อมูลตาม filter ปัจจุบันเป็นไฟล์:

```text
leave-analytics-FY2569.xlsx
```

## Empty / Error State

- ถ้าไม่มีข้อมูล แสดง “ไม่พบข้อมูลการลาตามเงื่อนไขที่เลือก”
- ถ้าโหลดข้อมูลไม่สำเร็จ แสดง “ไม่สามารถโหลดข้อมูลวิเคราะห์การลาได้”
