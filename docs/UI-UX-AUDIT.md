# HOP UI/UX Audit

วันที่ตรวจ: 19/07/2026

## Executive Summary

Frontend ของ HOP ใช้ React + TypeScript + Vite และ MUI เป็น component library หลัก มี theme กลางอยู่แล้วใน `frontend/src/theme` และมี common components สำหรับ table, filter, empty/loading state บางส่วน ระบบโดยรวมมี visual identity ชัดเจนคือ Deep Green + White + Gold Accent แต่ยังมี pattern ซ้ำในหลายหน้า เช่น status badge, priority badge, empty state, table overflow และ spacing card บางส่วน

## Technology / UI Stack

| หัวข้อ | สถานะ |
|---|---|
| UI Framework | React 18 + TypeScript |
| Component Library | MUI v5, MUI X Date Pickers |
| Styling Strategy | MUI `sx`, theme override, component-level style |
| Theme | `frontend/src/theme/palette.ts`, `theme.ts`, `components.ts` |
| Layout | `MainLayout`, `AppHeader`, `AppSidebar`, `AppFooter`, `PageTitle`, `PageBreadcrumbs` |
| Shared Components | `PageHeader`, `DataTableCard`, `FilterToolbar`, `EmptyState`, `LoadingState`, `InfoCard`, `ConfirmDeleteDialog`, `ManagementDataGrid` |

## Critical

| Issue | Evidence | Recommendation |
|---|---|---|
| ไม่มีมาตรฐาน status badge เดียวกันทุก domain | หลายหน้ามี `getStatusColor`, `statusLabels`, `priorityLabels` เฉพาะหน้า | ใช้ `StatusBadge` + `statusLabels.ts` เป็น mapping กลาง |
| ตารางบางหน้าเสี่ยง overflow บน mobile/tablet | หน้า Audit, Notification, Leave Support, Diagnostics มี table columns จำนวนมาก | ใช้ `DataTableCard` พร้อม horizontal scroll และ min table width |
| Diagnostics/Health/Backup แสดงข้อมูลระบบ ต้องคุม privacy | Diagnostics มี log/support bundle | ยืนยัน redaction ก่อน display/export และเขียน guideline privacy |

## High

| Issue | Evidence | Recommendation |
|---|---|---|
| Card spacing ใน admin/system pages บางจุดไม่สม่ำเสมอ | Admin Dashboard quick actions, Diagnostics cards, Health Center grid | ใช้ grid pattern กลางและห้ามใช้ MUI Grid ที่ทำ negative margin ใน section ที่ชิดขอบ |
| Empty/loading state ยังต่างกัน | บางหน้าใช้ text เช่น `ไม่พบ...`, บางหน้าใช้ component | เพิ่ม `EmptyState` ให้รองรับ title/description/action และค่อย migrate |
| Filter toolbar ยังใช้หลาย pattern | บางหน้ามี PageToolbar, FilterToolbar, inline Stack | รักษา `FilterToolbar` เดิมแต่เพิ่ม responsive safety และค่อยรวม pattern |
| Dialog/confirmation ยังมีหลายรูปแบบ | Delete, restore, retention, approve/reject | ต้องรวม guideline และค่อย refactor เป็น `ActionDialog` ในรอบถัดไป |

## Medium

| Issue | Evidence | Recommendation |
|---|---|---|
| Hardcoded colors ยังพบใน LINE Flex preview และ leave type color | `LineSettingsPage`, `leaveLabels.ts` | ค่อยย้ายสีไป token/mapping กลาง |
| Page title/header hierarchy ไม่เท่ากันบางหน้า | dashboard/admin/report pages | ใช้ `PageHeader` และ spacing section เดียวกัน |
| Thai wording บาง domain ยังไม่ครบ mapping | `Submitted`, `InApproval`, backup/diagnostics | เพิ่ม mapping กลางและ checklist |
| Button/action order ใน admin pages ยังต่างกัน | Export/clear/search และ quick action | ใช้ primary action ขวาบน และ destructive action สี danger |

## Low

| Issue | Evidence | Recommendation |
|---|---|---|
| Bundle size warning จาก Vite | `npm run build` เตือน chunk > 500 KB | พิจารณา route lazy loading หลัง pilot |
| Icon mapping ยังไม่ได้รวมทุก action | หลายหน้ import icons เอง | สร้าง icon registry เมื่อเริ่ม refactor รอบใหญ่ |
| Typography token ยังไม่ได้ใช้ทั่วทุกหน้า | theme มี typography แต่ยังไม่มี token explicit | เพิ่ม `hopTokens` แล้วทยอยใช้ |

## Quick Wins ที่ทำแล้ว

- เพิ่ม `frontend/src/theme/tokens.ts`
- เพิ่ม `frontend/src/utils/statusLabels.ts`
- เพิ่ม `frontend/src/components/common/StatusBadge.tsx`
- ปรับ `EmptyState` ให้รองรับ title/description/action/icon
- ปรับ `DataTableCard` ให้มี min table width และ horizontal scroll ปลอดภัย
- ปรับ `FilterToolbar` ให้คง MUI Grid เดิมและเพิ่ม `minWidth: 0`
- ปรับ leave status mapping ให้ใช้ mapping กลางโดยไม่เปลี่ยน API helper เดิม
- ปรับ Notification Center, Pending Approvals และ LINE Users ให้เริ่มใช้ `StatusBadge`
- เพิ่ม `ActionDialog` กลาง และปรับ `ConfirmDeleteDialog` ให้ใช้แกนเดียวกัน
- ปรับ `ManagementDataGrid` ให้รองรับ horizontal scroll และ pagination ที่ wrap ได้บนจอเล็ก
- ปรับ User/Department/Role grid และ Notification Bell ให้ใช้ `StatusBadge`
- เคลียร์ lint errors/warnings ที่ทำให้ `npm run lint` fail

## Refactor Required

1. ขยายการใช้ `ActionDialog` ไปยัง approve, reject, cancel, restore และ retention dialog
2. สร้าง `ResponsiveTable` หรือ table-to-card pattern สำหรับ mobile ที่ซับซ้อนกว่าตาราง management
3. ย้าย priority/status ทุกหน้าที่ยังเหลือไป `StatusBadge`
4. เพิ่ม URL query sync สำหรับ filter ที่ต้อง deep link เช่น audit, leave list, reports
5. เพิ่ม route-level lazy loading เพื่อลด bundle size
6. เพิ่ม component tests สำหรับ status badge, empty/error/loading state

## Pages ที่ควรตรวจ manual viewport

| Page | Risk |
|---|---|
| Dashboard Hub | card grid, coming soon badge |
| Leave Dashboard | role-based section visibility |
| Leave List | table/filter/pagination |
| Leave Cancellation | card summary and list filters |
| Pending Approval | table mobile overflow |
| User Management | filter/data grid/actions |
| Health Center | status grid |
| Diagnostics Center | log viewer/support bundle form |
| Backup Center | restore/retention dialog |
| LINE Settings | long payload preview and tabs |
| Audit Log | filter/export/table |

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
