# HOP Vehicle Paging Specification

## URL and API convention

`?page=1&pageSize=20&search=&status=&sortBy=createdAt&sortDirection=desc`

ใช้ชื่อจริงของ Leave คือ `page`, `pageSize` และ response `items`, `page`, `pageSize`, `totalItems`, `totalPages`; filter ว่างไม่ต้องส่งใน URL

## Endpoint migration inventory

| Endpoint | Current | Target |
|---|---|---|
| `GET /api/fleet/requests/mine` | array | paged + search/status/date/sort |
| `GET /api/fleet/requests/dispatcher/queue` | array | paged + search/status/date/urgent/sort |
| admin-review/director-approval | array | paged + search/date/sort |
| `GET /api/fleet/driver-jobs` | array | paged + status/date/sort |
| cancellations | array | paged |
| maintenance/emergency review | `{items,total,page,pageSize}` | standard metadata |
| vehicles/driver profiles/unavailability | array/route gaps | paged, scoped, searchable |

## Sorting whitelist

Requests: requestNo, createdAt, departureAt, expectedReturnAt, status, requesterName. Vehicles: vehicleCode, registrationNumber, status. Drivers: fullname, licenseExpiryDate, status. ทุก sort เติม ID เป็น tie-breaker

Default pageSize 20, max 100. Invalid values normalizeที่ boundary และ empty/out-of-range page คืน metadata ที่ถูกต้อง; frontendย้อนสู่หน้าสุดท้ายเมื่อเกิดจาก mutation
