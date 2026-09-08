# HOP Vehicle UI Migration Plan

## File change plan

- UI-2: common pagination/API types, shared layout primitives, Leave regression tests
- UI-3: `fleetApi`, request/dispatch/workflow/driver list pages และ corresponding controllers/tests
- UI-4: request form/detail, date fields, unsaved guard, responsive sections
- UI-5: shared approval timeline/action dialogs และ approval pages
- UI-6: driver/admin/maintenance/emergency/reports/master-data surfaces
- UI-7: component/integration/Playwright, guides และ screenshots

## Backward-compatible API strategy

1. เพิ่ม query paging และ paged DTO โดยให้ frontend deploy พร้อมกัน
2. หากมี consumer ภายนอก ให้คง legacy array endpoint หรือใช้ explicit `paged=true` ชั่วคราว
3. เปลี่ยน read query เท่านั้น; ไม่แตะ workflow, permissions, audit, notifications หรือ relationships
4. ไม่มี schema migration ใน UI-1/UI-2; index เพิ่มได้หลังวัด query plan ใน UI-3

## Regression gates

ทุก milestone ต้องผ่าน backend build/test, frontend lint/test/build, Leave list/form/detail smoke tests, Fleet permission/rollout guards และ `git diff --check`
