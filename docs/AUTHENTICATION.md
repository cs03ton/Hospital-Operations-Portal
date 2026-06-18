# Authentication

Phase 1 implements the authentication foundation for Hospital Operations Portal.

## Login Flow

1. User opens `/login`.
2. Frontend posts username and password to `POST /api/auth/login`.
3. Backend verifies the BCrypt password hash.
4. Backend returns an access token, refresh token, and user profile.
5. Frontend stores the session in local storage and redirects to `/dashboard`.

## JWT Flow

The access token is a JWT signed with the configured `Jwt:Key`.

The token includes:

- User id
- Username
- Full name
- Role
- Department name when available

Authentication responses also include the user's effective permission codes for frontend route, menu, and action guards.

Protected APIs require the `Authorization` header:

```text
Authorization: Bearer <access-token>
```

Admin APIs require permission policies such as `UserManagement.View` or `RoleManagement.Manage`.

## Refresh Token Flow

`POST /api/auth/refresh-token` accepts a refresh token and returns a new access token and refresh token.

When a refresh token is used, the old token is revoked and a new token is stored in the `refresh_tokens` table.

## Logout Flow

`POST /api/auth/logout` revokes the submitted refresh token and clears the frontend session.

## Default Development Admin

Local development default:

```text
Username: admin
Password: Admin@1234
Role: SuperAdmin
```

This account is for development only. Change or remove it before production deployment.

## Protected Route Behavior

Frontend protected routes check the auth context.

- Authenticated users can access `/dashboard`, `/admin/users`, `/admin/departments`, and `/admin/settings`.
- Unauthenticated users are redirected to `/login`.
- Logout clears local session storage and returns the user to `/login`.

## Refresh Token Auto-Retry

The frontend Axios client attaches the access token to API requests automatically.

If a request returns `401`, the client calls `POST /api/auth/refresh-token` with the stored refresh token and retries the original request once.

If refresh fails, the local session is cleared and the user is redirected to `/login`.

## Thai UI Default

Frontend authentication screens and admin screens display Thai text by default.

Backend endpoint names, source code, database table names, and column names remain English for development standards.
