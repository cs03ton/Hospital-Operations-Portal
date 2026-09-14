# Repair UI Alignment

Date: 2026-09-11. Local UI-only follow-up to repair v1.

## Scope

- Repair navigation follows VehicleBooking, with permission filtering preserved even when Fleet rollout/access is unavailable.
- Menu, page header and breadcrumb use `Dashboard แจ้งซ่อม`; URL stays `/dashboard/repair`.
- Dashboard/list use existing PageHeader, PageToolbar, InfoCard, DataTableCard and loading/empty/error patterns. From `md` (900px), a table shows number/title, team, location, status, priority, round, updated time and detail action. Smaller screens use equivalent cards.
- Filters reset page to 1; clear resets scope/status/search. Dashboard counters represent all authorized visible work, independently of list filters. Admin scope is explicitly labelled system-wide.
- Forms group category/department, symptoms, location/contact and images. Photo previews release object URLs on removal/unmount. Existing JPG/PNG/WebP, 10 MB and five-file limits remain.
- Details group request fields, actions, event history, solver/contributors, rounds/acceptance and waiting intervals. Protected image access is unchanged.
- A stale action retains entered text. Explicit refresh updates the concurrency token; confirmation remains disabled when the action is no longer available or the token is stale.
- Settings separate categories, MorProm destinations and delivery results, with Thai display labels. Retrying a settings conflict can reload the token without discarding inputs.
- Dates use a shared Buddhist-calendar formatter without changing existing date formatter semantics for other modules.

No backend API, schema, permission enforcement, workflow, worker or production configuration was changed in this UI follow-up. Existing 15-second foreground polling, focus/reconnect behaviour and repair query invalidation remain enabled.

## Verification

The automated browser suite uses mocked API responses only. It cannot prove backend authorization or real message delivery.

| Check | Coverage |
| --- | --- |
| Responsive browser matrix | Staff, IT, general maintenance, dual technician, Admin, SuperAdmin; widths 375, 430, 768, 1366, 1920 |
| Routes | Dashboard, list, detail, create; settings for Admin/SuperAdmin |
| Layout assertions | No page-level horizontal overflow; desktop table/mobile cards; visible actions; dialog horizontal bounds |
| UI behaviour | Required solver/note validation, selected-image preview/removal, explicit reload after 409 retaining note and submitting latest token |
| Unit tests | Actual sidebar permission filtering, Fleet/repair order, route/header/breadcrumb label, filter pagination reset, manual refresh, image limits/object URL cleanup, Buddhist year |
| Polling | Repair query interval assertion and existing shared fake-timer foreground/focus/reconnect tests |

Before screenshots: `tmp/repair-ui-before/repairs-responsive-*` (create and solve dialog snapshots retained from the previous UI).
After screenshots: `frontend/test-results/repairs-responsive-*` (`repair-dashboard.png`, `repair-list.png`, `repair-detail.png`, `repair-create.png`, and technician `repair-solve.png`). These are local ignored QA artifacts.

## Commands

Run from `frontend`:

```powershell
npm run test -- --fileParallelism=false
npx playwright test --config playwright.repairs.config.ts --fully-parallel --workers=2
npm run build
```

Browser tests launch an isolated Vite server on port 5193 and intercept API calls. They do not use production or send LINE/MorProm notifications.
Production deployment, real-account workflow UAT, live file scanning and live notification delivery remain outside this change.

## Execution Results

| Gate | Result |
| --- | --- |
| Frontend unit/component regression | 112 passed across 20 files, sequential file execution |
| Browser checks | 31 passed: 30 viewport/role cases plus explicit 409 recovery |
| Targeted ESLint | Passed with zero warnings |
| TypeScript and Vite build | Passed; output `frontend/dist`, bundle `index-f016b22f.js` |
| Working-tree whitespace check | Passed |
| Existing local Vite route | `/dashboard/repair` returned HTTP 200 on port 5173 with HTML Accept header |

Mobile create/detail/solve and desktop dashboard/list screenshots were visually inspected against the retained pre-change screenshots. No page overflow was detected by the 30-case matrix.
The first browser attempt used an ambiguous heading locator (header and page both correctly have the same label); it was corrected to the page's level-4 heading before rerunning successfully.
The first parallel unit run timed out in an existing Fleet feedback test. Both subsequent sequential runs passed without changing Fleet code.
Existing React Router future-flag/test fixture warnings and the Vite bundle-size warning remain. No production deployment or real notification was performed.

## Dropdown And Requester Follow-up

- Repair-only select menus now use an opaque surface, green border, stronger shadow, distinct hover/focus/selected states, wrapping labels and a 360px viewport-capped scrolling popup. Other modules' theme defaults are unchanged.
- The requester department is disabled, not selectable. It loads via the existing authenticated `/api/auth/me` API on entry/focus/reconnect, rather than relying on the saved login snapshot. Error/retry and missing-department states are explicit; refreshing does not reset entered form fields. Backend still derives the saved department from the authenticated user's database record.
- New requester tests passed (2 cases). Full regression: 113 passed, one existing Fleet feedback timeout; isolated rerun of Fleet feedback and requester tests passed all 5 cases without code changes to Fleet.
- Browser matrix passed all 36 cases; an additional 5-case long-menu check used 11 options at all five viewport widths. Screenshots are under `frontend/test-results/repair-dropdown-followup/`.
- Targeted ESLint and build passed. Latest build output: `frontend/dist/assets/index-ca8e861d.js`. Existing bundle-size warning remains. Local only; no API/schema/authorization change or production deployment.
