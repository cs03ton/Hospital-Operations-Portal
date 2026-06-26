# Leave Balance Adjustment

Phase 2.1 adds admin tools to adjust leave balance entitlements.

## Table

`leave_balance_adjustments`

Stores:

- User
- Leave type
- Year
- Adjustment days
- Reason
- Adjusted by user
- Created timestamp

## APIs

- `GET /api/leave-balance-adjustments`
- `POST /api/leave-balance-adjustments`

## Frontend

- `/admin/leave-balances/adjustments`

## Behavior

Creating an adjustment updates `leave_balances.adjusted_days`.

Positive values increase entitlement.
Negative values decrease entitlement.

The displayed available balance uses:

```text
entitledDays + carriedOverDays + adjustedDays - usedDays - pendingDays
```

## Permission

- `LeaveBalance.Adjusted`

## Audit Event

- `LeaveBalance.Adjust`
