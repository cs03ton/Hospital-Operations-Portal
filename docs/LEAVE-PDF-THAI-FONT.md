# Leave PDF Thai Font

เอกสารนี้อธิบายวิธีตั้งค่าฟอนต์ไทยสำหรับ PDF ใบคำขอลา เพื่อป้องกันปัญหาตัวอักษรไทยหาย เว้นห่างเป็นตัว ๆ หรือ layout เพี้ยน

## PDF Library

ระบบใช้ `QuestPDF` สำหรับสร้าง PDF แบบ Generated Form ขนาด A4 Portrait

เหตุผล:

- รองรับ layout แบบ section/table/grid
- รองรับการ embed/register font
- ไม่ต้องใช้ absolute positioning เป็นหลัก
- ลดปัญหา letter spacing ของภาษาไทยจาก PDF writer แบบ custom

## Default Font

ค่า default:

```text
Font Family: TH SarabunPSK
Font Size: 16
Line Height: 1.2
```

หากไม่มี `TH SarabunPSK` ในเครื่อง ให้ติดตั้งหรือวางไฟล์ฟอนต์ไว้ใน `assets/fonts` ก่อนใช้งานจริง ส่วน `Noto Sans Thai` ใช้เป็น fallback ได้หากโรงพยาบาลอนุญาต

## Font Location

แนะนำให้วางฟอนต์ไว้ที่:

```text
backend/Hop.Api/assets/fonts/THSarabunPSK.ttf
backend/Hop.Api/assets/fonts/THSarabunPSK-Bold.ttf
```

ระบบจะ copy ไฟล์ `assets/fonts/*.ttf` ไปกับ output/publish อัตโนมัติ

ห้าม commit ฟอนต์ที่ไม่แน่ใจเรื่อง license หากใช้ฟอนต์ของโรงพยาบาลหรือฟอนต์เชิงพาณิชย์ ให้ติดตั้งบน server หรือ mount ผ่าน volume แทน

## Configuration

ตั้งค่าผ่าน environment variables:

```text
LeavePdf__FontPath=/app/assets/fonts/THSarabunPSK.ttf
LeavePdf__FontFamily=TH SarabunPSK
LeavePdf__FontSize=16
LeavePdf__LineHeight=1.2
```

ถ้าไม่กำหนด `LeavePdf__FontPath` ระบบจะลองค้นหาตามลำดับ:

1. `backend/Hop.Api/assets/fonts/THSarabunPSK.ttf`
2. `backend/Hop.Api/assets/fonts/TH SarabunPSK.ttf`
3. `backend/Hop.Api/assets/fonts/THSarabunNew.ttf`
4. `backend/Hop.Api/assets/fonts/NotoSansThai-Regular.ttf`
5. `assets/fonts/THSarabunPSK.ttf`
6. `assets/fonts/TH SarabunPSK.ttf`
7. Windows fonts เช่น `THSarabun.ttf`, `tahoma.ttf`, `LeelawUI.ttf`

สำหรับ Ubuntu/Docker แนะนำให้วางไฟล์ฟอนต์ใน `assets/fonts` หรือ mount font แล้วตั้ง `LeavePdf__FontPath`

## Troubleshooting

ถ้า PDF ภาษาไทยเพี้ยน:

1. ตรวจว่า `LeavePdf__FontPath` ชี้ไปยังไฟล์ `.ttf` ที่มีอยู่จริง
2. ตรวจว่า font รองรับภาษาไทย
3. หลีกเลี่ยงการใช้ PDF writer แบบ custom ที่ไม่ embed font
4. ห้ามใช้ letter spacing / character spacing กับข้อความไทย
5. ใช้หน้า Admin > ตั้งค่าระบบ > ตั้งค่าเอกสาร PDF เพื่อตรวจค่า font/template ที่ระบบเห็นจริง

## Test

1. สร้างคำขอลาที่มีข้อความภาษาไทย เช่น `ลาพักผ่อน`, `โรงพยาบาลนาหมื่น`, `อนุมัติแล้ว`
2. ดาวน์โหลด PDF:

```powershell
curl.exe -L "http://localhost:5000/api/leave-requests/<leave-request-id>/pdf" `
  -H "Authorization: Bearer <token>" `
  -o leave-request.pdf
```

3. เปิด PDF แล้วตรวจ:

- ภาษาไทยอ่านได้
- ไม่มีตัวอักษรเว้นห่างเป็นตัว ๆ
- วันที่แสดงเป็น `DD/MM/BBBB`
- ตารางการอนุมัติไม่ล้นหน้า
- เอกสารยังบันทึก audit event `LeaveRequest.PdfGenerated`
