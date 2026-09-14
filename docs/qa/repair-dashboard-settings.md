# Repair Dashboard And Settings UI

Date: 2026-09-14. Local frontend changes only.

## Scope

- `/dashboard/repair` now renders `RepairDashboardPage`, separately from the searchable, paginated `/repairs` list.
- Seven status tiles and the five most recently updated jobs use the same backend-visible scope. The existing API orders by `UpdatedAt` then job number, descending. `scope=all` means all jobs permitted by the backend, not unrestricted access.
- Tile links preserve scope and status. The list validates URL parameters, falls back on unsupported values, and resets pagination when filters change.
- Dashboard summary, recent jobs and settings retain the shared 15-second foreground polling options. Manual refresh does not reload the page. Initial loading uses skeletons; errors are not displayed as zero counts.
- Settings has category, notification group and delivery-result tabs. Categories support search/team/active filters. Tables are used at the existing `md` breakpoint; smaller screens use cards.
- Group summaries expose only configured-secret readiness, never the secret. Existing HTTPS validation and concurrency-token behavior are retained; conflict recovery keeps entered text.
- No backend, schema, authorization, LINE configuration or production deployment changes were made for this UI task.

## Verification

| Check | Result |
| --- | --- |
| Sequential frontend regression | 117 tests passed, 22 files |
| Final dashboard/settings targeted tests | 6 passed, including three additional cases beyond the regression run |
| Browser matrix | 36 passed: six roles at five widths, five dropdown checks and a workflow 409 check |
| Final compact settings toolbar | Four additional Admin/SuperAdmin mobile cases passed |
| Targeted ESLint | Passed, zero warnings |
| TypeScript / Vite build | Passed; `frontend/dist/assets/index-f186b904.js` |

Roles: Staff, IT, General, Dual, Admin and SuperAdmin. Widths: 375, 430, 768, 1366 and 1920 pixels. The matrix checks page-level horizontal overflow, dashboard/list/detail/create actions, and authorized settings dialogs. Browser tests intercept all API requests and use synthetic records; they do not create real jobs or send notifications.

Unit tests additionally cover five-job limiting, API scope/pageSize, KPI links, valid/invalid list URL filters, category search, tab content, delivery links and explicit adoption of the latest concurrency token without losing edits. Dashboard fake timers verify both data sources refetch at 15 seconds, pause when hidden, and refetch on focus. Shared polling regression covers network reconnection.

## Screenshots

- Before: `C:/Users/CR7iToNz/AppData/Local/Temp/hop-repair-dashboard-before-25690914-090504/` retains screenshots of the previous dashboard/list UI. No pre-change settings screenshot was available.
- After: `frontend/test-results/repairs-responsive-<Role>-at-<width>px/` contains dashboard/list and authorized settings category, group, delivery and dialog captures.
- Final mobile settings: `frontend/test-results/repair-dashboard-final-mobile/` contains the captures after integrating Refresh beside the tabs.
- Desktop 1366px and mobile 375px dashboard/settings captures were visually inspected. No clipping or page-level horizontal overflow was observed in the tested fixtures.

## Reproduce

Run inside `frontend`:

```powershell
npm run test -- --fileParallelism=false
npx playwright test --config playwright.repairs.config.ts --fully-parallel --workers=2
npm run build
```

Browser tests start an isolated Vite server on port 5193. Real-account UAT, live authorization/data accuracy and notification delivery remain separate checks; these mocked UI results do not certify production data or external LINE connectivity.

The existing Vite warning about a bundle larger than 500 kB remains. Existing React Router future-flag and unrelated test-fixture warnings also remain; no broad dependency or shared-theme changes were included.
