# HOP Fleet Dashboard Permission Matrix

| Capability | Actual permission group | M2 navigation | M3 dashboard section |
| --- | --- | --- | --- |
| Requester | `FleetRequest.ViewOwn`, `FleetRequest.Create` | Dashboard, requests, calendar | Own requests, next trip, own actions |
| Driver | `FleetDriver.ViewOwnJobs`, `FleetDriver.ViewOwn` | Dashboard, my trips, calendar | Own jobs and own trip summary |
| Dispatcher | `FleetDispatch.View`, `FleetDispatch.Assign` | Dashboard, dispatch, calendar | Dispatch queue and scoped operations |
| Admin reviewer | `FleetRequest.ViewOwn/Create` + `FleetAdminReview.*` | Dashboard, own requests, combined review/approval menu, calendar | Admin-review queue |
| Director | `FleetRequest.ViewOwn/Create` + `FleetDirector.*` | Dashboard, own requests, combined review/approval menu, calendar | Director approval queue |
| Reports | `FleetReport.View`, `FleetReport.Export` | Reports | Scoped reporting |
| Fleet settings | `FleetSettings.Manage` | Settings | Fleet-only administration and health |

M3 must evaluate the effective permission set server-side and return `null` for unauthorized sections. Frontend widget composition must use the same capability metadata and must not fetch hidden sections.
