# Fleet Feedback Management Report (M3)

## Access

The report requires `FleetFeedback.ViewManagement`. M3 maps this permission to active `Admin`, `SuperAdmin`, and `Director` roles. Driver, Dispatcher, and Administration Reviewer roles receive no implicit access.

Normal management responses never contain `SubmittedByUserId`, employee code, username, or reviewer name. Identity access remains reserved for the separate M5 permission and audited endpoint.

## Endpoints

| Endpoint | Result |
|---|---|
| `GET /api/fleet/reports/feedback/trips` | Per-trip eligible count, feedback count, response rate, six averages, and incident count |
| `GET /api/fleet/reports/feedback/drivers` | Driver trip/response totals and driver-specific averages |
| `GET /api/fleet/reports/feedback/vehicles` | Vehicle trip/feedback totals, condition/cleanliness averages, and incident count |

All endpoints support `page` and `pageSize`. Trip filtering supports date range, driver, vehicle, department, mission type, overall rating, safety rating, incident flag/category, and request number.

## Calculation rules

- Eligible participant: actual `EMPLOYEE` participant whose user is not the assigned driver.
- Requester is counted once only when present as an actual participant.
- External passengers are excluded in this phase.
- Response rate: `FeedbackCount / EligibleParticipants × 100`, rounded to two decimal places.
- Driver averages use punctuality, safety, service, and overall ratings only.
- Vehicle averages use vehicle condition and cleanliness only.
- No leaderboard or best/worst driver designation is produced.
- Aggregates are calculated from `fleet_trip_feedbacks`; no mutable average is stored on driver or vehicle.

Every successful management report read records `Fleet.FeedbackViewedByManagement` without logging feedback comments.

