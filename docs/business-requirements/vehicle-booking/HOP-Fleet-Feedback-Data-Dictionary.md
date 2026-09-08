# Fleet Feedback Data Dictionary

## `fleet_trip_feedbacks`

| Column | Meaning |
|---|---|
| `trip_id` | Completed trip being evaluated |
| `fleet_request_id` | Immutable request snapshot reference |
| `vehicle_assignment_id` | Assignment used by the trip |
| `vehicle_id` | Vehicle snapshot reference at submission |
| `driver_user_id` | Actual driver snapshot reference at submission |
| `submitted_by_user_id` | Authenticated actual participant; retained for eligibility, duplicate prevention, own retrieval, audit, and abuse investigation |
| `punctuality_rating` | Driver punctuality, 1–5 |
| `safety_rating` | Driving safety, 1–5 |
| `service_rating` | Courtesy/service, 1–5 |
| `overall_rating` | Overall driver experience, 1–5 |
| `vehicle_condition_rating` | Vehicle condition, 1–5 |
| `vehicle_cleanliness_rating` | Vehicle cleanliness, 1–5 |
| `has_incident` | Indicates a report requiring attention |
| `incident_category` | `DRIVING`, `PUNCTUALITY`, `SERVICE`, `VEHICLE_CONDITION`, `CLEANLINESS`, or `OTHER` |
| `comment` | Optional unless an incident is reported |
| `submitted_at` | UTC submission timestamp |
| `concurrency_token` | Optimistic concurrency token |

Identity is not part of the normal management projection. Management aggregation and response-rate reporting are deferred to M3.

## Response-rate definition (for M3)

`feedback count / eligible actual employee participants × 100`.

The denominator excludes the assigned driver, external participants, and non-actual requested passengers. A requester who actually travelled counts once.

