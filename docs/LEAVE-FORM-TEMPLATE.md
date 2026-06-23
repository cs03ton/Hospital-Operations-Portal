# Leave Form Template

ระบบรองรับการสร้าง PDF แบบฟอร์มใบลาจาก template mapping สำหรับนำข้อมูลคำขอลาไปวางในตำแหน่งที่กำหนด

## Template Location

วางไฟล์ config ไว้ที่:

```text
storage/templates/leave/leave_form_template.json
```

สามารถ override path ได้ด้วย config:

```text
LeavePdf__TemplateConfigPath=/absolute/path/to/leave_form_template.json
```

## Supported Template Format

Phase นี้รองรับ JSON field mapping สำหรับสร้าง PDF A4:

- `staticText`: ข้อความคงที่ เช่น ชื่อฟอร์ม หัวข้อ ลายเซ็น
- `fields`: mapping ข้อมูลคำขอลาไปยังตำแหน่ง X/Y บนหน้า A4
- `approvalRows`: รูปแบบแถวประวัติการอนุมัติ

หมายเหตุ: หากโรงพยาบาลมี PDF/DOCX ฟอร์มจริง ให้เก็บไฟล์ไว้ในโฟลเดอร์นี้และใช้ JSON mapping เป็น source of truth สำหรับตำแหน่ง field ปัจจุบัน ระบบยังไม่ commit template ที่มีข้อมูลจริงของบุคลากร

## Supported Fields

| Field Key | Description |
|---|---|
| `hospitalName` | ชื่อโรงพยาบาลจาก configuration |
| `requestNumber` | เลขที่คำขอ |
| `requesterName` | ชื่อผู้ขอลา |
| `employeeCode` | รหัสพนักงาน |
| `position` | ตำแหน่ง/บทบาทจาก role ผู้ใช้งาน |
| `departmentName` | หน่วยงาน |
| `leaveTypeName` | ประเภทลา |
| `startDate` | วันที่เริ่มลา |
| `endDate` | วันที่สิ้นสุด |
| `totalDays` | จำนวนวัน |
| `durationType` | เต็มวัน / ครึ่งวันเช้า / ครึ่งวันบ่าย |
| `reason` | เหตุผล |
| `submittedAt` | วันที่ยื่นคำขอ |
| `status` | สถานะคำขอ |
| `currentApproverName` | ผู้อนุมัติปัจจุบัน |

## Approval Row Fields

ใช้ใน `approvalRows.format`:

| Placeholder | Description |
|---|---|
| `{{stepOrder}}` | ลำดับขั้น |
| `{{stepName}}` | ชื่อขั้นอนุมัติ |
| `{{approverName}}` | ชื่อผู้อนุมัติ |
| `{{status}}` | สถานะการอนุมัติ |
| `{{actionAt}}` | วันที่ดำเนินการ |
| `{{remark}}` | หมายเหตุ |

## Coordinate System

PDF ใช้ขนาด A4:

- จุดเริ่ม X/Y อยู่มุมซ้ายล่าง
- X เพิ่มไปทางขวา
- Y เพิ่มขึ้นด้านบน
- ขนาดหน้าโดยประมาณ 595 x 842 points

## Testing

1. สร้างคำขอลาและ submit/approve ตาม workflow
2. เปิดหน้ารายละเอียดคำขอลา
3. กด `ดาวน์โหลดแบบฟอร์มใบลา`
4. ตรวจว่า PDF มีข้อมูลคำขอและข้อมูลอนุมัติถูกต้อง

```powershell
curl.exe -L "http://localhost:5000/api/leave-requests/<leave-request-id>/pdf" `
  -H "Authorization: Bearer <token>" `
  -o leave-request.pdf
```
