# Leave Fiscal Year Balance

Phase 1 uses a fiscal year balance model for leave quota tracking.

## Fiscal Year

Fiscal year starts on 1 October and ends on 30 September.

Examples:

| Date | Fiscal Year |
|---|---:|
| 30/09/2026 | 2026 |
| 01/10/2026 | 2027 |

## Balance Formula

```text
availableDays = entitledDays + carriedOverDays + adjustedDays - usedDays - pendingDays
```

Field meaning:

| Field | Meaning |
|---|---|
| entitledDays | Annual quota for the fiscal year |
| carriedOverDays | Days carried from the previous fiscal year |
| adjustedDays | Manual adjustment total for the fiscal year |
| usedDays | Days already approved and used |
| pendingDays | Days reserved by pending leave requests |
| availableDays | Days available after pending requests |

## Carry Over Rule

Carry over applies only to leave types with `allowCarryOver=true`.

```text
carriedOverDays = min(unusedDaysFromPreviousFiscalYear, carryOverMaxDays)
```

Carry over cap is resolved by `LeavePolicyService` from policy rules and employment type. Controllers must not hardcode the cap.

## Leave Type Configuration

Leave types support these balance policy fields:

| Field | Purpose |
|---|---|
| requiresBalance | Whether submit must validate leave balance |
| useFiscalYear | Whether the balance year follows 1 Oct - 30 Sep |
| allowCarryOver | Whether unused days can move to the next fiscal year |
| carryOverMaxDays | Maximum carried over days |

## Submit Validation

Backend validation is enforced on submit. The system checks:

- Leave type requires balance.
- Fiscal year balance exists or falls back to default annual quota.
- Pending days are deducted before allowing a new request.
- Half-day requests consume 0.5 day.
- Rejected and cancelled requests are not counted as pending.

If the balance is not enough, the API returns a Thai validation message similar to:

```text
วันลาคงเหลือไม่เพียงพอ คงเหลือ 5 วัน มีคำขอรออนุมัติ 3 วัน เหลือใช้ได้ 2 วัน แต่ขอลา 3 วัน
```

## Manual Rollover

Admin can run rollover from the Leave Balance Management page.

There are two modes:

- Batch fiscal year rollover with filters.
- Individual rollover by sending `userId` to the same preview/confirm API.

Preview API:

```http
POST /api/leave-balances/rollover/preview
Content-Type: application/json

{
  "fromFiscalYear": 2026,
  "toFiscalYear": 2027,
  "departmentId": null,
  "employmentType": null,
  "leaveTypeId": null,
  "userId": null
}
```

Confirm API:

```http
POST /api/leave-balances/rollover/confirm
Content-Type: application/json

{
  "fromFiscalYear": 2026,
  "toFiscalYear": 2027,
  "reason": "ยกยอดวันลาปีงบประมาณ 2570"
}
```

Behavior:

- Preview is dry-run and does not create/update leave balances.
- Confirm reruns preview before writing.
- Confirm requires a reason.
- Confirm writes in a transaction.
- Existing target balances are not duplicated.
- If target balance exists, only `carriedOverDays` is updated when policy allows.
- Confirm creates `leave_balance_rollover_runs` and `leave_balance_snapshots`.
- Backend fiscal years must be CE. Buddhist years are rejected.

Export preview:

```http
POST /api/leave-balances/rollover/export-preview
```

## Testing

Backend focused tests cover:

- 30 September stays in the current fiscal year.
- 1 October moves to the next fiscal year.
- Carry over is capped at 30 days by default.
- Pending days reduce available days.
- Submit is rejected when available days are not enough.
- Rollover preview does not create/update balances.
- Rollover confirm requires a reason.
- Rollover confirm is idempotent and does not duplicate target year balances.
- Buddhist fiscal year values are rejected at backend boundary.
