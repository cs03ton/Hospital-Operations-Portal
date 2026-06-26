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

Default maximum carry over is 30 days.

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

- Bulk fiscal year rollover for all active users.
- Individual rollover with preview and confirm.

API:

```http
POST /api/leave-balances/rollover
Content-Type: application/json

{
  "targetFiscalYear": 2027
}
```

Behavior:

- Reads previous fiscal year balances.
- Calculates available days.
- Creates new balances for active users and active leave types that require balance.
- Applies carry over only when the leave type allows it.
- Skips balances that already exist for the target year.
- Writes audit event `LeaveBalance.Rollover`.

Individual rollover APIs:

```http
POST /api/leave-balances/{id}/rollover-preview
```

```http
POST /api/leave-balances/{id}/rollover-confirm
Content-Type: application/json

{
  "toFiscalYear": 2027,
  "newEntitlementDays": 10,
  "reason": "ยกยอดวันลาปีงบประมาณ 2570",
  "updateExistingCarriedOverOnly": false
}
```

Individual rollover always requires preview before confirm in the UI. If the target fiscal year balance already exists, the system shows a warning and requires explicit confirmation to update only `carriedOverDays`.

## Testing

Backend focused tests cover:

- 30 September stays in the current fiscal year.
- 1 October moves to the next fiscal year.
- Carry over is capped at 30 days by default.
- Pending days reduce available days.
- Submit is rejected when available days are not enough.
- Individual rollover preview calculates carry over and forfeited days.
- Individual rollover confirm requires a reason.
