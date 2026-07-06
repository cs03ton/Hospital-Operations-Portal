# Environment Variables

เอกสารนี้สรุป environment variables ที่ใช้กับ HOP ใน Development, Production, Docker และ Ubuntu Server

## Development บน Windows

1. คัดลอก `.env.example` เป็น `.env.development`
2. ใส่ค่าที่ต้องใช้จริงเฉพาะเครื่อง development
3. รัน backend ด้วย `dotnet run`
4. รัน frontend ด้วย `npm run dev`

```powershell
Copy-Item .env.example .env.development
dotnet run --project backend/Hop.Api/Hop.Api.csproj
```

## Production บน Ubuntu + Docker Compose

1. คัดลอก `.env.production.example` เป็น `.env.production`
2. ใส่ค่าจริงบน server
3. ตรวจ env ก่อน deploy
4. รัน Docker Compose

```bash
cp .env.production.example .env.production
nano .env.production
./deploy/00-check-env.sh
docker compose --env-file .env.production -f docker-compose.prod.yml up -d --build
```

## Required Production Keys

| Key | Required | หมายเหตุ |
|---|---:|---|
| `ASPNETCORE_ENVIRONMENT` | Yes | Production |
| `ConnectionStrings__DefaultConnection` | Yes | ตั้งตรงหรือให้ deploy script สร้างจาก DB keys |
| `Jwt__Key` | Yes | Secret |
| `Line__Enabled` | Yes | true/false |
| `Line__AccessToken` | Yes | Secret |
| `Line__ChannelSecret` | Yes | Secret |
| `PUBLIC_APP_URL` | Yes | Public HTTPS URL |
| `Storage__RootPath` | Yes | Path เก็บไฟล์ |
| `Storage__PublicBaseUrl` | Yes | Public file URL |
| `DB_HOST` | Yes | Host หรือ service name |
| `DB_PORT` | Yes | PostgreSQL port |
| `DB_NAME` | Yes | Database name |
| `DB_USER` | Yes | Database user |
| `DB_PASSWORD` | Yes | Secret |

## Backward-Compatible Aliases

ระบบยังรองรับ key เดิมบางตัวเพื่อไม่ให้เครื่องเดิมพังทันที

| Legacy Key | Standard Key |
|---|---|
| `LINE_ACCESS_TOKEN` | `Line__AccessToken` |
| `LINE_CHANNEL_ACCESS_TOKEN` | `Line__AccessToken` |
| `LINE_CHANNEL_SECRET` | `Line__ChannelSecret` |
| `LINE_ENABLED` | `Line__Enabled` |
| `STORAGE_ROOT_PATH` | `Storage__RootPath` |
| `STORAGE_PUBLIC_BASE_URL` | `Storage__PublicBaseUrl` |

## Secret Handling Policy

1. ห้าม commit `.env`, `.env.development`, `.env.production`
2. ห้าม commit token หรือ password ลงเอกสาร
3. ให้ใช้ `.env.production.example` เป็น template เท่านั้น
4. Log ได้เฉพาะสถานะ `configured=true/false`
5. Frontend รับเฉพาะ masked status เช่น `hasAccessToken`

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
