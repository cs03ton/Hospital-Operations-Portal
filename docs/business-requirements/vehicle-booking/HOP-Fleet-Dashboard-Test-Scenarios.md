# HOP Fleet Dashboard Test Scenarios

| ID | Actor | Expected result |
|---|---|---|
| FD-01 | Requester | Requester widgets and own-request navigation only |
| FD-02 | Driver | Own driver widgets; no requester/dispatch widgets without added permission |
| FD-03 | Dispatcher | Dispatch widgets, dispatch/review navigation, no approval widget |
| FD-04 | Admin reviewer | Review widget and guarded review deep link |
| FD-05 | Director | Director widget and guarded approval deep link |
| FD-06 | Admin | Union of all sections once, without duplicate actions |
| FD-07 | Multi-permission | Navigation and widgets are ordered unions without duplicates |
| FD-08 | Active delegation | Delegated menu, widget and route become available |
| FD-09 | Expired delegation | Delegated menu and route disappear after refresh/focus interval |
| FD-10 | Badge zero/large | Zero is hidden; values above 99 display as `99+` |
| FD-11 | Workflow success | Shared dashboard query is invalidated and counts refresh |
| FD-12 | Workflow failure | Last confirmed counts remain; conflict/error is shown |
| FD-13 | Mobile 390×844 | No horizontal overflow and actions remain reachable |
| FD-14 | API failure | Retryable error state is shown without rendering stale unauthorized sections |
| FD-15 | Leave regression | Leave sidebar, dashboard and permission guards behave unchanged |

Automation coverage: Vitest covers navigation union, badge mapping/rendering, dashboard composition and delegated guard. `fleet-dashboard-m6.spec.ts` covers the six Fleet actors, deep links, badge cap and mobile overflow using isolated QA fixtures.
