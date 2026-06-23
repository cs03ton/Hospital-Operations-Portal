# Database Cleanup

เอกสารนี้อธิบายการล้างข้อมูล Leave Management สำหรับ development/QA เท่านั้น

## Warning

```text
ห้ามรัน cleanup script บน production
```

Script นี้ไม่ลบ:

- users
- departments
- roles
- permissions
- audit_logs

## Script

```text
database/scripts/clear-leave-dev-data.sql
```

## What It Clears

- leave_requests
- leave_approvals
- leave_attachments
- leave_balances
- leave_balance_adjustments
- leave_types
- leave_holidays
- approval_chains
- approval_chain_steps
- approval_delegations
- approval_escalation_rules
- approval_override_logs
- line_delivery_logs
- leave-related notifications

## Run Command

บน Docker Desktop local development:

```powershell
Get-Content database/scripts/clear-leave-dev-data.sql | docker exec -i hop-postgres psql -U hop_user -d hop_db
```

หรือบน Linux/macOS:

```bash
docker exec -i hop-postgres psql -U hop_user -d hop_db < database/scripts/clear-leave-dev-data.sql
```

## Recreate Basic Leave Data

หลังล้างข้อมูล ให้ start backend ด้วย seeding:

```powershell
$env:Database__SeedOnStartup="true"
$env:Seed__CreateStandardItUsers="true"
dotnet run --project backend/Hop.Api/Hop.Api.csproj --urls http://localhost:5000
```

Seeder จะสร้าง:

- ประเภทลาเริ่มต้น
- แผนก Information Technology ถ้ายังไม่มี
- user ชุดมาตรฐาน IT สำหรับ development
- Approval chain: `head01` → `director01`

## Verification

ตรวจว่าข้อมูลหลักยังอยู่:

```sql
select count(*) from users;
select count(*) from departments;
select count(*) from roles;
select count(*) from permissions;
```

ตรวจว่าข้อมูล leave ถูกล้าง:

```sql
select count(*) from leave_requests;
select count(*) from approval_chains;
select count(*) from leave_balances;
```
