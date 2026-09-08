# HOP Vehicle Rollout Plan

1. Deploy additive migration โดยยังไม่เปิด navigation/feature flag
2. Seed permission/type/settings แบบ idempotent; ไม่ assign role และไม่ seed ข้อมูลจริง
3. Import preview/validate รถและ driver profile ใน staging; ผู้มีอำนาจยืนยันก่อน commit
4. Assign permissions ผ่าน role management และทดสอบ segregation of duties
5. Pilot กับงานยานพาหนะและกลุ่มผู้ขอจำกัด; manual process ยังเป็น fallback
6. ตรวจ overlap, LINE delivery, audit, timezone และ mileage reconciliation
7. เปิด requester → dispatcher → reviewer/director → driver เป็นลำดับ
8. Monitor error/queue/overdue jobs และทำ post-rollout review

## Migration Safety

- backup และ ownership preflight ตาม runbook เดิม
- validate `dotnet ef migrations script --idempotent`
- Down migration ใช้เฉพาะ dev/staging; production rollback ใช้ application rollback/feature flag เพราะการ drop ตารางอาจทำลาย transaction history
- ห้าม deploy schema change พร้อม destructive cleanup
