# Notification Architecture

Hospital Operations Portal uses a role-based notification architecture.

## Goals

- Users see only notifications related to their role and responsibility.
- Action required notifications are separated from information notifications.
- Badge count is calculated from `ActionRequired` and unread items only.
- Notification Center supports filtering, search, and paging.

## Backend Model

The `notifications` table supports:

- `category`
- `notification_type`
- `priority`
- `target_role`
- `action_url`
- `reference_entity`
- `reference_id`
- `expires_at`
- `archived_at`
- `read_at`

Existing notifications remain compatible through defaults:

- Category: `Leave`
- Type: `Information`
- Priority: `Information`

## API

| Endpoint | Purpose |
|---|---|
| `GET /api/notifications/me` | Bell dropdown summary |
| `GET /api/notifications` | Notification Center with paging/filter/search |
| `GET /api/notifications/badge` | Badge count |
| `POST /api/notifications/{id}/read` | Mark persisted notification as read |
| `POST /api/notifications/{id}/archive` | Archive persisted notification |

## Role Filtering

The backend loads the current user's roles and permissions, then composes notifications from:

- persisted in-app notifications assigned to the user
- persisted role-targeted notifications
- pending approval queue for current approvers
- owner leave request updates
- department head team summaries
- director executive summaries
- admin/superadmin system alerts

Frontend filtering is UX only. Backend remains the authority.
