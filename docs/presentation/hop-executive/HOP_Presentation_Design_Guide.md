# HOP Executive Presentation Design Guide

เอกสารนี้กำหนดแนวทางการออกแบบ PowerPoint สำหรับโครงการ Hospital Operations Portal (HOP) เพื่อให้ภาพรวมดูเป็นมืออาชีพ เหมาะกับผู้บริหารโรงพยาบาล และสอดคล้องกับภาพลักษณ์ Modern Healthcare

## 1. Visual Direction

แนวทางภาพรวม:

- Professional Healthcare
- Premium แต่ไม่หรูเกินบริบทโรงพยาบาล
- Modern Hospital Dashboard
- Cozy Minimal
- อ่านง่าย เหมาะกับผู้บริหารและหน่วยงานราชการ

ควรหลีกเลี่ยง:

- สีฉูดฉาด
- การใช้ gradient หนัก
- ข้อความยาวเต็มหน้า
- icon หลากหลาย style ปะปนกัน
- ภาพ stock ที่ไม่เกี่ยวกับโรงพยาบาลหรือระบบสารสนเทศ

## 2. Font

ฟอนต์หลัก:

- TH SarabunPSK

ขนาดแนะนำ:

| ส่วน | ขนาด |
|---|---:|
| Slide title | 20 pt |
| Section heading | 18 pt |
| Body text | 16 pt |
| Caption / label | 14 pt |
| KPI number | 28-36 pt |

น้ำหนักตัวอักษร:

- Title: Bold
- Heading: Bold
- Body: Regular
- KPI number: Bold

## 3. Color Palette

| สี | Hex | การใช้งาน |
|---|---|---|
| Deep Hospital Green | `#1F5E4F` | Title, header bar, primary accent |
| Medical Blue Light | `#E3F2FD` | Background highlight, workflow box |
| Soft Mint | `#EAF5F0` | Card background เฉพาะจุด |
| White | `#FFFFFF` | พื้นหลัก |
| Light Gray | `#F5F6F7` | Section background |
| Border Gray | `#E4E7EA` | เส้นแบ่งและ card border |
| Text Charcoal | `#2C2C2C` | เนื้อหาหลัก |
| Muted Text | `#6B7280` | คำอธิบายรอง |
| Success Green | `#2E7D32` | สถานะสำเร็จ/อนุมัติ |
| Warning Amber | `#C8A96B` | จุดเน้นเชิงบริหาร ไม่ใช้เป็นพื้นใหญ่ |

หลักการใช้สี:

- พื้นหลักควรเป็นขาวหรือเทาอ่อน
- สีเขียวใช้กับหัวข้อ เส้นนำสายตา และ icon สำคัญ
- ฟ้าอ่อนใช้เป็น background ของ workflow หรือ diagram
- ทองใช้เฉพาะ accent เล็ก ๆ เช่น เส้น divider หรือ key highlight

## 4. Slide Layout System

### Layout A: Executive Title

ใช้กับสไลด์ Cover, Conclusion, Thank You

- Logo ด้านบนซ้ายหรือกลาง
- ชื่อโครงการขนาดใหญ่
- Subtitle สั้น
- แถบสีเขียวบางด้านล่าง

### Layout B: Two-Column Explanation

ใช้กับสไลด์ Pain Points, Why HOP, Benefits

- ซ้าย: Key message หรือ Before
- ขวา: After / แนวทางแก้ไข
- ใช้ card สีขาวและเส้นขอบเทาอ่อน

### Layout C: Module Cards

ใช้กับ Module Overview และสไลด์โมดูล

- Grid 2x3 หรือ 3x3
- Card แต่ละใบมี icon, title, benefit 1-2 บรรทัด
- ไม่ควรใส่ข้อความเกิน 5 bullet ต่อ card

### Layout D: Diagram / Architecture

ใช้กับ System Architecture และ Data Flow

- ใช้กล่องสีอ่อนเรียงตาม flow
- ใช้ arrow เส้นบาง
- เน้นคำว่า Frontend, Backend API, PostgreSQL, Docker, LINE OA
- ไม่ควรใส่รายละเอียด network ระดับลึกเกินไป

### Layout E: KPI / Metrics

ใช้กับ Success KPI และ Expected Benefits

- 3-5 KPI cards ต่อหน้า
- ตัวเลขเด่น
- คำอธิบายสั้นด้านล่าง

## 5. Icon Style

แนะนำใช้ icon แบบ line icon สีเขียวเข้มหรือเทาเข้ม

Icon suggestion:

| หมวด | Icon |
|---|---|
| Dashboard | dashboard / chart |
| User Management | users / user shield |
| Leave Management | calendar check |
| Security | shield / lock |
| Notification | bell / message |
| Architecture | server / database / cloud |
| Report | bar chart / file chart |
| Risk | alert triangle |
| Roadmap | map / timeline |

## 6. Data Visualization

ควรใช้:

- Timeline สำหรับ roadmap
- Before / After cards
- Simple workflow diagram
- KPI cards
- Architecture block diagram

หลีกเลี่ยง:

- Pie chart ถ้าไม่มีข้อมูลจริง
- ตารางใหญ่เกิน 5 แถว
- Diagram ที่มีเส้นไขว้กันมาก

## 7. Recommended Slide Density

ต่อ 1 slide:

- Title 1 บรรทัด
- Key message 1 ประโยค
- Bullet 3-5 ข้อ
- Visual 1 ชิ้น

ถ้าข้อมูลเยอะ ให้แยกเป็น 2 สไลด์แทนการยัดเนื้อหา

## 8. Presentation Tone

ภาษา:

- สุภาพ
- ชัดเจน
- เป็นทางการแบบอ่านง่าย
- เน้นประโยชน์ต่อโรงพยาบาลและประชาชน

ตัวอย่างคำที่ควรใช้:

- เพิ่มประสิทธิภาพ
- ลดขั้นตอนซ้ำซ้อน
- ตรวจสอบย้อนหลังได้
- รวมศูนย์ข้อมูล
- รองรับการขยายระบบ
- สนับสนุนการตัดสินใจของผู้บริหาร

ตัวอย่างคำที่ควรหลีกเลี่ยง:

- ระบบเทพ
- ใช้งานง่ายสุด ๆ
- เปลี่ยนทุกอย่างทันที
- AI ทำแทนทั้งหมด

## 9. Recommended Cover Copy

Hospital Operations Portal (HOP)  
ระบบศูนย์กลางสำหรับบริหารงานภายในโรงพยาบาล  
โรงพยาบาลนาหมื่น  

Supporting line:

ลดกระดาษ ลดงานซ้ำซ้อน ติดตามงานแบบ Real-time และเตรียมความพร้อมสู่ Digital Hospital

## 10. Final Production Checklist

- [ ] ใช้ TH SarabunPSK ทุกสไลด์
- [ ] Title ขนาด 20 pt
- [ ] Body ขนาด 16 pt
- [ ] ใช้สีหลักเขียว/ขาว/ฟ้าอ่อน/เทาอ่อน
- [ ] ทุกสไลด์มี Key Message
- [ ] Diagram อ่านเข้าใจใน 10 วินาที
- [ ] ไม่มีข้อความแน่นเกินไป
- [ ] ใช้ icon style เดียวกัน
- [ ] มี logo โรงพยาบาลใน cover และ thank you
- [ ] ตรวจคำสะกดและภาษาไทยก่อนนำเสนอ
