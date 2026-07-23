# Leave Policy Mapping

## Mapping ปัจจุบัน

ระบบยังไม่มี table `leave_policies` หรือ `employment_type_leave_policy_mapping` แยกต่างหาก

Mapping ปัจจุบันอยู่ใน:

```text
leave_policy_rules.employment_type
leave_policy_rules.leave_type_id
leave_policy_rules.fiscal_year
leave_policy_rules.is_active
```

ระบบเลือก rule โดย:

1. employment type ของ user
2. leave type
3. fiscal year ตรงปี หรือ rule กลางที่ `fiscal_year = null`
4. active rule

## ข้อจำกัด

- ยังไม่มี policy version id
- ยังไม่มี effective_from/effective_to
- ยังไม่มี default policy ต่อ employment type แบบเป็น entity
- ประวัติสิทธิ์ย้อนหลังต้องอาศัย balance/audit มากกว่า policy version

## Recommendation

ควรเพิ่มใน phase ถัดไป:

```text
leave_policies
employment_type_leave_policy_mappings
employee_employment_histories
```

เพื่อรองรับการเปลี่ยน policy ในอนาคตโดยไม่กระทบอดีต

