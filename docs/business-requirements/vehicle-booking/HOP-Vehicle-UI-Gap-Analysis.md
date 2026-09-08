# HOP Vehicle UI Gap Analysis

| Priority | Gap | Affected surfaces | Target milestone |
|---|---|---|---|
| P0 | array endpoints ไม่มี server paging | mine, dispatch, review, director, driver | UI-3 |
| P0 | list state ไม่ sync URL | Fleet lists ทั้งหมด | UI-3 |
| P1 | layout/loading/error/empty ไม่สม่ำเสมอ | Fleet pages ส่วนใหญ่ | UI-3..6 |
| P1 | approval ใช้ browser prompt/confirm | review/director/detail | UI-5 |
| P1 | mobile table overflow | requester/dispatch/admin lists | UI-3/6 |
| P1 | form/detail ไม่ reuse Leave sections/date controls | request create/edit/detail | UI-4 |
| P2 | status chip สี/label กระจาย | dashboard/list/detail/timeline | UI-2/3 |
| P2 | master/report screens ไม่ครบ route | admin/report | UI-6 |

UI-1 จัดทำ inventory จาก route/controller จริงแล้ว UI-2 เริ่ม shared `PagedResponse<T>` และ `ListPagination` พร้อม refactor consumer ที่มีอยู่แบบ backward-compatible
