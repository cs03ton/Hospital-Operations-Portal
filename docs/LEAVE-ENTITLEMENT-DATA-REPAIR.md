# Leave Entitlement Data Repair

## วัตถุประสงค์

ใช้ตรวจข้อมูลเดิมก่อน pilot/production โดยไม่แก้ข้อมูลทันที

## Dry-run Check ที่ควรทำ

```sql
-- active users without employment type or start date
select id, username, fullname, employment_type, employment_start_date
from users
where is_active = true
  and (employment_type is null or employment_start_date is null);

-- duplicate balance should return zero because DB has unique index
select user_id, leave_type_id, year, count(*)
from leave_balances
group by user_id, leave_type_id, year
having count(*) > 1;

-- active users without current fiscal year balances
select u.id, u.username, u.fullname, u.employment_type
from users u
where u.is_active = true
  and u.employment_type is not null
  and u.employment_start_date is not null
  and not exists (
    select 1
    from leave_balances b
    where b.user_id = u.id
      and b.year = extract(year from current_date)::int
  );

-- negative available balance
select b.*, (b.entitled_days + b.carried_over_days + b.adjusted_days - b.used_days - b.pending_days) as available_days
from leave_balances b
where (b.entitled_days + b.carried_over_days + b.adjusted_days - b.used_days - b.pending_days) < 0;
```

## Recommendation

สร้าง repair tool แบบ:

- default เป็น dry-run
- export CSV/JSON
- apply ต้องระบุ flag ชัดเจน
- ทำทีละ employee หรือ batch เล็ก
- เขียน audit ทุกครั้ง

