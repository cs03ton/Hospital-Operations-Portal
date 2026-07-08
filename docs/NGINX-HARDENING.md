# Nginx Hardening for HOP

`deploy/nginx.conf` เป็น reverse proxy สำหรับ HOP Phase 1

## สิ่งที่ตั้งค่าแล้ว

- Proxy `/api/*` ไป backend
- Proxy `/health`, `/healthz`, `/health/live`, `/health/ready` ไป backend
- SPA fallback ผ่าน frontend container
- `gzip` สำหรับ static/API text payload
- Security headers:
  - `X-Frame-Options`
  - `X-Content-Type-Options`
  - `Referrer-Policy`
  - `Permissions-Policy`
  - `Content-Security-Policy`
  - `Strict-Transport-Security` เมื่อมี `X-Forwarded-Proto=https`
- `client_max_body_size 20m` สำหรับไฟล์แนบใน Phase 1

## TLS

Production ควรใช้ HTTPS เสมอ

รูปแบบที่แนะนำสำหรับ Phase 1:

1. Terminate TLS ที่ reverse proxy ชั้นหน้า เช่น Nginx host, load balancer, หรือ Cloudflare Tunnel
2. ส่ง traffic เข้า `hop-nginx` container ผ่าน HTTP ภายในเครื่องหรือ Docker network
3. ส่ง header:
   ```text
   X-Forwarded-Proto: https
   ```

เมื่อมี `X-Forwarded-Proto=https`, HOP Nginx จะส่ง HSTS header ให้ browser

## ตรวจสอบ Config

```bash
docker compose --env-file .env.production -f docker-compose.prod.yml exec nginx nginx -t
docker compose --env-file .env.production -f docker-compose.prod.yml exec nginx nginx -T
```

## Smoke Check

```bash
curl -I https://<hop-domain>/health/live
curl -I https://<hop-domain>/health/ready
curl -I https://<hop-domain>/
```

ตรวจว่ามี header:

```text
X-Frame-Options
X-Content-Type-Options
Referrer-Policy
Permissions-Policy
Content-Security-Policy
```

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
