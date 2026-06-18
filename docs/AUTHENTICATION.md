# Authentication

Phase 1 implements the authentication foundation for Hospital Operations Portal.

## Login Flow

1. User opens `/login`.
2. Frontend posts username and password to `POST /api/auth/login`.
3. Backend verifies the BCrypt password hash.
4. Backend returns an access token, refresh token, and user profile.
5. Frontend stores the session according to `AUTH_TOKEN_STORAGE_MODE` and redirects to `/dashboard`.

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

In cookie mode, the refresh token is read from an httpOnly cookie and the response does not expose the refresh token in the JSON payload.

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

## Login Rate Limit

The backend includes configurable in-memory login lockout for repeated failed login attempts.

Environment keys:

```text
LoginRateLimit__Enabled=true
LoginRateLimit__MaxFailedAttempts=5
LoginRateLimit__WindowMinutes=15
LoginRateLimit__LockoutMinutes=15
```

When the limit is reached, login returns HTTP `429` and records:

```text
Auth.LoginLocked
```

## Token Storage Modes

Default mode remains backward compatible:

```text
AUTH_TOKEN_STORAGE_MODE=LocalStorage
VITE_AUTH_TOKEN_STORAGE_MODE=localStorage
```

Hardened cookie mode:

```text
AUTH_TOKEN_STORAGE_MODE=Cookie
AUTH_COOKIE_SECURE=true
AUTH_COOKIE_SAMESITE=Lax
AUTH_COOKIE_DOMAIN=
AUTH_COOKIE_CSRF_ENABLED=true
CORS_ALLOW_CREDENTIALS=true
VITE_AUTH_TOKEN_STORAGE_MODE=cookie
VITE_AUTH_CSRF_COOKIE_NAME=hop_csrf_token
VITE_AUTH_CSRF_HEADER_NAME=X-CSRF-TOKEN
```

Cookie mode behavior:

- Refresh token is stored in an httpOnly cookie named `hop_refresh_token`.
- Cookie path is `/api/auth`.
- Cookie uses configured `Secure`, `SameSite`, and optional `Domain`.
- Cookie mode issues a readable CSRF cookie named `hop_csrf_token`.
- Unsafe requests must send the same value in the `X-CSRF-TOKEN` header.
- Access token is kept memory-only in the frontend.
- The frontend sends refresh requests with credentials.
- Existing JSON refresh-token flow remains available in localStorage mode.

## CSRF Protection

Cookie mode uses double-submit CSRF protection.

Rules:

- `GET`, `HEAD`, `OPTIONS`, and `TRACE` are not checked.
- `POST /api/auth/login` is not checked because no CSRF cookie exists before login.
- Other unsafe requests in cookie mode require:

```text
Cookie: hop_csrf_token=<token>
X-CSRF-TOKEN: <token>
```

Failed validation returns HTTP `400` and records:

```text
Security.CsrfValidationFailed
```

Before using `AUTH_COOKIE_SAMESITE=None`, keep CSRF enabled and set `AUTH_COOKIE_SECURE=true`.

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
