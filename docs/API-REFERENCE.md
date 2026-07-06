# API Reference

## Leave Policy Preview

ใช้สำหรับให้ frontend ตรวจ policy ก่อนสร้างหรือแก้ไขคำขอลา โดย backend ยัง validate ซ้ำเสมอเมื่อ create/update/submit

```http
POST /api/leave-requests/policy-preview
```

Request:

```json
{
  "leaveTypeId": "uuid",
  "startDate": "2026-07-02",
  "endDate": "2026-07-02",
  "durationType": "FULL_DAY"
}
```

Response สำคัญ:

```json
{
  "employmentType": "MOPH_EMPLOYEE",
  "employmentTypeName": "พนักงานกระทรวงสาธารณสุข",
  "fiscalYear": 2569,
  "entitlementDays": 10,
  "usedDays": 2,
  "pendingDays": 1,
  "availableDays": 7,
  "requestedDays": 1,
  "requiresBalance": true,
  "canSubmit": true,
  "warnings": [],
  "errors": [],
  "policyNotes": []
}
```

ถ้าไม่ผ่านเงื่อนไขตามเพศ ระบบจะคืน `canSubmit = false` และใส่ข้อความใน `errors` เช่น:

- `ประเภทการลาคลอดบุตร ใช้ได้เฉพาะบุคลากรเพศหญิง`
- `ประเภทการลาบวช ใช้ได้เฉพาะบุคลากรเพศชาย`

## Leave Balance Rollover

ใช้สำหรับ preview และยืนยันการยกยอดวันลา รองรับทั้งแบบรายบุคคลและ batch ด้วย filter เดียวกัน

Backend รับปีงบประมาณเป็น ค.ศ. เท่านั้น เช่น `2026`, `2027`

Preview:

```http
POST /api/leave-balances/rollover/preview
```

Confirm:

```http
POST /api/leave-balances/rollover/confirm
```

Export preview:

```http
POST /api/leave-balances/rollover/export-preview
```

Request:

```json
{
  "fromFiscalYear": 2026,
  "toFiscalYear": 2027,
  "departmentId": null,
  "employmentType": "MOPH_EMPLOYEE",
  "leaveTypeId": null,
  "userId": null,
  "reason": "ยกยอดวันลาปีงบประมาณ 2570"
}
```

`reason` จำเป็นเฉพาะ confirm

ถ้าส่งปี พ.ศ. เช่น `2569` backend จะตอบ:

```text
ปีงบประมาณไม่ถูกต้อง ระบบต้องใช้ปี ค.ศ. ภายใน backend
```
