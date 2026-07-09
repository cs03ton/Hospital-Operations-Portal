# User Form Save UX Test Checklist

วันที่ทดสอบ: 08/07/2026

## Scope

ตรวจ UX การเลือก dropdown และการแจ้งผลหลังบันทึกข้อมูลในหน้า Admin Forms ที่เกี่ยวข้องกับ Phase 1

## Test Cases

| Case | ขั้นตอน | ผลที่คาดหวัง |
|---|---|---|
| Role dropdown closes after select | ไปที่ `/admin/users/create` แล้วเลือกบทบาท `เจ้าหน้าที่` | Dropdown ปิดทันทีและค่าบทบาทถูกแสดงในช่อง |
| Department dropdown | เลือกหน่วยงานในหน้าเพิ่ม/แก้ไขผู้ใช้ | Dropdown ปิดตามปกติและค่าถูก set |
| Employment type dropdown | เลือกประเภทพนักงาน | Dropdown ปิดตามปกติและค่าถูก set |
| Gender dropdown | เลือกเพศ | Dropdown ปิดตามปกติและค่าถูก set |
| Approval rule dropdown | เลือกกฎการอนุมัติวันลา | Dropdown ปิดตามปกติและค่าถูก set |
| Create user success | กรอกข้อมูลถูกต้องและกดบันทึก | แสดง toast `เพิ่มผู้ใช้งานสำเร็จ` แล้วกลับ `/admin/users` หลังประมาณ 3 วินาที |
| Edit user success | แก้ไขข้อมูลและกดบันทึก | แสดง toast `แก้ไขผู้ใช้งานสำเร็จ` แล้วกลับ `/admin/users` หลังประมาณ 3 วินาที |
| User save validation error | กรอก LINE User ID ที่ซ้ำแล้วกดบันทึก | แสดงข้อความจาก backend เช่น `LINE User ID นี้ถูกใช้กับบัญชี ... แล้ว` และไม่ redirect |
| Prevent double submit | กดบันทึกและลองกดซ้ำระหว่างโหลด | ปุ่มบันทึก disabled ระหว่างบันทึก |
| Department save success | เพิ่ม/แก้ไขหน่วยงาน | แสดง toast สำเร็จแล้วกลับหน้าจัดการหลังประมาณ 3 วินาที |
| Approval rule save success | เพิ่ม/แก้ไขกฎการอนุมัติ | แสดง toast สำเร็จและ redirect ตาม flow |
| Leave type save error | บันทึกประเภทลาที่ backend reject | แสดง error toast และคงข้อมูลในฟอร์มให้แก้ไขต่อ |
| Holiday save error | บันทึกวันหยุดที่ backend reject | แสดง error toast และคงข้อมูลในฟอร์มให้แก้ไขต่อ |

## Build Verification

- `npm run build` ผ่าน
- `dotnet build` ผ่าน

เอกสารนี้เป็นส่วนหนึ่งของโครงการ Hospital Operations Portal (HOP) โรงพยาบาลนาหมื่น
