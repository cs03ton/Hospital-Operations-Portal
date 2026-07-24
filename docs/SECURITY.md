# Security

เอกสารนี้สรุปมาตรการความปลอดภัยหลักของ Hospital Operations Portal (HOP) Phase 1

## Authentication

- ใช้ JWT access token
- ใช้ refresh token พร้อม rotation
- รองรับ hardened cookie mode พร้อม CSRF protection
- password hash ด้วย BCrypt
- login rate limit ป้องกัน brute force เบื้องต้น
- ค่า `Jwt__AccessTokenMinutes` ควรตั้งเป็น 15 นาทีใน production
- refresh token รุ่นใหม่ถูกเก็บแบบ hash at rest ผ่าน `refresh_tokens.token_hash`

## MFA Readiness

Phase 1 ยังไม่เปิดใช้งาน MFA จริง แต่กำหนดแนวทางรองรับไว้สำหรับ Phase ถัดไป:

1. เพิ่มสถานะระดับบัญชี เช่น `MfaEnabled`, `MfaMethod`, `MfaEnrolledAt`
2. รองรับวิธี MFA แบบ TOTP หรือ LINE second factor ตามนโยบายโรงพยาบาล
3. ใช้ step-up authentication เฉพาะงานสำคัญ เช่น reset password, permission change, backup restore, superadmin override
4. บันทึก audit event เช่น `Auth.MfaChallenge`, `Auth.MfaSuccess`, `Auth.MfaFailed`
5. ไม่เก็บ secret ของ TOTP แบบ plaintext และต้องเข้ารหัสหรือป้องกันด้วย secret manager

ข้อเสนอสำหรับ production hardening ระยะถัดไป:

- เพิ่ม `SecurityStamp` หรือ `TokenVersion` ใน user เพื่อ revoke access token ได้ทันทีหลังเปลี่ยนรหัสผ่าน/เปลี่ยนสิทธิ์
- เพิ่ม policy บังคับ MFA สำหรับ Admin/SuperAdmin ก่อนเปิดใช้งาน production เต็มรูปแบบ

## Self-Service Password Change

ผู้ใช้สามารถเปลี่ยนรหัสผ่านของตนเองผ่าน:

```text
POST /api/me/change-password
```

Security controls:

- ใช้ user id จาก JWT/session เท่านั้น
- ไม่รับ `userId` จาก frontend
- ต้องยืนยันรหัสผ่านปัจจุบันก่อน
- validate Password Policy ฝั่ง backend เสมอ
- ไม่ส่ง password หรือ password hash กลับ frontend
- ไม่ log password จริง
- revoke refresh token ทุก token หลังเปลี่ยนสำเร็จ
- บันทึก audit event `User.PasswordChanged`

## Admin Reset Password

Admin reset password เป็น workflow แยกจาก self-service password change

- ต้องใช้ permission ของ User Management
- ไม่ต้องทราบรหัสผ่านเดิมของผู้ใช้
- ต้องไม่ใช้ endpoint `/api/me/change-password`

## Audit Events

Events ที่เกี่ยวข้อง:

```text
Auth.LoginSuccess
Auth.LoginFailed
Auth.LoginLocked
Auth.Logout
User.PasswordChanged
User.PasswordChangeFailed
Security.CsrfValidationFailed
PermissionDenied
```

## Production Reminder

- ตั้ง `Jwt__Key` จาก environment variable และยาวอย่างน้อย 32 ตัวอักษร
- ห้าม commit secret/token/password ลง repository
- ใช้ HTTPS และตั้ง cookie `Secure=true` เมื่อใช้งาน production
- ตรวจ backup และ restore test ก่อน pilot

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
