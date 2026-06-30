# Phase 1 Pilot Test Report

## Test Summary

- Test date: 2026-06-30
- Environment: Local development / Phase 1 pilot readiness
- Scope: User Management, Leave Management, Notification, LINE, PDF, Leave Balance
- Automated test project: `backend/Hop.Api.Tests`
- Frontend build: `frontend`
- Automated result: 86 passed, 0 failed, 0 skipped
- Frontend build result: Passed
- Recommendation: Conditional GO after manual browser E2E is rerun against the target pilot database and LINE test user.

## Test Accounts

Use development or pilot seed accounts only. Do not use real hospital staff data.

| Username | Role | Purpose |
|---|---|---|
| staff01 | Staff | Create, submit, cancel, and track own leave request |
| head01 | DepartmentHead | Approve current step and verify pending notifications |
| director01 | Director | Approve final step and verify self-approval protection |
| admin_support | Admin | Monitor/support functions; must not create leave |
| SuperAdmin | SuperAdmin | Support and system administration; must not use normal approval silently |

## Critical Flow Cases

| No | Module | Test Case | Step | Expected | Actual | Status | Severity | Screenshot |
|---|---|---|---|---|---|---|---|---|
| 1 | Auth | Staff login | Login as `staff01` | Login succeeds and dashboard opens | Covered by manual E2E checklist | Manual | High | - |
| 2 | Leave | Create leave request | Staff creates full-day leave | Draft created with request number | Covered by manual E2E checklist | Manual | High | - |
| 3 | Leave | Submit leave request | Staff submits draft | Status becomes pending, current approver is Head, pending balance increases | Existing submit validation and manual E2E | Manual | Critical | - |
| 4 | Notification/LINE | Head receives approval task | Login as `head01` | Bell/dashboard/pending page shows only current task; LINE sends/queues safely | Existing notification + LINE tests, manual LINE send required | Partial | Critical | - |
| 5 | Approval | Head approves | Approve current step | Current approver moves to Director, status stays Pending | Automated in `Phase1CriticalLeaveWorkflowTests` | Passed | Critical | - |
| 6 | Notification/LINE | Director receives approval task | Login as `director01` after Head approval | Director sees 1 task; Head no longer sees it | Automated state transition, manual UI still required | Partial | Critical | - |
| 7 | Approval | Director approves final step | Approve final step | Status Approved, CurrentApproverId null, pending cleared, used balance deducted | Automated in `Phase1CriticalLeaveWorkflowTests` | Passed | Critical | - |
| 8 | PDF | Download leave PDF | Download from leave detail | PDF starts with valid PDF header and Thai-capable generation path works | Existing `LeavePdfTests`, manual visual PDF check required | Partial | High | - |
| 9 | Reject | Head rejects | Head rejects pending request | Status Rejected, notification cleared, pending balance returned, used balance unchanged | Automated in `Phase1CriticalLeaveWorkflowTests` | Passed | Critical | - |
| 10 | Cancel | Staff cancels pending request | Staff cancels before approval | Status Cancelled, current approver cleared, pending balance returned | Automated in `Phase1CriticalLeaveWorkflowTests` | Passed | Critical | - |
| 11 | Permission | Staff visibility | Staff opens leave list/calendar | Sees only own allowed records | Existing access tests cover own access; manual UI required | Partial | Critical | - |
| 12 | Permission | Head visibility | Head opens leave list/calendar | Sees own + staff in department, not other departments | Existing access tests cover department staff behavior | Passed | Critical | - |
| 13 | Permission | Approve wrong step | Director attempts approve before current step | API forbids action | Automated in `Phase1CriticalLeaveWorkflowTests` | Passed | Critical | - |
| 14 | Permission | Admin/SuperAdmin create leave | Admin opens create route/API | Create is blocked; button hidden in UI | Backend has explicit Admin/SuperAdmin guard; manual UI required | Partial | High | - |
| 15 | Balance | Half-day leave | Submit half-day AM/PM | Calculates 0.5 day and rejects multi-date half-day | Existing `LeaveValidationTests` | Passed | High | - |
| 16 | Balance | Over available balance | Submit leave exceeding available days | Backend rejects with Thai balance message | Existing `LeaveValidationTests` | Passed | Critical | - |
| 17 | Balance | Pending balance included | Submit while pending requests consume quota | Backend rejects when available minus pending is insufficient | Existing `LeaveValidationTests` | Passed | Critical | - |
| 18 | Rollover | Individual rollover preview/confirm | Preview and confirm next fiscal year | Carry over capped and auditable | Existing `LeaveBalanceRolloverTests` | Passed | High | - |
| 19 | LINE | LINE disabled/failure | Disable token or return LINE failure | Workflow does not fail; delivery log records status | Existing `LineRetryTests` | Passed | High | - |
| 20 | LINE Flex | Pending/approved/rejected cards | Build Flex JSON | Valid flex payload with safe URLs and status-specific buttons | Existing `LineRetryTests` | Passed | High | - |

## Bugs Found

## BUG-001: Director visibility may be broader than policy

- Module: Leave visibility
- Step: Inspect `LeaveRequestAccessService.GetVisibilityAsync`
- Expected: Director sees own requests and current pending approvals unless granted explicit wider permission.
- Actual: Fixed. Director role is no longer treated as `ViewAll` in the access service.
- Severity: High
- Screenshot: -
- Status: Fixed
- Suggested Fix: Completed. `Director`, `Admin`, and `SuperAdmin` no longer receive `ViewAll` from role name alone. All-request visibility now requires `LeaveRequest.ViewAll` or `LeaveSupport.ViewAll`.

## Remaining Risks

- Manual browser E2E and screenshots were not produced by this report update. Rerun the browser flow on the exact pilot database before go-live.
- LINE real delivery requires a valid public channel access token and a real test LINE user ID. Automated tests only mock LINE API behavior.
- PDF tests verify valid bytes and generation path; final Thai visual rendering should still be opened and inspected manually.
- Docker/PostgreSQL smoke test depends on Docker Desktop or target server availability.
- `dotnet test` passed but emitted a non-blocking warning that the test project could not write `obj\Debug\net9.0\Hop.Api.Tests.csproj.AssemblyReference.cache`.
- `npm run build` passed but emitted the existing Vite warning that the main JavaScript chunk is larger than 500 kB.

## Go / No-Go Recommendation

Conditional GO:

- Automated backend critical tests and frontend production build must pass.
- Manual E2E must confirm Staff -> Head -> Director approval with notification bell, LINE delivery logs, PDF visual output, and leave balance before pilot.
- BUG-001 has been fixed in backend visibility logic and regression tests.

## Commands Used

```powershell
dotnet test backend/Hop.Api.Tests/Hop.Api.Tests.csproj
cd frontend
npm run build
```

## Latest Automated Results

```text
dotnet test backend/Hop.Api.Tests/Hop.Api.Tests.csproj
Result: Passed
Total: 86
Passed: 86
Failed: 0
Skipped: 0
Note: Non-blocking MSB3101 warning while writing a test obj cache file.

BUG-001 regression rerun:
dotnet test backend/Hop.Api.Tests/Hop.Api.Tests.csproj --filter "FullyQualifiedName~LeaveSecurityHardeningTests|FullyQualifiedName~Phase1CriticalLeaveWorkflowTests|FullyQualifiedName~AuthAndPermissionTests"
Result: Passed
Total: 13
Passed: 13
Failed: 0

npm run build
Result: Passed
Note: Existing Vite chunk-size warning for assets/index-*.js.
```

If local dev processes lock build outputs, use the isolated build/vstest workaround documented in `docs/TESTING.md`.
