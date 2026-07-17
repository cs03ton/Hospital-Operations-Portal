# Report Center

Report Center คือกลุ่มหน้ารายงานของ HOP Phase 1 และ Phase 1.5

## รายงานที่เปิดใช้

| Report | Route | API | Permission |
|---|---|---|---|
| รายงานการลา | `/reports/leaves` | `/api/reports/leaves` | `ReportManagement.View`, `LeaveAnalytics.View`, หรือ role `Director` / `Admin` / `SuperAdmin` |
| วิเคราะห์ข้อมูลการลา | `/reports/leave-analytics` | `/api/reports/leave-analytics` | `LeaveAnalytics.View`, `ReportManagement.View`, หรือ role `Director` / `Admin` / `SuperAdmin` |

## Leave Analytics

หน้า Leave Analytics ใช้สำหรับวิเคราะห์ข้อมูลการลาเชิงลึก เช่น:

- แนวโน้มรายเดือน
- หน่วยงานที่มีการลามาก
- สัดส่วนประเภทลา
- Heatmap ความหนาแน่นการลา
- Export Excel ตาม filter

ดูรายละเอียดเพิ่มเติมที่ `docs/LEAVE-ANALYTICS.md`

## Report vs Analytics

- `รายงานการลา` ใช้สำหรับดูรายการข้อมูลเชิงปฏิบัติการ เช่น คำขอลา ยอดวันลา และ export รายการเพื่อส่งต่อ/ตรวจสอบ
- `วิเคราะห์ข้อมูลการลา` ใช้สำหรับมุมมองผู้บริหาร เช่น KPI, trend, breakdown ตามหน่วยงาน, heatmap และ insight เชิงเปรียบเทียบ
- สองหน้านี้ใช้ข้อมูลชุดเดียวกันบางส่วน แต่เจตนาใช้งานต่างกัน: report คือ “รายการตรวจสอบ/ส่งออก”, analytics คือ “สรุปแนวโน้มเพื่อการตัดสินใจ”

## Leave Report UI

หน้า `รายงานการลา` ปัจจุบันมี:

- ตัวกรองช่วงวันที่
- ตัวกรองหน่วยงาน
- ตัวกรองประเภทลา
- ปุ่ม `ล้าง`, `Excel`, `PDF` ในรูปแบบปุ่มมาตรฐานเดียวกัน
- ส่วน `รายงานคำขอยกเลิกใบลา` พร้อมปุ่ม `ดูทั้งหมด`
- ส่วน `รายการคำขอลา` พร้อมปุ่ม `ดูทั้งหมด`

การกด `ดูทั้งหมด` ในรายงานคำขอยกเลิกใบลาจะไปที่ `/leave/cancellations` ส่วนรายการคำขอลาปกติจะไปที่ `/leave`

## Security

- Backend enforce permission จริง ไม่พึ่ง frontend guard อย่างเดียว
- Staff ปกติไม่สามารถเข้าหน้า analytics ได้
- Export รายงานการลาใช้ `ReportManagement.Export` หรือ role ผู้บริหาร/ผู้ดูแลที่ได้รับอนุญาตตาม backend policy
