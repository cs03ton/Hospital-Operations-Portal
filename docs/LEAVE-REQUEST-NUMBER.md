# Leave Request Number

เลขที่คำขอลาใช้รูปแบบ:

```text
LV-YYYYMM-001
```

ตัวอย่าง:

```text
LV-202606-001
LV-202606-002
LV-202607-001
```

## Rules

- Prefix คือ `LV`
- `YYYYMM` คือปี ค.ศ. และเดือนของเวลาสร้างคำขอ
- Running number reset ทุกเดือน
- Running number แสดงอย่างน้อย 3 หลัก
- เลขคำขอ unique ด้วย database index
- Draft ใช้เลขเดิม ไม่ regenerate เมื่อแก้ไขคำขอ

## Backend

เลขคำขอสร้างตอน `POST /api/leave-requests` ด้วย `LeaveRequestNumberService`

ระบบใช้ transaction isolation ระดับ `Serializable` ตอน create เพื่อช่วยลด race condition เมื่อมีผู้ใช้สร้างคำขอพร้อมกัน

## Frontend

หน้า UI แสดงเลขที่คำขอใน:

- รายการคำขอลา
- รายละเอียดคำขอลา
- สถานะเอกสาร
- PDF ใบลา
- Support view

ถ้ายังไม่มีเลขคำขอ ให้แสดง:

```text
-
```

## Database

Migration เพิ่ม column:

```text
leave_requests.request_number
```

และ unique index:

```text
IX_leave_requests_request_number
```

ข้อมูลเก่าถูก backfill ตามเดือนที่สร้างคำขอ
