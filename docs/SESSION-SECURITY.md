# Session Security

Phase 2 adds session management on top of refresh token rotation.

## Refresh Token Metadata

Added metadata:

- `created_by_ip`
- `user_agent`
- `last_used_at`
- `revoked_reason`
- `replaced_by_token`

## Reuse Detection

When `REFRESH_TOKEN_REUSE_DETECTION_ENABLED=true`, a reused inactive refresh token revokes active sessions for that user and records:

```text
Auth.RefreshTokenReuseDetected
```

## APIs

- `GET /api/sessions`
- `POST /api/sessions/{id}/revoke`

Both require:

```text
SystemSettings.Manage
```
