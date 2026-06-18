# Admin Module

Phase 1.1 implements the admin foundation for Hospital Operations Portal.

## User Management

Routes:

- `/admin/users`
- `/admin/users/create`
- `/admin/users/{id}/edit`

API:

- `GET /api/users`
- `GET /api/users/{id}`
- `POST /api/users`
- `PUT /api/users/{id}`
- `DELETE /api/users/{id}`

Rules:

- Password is required when creating a user.
- Password is optional when editing a user.
- Passwords are hashed with BCrypt.
- `passwordHash` is never returned to the frontend.
- Delete is implemented as soft delete by setting `isActive = false`.

## Department Management

Routes:

- `/admin/departments`
- `/admin/departments/create`
- `/admin/departments/{id}/edit`

API:

- `GET /api/departments`
- `GET /api/departments/{id}`
- `POST /api/departments`
- `PUT /api/departments/{id}`
- `DELETE /api/departments/{id}`

Rules:

- Department name must be unique.
- Delete is implemented as soft delete by setting `isActive = false`.

## Role Management

Routes:

- `/admin/roles`
- `/admin/roles/{id}/permissions`

API:

- `GET /api/roles`
- `GET /api/roles/{id}`
- `POST /api/roles`
- `PUT /api/roles/{id}`
- `DELETE /api/roles/{id}`

Rules:

- Role name must be unique.
- System roles cannot be deleted.
- Custom roles can be deactivated.

## Dashboard

API:

- `GET /api/dashboard/summary`

Current summary values:

- Total users
- Total departments
- Pending approvals
- Staff on leave today
- Staff on leave this week
- Staff on leave this month
- Current user's remaining leave days
- Open repair requests
- Active borrow requests
- Inventory items

Modules not implemented yet still return `0`.

## Audit Logs

Route:

- `/admin/audit-logs`

API:

- `GET /api/audit-logs`
- `GET /api/audit-logs/{id}`

Rules:

- Requires `SystemSettings.View`.
- Supports pagination, search, action filter, user filter, and date filter.
- Denied permission attempts are logged with result `Denied`.
