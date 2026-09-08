# HOP Pagination Pattern

## Contract

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalItems": 0,
  "totalPages": 0
}
```

Frontend ใช้ `PagedResponse<T>` จาก `frontend/src/api/pagination.ts` และ `ListPagination` จาก common components หน้าเป็น one-based ทั้ง URL/API/shared component; MUI zero-based conversion อยู่ภายใน component เท่านั้น

Backend ต้อง clamp page >= 1 และ pageSize 1..100, scope ก่อน Count, `AsNoTracking`, projection ก่อน materialize, stable whitelist sort พร้อม ID tie-breaker, `CountAsync`, `Skip/Take` และ cancellation token

เมื่อ search/filter/sort/pageSize เปลี่ยนให้ page กลับ 1 หาก page เกิน totalPages หลัง mutation ให้ย้อนสู่หน้าสุดท้ายที่ถูกต้อง
