# Leave Calendar

## API

```text
GET /api/leave-calendar
```

## Permission

```text
LeaveManagement.View
```

## Filters

- `year`
- `month`
- `departmentId`
- `leaveTypeId`

## Behavior

- Shows leave requests with status `Pending` and `Approved`.
- Returns leave overlapping the selected month.
- Supports department and leave type filters.
- Frontend supports Thai month and Buddhist year selectors.

## Frontend

Route:

```text
/leave/calendar
```

UI language is Thai.
