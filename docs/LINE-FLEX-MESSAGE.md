# LINE Flex Message

HOP ใช้ LINE Flex Message สำหรับแจ้งเตือนคำขอลาที่ต้องอนุมัติ โดยแสดงข้อมูลสำคัญใน card เดียว เช่น เลขที่คำขอ ผู้ขอ หน่วยงาน ประเภทลา วันที่ จำนวนวัน เหตุผล สถานะ และขั้นอนุมัติปัจจุบัน

ปุ่มใน card จะเปิดหน้าเว็บเท่านั้น:

- ดูรายละเอียด: `{PUBLIC_APP_URL}/leave/{leaveRequestId}`
- อนุมัติ: `{PUBLIC_APP_URL}/line/leave-approval/{leaveRequestId}?action=approve`
- ไม่อนุมัติ: `{PUBLIC_APP_URL}/line/leave-approval/{leaveRequestId}?action=reject`

ระบบไม่อนุมัติจาก LINE โดยตรง เพื่อให้ backend ตรวจ login, permission, current approver และ workflow state ทุกครั้ง

## Configuration

```text
PUBLIC_APP_URL=https://your-hop-url
Line__PublicAppUrl=https://your-hop-url
PUBLIC_FILE_BASE_URL=https://your-hop-url
Line__PublicFileBaseUrl=https://your-hop-url
```

ห้าม hardcode localhost สำหรับ production
ห้ามใช้ IP วง LAN เช่น `10.x.x.x`, `172.16.x.x`, `192.168.x.x` สำหรับรูปใน LINE Flex เพราะ LINE server เข้าถึงไม่ได้

## Debug Mode

หน้า `Admin > ตั้งค่า LINE` มีส่วน `Flex Message Debug Mode` สำหรับตรวจสอบปัญหา Flex Message โดยไม่ต้องสร้างคำขอลาใหม่ทุกครั้ง

- เลือกตัวอย่างได้: Pending Approval, Approved, Rejected, Cancelled
- `Preview Flex JSON` สร้าง payload ตัวอย่างจากคำขอล่าสุด หรือข้อมูลตัวอย่างถ้ายังไม่มีคำขอ
- `Copy JSON` คัดลอก payload ไปตรวจใน LINE Flex Message Simulator
- `Validate` ตรวจ `type:flex`, `altText`, `bubble`, footer action และ URI action
- `Send Test Flex` ส่ง payload ไป LINE Push API จริงโดยใช้ Channel Access Token จาก backend config เท่านั้น

ผลลัพธ์จะแสดง `HTTP Status`, response body/error จาก LINE, latency และบันทึกลง `line_delivery_logs`

## Button Style

Card ใช้แนว modern minimal อ้างอิง mockup:

- Header สี deep green `#064E3B` พร้อม accent gold
- Avatar initials ในกรอบทอง สำหรับกรณียังไม่มีรูปโปรไฟล์
- ถ้ามีรูปโปรไฟล์และ `PUBLIC_FILE_BASE_URL` หรือ `PUBLIC_APP_URL` เป็น URL ที่ LINE เข้าถึงได้จริง ระบบจะแสดงรูปโปรไฟล์ใน header
- แถวข้อมูลใช้ icon + label + value เพื่ออ่านง่าย
- Pending card มี section สถานะปัจจุบันและ progress ของขั้นอนุมัติ

Pending approval card ใช้ปุ่ม 3 รายการ:

- `ดูรายละเอียด` สีฟ้า `#2563EB`
- `อนุมัติ` สีเขียว `#16A34A`
- `ไม่อนุมัติ` สีแดง `#DC2626`

Approved, rejected และ cancelled card จะไม่แสดงปุ่มอนุมัติ/ไม่อนุมัติซ้ำ โดยแสดง badge สถานะและปุ่ม `ดูรายละเอียด` เท่านั้น

## Common Failure Causes

- `Line__PublicAppUrl` หรือ `PUBLIC_APP_URL` ยังเป็น `localhost`/`127.0.0.1`
- `PUBLIC_FILE_BASE_URL` เป็น localhost หรือ IP ภายใน LAN
- Action URL ไม่ใช่ absolute URL หรือ production ไม่ใช่ HTTPS
- `altText` ว่าง หรือยาวเกินข้อกำหนดของ LINE
- `contents.type` ไม่ใช่ `bubble`
- footer button ไม่มี `uri` action ที่ถูกต้อง
- Channel Access Token ไม่ถูกต้องหรือไม่มีสิทธิ์ Push API
- LINE User ID ของผู้รับไม่ถูกต้อง หรือผู้รับยังไม่ได้เพิ่ม LINE OA เป็นเพื่อน
