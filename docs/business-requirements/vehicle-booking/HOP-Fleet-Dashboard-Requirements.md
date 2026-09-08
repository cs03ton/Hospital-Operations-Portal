# HOP Fleet Dashboard Requirements

## Purpose

Fleet uses one dashboard at `/fleet/dashboard`. Server-resolved permissions and active `Scope=FLEET` delegation determine which sections are returned and displayed; role names are not authorization inputs.

## Composition

| Capability | Dashboard section | Primary action |
|---|---|---|
| Requester | Own status summary, action required, next trip | Own requests |
| Driver | Today/upcoming jobs, acknowledgement/action required, monthly totals | My driving jobs |
| Dispatcher | Dispatch queue, vehicle/driver availability, overdue work | Dispatch queue |
| Admin reviewer | Pending, urgent, near-departure and daily review totals | Review queue |
| Director | Pending, urgent, near-departure and daily approval totals | Approval queue |
| Fleet admin | Monthly requests/trips/distance, cancellation, overdue and outbox health | Reports |

All Fleet users receive the permission-scoped shared overview. Nullable server sections must not be reconstructed from frontend role names.

## Interaction requirements

- Loading, scoped empty, retryable error, manual refresh, mobile layout and Thai/Asia-Bangkok time are mandatory.
- Sidebar badges and dashboard counts share the `['fleet-dashboard']` query.
- Successful workflow actions invalidate that query. Failed actions do not optimistically change confirmed counts.
- Deep links retain route and backend authorization.
- Active delegated permissions enable the corresponding navigation, widget and guarded route; expiry is re-evaluated within 60 seconds or on window focus.

## Privacy

Requester scope contains only the signed-in user's requests. Driver scope contains only active assignments for that driver. Internal queues, driver availability and system health are omitted unless the effective permission permits them.
