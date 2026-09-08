# HOP Vehicle API Specification

Base path: `/api/fleet`. ใช้ JWT, backend permission policy, correlation ID และ `ApiResponse<T>` convention เดิม

Milestone 2 implemented endpoints:

- `GET /requests/mine`, `GET/POST/PUT /requests[/{id}]`
- `POST /requests/{id}/submit|cancel`
- `GET /requests/dispatcher/queue`
- `GET /requests/{id}/availability`
- `POST /requests/{id}/assign|return|reject`

## Foundation / Master Data

- `GET/POST /vehicle-types`; `GET/PUT /vehicle-types/{id}`
- `GET/POST /vehicles`; `GET/PUT /vehicles/{id}`
- `GET/POST /driver-profiles`; `GET/PUT /driver-profiles/{id}`
- `GET/POST/PUT /vehicle-unavailability[/{id}]`
- `GET/POST/PUT /driver-unavailability[/{id}]`

## Request / Dispatch

- `GET/POST /requests`, `GET/PUT /requests/{id}`
- `POST /requests/{id}/submit|cancel|copy`
- `GET /requests/{id}/timeline`
- `GET /dispatch/queue`
- `GET /recommendations/vehicles|drivers?requestId=...`
- `POST /requests/{id}/assign|reassign|forward-admin-review`

## Approval / Driver

- `GET /admin-review/queue`; `POST /requests/{id}/admin-review/forward|return|reject`
- `GET /director-approval/queue`; `POST /requests/{id}/director/approve|return|reject`
- `GET /driver/jobs`; `POST /requests/{id}/acknowledge|start|complete`
- Milestone 3 plan: UI/API แยกตามโมดูล แต่ใช้ shared core service/table; Fleet endpoint บังคับ `Scope=FLEET`, ใช้ `FleetDelegation.Manage`, และตรวจ `RequiredPermissionCode` ว่าเป็น Fleet permission

## Reporting

- `GET /dashboard`; `GET /reports`; `GET /reports/export?format=csv|xlsx|pdf`

Write DTOs ใช้ allowlist fields, ไม่รับ requester identity จาก client, ใช้ concurrency token (`If-Match` หรือ field ตาม implementation decision) และ action endpoint ใช้ idempotency keyสำหรับ submit/approval ที่เสี่ยงกดซ้ำ
