# HOP Vehicle UI Audit

วันที่ตรวจ: 5 สิงหาคม 2026  
Reference: frontend ระบบลาใน worktree ปัจจุบัน

## 1. Leave UI reference summary

Leave ใช้ `PageHeader`, MUI Card/Grid/Table, theme กลาง, permission guard, React Query และ Thai date/status mapping. List หลักใช้ API paging และ `TablePagination`; filter บางส่วน sync URL แต่ page/pageSize ยังเป็น local state จึงยังไม่รักษาหน้าเมื่อ browser back ครบถ้วน. Form/detail มีโครง section ชัดกว่า Fleet และ approval มี `ApprovalWorkflowTimeline` แต่ shared approval panel ยังไม่สมบูรณ์

## 2. Fleet route inventory

| กลุ่ม | Routes ที่พบ |
|---|---|
| Requester | `/fleet/requests`, create, edit, detail, capabilities |
| Dispatcher/approval | `/fleet/dispatch`, `/fleet/review`, `/fleet/director` และ detail |
| Driver | `/fleet/driver`, `/fleet/driver/jobs`, trip action |
| Operations | dashboard, calendar, maintenance, vehicle documents |
| Admin | capabilities, vehicle capabilities, delegations, health |
| Emergency | create, queue, review, post-review, policies |

ไม่มี route UI แยกสำหรับ vehicle/driver/unavailability master, all-requests, cancellation list และ report list ตามชื่อใน requirement; backend master endpoints บางส่วนมีอยู่ จึงเป็น gap ไม่ใช่หน้าที่ตกหล่นจาก inventory

## 3. UI comparison matrix

| Area | Leave | Fleet ปัจจุบัน | Gap |
|---|---|---|---|
| Header/layout | consistent `PageHeader` | บางหน้าใช้ PageHeader, บางหน้า Container/Typography | สูง |
| Filter/URL | list หลัก sync filter บางส่วน | ส่วนใหญ่ไม่มี | สูง |
| Paging | server-side บน request/cancellation/holiday | เฉพาะ maintenance/emergency บางหน้า | วิกฤต |
| Table states | loading/empty ใน row | หลายหน้ามีเฉพาะ loading หรือ array map | สูง |
| Status | Leave mapping + Chip | Fleet label แต่สีไม่สม่ำเสมอ | กลาง |
| Detail/form | sectioned page | dense inline JSX, native datetime, prompt/confirm | สูง |
| Approval | timeline/reason patterns | card list + browser prompt | สูง |
| Mobile | responsive grid บางหน้า | driver ดีบางส่วน, list table ส่วนใหญ่ล้น | สูง |

## 4. Shared components ที่ reuse ได้

`PageHeader`, `PageBreadcrumbs`, `AppDatePicker`, `ActionDialog`, `StatusBadge`, `EmptyState`, `LoadingState`, `FilterToolbar`, `ManagementDataGrid`, `DataTableCard`, `InfoCard`, permission guards และ date formatting utilities

## 5. Components ที่ควร refactor

- ย้าย generic behavior จาก `ApprovalWorkflowTimeline` ไป `ApprovalTimeline` กลางโดยให้ Leave wrapper เดิมคงอยู่
- ขยาย `StatusBadge` ด้วย mapping adapter แทน Fleet chip รายหน้า
- ใช้ `ListPagination` กลาง (เริ่มใน UI-2)
- สร้าง `ListPageLayout`, `FormSection`, `DetailSection`, `ApprovalActionPanel` เมื่อ refactor หน้าจริง โดยไม่ duplicate ใต้ fleet

## 6. Pagination/API gaps

ไม่มี server paging: request mine, dispatcher queue, admin review, director approval, driver jobs, cancellations, health/outbox, master vehicle types/vehicles/driver profiles, vehicle maintenance history/documents. มี pagingแต่ contract ไม่มาตรฐาน: maintenance และ emergency review (`total` แทน `totalItems`, ไม่มี `totalPages`); emergency queue/บาง capability endpoint ต้องตรวจ response adapter รายหน้า

## 7. Risks and compatibility

- worktree Fleet เป็นชุดไฟล์ใหม่จำนวนมากและยังไม่ commit; ต้องแก้เฉพาะขอบเขตและไม่ทับงานเดิม
- เปลี่ยน array endpoint เป็น paged responseทันทีจะ breaking; ใช้ query opt-in หรือ endpoint/version adapter ระหว่าง migration
- Leave reference เองยังไม่ URL-sync page/pageSize ครบ จึงต้องยกระดับ shared foundationก่อนแล้วทำ regression
- approval prompt migration เสี่ยงเปลี่ยน required reason/returnTarget จึงทำใน UI-5 พร้อม tests

## 8. Open questions

ต้องยืนยันใน UI-3 ว่า “รายการคำขอทั้งหมด” จะเป็น endpoint ใหม่หรือขยาย request list ตาม scope, master data routes จะเปิดใน navigation ใด และ report export history ต้องแยก list endpoint หรือรวม dashboard
