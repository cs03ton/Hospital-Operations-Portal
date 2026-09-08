# HOP Module Dashboard Pattern

Module dashboards use a single route and compose sections from server-authorized capabilities. The visual order is: immediate work, today's overview, status summary, next activity and privileged system attention.

Required states are loading, scoped empty, retryable error and manual refresh. Cards use the HOP spacing, typography, border and responsive grid tokens. Mobile starts at two compact metric columns and must not overflow horizontally.

Frontend visibility is presentation only. Route guards and backend authorization remain mandatory. A missing or `null` section is not inferred from a role.
