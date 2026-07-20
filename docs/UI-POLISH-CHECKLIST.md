# HOP UI Polish Checklist

## Checklist Columns

- Page header
- Breadcrumb
- Standard layout
- Responsive
- Loading
- Empty
- Error
- Status badge
- Table/filter
- Dialog
- Toast
- Keyboard
- Permission visibility
- Thai wording
- Date format
- Build passed

## Page Checklist

| Page | Header | Responsive | Loading/Empty/Error | Badge | Table/Filter | Dialog/Toast | Notes |
|---|---|---|---|---|---|---|---|
| Main Dashboard | Done | Partial | Partial | Partial | N/A | N/A | Need role manual screenshots |
| Leave Dashboard | Done | Partial | Partial | Partial | N/A | N/A | Head sections must stay personal/team/approval |
| My Leave Requests | Done | Partial | Partial | Partial | Partial | Partial | Table pagination exists, mobile needs review |
| Leave Request Detail | Done | Partial | Partial | Partial | N/A | Partial | Attachment/timeline review needed |
| Leave Cancellation | Done | Partial | Partial | Partial | Partial | Partial | Cancellation wording should remain consistent |
| Approval Queue | Done | Partial | Partial | Partial | Partial | N/A | Priority badge migrated to StatusBadge |
| User Management | Done | Partial | Partial | Done | Done | Partial | ManagementDataGrid + StatusBadge used |
| Health Center | Done | Done | Partial | Partial | N/A | N/A | Status mapping should migrate next |
| Diagnostics Center | Done | Partial | Partial | Partial | Partial | Partial | New page, requires manual smoke test |
| Backup Center | Done | Partial | Partial | Partial | Partial | Partial | Restore/retention dialogs should migrate to ActionDialog next |
| Settings | Done | Partial | Partial | Partial | N/A | Partial | Verify sensitive config masked |
| Audit Log | Done | Partial | Partial | Partial | Partial | N/A | FilterToolbar used |

## Automated Frontend Gate

- [x] `npm run lint` ผ่านโดยไม่มี error/warning
- [x] `npm run build` ผ่านหลังปรับ shared UI components
- [ ] เพิ่ม component tests สำหรับ `StatusBadge`, `ActionDialog`, `EmptyState`

## Manual Test By Role

### Staff

- [ ] Dashboard Hub
- [ ] Leave Dashboard
- [ ] Create Leave
- [ ] My Leave Requests
- [ ] Leave Cancellation
- [ ] Profile
- [ ] Documentation Center

### Department Head

- [ ] Personal leave sections
- [ ] Team leave sections
- [ ] Pending approval queue
- [ ] Approve/reject/return flow

### Director

- [ ] Executive Dashboard
- [ ] Leave Analytics
- [ ] Leave Reports
- [ ] Pending approval queue

### Admin

- [ ] Admin Dashboard
- [ ] User Management
- [ ] Department Management
- [ ] Role/Permission
- [ ] Health Center
- [ ] Diagnostics Center
- [ ] LINE Settings

### SuperAdmin

- [ ] Backup Center
- [ ] Audit Log
- [ ] System Settings
- [ ] Support Bundle download

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
