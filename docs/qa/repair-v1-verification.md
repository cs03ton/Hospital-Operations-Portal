# Repair v1 Local Verification

Date: 2026-09-11. No production requests, accounts, role bindings or databases were changed.

## Results

| Check | Result |
| --- | --- |
| Backend regression suite | 392 passed, 6 skipped |
| Repair PostgreSQL integration tests | 7 passed on separate generated local databases |
| Frontend unit/component suite | 99 passed, 18 files |
| Browser responsive matrix | 25 passed |
| Targeted frontend ESLint | Passed |
| Frontend build | Passed; output in frontend/dist |
| Backend build | Passed |
| EF pending-model check | No pending model changes |
| Nginx syntax | Docker, bare-metal and documentation templates passed nginx -t |

The six skipped backend cases are the existing BackupSyncPostgresTests, whose separate connection setting was not supplied.
NuGet vulnerability metadata could not be fetched (NU1900). Existing EF tool/runtime version, nullable Fleet test and frontend bundle-size warnings remain; they did not block builds.

## Database And Workflow

The fixture creates only uniquely named `hop_repair_test_*` databases on localhost and drops those databases after each test.
It applies the existing migrations up to CorrectFleetRolePermissions, then runs both deployment SQL files twice.
It verifies two teams, eleven initial categories, unique request numbers and the repair migration history entry.

Covered: start/wait/resume, solve, failed acceptance, long acceptance notes, close/reopen with preserved rounds,
return/resubmit to another team, denied cross-team access, dual technician roles, primary solver/contributor/actor separation,
concurrent status updates (one success and one 409), protected image download, batch limits and stale uploads.
HTTP tests use the real permission handler and JWT authentication with test-only signing configuration;
anonymous calls return 401, missing permissions return 403, and a disabled account cannot reuse its earlier token.

Group delivery tests use a fake external sender to verify transient retry, optimistic claiming, no repeated successful dispatch,
team destination selection and absence of symptoms in the group message. No real LINE or MorProm message was sent.

## Browser Matrix

Roles: Staff, IT, general maintenance, both technician permissions, Admin.
Widths: 375, 430, 768, 1366 and 1920 pixels.
Routes: repair dashboard, list, detail, create and administrative settings.
Tests verify page overflow, visible actions, required solver validation and dialog bounds using mocked API responses.
Screenshots are generated under `frontend/test-results/repairs-responsive-*`; mobile and desktop output was visually inspected.
Shared query polling tests cover foreground-only polling/focus/reconnect; the repair query test asserts the 15-second interval.
Browser mocks are UI checks, not a substitute for the independent PostgreSQL and HTTP authorization tests.

## Before Pilot Use

1. Back up, then apply the repair migration and master SQL to the intended UAT database.
2. Assign technician roles manually to approved test accounts; no account receives a technician role automatically.
3. Configure the two HTTPS MorProm destinations, trusted hosts, persistent Data Protection keys and file scanner.
4. Apply the scoped Nginx upload limit; do not expose the repair storage directory through a public alias.
5. Enable Repairs__NotificationsEnabled only after configuration, then perform a real test message and requester/technician acceptance workflow in UAT.

Live MorProm credentials, live antivirus service behaviour, external reverse proxy and production end-to-end workflows were not exercised here.
SLA scoring, asset registry, stock, purchasing and PM remain outside v1.
