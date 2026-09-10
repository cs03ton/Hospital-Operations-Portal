# HOP Deployment Notes

This folder contains the initial deployment foundation for Hospital Operations Portal.

## Files

- `backend.Dockerfile` builds and runs the .NET 9 API.
- `frontend.Dockerfile` builds the React app and serves it with Nginx.
- `nginx.conf` proxies frontend traffic and API traffic.
- `sql/01-prd-schema-migrations.sql` is the idempotent EF Core schema bundle for DBeaver.
- `sql/02-prd-master-data.sql` loads production master data, 13 vehicles, and 5 driver profiles.

## Production Database Order

1. Create and verify a PostgreSQL backup.
2. Run `sql/01-prd-schema-migrations.sql` in DBeaver with **Stop on error** enabled.
3. Run `sql/02-prd-master-data.sql` in DBeaver.
4. Fill `.env.production` from `.env.production.example`; never commit the populated file.
5. Validate configuration, restart services, and run the smoke test.

Do not use `database/seed.sql` for production bootstrap.

## Local Docker Run

From the project root:

```bash
docker compose up --build
```

Open:

- Frontend: http://localhost:5173
- API: http://localhost:5000/api
- Health check: http://localhost:5000/healthz
- Nginx gateway: http://localhost:8080
