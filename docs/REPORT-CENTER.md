# Report Center

Report Center คือกลุ่มหน้ารายงานของ HOP Phase 1 และ Phase 1.5

## รายงานที่เปิดใช้

| Report | Route | API | Permission |
|---|---|---|---|
| รายงานการลา | `/reports/leaves` | `/api/reports/leaves` | `ReportManagement.View` |
| วิเคราะห์ข้อมูลการลา | `/reports/leave-analytics` | `/api/reports/leave-analytics` | `LeaveAnalytics.View` หรือ `ReportManagement.View` |

## Leave Analytics

หน้า Leave Analytics ใช้สำหรับวิเคราะห์ข้อมูลการลาเชิงลึก เช่น:

- แนวโน้มรายเดือน
- หน่วยงานที่มีการลามาก
- สัดส่วนประเภทลา
- Heatmap ความหนาแน่นการลา
- Export Excel ตาม filter

ดูรายละเอียดเพิ่มเติมที่ `docs/LEAVE-ANALYTICS.md`

## Security

- Backend enforce permission จริง ไม่พึ่ง frontend guard อย่างเดียว
- Staff ปกติไม่สามารถเข้าหน้า analytics ได้
- Export ใช้ permission เดียวกับการดู analytics/report
