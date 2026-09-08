# LINE Group Production Configuration

ใช้ configuration กลางเดิม:

- `Line:Enabled`
- `Line:ChannelId`
- `Line:ChannelSecret`
- `Line:AccessToken` หรือ `Line:ChannelAccessToken`
- `Line:WebhookUrl`

Feature flag ใหม่ต้องเริ่มต้นเป็น false:

```json
{
  "LineGroupNotifications": {
    "Enabled": false
  }
}
```

เมื่อ false ระบบไม่ enqueue/ประมวลผล registration command และ group worker เป็น no-op โดย LINE USER/Leave เดิมยังทำงานตามปกติ

ห้ามกำหนด groupId ใน environment variable กลุ่มทั้งหมดมาจาก signed webhook และฐานข้อมูล ก่อน production ต้อง apply migration ผ่านขั้นตอนปกติ ตรวจ HTTPS webhook, secret rotation, worker health, permission mapping และยืนยันกลุ่มด้วย Fleet Admin เท่านั้น

Migration M1: `20260805022643_AddFleetLineGroupRegistration` เป็น additive และไม่ activate กลุ่มอัตโนมัติ
