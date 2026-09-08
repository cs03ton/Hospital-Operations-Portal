# LINE Group Security and Privacy

- ตรวจ signature กับ raw request body ทุกครั้งและเปรียบเทียบแบบ constant-time
- ไม่คืนหรือ log groupId แบบเต็มใน Admin response/log
- กลุ่มใหม่ไม่รับ notification จน Admin ยืนยัน
- deep link ต้องผ่าน HOP authentication, rollout guard และ Fleet permission
- template ห้ามมีข้อมูลผู้ป่วย/สุขภาพ เลขใบขับขี่ เหตุผลการลา หรือข้อมูลส่วนบุคคลเกินจำเป็น
- sanitize LINE error ก่อนเก็บ/แสดง และห้ามเก็บ access token ใน delivery payload
- LINE เป็นช่องแจ้งเตือนเท่านั้น ฐานข้อมูล HOP เป็น source of truth
