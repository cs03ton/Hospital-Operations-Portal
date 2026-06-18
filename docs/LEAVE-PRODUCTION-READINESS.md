# Leave Production Readiness

Phase นี้ทำให้ Leave Management พร้อมขึ้นสำหรับการใช้งานจริงในโรงพยาบาล

## Completed

- `database/schema.sql` aligned with the EF Core model and latest migrations.
- `database/seed.sql` aligned with `DevelopmentDataSeeder` permission groups and actions.
- Leave PDF export endpoint added.
- Dashboard leave metrics now read from real leave tables.
- LINE Messaging service now attempts real push delivery when enabled and records failed delivery when configuration or recipient mapping is missing.
- Backend unit tests added for auth, permission, leave validation, and PDF generation.
- GitHub Actions CI added for backend build/test and frontend build.

## Production Notes

- Use EF Core migrations as the database source of truth.
- Use `database/schema.sql` only for fresh Docker initialization.
- Do not use a PostgreSQL superuser in the application connection string.
- Replace the development admin password before production.
- Enable LINE only after `LINE_ACCESS_TOKEN` and user `line_user_id` values are configured.

## Verification Commands

```powershell
dotnet build backend/Hop.Api/Hop.Api.csproj
dotnet test backend/Hop.Api.Tests/Hop.Api.Tests.csproj
cd frontend
npm ci
npm run build
```
