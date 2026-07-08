# Bare-Metal Deployment Layout

เอกสารนี้อธิบายการ deploy HOP แบบติดตั้งบน Ubuntu เครื่องเดียว โดยไม่ใช้ Docker Compose เป็น runtime หลัก

## Production Structure

```text
/opt/hop/
├── backend/     # publish output ของ .NET API
├── uploads/     # ไฟล์แนบจากระบบลา/เอกสาร/profile images
├── logs/        # backend logs
├── scripts/     # deploy/backup scripts
├── backups/     # backup db/config/uploads
└── releases/    # release ย้อนหลัง

/var/www/hop/    # frontend static files จาก React/Vite build
/etc/hop/        # config เช่น hop-api.env หรือ appsettings.Production.json
```

## Required Config

ใช้ไฟล์หลัก:

```text
/etc/hop/hop-api.env
```

สำหรับการใช้งานผ่าน HTTP ใน LAN ชั่วคราว:

```env
PUBLIC_APP_URL=http://172.16.2.99
VITE_API_BASE_URL=
VITE_AUTH_TOKEN_STORAGE_MODE=cookie

Auth__TokenStorageMode=Cookie
Auth__Cookie__Secure=false
Auth__Cookie__SameSite=Lax
Auth__Cookie__Domain=
Auth__Cookie__CsrfEnabled=true
Auth__Cookie__CsrfTokenName=hop_csrf_token
Auth__Cookie__CsrfHeaderName=X-CSRF-TOKEN

Cors__AllowCredentials=true
Cors__AllowedOrigins__0=http://172.16.2.99
Storage__RootPath=/opt/hop/uploads
```

เมื่อใช้ HTTPS จริง ให้เปลี่ยน:

```env
PUBLIC_APP_URL=https://<domain>
Auth__Cookie__Secure=true
Cors__AllowedOrigins__0=https://<domain>
```

## First-Time Setup

```bash
sudo mkdir -p /opt/hop/{backend,uploads,logs,scripts,backups,releases}
sudo mkdir -p /var/www/hop /etc/hop
sudo cp .env.production.example /etc/hop/hop-api.env
sudo nano /etc/hop/hop-api.env
sudo chmod 600 /etc/hop/hop-api.env
```

Install systemd service:

```bash
sudo cp systemd/hop-api.service.example /etc/systemd/system/hop-api.service
sudo systemctl daemon-reload
sudo systemctl enable hop-api
```

Install Nginx config:

```bash
sudo cp deploy/nginx.baremetal.conf.example /etc/nginx/sites-available/hop
sudo ln -sfn /etc/nginx/sites-available/hop /etc/nginx/sites-enabled/hop
sudo nginx -t
sudo systemctl reload nginx
```

## Deploy

จาก repository checkout:

```bash
DEPLOY_MODE=baremetal ENV_FILE=/etc/hop/hop-api.env bash deploy/00-check-env.sh
ENV_FILE=/etc/hop/hop-api.env bash scripts/deploy/deploy-baremetal.sh
```

ลำดับที่ script ทำ:

1. โหลด `/etc/hop/hop-api.env`
2. backup database และ `/opt/hop/uploads`
3. run EF Core migration
4. `dotnet publish` backend ไป `/opt/hop/releases/<timestamp>/backend`
5. sync backend ไป `/opt/hop/backend`
6. build frontend และ sync ไป `/var/www/hop`
7. restart `hop-api`
8. reload Nginx
9. ตรวจ `/`, `/health/live`, `/health/ready`

## Deploy เฉพาะ Backend

```bash
ENV_FILE=/etc/hop/hop-api.env bash scripts/deploy/publish-backend-baremetal.sh
sudo systemctl restart hop-api
```

## Deploy เฉพาะ Frontend

```bash
ENV_FILE=/etc/hop/hop-api.env bash scripts/deploy/publish-frontend-baremetal.sh
sudo nginx -t
sudo systemctl reload nginx
```

## Deploy จาก Prebuilt Artifact

หาก build frontend/backend จากเครื่องหรือ pipeline อื่น แล้ว upload artifact มาที่ server ก่อน deploy ให้ใช้ path นี้:

```text
/home/admin/hop-frontend/
/home/admin/hop-backend/
```

จากนั้น sync เข้า runtime path:

```bash
bash scripts/deploy/sync-prebuilt-baremetal.sh
```

ค่า default ของ script:

```text
FRONTEND_SOURCE=/home/admin/hop-frontend
BACKEND_SOURCE=/home/admin/hop-backend
FRONTEND_TARGET=/var/www/hop
BACKEND_TARGET=/opt/hop/backend
FRONTEND_OWNER=hop:www-data
BACKEND_OWNER=hop:hop
FRONTEND_MODE=755
BACKEND_MODE=750
HOP_API_SERVICE=hop-api
NGINX_SERVICE=nginx
```

เทียบเท่าคำสั่ง manual:

```bash
sudo rsync -av --delete /home/admin/hop-frontend/ /var/www/hop/
sudo chown -R hop:www-data /var/www/hop
sudo chmod -R 755 /var/www/hop

sudo rsync -av --delete /home/admin/hop-backend/ /opt/hop/backend/
sudo chown -R hop:hop /opt/hop/backend
sudo chmod -R 750 /opt/hop/backend

sudo systemctl restart hop-api
sudo nginx -t
sudo systemctl reload nginx
```

ถ้าต้องการ sync เฉพาะ frontend:

```bash
BACKEND_SOURCE=/home/admin/hop-backend RESTART_BACKEND=false bash scripts/deploy/sync-prebuilt-baremetal.sh
```

หรือ sync เฉพาะ backend ให้ใช้ rsync manual หรือเพิ่ม artifact ทั้งสองฝั่งไว้ครบก่อนรัน script เพื่อให้ deploy atomic กว่า

## CSRF Check

หลัง deploy ให้ clear site data แล้วตรวจ:

1. เปิด `http://172.16.2.99/api/csrf`
2. Browser ต้องมี cookie `hop_csrf_token`
3. Unsafe request เช่น `PUT /api/departments/{id}` ต้องมี header:

```text
X-CSRF-TOKEN: <same value as hop_csrf_token>
```

## Backup

Bare-metal mode ใช้:

```bash
BACKUP_MODE=host \
BACKUP_ROOT=/opt/hop/backups \
STORAGE_PATH=/opt/hop/uploads \
ENV_FILE=/etc/hop/hop-api.env \
bash scripts/backup/backup-hop.sh
```

## Rollback

ไฟล์ release ย้อนหลังอยู่ที่:

```text
/opt/hop/releases/<timestamp>/
```

Rollback application แบบ manual:

```bash
sudo rsync -a --delete /opt/hop/releases/<timestamp>/backend/ /opt/hop/backend/
sudo rsync -a --delete /opt/hop/releases/<timestamp>/frontend/ /var/www/hop/
sudo systemctl restart hop-api
sudo systemctl reload nginx
```

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
