# Password Policy

Hospital Operations Portal ใช้ Password Policy กลางสำหรับการเปลี่ยนรหัสผ่านด้วยตนเองและสามารถกำหนดค่าผ่าน configuration ได้

## Default Policy

```text
PasswordPolicy__MinimumLength=8
PasswordPolicy__RequireUppercase=true
PasswordPolicy__RequireLowercase=true
PasswordPolicy__RequireDigit=true
PasswordPolicy__RequireSpecialCharacter=true
PasswordPolicy__DisallowUsername=true
PasswordPolicy__PasswordHistoryCount=0
PasswordPolicy__ExpireDays=0
```

## User-Facing Rule

รหัสผ่านใหม่ต้อง:

1. มีความยาวตามค่า `MinimumLength`
2. มีตัวพิมพ์ใหญ่ถ้าเปิด `RequireUppercase`
3. มีตัวพิมพ์เล็กถ้าเปิด `RequireLowercase`
4. มีตัวเลขถ้าเปิด `RequireDigit`
5. มีอักขระพิเศษถ้าเปิด `RequireSpecialCharacter`
6. ไม่มี username เป็นส่วนหนึ่งของ password ถ้าเปิด `DisallowUsername`
7. ไม่ตรงกับรหัสผ่านเดิม

## API

Frontend ดึง policy จาก backend:

```text
GET /api/me/password-policy
```

Backend validate ซ้ำทุกครั้งก่อนอัปเดต password hash:

```text
POST /api/me/change-password
```

## Notes

- Phase 1 ยังไม่บังคับ password history และ expiration แม้มี key เตรียมไว้
- Admin reset password แยกจาก self-service password change
- ห้ามเก็บหรือ log password จริงทุกกรณี

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
