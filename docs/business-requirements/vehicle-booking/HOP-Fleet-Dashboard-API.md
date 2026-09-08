# HOP Fleet Dashboard API

## Endpoint

`GET /api/fleet/dashboard`

The endpoint requires an authenticated user and at least one effective Fleet permission. It returns `403` when the user has no Fleet capability.

The pre-M3 operational-count payload remains available at `GET /api/fleet/dashboard/legacy-operations` and continues to require `FleetDashboard.View`. This explicit legacy route avoids an ambiguous endpoint match while the new role-aware contract owns the canonical dashboard route.

## Authorization and delegation

Effective permissions are the union of active permissions assigned through active roles and active `ApprovalDelegation` records where `Scope=FLEET`, the current user is the delegate, the current UTC time is inside the delegation period, and the delegator currently owns the delegated permission.

Role names are never evaluated. Expired, disabled, wrong-scope, and invalid delegations are ignored.

## Contract

The standard HOP `ApiResponse<T>` wraps:

- `capabilities`: server-resolved capability metadata and delegated permission codes;
- `badges`: dispatch queue, review queue, approval queue, and current driver's jobs;
- `shared`: non-sensitive aggregate vehicle/today counts scoped to the user's capability;
- `requester`, `driver`, `dispatcher`, `adminReviewer`, `director`, `admin`: nullable sections.

Unauthorized sections are `null` and their sensitive queries are not executed. Requester data is restricted by `RequesterUserId`; driver data is restricted by active assignment `DriverUserId`.

## Cache and invalidation

Frontend must use query key `['fleet-dashboard']`. M4 reuses `badges` from this response and invalidates that key after Fleet workflow mutations instead of adding badge-specific requests.

## M4 frontend behavior

- The dashboard renders only role sections returned by the server; a `null` section is never inferred from a role name.
- Shared availability cards, role-specific cards, queue badges, delegated-permission indicator, loading, scoped empty, retryable error, and manual refresh states use the same response.
- Request, assignment, approval, driver acknowledgement, trip start, and trip completion success paths invalidate `['fleet-dashboard']`.
- Dates are displayed with the existing HOP Thai/`Asia/Bangkok` formatter, and every card action is still protected by the destination route permission guard.

## M5 integration behavior

- The Sidebar and Dashboard share `['fleet-dashboard']`; no badge-specific endpoint or polling loop exists.
- Pending task badges are mapped to driver jobs, dispatch/review queues, and the combined reviewer/director approval entry. Zero is hidden and values above 99 render as `99+`.
- Fleet navigation and Fleet route guards union the signed-in user's direct permissions with server-resolved `delegatedPermissions`. Leave and other module guards remain unchanged.
- The dashboard is refreshed every 60 seconds and on window focus where delegation-aware navigation is active, so an expired delegation disappears without a new login.
- Workflow mutations invalidate the shared query only after success. A failed or concurrency-conflicted action retains the last confirmed badge state until the next successful fetch.
