# Fleet Participant Feedback API (M2)

## Participant endpoints

All endpoints derive the employee from the authenticated user. They never accept a participant, driver, vehicle, or assignment identity from the client.

| Method | Route | Permission | Purpose |
|---|---|---|---|
| GET | `/api/fleet/trips/{tripId}/feedback-context` | `FleetFeedback.ViewOwn` | Return participant-safe trip context and derived eligibility status |
| GET | `/api/fleet/trips/{tripId}/feedback/me` | `FleetFeedback.ViewOwn` | Return the current employee's submitted feedback |
| POST | `/api/fleet/trips/{tripId}/feedback` | `FleetFeedback.Create` | Submit one immutable feedback record |
| GET | `/api/fleet/my-feedback-eligible-trips` | `FleetFeedback.ViewOwn` | Return recent completed trips where feedback is available |

The context intentionally excludes approval comments, audit history, internal notes, licence data, and the passenger list. Request-detail permission is not granted through participation.

## Submission behavior

- Ratings are integers from 1 to 5.
- `IncidentCategory` and `Comment` are required when `HasIncident=true`.
- The service validates completed state, configured deadline, actual employee participation, driver exclusion, and prior submission.
- `UNIQUE(TripId, SubmittedByUserId)` is the final concurrent duplicate guard.
- A successful submission writes `Fleet.FeedbackCreated`; viewing own feedback writes `Fleet.FeedbackViewedOwn`.
- Feedback remains optional and does not mutate Fleet request, assignment, or trip status.

## Derived statuses

`AVAILABLE`, `SUBMITTED`, `EXPIRED`, and `NOT_ELIGIBLE` are calculated by `FleetFeedbackEligibilityService`; they are not Fleet workflow states.

