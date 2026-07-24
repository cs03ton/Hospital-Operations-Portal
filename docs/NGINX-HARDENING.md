# Nginx Hardening for HOP

`deploy/nginx.conf` เป็น reverse proxy สำหรับ Docker deployment และ `deploy/nginx.baremetal.conf.example` เป็นตัวอย่างสำหรับเครื่อง production แบบ build แล้ว sync ลง `/var/www/hop` และ `/opt/hop/backend`

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
- `server_tokens off` เพื่อลดข้อมูล fingerprint ของ nginx
- ตัวอย่าง rate limit สำหรับ login/API ใน `deploy/nginx.conf`

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

สำหรับ bare-metal:

```bash
sudo nginx -t
sudo nginx -T | grep -E "server_tokens|Content-Security-Policy|X-Frame-Options|limit_req"
sudo systemctl reload nginx
```

หากต้องการเปิด rate limit ใน bare-metal ให้เพิ่ม zone ใน `http` context ของ `/etc/nginx/nginx.conf` ก่อน เช่น:

```nginx
limit_req_zone $binary_remote_addr zone=hop_login:10m rate=5r/m;
limit_req_zone $binary_remote_addr zone=hop_api:10m rate=20r/s;
```

จากนั้นค่อยเปิด `limit_req` ใน location ที่เกี่ยวข้อง ห้ามใส่ `limit_req_zone` ภายใน `server` block เพราะ `nginx -t` จะไม่ผ่าน

## Smoke Check

```bash
curl -I https://<hop-domain>/health/live
curl -I https://<hop-domain>/health/ready
curl -I https://<hop-domain>/
curl -I https://<hop-domain>/api/csrf
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
