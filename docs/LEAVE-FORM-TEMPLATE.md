# Leave Form Template

ระบบรองรับการสร้าง PDF แบบฟอร์มใบลาจาก template mapping สำหรับนำข้อมูลคำขอลาไปวางในตำแหน่งที่กำหนด

## Standard Template

ชื่อ template:

```text
Universal Leave Form
Version: 1.0
```

ฟอร์มกลางนี้รองรับการลาทุกประเภทในเอกสารเดียว โดยใช้ checkbox สำหรับประเภทลาและช่วงเวลาลา ระบบเติมข้อมูลจากคำขอลา ข้อมูลส่วนตัวของผู้ใช้ ยอดวันลา ไฟล์แนบ และสายอนุมัติอัตโนมัติ

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
- `documentSettings`: ค่า font เริ่มต้นของเอกสาร เช่น font family, font size, line height

หมายเหตุ: หากโรงพยาบาลมี PDF/DOCX ฟอร์มจริง ให้เก็บไฟล์ไว้ในโฟลเดอร์นี้และใช้ JSON mapping เป็น source of truth สำหรับตำแหน่ง field ปัจจุบัน ระบบยังไม่ commit template ที่มีข้อมูลจริงของบุคลากร

## Font Settings

ค่าเริ่มต้น:

```json
{
  "documentSettings": {
    "fontFamily": "TH SarabunPSK",
    "fontSize": 16,
    "lineHeight": 1.2
  }
}
```

แต่ละ field สามารถ override `fontFamily`, `fontSize`, และ `lineHeight` ได้ อ่านรายละเอียดที่:

```text
docs/PDF-TEMPLATE-STYLING.md
```

## Supported Fields

| Field Key | Description |
|---|---|
| `hospitalName` | ชื่อโรงพยาบาลจาก configuration |
| `requestNumber` | เลขที่คำขอ |
| `requesterName` | ชื่อผู้ขอลา |
| `employeeCode` | รหัสพนักงาน |
| `position` | ตำแหน่ง/บทบาทจาก role ผู้ใช้งาน |
| `departmentName` | หน่วยงาน |
| `phoneNumber` | เบอร์โทรศัพท์จากข้อมูลส่วนตัว |
| `email` | อีเมล ถ้ายังไม่มีข้อมูลจะแสดง `-` |
| `leaveContactAddress` | ที่อยู่ระหว่างลา / ที่อยู่ติดต่อ |
| `leaveTypeName` | ประเภทลา |
| `leaveTypeCheckboxLine1` | checkbox ลาป่วย/ลากิจ/ลาพักผ่อน/ลาคลอด/ลาอุปสมบท |
| `leaveTypeCheckboxLine2` | checkbox ประเภทลาอื่น ๆ |
| `startDate` | วันที่เริ่มลา |
| `endDate` | วันที่สิ้นสุด |
| `totalDays` | จำนวนวัน |
| `durationType` | เต็มวัน / ครึ่งวันเช้า / ครึ่งวันบ่าย |
| `durationCheckboxes` | checkbox เต็มวัน/ครึ่งวันเช้า/ครึ่งวันบ่าย |
| `workingDays` | จำนวนวันทำการในช่วงลา |
| `holidayDays` | จำนวนวันหยุดราชการในช่วงลา |
| `weekendDays` | จำนวนวันเสาร์-อาทิตย์ในช่วงลา |
| `balanceBefore` | วันลาคงเหลือก่อนลา |
| `balanceUsedThisRequest` | จำนวนวันที่ใช้ครั้งนี้ |
| `balancePending` | จำนวนวันที่รออนุมัติ |
| `balanceAfterApproval` | คงเหลือหลังอนุมัติ |
| `reason` | เหตุผล |
| `attachmentCheckboxes` | checkbox เอกสารแนบ |
| `attachmentCount` | จำนวนไฟล์แนบ |
| `submittedAt` | วันที่ยื่นคำขอ |
| `status` | สถานะคำขอ |
| `currentApproverName` | ผู้อนุมัติปัจจุบัน |
| `finalApprovalCheckboxes` | ผลการพิจารณา อนุมัติ/ไม่อนุมัติ |
| `finalApproverName` | ผู้อนุมัติขั้นสุดท้าย |
| `finalActionAt` | วันที่อนุมัติ/ไม่อนุมัติขั้นสุดท้าย |
| `finalRemark` | เหตุผลหรือหมายเหตุขั้นสุดท้าย |
| `headApproverName` | ชื่อหัวหน้าผู้อนุมัติ |
| `headApprovalStatus` | สถานะการอนุมัติของหัวหน้า |
| `headApprovalActionAt` | วันที่หัวหน้าดำเนินการ |
| `headApprovalRemark` | ความเห็นของหัวหน้า |
| `directorApproverName` | ชื่อผู้อำนวยการผู้อนุมัติ |
| `directorApprovalStatus` | สถานะการอนุมัติของผู้อำนวยการ |
| `directorApprovalActionAt` | วันที่ผู้อำนวยการดำเนินการ |
| `directorApprovalRemark` | ความเห็นของผู้อำนวยการ |
| `generatedAt` | วันที่สร้างเอกสาร |
| `applicationVersion` | version ของระบบ |

## Approval Row Fields

ใช้ใน `approvalRows.format`:

| Placeholder | Description |
|---|---|
| `{{stepOrder}}` | ลำดับขั้น |
| `{{stepName}}` | ชื่อขั้นอนุมัติ |
| `{{approverName}}` | ชื่อผู้อนุมัติ |
| `{{approverPosition}}` | ตำแหน่งหรือบทบาทของผู้อนุมัติ |
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
