# HOP Approval UI Pattern

Approval UI ของ Fleet ต้อง reuse pattern จาก Leave: identity/status ด้านบน, request details, timeline, delegation/on-behalf-of context, comment/reason field และ action panelเดียวกัน

- primary: approve/advance
- secondary: return
- destructive: reject/cancel
- reason required for return/reject
- confirm dialog ก่อน transition และ disable ขณะ pending
- 409 แสดง conflict dialog พร้อม reload; 403 แสดง permission state; success invalidate list/detail

ห้ามใช้ `window.prompt`/`window.confirm` ในปลายทาง UI; ให้ย้ายไป shared `ConfirmActionDialog` ใน UI-5 โดยไม่เปลี่ยน workflow/API semantics
