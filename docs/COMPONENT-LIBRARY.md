# HOP Component Library

## Existing Shared Components

| Component | Path | Usage |
|---|---|---|
| PageHeader | `frontend/src/components/PageHeader.tsx` | หัวข้อหน้า |
| DataTableCard | `frontend/src/components/common/DataTableCard.tsx` | card สำหรับ table พร้อม horizontal scroll |
| FilterToolbar | `frontend/src/components/common/FilterToolbar.tsx` | filter form แบบ responsive |
| EmptyState | `frontend/src/components/common/EmptyState.tsx` | empty state แบบ icon/title/description/action |
| LoadingState | `frontend/src/components/common/LoadingState.tsx` | loading state แบบ progress |
| StatusBadge | `frontend/src/components/common/StatusBadge.tsx` | status/priority badge mapping กลาง |
| ActionDialog | `frontend/src/components/common/ActionDialog.tsx` | dialog ยืนยัน action แบบกลาง |
| InfoCard | `frontend/src/components/common/InfoCard.tsx` | card รายละเอียดทั่วไป |
| ConfirmDeleteDialog | `frontend/src/components/common/ConfirmDeleteDialog.tsx` | wrapper สำหรับ delete/soft delete โดยใช้ `ActionDialog` |
| ManagementDataGrid | `frontend/src/components/common/ManagementDataGrid.tsx` | grid สำหรับ management page พร้อม horizontal scroll และ responsive pagination |

## StatusBadge

รองรับ domain:

- `leave`
- `backup`
- `diagnostics`
- `notificationPriority`
- `notificationType`
- `lineBinding`
- `active`

## EmptyState

ตัวอย่าง:

```tsx
<EmptyState
  title="ยังไม่มีคำขอลาของหน่วยงาน"
  description="เมื่อมีคำขอลาในหน่วยงาน รายการจะแสดงที่นี่"
  action={<Button>สร้างคำขอ</Button>}
/>
```

## ActionDialog

ใช้สำหรับ action ที่ต้องยืนยัน เช่น approve, reject, cancel, restore, retention หรือ delete โดยกำหนด `confirmColor`, `confirmLabel`, `isLoading` และ `isConfirmDisabled` ได้

```tsx
<ActionDialog
  open={open}
  title="ยืนยันการดำเนินการ"
  confirmLabel="ยืนยัน"
  confirmColor="error"
  isLoading={mutation.isPending}
  onClose={close}
  onConfirm={confirm}
>
  <Typography>โปรดตรวจสอบข้อมูลก่อนดำเนินการ</Typography>
</ActionDialog>
```

## Refactor Backlog

- ErrorState
- PageLoading skeleton variants
- ResponsiveTable / Mobile card table
- DateRangeDisplay
- UserDisplay
- FilePreview wrapper

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
