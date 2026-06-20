# Code Cleanup Report

วันที่ตรวจสอบ: 2026-06-20

## Summary

ตรวจสอบ repository ก่อนนำขึ้น GitHub โดยโฟกัสไฟล์ generated, runtime data, environment secrets, QA artifacts, build output และไฟล์ที่ไม่ควร commit

ผลลัพธ์หลัก:

- อัปเดต `.gitignore` ให้ครอบคลุม .NET, Visual Studio, React/Vite, Node.js, Docker, PostgreSQL backup, environment files, logs, Playwright และ test artifacts
- ไม่ได้ลบไฟล์ใด ๆ ออกจาก working tree
- พบไฟล์ runtime upload ที่ถูก track อยู่แล้ว 1 ไฟล์ ควรถอดออกจาก Git index ก่อน push
- พบ QA artifacts และ Chrome profile จำนวนมาก แต่หลังอัปเดต `.gitignore` ถูกจัดเป็น ignored แล้ว
- ไม่พบ `console.log`, `debugger`, `TODO`, `FIXME` สำคัญใน source ที่สแกน

## Files/Folders That Should Be Ignored

| กลุ่ม | Pattern / Path | เหตุผล |
| --- | --- | --- |
| Environment | `.env`, `.env.*` | มีค่า secret/local runtime |
| Environment examples | `!.env.example`, `!**/.env.example` | เก็บ template config ได้ |
| Visual Studio | `.vs/`, `**/.vs/`, `*.suo`, `*.user` | local IDE state |
| .NET build | `bin/`, `obj/`, `TestResults/`, `coverage/` | build/test generated output |
| Node/Vite | `node_modules/`, `dist/`, `build/`, `.vite/` | dependency/build output |
| Playwright | `playwright-report/`, `test-results/`, `blob-report/` | generated test artifacts |
| QA artifacts | `docs/qa/screenshots/`, `docs/qa/downloads/`, `docs/qa/chrome-profile/` | screenshots, browser profile, downloads |
| QA runtime files | `docs/qa/*.log`, `docs/qa/*.pid`, `docs/qa/*.json`, `docs/qa/*.pdf`, `docs/qa/*.png`, `docs/qa/*.txt` | logs, process ids, test payloads, generated files |
| Runtime uploads | `backend/Hop.Api/storage/`, `uploads/`, `**/uploads/` | uploaded user files |
| Root storage | `storage/*`, `!storage/.gitkeep` | local runtime storage, keep folder marker |
| Database backups | `*.backup`, `*.bak`, `*.dump`, `*.sql.gz`, `database/backup/*`, `!database/backup/.gitkeep` | DB dump/backup files |
| Docker local data | `postgres-data/`, `pgdata/`, `docker-data/`, `volumes/`, `.docker/` | local volume/state |
| Temp files | `tmp/`, `temp/`, `*.tmp`, `*.cache` | temporary/generated files |

## Files Recommended To Remove From Git Index

ไม่ควรลบไฟล์จริงทันทีถ้ายังต้องใช้สำหรับ local QA แต่ควรถอดออกจาก Git index ก่อน push:

```text
backend/Hop.Api/storage/leave-attachments/2026/06/019ed2de-9699-7799-b6dc-8e71cb3daa88/8a60bd6d60524b739188b1a035bee390.pdf
```

คำสั่งแนะนำ:

```powershell
git rm --cached -- "backend/Hop.Api/storage/leave-attachments/2026/06/019ed2de-9699-7799-b6dc-8e71cb3daa88/8a60bd6d60524b739188b1a035bee390.pdf"
```

## Files/Folders Recommended To Delete Locally After Review

รายการนี้เป็น local/generated artifacts และไม่จำเป็นต่อ source repository แต่ควรลบหลังยืนยันว่าไม่ต้องใช้หลักฐาน QA หรือ debug log แล้ว:

```text
.vs/
backend/.vs/
backend/Hop.Api/.vs/
frontend/dist/
frontend/test-results/
docs/qa/chrome-profile/
docs/qa/screenshots/
docs/qa/downloads/
docs/qa/*.log
docs/qa/*.pid
docs/qa/*.json
docs/qa/*.pdf
docs/qa/*.txt
backend/Hop.Api/storage/
tmp/
```

ใช้คำสั่ง preview ก่อนลบ:

```powershell
git clean -ndX
```

หากผลลัพธ์ถูกต้องแล้วค่อยลบ ignored files:

```powershell
git clean -fdX
```

## Files Recommended To Keep

| ไฟล์/โฟลเดอร์ | เหตุผล |
| --- | --- |
| `.env.example` | template environment สำหรับ deploy/dev |
| `frontend/.env.example` | template frontend environment |
| `database/schema.sql` | database reference schema |
| `database/seed.sql` | seed reference |
| `database/backup/.gitkeep` | เก็บโฟลเดอร์ backup เปล่าไว้ |
| `storage/.gitkeep` | เก็บโฟลเดอร์ storage เปล่าไว้ |
| `Hospital-Operations-Portal.sln` | solution หลักสำหรับ Visual Studio 2022 |
| `frontend/package-lock.json` | lock dependency version |
| `docs/**/*.md` | project documentation |
| `docs/qa/*.md` | QA report แบบ Markdown สามารถเก็บเป็นหลักฐานได้ |
| `frontend/src/assets/logo/hospital-logo.png` | static branding asset |

หมายเหตุ: `backend/HospitalOperationsPortal.sln` เป็น solution อีกไฟล์ที่ยัง untracked ควรตัดสินใจว่าจะเก็บคู่กับ root solution หรือใช้ root solution เป็นตัวหลักเพียงไฟล์เดียว

## Secret / Config Risk

| จุดที่พบ | สถานะ | ความเสี่ยง | คำแนะนำ |
| --- | --- | --- | --- |
| `.env` และ `frontend/.env` | ignored | อาจมี secret/local credential | ห้าม commit |
| `.env.example` | tracked ได้ | มีค่า default dev เช่น `Admin@1234` และ placeholder password | ใช้เป็นตัวอย่างเท่านั้น ห้ามใช้ production |
| `docs/qa/login.json` | ignored | มี `admin/Admin@1234` สำหรับ QA | ห้าม commit |
| `README.md` และ docs | tracked | มีตัวอย่าง dev admin password | ระบุชัดว่า dev-only และ production ต้องเปลี่ยน |
| LINE/JWT/Postgres settings | env-driven | ถ้าใส่ค่าจริงใน `.env` จะเป็น secret | ใช้ GitHub Secrets หรือ secret manager |
| `frontend/src/config/appConfig.ts` | source fallback | มี fallback hospital name สำหรับ local/dev | ถ้าต้องเผยแพร่ public generic repo ให้ใช้ env เป็นหลักและทบทวน fallback |

## Clean Code Scan Notes

- ไม่พบ `console.log` หรือ `debugger` ใน source ที่สแกน
- ไม่พบ `TODO/FIXME/HACK` สำคัญใน source ที่สแกน
- พบ runtime/test artifacts จำนวนมาก แต่ถูกจัดการด้วย `.gitignore` แล้ว
- พบไฟล์ upload PDF ที่ถูก track อยู่แล้ว ต้อง `git rm --cached` ก่อน push
- `docs/qa/*.md` ยังไม่ถูก ignore เพื่อให้เก็บ QA report ได้ ส่วน screenshots/downloads/logs ถูก ignore

## Recommended Git Commands Before Commit

ตรวจสถานะ:

```powershell
git status --short --ignored
```

ถอด runtime upload ที่ถูก track ออกจาก Git index:

```powershell
git rm --cached -- "backend/Hop.Api/storage/leave-attachments/2026/06/019ed2de-9699-7799-b6dc-8e71cb3daa88/8a60bd6d60524b739188b1a035bee390.pdf"
```

preview ignored files ที่จะลบจาก working tree:

```powershell
git clean -ndX
```

ตรวจว่าไฟล์สำคัญไม่ถูก ignore ผิด:

```powershell
git check-ignore -v .env frontend/.env frontend/dist docs/qa/screenshots/phase1/01-login-page.png
git check-ignore -v .env.example frontend/.env.example
```

stage เฉพาะไฟล์ source/docs/config ที่ต้องการ:

```powershell
git add .gitignore docs/CODE-CLEANUP-REPORT.md
git add README.md docs frontend/src frontend/package.json frontend/package-lock.json frontend/.env.example backend database deploy .github Hospital-Operations-Portal.sln
```

ตรวจ staged files ก่อน commit:

```powershell
git diff --cached --stat
git diff --cached --name-only
```

## GitHub Push Risk

ก่อน push ควรแก้รายการนี้:

1. ถอด tracked runtime upload PDF ออกจาก Git index
2. ตรวจว่า `.env` และ `frontend/.env` ไม่ถูก stage
3. ตรวจว่า `docs/qa/login.json`, screenshots, downloads, logs และ Chrome profile ไม่ถูก stage
4. ตัดสินใจเรื่อง duplicate solution file `backend/HospitalOperationsPortal.sln`
5. ทบทวน dev credential `Admin@1234` ใน docs ว่าเขียนชัดว่าใช้เฉพาะ local/dev

