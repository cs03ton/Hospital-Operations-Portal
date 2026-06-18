# Security Hardening

## Token Storage

Default mode remains backward compatible:

```text
AUTH_TOKEN_STORAGE_MODE=LocalStorage
VITE_AUTH_TOKEN_STORAGE_MODE=localStorage
```

Hardened mode:

```text
AUTH_TOKEN_STORAGE_MODE=Cookie
AUTH_COOKIE_SECURE=true
AUTH_COOKIE_SAMESITE=Lax
AUTH_COOKIE_DOMAIN=
CORS_ALLOW_CREDENTIALS=true
VITE_AUTH_TOKEN_STORAGE_MODE=cookie
```

Cookie mode stores the refresh token in an httpOnly cookie and keeps the access token memory-only in the frontend.

Recommended production settings:

- `AUTH_COOKIE_SECURE=true`
- `AUTH_COOKIE_SAMESITE=Lax` or `Strict` when cross-site login is not required
- Set `AUTH_COOKIE_DOMAIN` only when sharing cookies across approved subdomains
- Keep `Cors__AllowedOrigins` restricted to trusted frontend origins
- Enable `Cors__AllowCredentials` only for cookie mode

## Login Rate Limit

```text
LoginRateLimit__Enabled=true
LoginRateLimit__MaxFailedAttempts=5
LoginRateLimit__WindowMinutes=15
LoginRateLimit__LockoutMinutes=15
```

Locked attempts return HTTP `429` and record `Auth.LoginLocked`.

## File Scanning

Recommended production setting:

```text
FILE_SCAN_ENABLED=true
FILE_SCAN_PROVIDER=ClamAV
FILE_SCAN_FAIL_CLOSED=true
CLAMAV_HOST=<clamav-host>
CLAMAV_PORT=3310
```

Fail-closed mode rejects uploads when ClamAV is unavailable.

## Remaining Hardening Backlog

- Move access token fully out of localStorage in all deployed environments by enabling cookie mode.
- Add CSRF protection before using `SameSite=None`.
- Add security headers at the reverse proxy.
- Add centralized monitoring for failed login, scan failure, and permission denied audit events.
