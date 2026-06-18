# HOP Project Summary

## Current Status

The initial project scaffold has been created according to `docs/SETUP-PROJECT.md` and `docs/TASK.md`.

This scaffold focuses on structure, configuration, and future-ready foundations. It does not include full business logic yet.

## Completed Scaffold

- Root project structure
- Frontend React + Vite + TypeScript structure
- Material UI healthcare theme
- Main layout with sidebar navigation
- Dashboard placeholder
- Placeholder pages for planned modules
- Backend .NET 9 Web API structure
- Entity Framework Core-ready database context
- JWT-ready settings
- CORS configuration
- Swagger configuration
- Health check endpoint
- Basic API response model
- PostgreSQL base schema
- Initial seed data for departments, roles, and permissions
- Docker Compose foundation
- Backend Dockerfile
- Frontend Dockerfile
- Nginx reverse proxy configuration

## Phase 1 Foundation Completed

- Authentication API
- JWT access token generation
- Refresh token storage
- Logout and current user endpoint
- BCrypt password hashing
- Basic RBAC with `SuperAdmin` and `Admin` protected admin APIs
- Default development admin user
- User management API foundation
- Department management API foundation
- Role lookup API foundation
- Audit log foundation for login success, login failure, and logout
- Frontend login page
- Frontend auth context and local session storage
- Protected routes
- Main layout with sidebar, topbar, dashboard, and logout
- User management foundation page
- Department management foundation page

## Phase 1.1 Admin Foundation Completed

- User create/edit/deactivate API and UI
- Department create/edit/deactivate API and UI
- Role create/edit/deactivate API and UI
- Permission list API
- Role permission get/update API
- Permission checkbox matrix UI
- Dashboard summary API
- Dashboard data integration
- Refresh token auto-retry in Axios interceptor
- EF Core migration created: `InitialAdminFoundation`
- Local EF tool manifest added for repeatable migration commands
- Thai-first frontend labels, menus, buttons, validation messages, and dashboard

## Phase 1.2 Security, Governance and Branding Foundation Completed

- Backend permission policy enforcement with `[RequirePermission]`
- Dynamic permission policies backed by `role_permissions`
- 403 denied-access audit logging
- Audit log viewer API with pagination, search, action filter, user filter, and date filter
- Frontend permission provider, permission guard, route protection, menu visibility, and action button visibility
- Audit log viewer page at `/admin/audit-logs`
- Hospital logo branding on login page, sidebar, header, and favicon
- Logo-based Material UI theme using primary green `#056839`
- Environment configuration foundation with `.env` and `.env.example`
- Leave Management database model prepared without Leave UI
- EF Core migration created: `Phase12SecurityGovernanceBranding`
- Backup and restore strategy documented

## Current Limitations

- Business workflows are not implemented yet.
- LINE notification sending is not implemented yet.
- Full module-specific business workflows are not implemented yet.
- Production secrets must be replaced before deployment.
- Refresh token rotation is implemented, but advanced token reuse detection is not yet implemented.
- Dashboard still returns `0` for modules not implemented yet.

## Phase 2 Leave Workflow Started

- Leave type API and management page
- Leave request list, create, detail, submit, cancel, approve, and reject
- Leave attachment upload with configured storage, extension allowlist, and size limit
- Leave balance API and My Leave Balance page
- Session management API and page
- Refresh token reuse detection
- Audit log CSV export and retention run endpoint
- LINE Messaging placeholder service

## Main URLs

- Frontend: `http://localhost:5173`
- API root: `http://localhost:5000/api`
- Health check: `http://localhost:5000/healthz`
- Nginx gateway: `http://localhost:8080`
