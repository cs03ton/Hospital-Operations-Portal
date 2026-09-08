# HOP Fleet Navigation Matrix

Fleet navigation is composed from effective permissions. Role names are not used. Effective permissions may include an active `FLEET` delegation resolved by the existing authorization pipeline.

| Order | Menu | Route | Required any permission |
| --- | --- | --- | --- |
| 1 | Dashboard รถ | `/fleet/dashboard` | Any Fleet capability, or `FleetDashboard.View` |
| 2 | คำขอใช้รถ | `/fleet/requests` | `FleetRequest.ViewOwn` or `FleetRequest.Create` |
| 3 | งานขับรถของฉัน | `/fleet/my-trips` | `FleetDriver.ViewOwnJobs` or `FleetDriver.ViewOwn` |
| 4 | คิวจัดรถ | `/fleet/dispatch` | `FleetDispatch.View` or `FleetDispatch.Assign` |
| 5 | งานรอตรวจสอบและอนุมัติคำขอใช้รถ | `/fleet/approvals` | Admin-review or director decision capability |
| 6 | ปฏิทินรถ | `/fleet/calendar` | Calendar or operational Fleet capability |
| 7 | รายงาน | `/fleet/reports` | `FleetReport.View` or `FleetReport.Export` |
| 8 | ตั้งค่า | `/fleet/settings` | `FleetSettings.Manage` |

The registry preserves this order and returns a set union for multi-permission users, so paths and labels are never duplicated. The former dispatcher alias `/fleet/requests/review` and existing routes such as `/fleet/review`, `/fleet/director`, and `/fleet/driver/jobs` remain available for backward compatibility, but the alias is not shown in the Sidebar.

## M2 security boundary

- Sidebar visibility is not authorization. Every new route uses the existing permission guard plus `FleetRolloutGuard`.
- The legacy KPI API remains protected by `FleetDashboard.View`.
- A user who can open the Fleet dashboard through another Fleet capability does not load legacy system-wide KPI data. Permission-scoped dashboard data will replace this shell in M3.
- No Leave navigation, route, permission, or API contract is changed.
