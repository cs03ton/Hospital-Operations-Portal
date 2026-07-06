const fs = require("fs");
const fsp = require("fs/promises");
const path = require("path");
const pptxgen = require("pptxgenjs");
const sharp = require("sharp");
const SHAPE = new pptxgen()._shapeType;

const ROOT = "D:/HOSPITAL/Hospital-Projects/Hospital-Operations-Portal";
const OUT = path.join(ROOT, "docs/presentation");
const ASSETS = path.join(OUT, "assets");
const PREVIEW = path.join(OUT, "preview");
const PDF_IMAGES = path.join(OUT, "pdf-images");
const QA = path.join(OUT, "qa");
const PPTX = path.join(OUT, "HOP_Executive_Presentation.pptx");

const C = {
  primary: "0F766E",
  secondary: "14B8A6",
  bg: "F8FAFC",
  white: "FFFFFF",
  text: "0F172A",
  muted: "64748B",
  border: "DDE7E5",
  success: "22C55E",
  warning: "F59E0B",
  danger: "EF4444",
  tealSoft: "CCFBF1",
  blueSoft: "E0F2FE",
  greenSoft: "DCFCE7",
  amberSoft: "FEF3C7",
  redSoft: "FEE2E2",
};

const FONT = "TH Sarabun New";
const SLIDE_W = 13.333;
const SLIDE_H = 7.5;

const slides = [
  {
    title: "Hospital Operations Portal (HOP)",
    subtitle: "ระบบศูนย์กลางสำหรับบริหารงานภายในโรงพยาบาลนาหมื่น",
    icon: "hospital",
    layout: "cover",
    accent: C.primary,
    body: [
      "ยกระดับงานบริหารภายในจากเอกสารกระดาษสู่ระบบดิจิทัลที่ติดตามได้แบบ Real-time",
      "รองรับ Dashboard, User Management และ Leave Management ใน Phase 1",
      "ออกแบบต่อยอดสู่ Digital Hospital อย่างเป็นระบบ",
    ],
    note: "เปิดด้วยภาพรวมโครงการ HOP ในฐานะระบบกลางของโรงพยาบาล เป้าหมายคือช่วยลดงานซ้ำ ลดเอกสาร และทำให้ผู้บริหารเห็นข้อมูลสำคัญได้รวดเร็วขึ้น โดย Phase 1 เน้นโมดูลที่พร้อมใช้งานจริงก่อน ได้แก่ Dashboard, User Management และ Leave Management",
  },
  {
    title: "Executive Summary",
    subtitle: "HOP คือฐานรากดิจิทัลสำหรับงานปฏิบัติการโรงพยาบาล",
    icon: "summary",
    layout: "metrics",
    accent: C.secondary,
    body: [
      "รวมข้อมูลและขั้นตอนงานไว้ในระบบเดียว ลดการกระจายของข้อมูล",
      "กำหนดสิทธิ์ตามบทบาท ลดความเสี่ยงด้านข้อมูลบุคลากร",
      "แจ้งเตือนผ่าน LINE OA เพื่อเร่งการอนุมัติและติดตามงาน",
      "รองรับการขยายโมดูลในปีงบประมาณ 2570",
    ],
    note: "สไลด์นี้ควรสื่อสารแบบผู้บริหาร: HOP ไม่ใช่แค่ระบบลา แต่เป็น platform กลางของโรงพยาบาลที่เริ่มจากงานสำคัญและขยายต่อได้ จุดขายคือความเป็นศูนย์กลาง ความปลอดภัย และการติดตามงานได้ทันที",
  },
  {
    title: "Current Pain Points",
    subtitle: "ปัญหาที่พบในงานบริหารภายในรูปแบบเดิม",
    icon: "warning",
    layout: "pain",
    accent: C.warning,
    body: [
      "เอกสารกระดาษกระจายหลายจุด ทำให้ค้นหาและติดตามสถานะล่าช้า",
      "การอนุมัติอาศัยการส่งต่อด้วยคน มีโอกาสตกหล่นหรือค้างนาน",
      "ผู้บริหารมองภาพรวมทรัพยากรและงานค้างได้ไม่ทันเวลา",
      "สิทธิ์การเข้าถึงข้อมูลยังควบคุมยากเมื่อทำงานหลายหน่วยงาน",
    ],
    note: "เล่าปัญหาในภาษางานจริง ไม่เน้นตำหนิระบบเดิม แต่ชี้ให้เห็นว่าขั้นตอนกระดาษและการประสานงานแบบ manual ทำให้เสียเวลาและตรวจสอบย้อนหลังได้ยาก",
  },
  {
    title: "Current Workflow",
    subtitle: "กระบวนการเดิมมีหลายจุดส่งต่อและตรวจสอบย้อนหลังยาก",
    icon: "workflow",
    layout: "workflow-before",
    accent: C.warning,
    body: [
      "เจ้าหน้าที่กรอกแบบฟอร์มกระดาษ",
      "ส่งต่อหัวหน้าหรือผู้อนุมัติทีละขั้น",
      "HR ตรวจสอบและบันทึกข้อมูลซ้ำ",
      "ผู้บริหารต้องรวบรวมข้อมูลจากหลายแหล่ง",
    ],
    note: "อธิบาย workflow เดิมแบบ Before ให้เห็น friction หลัก คือการกรอกซ้ำ การรอเอกสาร และการติดตามสถานะที่ไม่ชัดเจน",
  },
  {
    title: "Future Workflow",
    subtitle: "ขั้นตอนใหม่เชื่อมต่อผ่านระบบกลางและแจ้งเตือนอัตโนมัติ",
    icon: "workflow",
    layout: "workflow-after",
    accent: C.success,
    body: [
      "เจ้าหน้าที่ส่งคำขอผ่านระบบ",
      "ระบบตรวจสิทธิ์ วันลาคงเหลือ และสายอนุมัติ",
      "ผู้อนุมัติได้รับแจ้งเตือนเฉพาะเมื่อถึงคิว",
      "ข้อมูลสรุปและ audit log พร้อมตรวจสอบทันที",
    ],
    note: "เปรียบเทียบ After ให้เห็นความคล่องตัว: ระบบช่วยตรวจข้อมูลก่อนส่ง แจ้งเตือนถูกคน และเก็บหลักฐานทุกขั้นตอน",
  },
  {
    title: "Vision",
    subtitle: "ศูนย์กลางงานปฏิบัติการที่พร้อมต่อยอดสู่ Digital Hospital",
    icon: "vision",
    layout: "statement",
    accent: C.primary,
    body: [
      "ลดภาระงานเอกสารและการบันทึกซ้ำ",
      "เพิ่มความโปร่งใสของกระบวนการอนุมัติและการติดตามงาน",
      "สร้างฐานข้อมูลกลางเพื่อรองรับการวิเคราะห์และรายงานผู้บริหาร",
    ],
    note: "วิสัยทัศน์ควรผูกกับยุทธศาสตร์โรงพยาบาล ไม่ใช่แค่เรื่องเทคโนโลยี แต่คือการทำให้คนทำงานเร็วขึ้นและข้อมูลน่าเชื่อถือขึ้น",
  },
  {
    title: "Project Objectives",
    subtitle: "วัตถุประสงค์เชิงบริหารและเชิงปฏิบัติการ",
    icon: "target",
    layout: "objectives",
    accent: C.secondary,
    body: [
      "จัดทำระบบกลางสำหรับงานภายในโรงพยาบาล",
      "กำหนดสิทธิ์การใช้งานตามบทบาทและหน้าที่",
      "ลดระยะเวลาการอนุมัติและเพิ่มการแจ้งเตือนอัตโนมัติ",
      "เตรียมข้อมูลสำหรับ Dashboard และรายงานผู้บริหาร",
    ],
    note: "เน้นว่า objectives ถูกออกแบบให้วัดผลได้ ทั้งด้านความเร็ว ความปลอดภัย และข้อมูลสำหรับการตัดสินใจ",
  },
  {
    title: "Project Scope",
    subtitle: "Phase 1 เริ่มจากโมดูลที่กระทบงานประจำและพร้อมใช้งานจริง",
    icon: "scope",
    layout: "scope",
    accent: C.primary,
    body: [
      "Dashboard: ภาพรวมงานและสถานะที่สำคัญ",
      "User Management: ผู้ใช้งาน บทบาท สิทธิ์ และหน่วยงาน",
      "Leave Management: คำขอลา สายอนุมัติ วันลาคงเหลือ และ PDF",
      "Notification: แจ้งเตือนผ่านระบบและ LINE OA",
    ],
    note: "อธิบายขอบเขตให้ชัดเพื่อลดความคาดหวังเกิน Phase 1 และเปิดทางให้เห็น roadmap ต่อไป",
  },
  {
    title: "System Architecture",
    subtitle: "สถาปัตยกรรมแยก Frontend, Backend และ Database พร้อม deploy ด้วย Docker",
    icon: "server",
    layout: "architecture",
    accent: C.primary,
    body: [
      "Frontend: React + TypeScript + Vite",
      "Backend API: ASP.NET Core Web API / .NET 9",
      "Database: PostgreSQL",
      "Deployment: Ubuntu Server + Docker",
    ],
    note: "สไลด์นี้พูดให้ผู้บริหารเข้าใจว่าโครงสร้างแยกส่วนทำให้ดูแลระบบง่าย ขยายระบบได้ และสามารถย้ายฐานข้อมูลออกเป็น server แยกในอนาคตโดยไม่กระทบ business logic",
  },
  {
    title: "Technology Stack",
    subtitle: "เลือกเทคโนโลยีมาตรฐาน เปิดกว้าง และพร้อมดูแลระยะยาว",
    icon: "tech",
    layout: "tech",
    accent: C.secondary,
    body: [
      "React + TypeScript สำหรับหน้าจอใช้งานที่เร็วและ responsive",
      ".NET 9 Web API สำหรับ backend ที่เสถียรและเหมาะกับงานองค์กร",
      "PostgreSQL สำหรับฐานข้อมูลเชิงสัมพันธ์ที่มีความน่าเชื่อถือ",
      "JWT + Refresh Token และ Role Permission สำหรับความปลอดภัย",
      "LINE Official Account สำหรับการแจ้งเตือนที่ผู้ใช้งานคุ้นเคย",
    ],
    note: "ไม่ต้องลงลึกเทคนิคมาก แต่ให้ความมั่นใจว่า stack นี้ทันสมัย มี community รองรับ และเหมาะกับการ deploy ภายในองค์กร",
  },
  {
    title: "Security & Access Control",
    subtitle: "ควบคุมข้อมูลตามบทบาท ตรวจสอบย้อนหลังได้ทุก action สำคัญ",
    icon: "shield",
    layout: "security",
    accent: C.primary,
    body: [
      "Authentication ด้วย JWT และ Refresh Token",
      "Authorization ด้วย Role & Permission แยกตามหน้าที่",
      "Audit Log บันทึกการเข้าสู่ระบบ การอนุมัติ และการเปลี่ยนข้อมูลสำคัญ",
      "Notification เฉพาะผู้เกี่ยวข้อง ลดการเห็นข้อมูลเกินสิทธิ์",
    ],
    note: "จุดสำคัญคือระบบไม่ได้พึ่งแค่การซ่อนเมนู แต่ backend ต้อง enforce สิทธิ์จริง เหมาะกับข้อมูลบุคลากรและเอกสารราชการ",
  },
  {
    title: "Dashboard Module",
    subtitle: "หน้าแรกที่สรุปสถานะงานตามบทบาทของผู้ใช้งาน",
    icon: "dashboard",
    layout: "mockup-dashboard",
    accent: C.secondary,
    body: [
      "ผู้ใช้งานเห็นคำขอลาของตนเองและสถานะล่าสุด",
      "หัวหน้างานเห็นงานรออนุมัติและข้อมูลทีม",
      "ผู้บริหารเห็นภาพรวมแนวโน้มและ KPI สำคัญ",
    ],
    note: "เน้น Role-Based Dashboard ที่ไม่แสดงข้อมูลเหมือนกันทุกคน แต่แสดงเฉพาะสิ่งที่ต้องใช้ตัดสินใจหรือทำงาน",
  },
  {
    title: "User Management Module",
    subtitle: "จัดการผู้ใช้งาน หน่วยงาน บทบาท และสิทธิ์อย่างเป็นระบบ",
    icon: "users",
    layout: "cards",
    accent: C.primary,
    body: [
      "เพิ่ม แก้ไข และปิดการใช้งานบัญชีผู้ใช้",
      "กำหนดหน่วยงาน บทบาท และ permission ที่ละเอียด",
      "รองรับข้อมูลส่วนตัวสำหรับใช้ในใบลาและเอกสาร PDF",
      "เชื่อมบัญชี LINE เพื่อรับแจ้งเตือนรายบุคคล",
    ],
    note: "อธิบายว่า User Management เป็นฐานของความปลอดภัยและข้อมูลบุคลากร ไม่ใช่แค่หน้าสร้าง user",
  },
  {
    title: "Leave Management Module",
    subtitle: "ระบบลาครบวงจร ตั้งแต่สร้างคำขอถึงออกเอกสาร PDF",
    icon: "leave",
    layout: "mockup-leave",
    accent: C.success,
    body: [
      "สร้างและส่งคำขอลา พร้อมแนบเอกสาร",
      "ตรวจสิทธิ์ตามประเภทบุคลากร เพศ ปีงบประมาณ และวันลาคงเหลือ",
      "อนุมัติหลายขั้น พร้อม timeline และ audit trail",
      "สร้าง PDF ใบลาภาษาไทยตามแบบฟอร์มโรงพยาบาล",
    ],
    note: "นี่เป็นโมดูลหลักของ Phase 1 ให้พูดทั้งมุมเจ้าหน้าที่ หัวหน้า HR และผู้บริหาร โดยเน้น workflow ครบและตรวจสอบย้อนหลังได้",
  },
  {
    title: "Inventory Module",
    subtitle: "แผนต่อยอดสำหรับติดตามครุภัณฑ์และวัสดุภายในโรงพยาบาล",
    icon: "inventory",
    layout: "mockup-inventory",
    accent: C.secondary,
    body: [
      "ทะเบียนวัสดุและครุภัณฑ์กลาง",
      "สถานะคงเหลือ การเบิกจ่าย และประวัติการใช้งาน",
      "รายงานสรุปเพื่อช่วยวางแผนจัดซื้อและบริหารทรัพยากร",
    ],
    note: "โมดูลนี้อยู่ใน roadmap ปีงบประมาณ 2570 ช่วยให้ผู้บริหารเห็นว่าระบบไม่ได้หยุดที่งานลา",
  },
  {
    title: "Repair Request Module",
    subtitle: "ระบบแจ้งซ่อมที่ติดตามสถานะและผู้รับผิดชอบได้ชัดเจน",
    icon: "repair",
    layout: "mockup-repair",
    accent: C.warning,
    body: [
      "เจ้าหน้าที่แจ้งซ่อมพร้อมรูปภาพและรายละเอียดปัญหา",
      "กำหนดผู้รับผิดชอบและติดตาม SLA",
      "Dashboard งานค้าง งานเสร็จ และประวัติการซ่อม",
    ],
    note: "เน้นการลดงานโทรประสานและลดการตกหล่นของใบแจ้งซ่อม โดยใช้ workflow คล้ายระบบลาแต่ปรับกับงานซ่อม",
  },
  {
    title: "Executive Report Module",
    subtitle: "รายงานเชิงบริหารสำหรับการตัดสินใจและติดตามผล",
    icon: "report",
    layout: "mockup-executive",
    accent: C.primary,
    body: [
      "สรุปภาพรวมบุคลากร การลา งานอนุมัติ และทรัพยากร",
      "เปรียบเทียบรายเดือน รายหน่วยงาน และแนวโน้มสำคัญ",
      "รองรับ export เพื่อใช้ประกอบการประชุม",
    ],
    note: "ใช้ภาษาผู้บริหาร เน้นข้อมูลพร้อมใช้ ไม่ใช่ข้อมูลดิบ และควรต่อยอดเป็น analytics ในอนาคต",
  },
  {
    title: "LINE Notification Integration",
    subtitle: "แจ้งเตือนถูกคน ถูกเวลา ลดงานค้างและการติดตามด้วยมือ",
    icon: "bell",
    layout: "notification",
    accent: C.secondary,
    body: [
      "แจ้งผู้อนุมัติเฉพาะเมื่อถึงคิวอนุมัติ",
      "แจ้งเจ้าของคำขอเมื่อสถานะเปลี่ยน",
      "รองรับ Flex Message พร้อมปุ่มดูรายละเอียดและ action ที่ปลอดภัย",
      "บันทึก delivery log เพื่อ troubleshooting",
    ],
    note: "สื่อสารให้ชัดว่าระบบไม่ spam ทุกคน แต่แจ้งเฉพาะผู้เกี่ยวข้อง ทำให้การแจ้งเตือนมีคุณค่าและลดความรำคาญ",
  },
  {
    title: "Data Flow",
    subtitle: "ข้อมูลไหลผ่านระบบอย่างเป็นขั้นตอนและตรวจสอบย้อนหลังได้",
    icon: "flow",
    layout: "dataflow",
    accent: C.primary,
    body: [
      "ผู้ใช้งานทำรายการผ่าน Frontend",
      "Backend ตรวจสิทธิ์และ business rules",
      "Database เก็บข้อมูลธุรกรรมและ audit log",
      "Notification service แจ้งเตือนผ่านระบบและ LINE OA",
    ],
    note: "Data flow ช่วยให้ผู้บริหารมั่นใจว่าข้อมูลไม่ได้วิ่งกระจัดกระจาย และทุกจุดสำคัญมีการตรวจสอบสิทธิ์",
  },
  {
    title: "Database Overview",
    subtitle: "ฐานข้อมูลออกแบบรองรับข้อมูลบุคลากร งานลา และ audit ระยะยาว",
    icon: "database",
    layout: "database",
    accent: C.primary,
    body: [
      "Users, Roles, Permissions และ Departments",
      "Leave Requests, Approval Steps, Leave Balances และ Holidays",
      "Notifications, LINE Delivery Logs และ Audit Logs",
      "รองรับการ backup/restore ด้วย PostgreSQL tools",
    ],
    note: "ไม่ต้องแสดง ERD ซับซ้อน ให้แสดงกลุ่มข้อมูลหลักและเหตุผลว่าทำไมต้องแยกข้อมูลเป็นหมวด",
  },
  {
    title: "Roadmap 2569–2570",
    subtitle: "เริ่มใช้งานจากแกนหลัก แล้วขยายโมดูลตามความพร้อมของหน่วยงาน",
    icon: "roadmap",
    layout: "roadmap",
    accent: C.secondary,
    body: [
      "ปี 2569: Dashboard, User Management, Leave Management",
      "ปีงบประมาณ 2570 เริ่ม 1 ตุลาคม 2569: Inventory, Repair Request, Executive Report",
      "ต่อยอด Mobile Responsive และ LINE Integration ให้ครอบคลุมทุกโมดูล",
    ],
    note: "ย้ำว่า roadmap แบ่งเป็นช่วงเพื่อลดความเสี่ยงในการเปลี่ยนผ่าน ไม่ทำทุกอย่างพร้อมกันเกินไป",
  },
  {
    title: "Implementation Plan",
    subtitle: "แผนดำเนินงานเป็นระยะเพื่อลดความเสี่ยงและสร้าง adoption",
    icon: "plan",
    layout: "timeline",
    accent: C.primary,
    body: [
      "1. เตรียมระบบและข้อมูลเริ่มต้น",
      "2. Pilot กับ 1 หน่วยงาน",
      "3. เก็บ feedback และปรับปรุง",
      "4. ขยายใช้งานทั้งโรงพยาบาล",
      "5. วางแผนโมดูล Phase 2",
    ],
    note: "แนะนำให้เริ่ม pilot ในวงจำกัด เช่น IT หรือหน่วยงานที่พร้อม เพื่อทดสอบ workflow, permission, LINE และเอกสาร PDF ก่อน rollout ใหญ่",
  },
  {
    title: "Success KPI",
    subtitle: "ตัวชี้วัดเพื่อประเมินผลหลังเริ่มใช้งาน",
    icon: "kpi",
    layout: "kpi",
    accent: C.success,
    body: [
      "ลดเวลาเฉลี่ยการอนุมัติคำขอลา",
      "ลดจำนวนเอกสารกระดาษในกระบวนการลา",
      "เพิ่มสัดส่วนคำขอที่ติดตามสถานะผ่านระบบ",
      "ลดจำนวนรายการที่ต้องสอบถามสถานะด้วยโทรศัพท์หรือ LINE ส่วนตัว",
    ],
    note: "เสนอ KPI ที่วัดได้จริงและเชื่อมกับประโยชน์เชิงบริหาร ไม่เน้นตัวเลขเกินจริง แต่ตั้งกรอบให้ติดตามต่อได้",
  },
  {
    title: "Expected Benefits",
    subtitle: "ผลลัพธ์ที่คาดหวังต่อเจ้าหน้าที่ หัวหน้างาน HR และผู้บริหาร",
    icon: "benefit",
    layout: "benefits",
    accent: C.secondary,
    body: [
      "เจ้าหน้าที่: ขอลาและติดตามสถานะได้สะดวก",
      "หัวหน้างาน: เห็นงานรออนุมัติและตัดสินใจเร็วขึ้น",
      "HR/Admin: ลดการบันทึกซ้ำและตรวจสอบข้อมูลง่ายขึ้น",
      "ผู้บริหาร: เห็นภาพรวมงานและแนวโน้มได้ทันเวลา",
    ],
    note: "พูดแบบ stakeholder-by-stakeholder เพื่อให้ผู้ฟังเห็นว่าแต่ละกลุ่มได้ประโยชน์ต่างกันอย่างไร",
  },
  {
    title: "Risk & Mitigation",
    subtitle: "ความเสี่ยงหลักและแนวทางลดผลกระทบ",
    icon: "risk",
    layout: "risk",
    accent: C.warning,
    body: [
      "การยอมรับของผู้ใช้งาน: ใช้คู่มือและอบรมแบบสั้น",
      "ข้อมูลตั้งต้นไม่ครบ: ตรวจสอบ user, role, department และ leave balance ก่อน pilot",
      "การเชื่อมต่อ LINE: มี fallback เป็น notification ในระบบ",
      "ความต่อเนื่องของระบบ: มี backup/restore และ deployment checklist",
    ],
    note: "แสดงให้เห็นว่าโครงการเตรียมรับมือความเสี่ยงไว้แล้ว ไม่ใช่เสนอเฉพาะด้านดี",
  },
  {
    title: "ROI & Resource Consideration",
    subtitle: "มูลค่าที่ได้รับจากการลดงานซ้ำและเพิ่มความโปร่งใสของข้อมูล",
    icon: "roi",
    layout: "roi",
    accent: C.primary,
    body: [
      "ลดเวลาการค้นหาเอกสารและติดตามสถานะ",
      "ลดต้นทุนกระดาษและการจัดเก็บเอกสาร",
      "ลดความเสี่ยงจากการอนุมัติผิดขั้นหรือข้อมูลตกหล่น",
      "ใช้ server และ open technology ที่ดูแลต่อได้ภายในองค์กร",
    ],
    note: "ROI ในบริบทโรงพยาบาลราชการควรพูดถึงคุณค่าด้านเวลา คุณภาพข้อมูล และความโปร่งใสมากกว่าผลตอบแทนทางการเงินล้วน",
  },
  {
    title: "Future Development",
    subtitle: "ต่อยอดสู่ระบบข้อมูลและบริการดิจิทัลของโรงพยาบาล",
    icon: "ai",
    layout: "future",
    accent: C.secondary,
    body: [
      "HOSxP Report Integration",
      "AI Assistant สำหรับค้นหาข้อมูลและสรุปรายงาน",
      "Data Analytics สำหรับแนวโน้มบุคลากรและทรัพยากร",
      "Mobile-first experience สำหรับการใช้งานนอกโต๊ะทำงาน",
    ],
    note: "วางภาพอนาคตแบบมีทิศทาง แต่ไม่ทำให้ดูไกลเกินจริง HOP คือฐานที่รองรับการต่อยอดเหล่านี้",
  },
  {
    title: "Conclusion",
    subtitle: "HOP คือก้าวสำคัญสู่การบริหารงานภายในแบบ Digital Hospital",
    icon: "check",
    layout: "conclusion",
    accent: C.primary,
    body: [
      "เริ่มจากงานที่ใช้จริงและเห็นผลเร็ว",
      "ลดภาระเอกสารและเพิ่มความโปร่งใสของ workflow",
      "สร้างฐานข้อมูลกลางที่รองรับการตัดสินใจของผู้บริหาร",
      "พร้อมต่อยอดสู่โมดูลอื่นในปีงบประมาณ 2570",
    ],
    note: "สรุปให้ผู้บริหารเห็น decision point: เห็นชอบให้ดำเนินการ pilot และสนับสนุนการขยายผลหลังทดสอบสำเร็จ",
  },
  {
    title: "Q&A",
    subtitle: "เปิดรับข้อเสนอแนะเพื่อปรับระบบให้เหมาะกับบริบทโรงพยาบาลนาหมื่น",
    icon: "qa",
    layout: "qa",
    accent: C.secondary,
    body: [
      "ข้อเสนอแนะด้าน workflow",
      "ข้อเสนอแนะด้านสิทธิ์และข้อมูลบุคลากร",
      "ข้อเสนอแนะด้านการ rollout และการอบรมผู้ใช้งาน",
    ],
    note: "ใช้สไลด์นี้เปิดพื้นที่สนทนา อาจเตรียมคำตอบเรื่อง security, backup, LINE, และการเริ่ม pilot ไว้ล่วงหน้า",
  },
  {
    title: "Thank You",
    subtitle: "Hospital Operations Portal (HOP)",
    icon: "thanks",
    layout: "thanks",
    accent: C.primary,
    body: [
      "โรงพยาบาลนาหมื่น",
      "Information Technology Department",
      "ขอบคุณครับ / ค่ะ",
    ],
    note: "ปิดด้วยน้ำเสียงอบอุ่นและมั่นใจ พร้อมเชิญผู้บริหารให้เสนอแนวทางการสนับสนุน pilot",
  },
];

async function ensureDirs() {
  const dirs = [
    ASSETS,
    path.join(ASSETS, "icons"),
    path.join(ASSETS, "illustrations"),
    path.join(ASSETS, "mockups"),
    path.join(ASSETS, "logo"),
    path.join(ASSETS, "backgrounds"),
    path.join(ASSETS, "diagrams"),
    PREVIEW,
    PDF_IMAGES,
    QA,
    path.join(OUT, "notes"),
  ];
  for (const dir of dirs) await fsp.mkdir(dir, { recursive: true });
}

function svgIcon(label, color = C.primary) {
  const glyph = {
    hospital: "H",
    summary: "Σ",
    warning: "!",
    workflow: "↔",
    vision: "◎",
    target: "⌾",
    scope: "▣",
    server: "▤",
    tech: "</>",
    shield: "✓",
    dashboard: "▦",
    users: "●●",
    leave: "☑",
    inventory: "▥",
    repair: "⚙",
    report: "▧",
    bell: "◔",
    flow: "⇢",
    database: "DB",
    roadmap: "→",
    plan: "1",
    kpi: "%",
    benefit: "+",
    risk: "!",
    roi: "฿",
    ai: "AI",
    check: "✓",
    qa: "?",
    thanks: "♥",
  }[label] || "H";
  return `<svg xmlns="http://www.w3.org/2000/svg" width="128" height="128" viewBox="0 0 128 128">
    <rect width="128" height="128" rx="28" fill="#${C.tealSoft}"/>
    <circle cx="64" cy="64" r="42" fill="#${color}" opacity=".12"/>
    <text x="64" y="73" text-anchor="middle" font-family="Arial" font-size="${glyph.length > 2 ? 24 : 38}" font-weight="700" fill="#${color}">${glyph}</text>
  </svg>`;
}

function xmlEscape(s) {
  return String(s).replace(/[&<>"']/g, (ch) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&apos;" }[ch]));
}

async function writeSvgAssets() {
  const keys = [...new Set(slides.map((s) => s.icon))];
  for (const key of keys) {
    await fsp.writeFile(path.join(ASSETS, "icons", `${key}.svg`), svgIcon(key), "utf8");
  }
  await fsp.writeFile(
    path.join(ASSETS, "logo", "hop-logo.svg"),
    `<svg xmlns="http://www.w3.org/2000/svg" width="500" height="180" viewBox="0 0 500 180">
      <rect width="500" height="180" rx="32" fill="#${C.primary}"/>
      <circle cx="86" cy="90" r="48" fill="#${C.white}" opacity=".95"/>
      <text x="86" y="106" text-anchor="middle" font-family="Arial" font-size="42" font-weight="700" fill="#${C.primary}">H</text>
      <text x="160" y="78" font-family="Arial" font-size="34" font-weight="700" fill="#${C.white}">HOP</text>
      <text x="160" y="116" font-family="Arial" font-size="22" fill="#CCFBF1">Hospital Operations Portal</text>
    </svg>`,
    "utf8"
  );
  await fsp.writeFile(
    path.join(ASSETS, "backgrounds", "premium-healthcare-bg.svg"),
    `<svg xmlns="http://www.w3.org/2000/svg" width="1280" height="720" viewBox="0 0 1280 720">
      <rect width="1280" height="720" fill="#${C.bg}"/>
      <circle cx="1130" cy="90" r="250" fill="#${C.tealSoft}" opacity=".7"/>
      <circle cx="80" cy="665" r="230" fill="#${C.blueSoft}" opacity=".65"/>
      <path d="M960 0h320v720H820c95-108 142-232 141-372C960 224 928 109 960 0Z" fill="#${C.primary}" opacity=".055"/>
    </svg>`,
    "utf8"
  );
  await fsp.writeFile(
    path.join(ASSETS, "diagrams", "architecture-overview.svg"),
    `<svg xmlns="http://www.w3.org/2000/svg" width="900" height="420" viewBox="0 0 900 420">
      <rect width="900" height="420" rx="28" fill="#FFFFFF"/>
      ${["Frontend", "Backend API", "PostgreSQL", "LINE OA"].map((t, i) => `<rect x="${70 + i * 205}" y="${120 + (i % 2) * 80}" width="160" height="70" rx="18" fill="#${i === 1 ? C.primary : C.tealSoft}" stroke="#${C.border}"/><text x="${150 + i * 205}" y="${162 + (i % 2) * 80}" text-anchor="middle" font-family="Arial" font-size="20" font-weight="700" fill="#${i === 1 ? C.white : C.primary}">${t}</text>`).join("")}
      <path d="M230 155h45M435 235h45M640 155h45" stroke="#${C.secondary}" stroke-width="5" stroke-linecap="round"/>
    </svg>`,
    "utf8"
  );
  await fsp.writeFile(path.join(ASSETS, "mockups", "dashboard-mockup.svg"), mockupSvg("Dashboard", ["คำขอรออนุมัติ", "เจ้าหน้าที่ลาวันนี้", "วันลาคงเหลือ", "แจ้งเตือนล่าสุด"]), "utf8");
  await fsp.writeFile(path.join(ASSETS, "mockups", "leave-mockup.svg"), mockupSvg("Leave Management", ["เลขที่คำขอ", "สถานะเอกสาร", "สายอนุมัติ", "PDF ใบลา"]), "utf8");
  await fsp.writeFile(path.join(ASSETS, "mockups", "inventory-mockup.svg"), mockupSvg("Inventory", ["คงเหลือ", "เบิกจ่าย", "ใกล้หมด", "รายงาน"]), "utf8");
  await fsp.writeFile(path.join(ASSETS, "mockups", "repair-mockup.svg"), mockupSvg("Repair Request", ["แจ้งซ่อมใหม่", "กำลังดำเนินการ", "เสร็จสิ้น", "SLA"]), "utf8");
  await fsp.writeFile(path.join(ASSETS, "mockups", "executive-dashboard-mockup.svg"), mockupSvg("Executive Dashboard", ["ภาพรวม", "แนวโน้ม", "เปรียบเทียบ", "KPI"]), "utf8");
  await fsp.writeFile(
    path.join(ASSETS, "illustrations", "hospital-portal-illustration.svg"),
    `<svg xmlns="http://www.w3.org/2000/svg" width="760" height="420" viewBox="0 0 760 420">
      <rect width="760" height="420" rx="30" fill="#FFFFFF"/>
      <rect x="72" y="70" width="280" height="230" rx="24" fill="#${C.tealSoft}"/>
      <rect x="390" y="96" width="250" height="56" rx="18" fill="#${C.primary}"/>
      <rect x="390" y="176" width="250" height="56" rx="18" fill="#${C.blueSoft}"/>
      <rect x="390" y="256" width="250" height="56" rx="18" fill="#${C.greenSoft}"/>
      <text x="212" y="198" text-anchor="middle" font-family="Arial" font-size="70" font-weight="700" fill="#${C.primary}">HOP</text>
    </svg>`,
    "utf8"
  );
}

function mockupSvg(title, labels) {
  return `<svg xmlns="http://www.w3.org/2000/svg" width="900" height="520" viewBox="0 0 900 520">
    <rect width="900" height="520" rx="34" fill="#FFFFFF"/>
    <rect x="0" width="900" height="74" rx="34" fill="#${C.primary}"/>
    <text x="44" y="48" font-family="Arial" font-size="28" font-weight="700" fill="#FFFFFF">${xmlEscape(title)}</text>
    ${labels.map((l, i) => `<rect x="${52 + (i % 2) * 410}" y="${112 + Math.floor(i / 2) * 118}" width="360" height="84" rx="22" fill="#${[C.tealSoft, C.blueSoft, C.greenSoft, C.amberSoft][i]}"/><text x="${84 + (i % 2) * 410}" y="${162 + Math.floor(i / 2) * 118}" font-family="Arial" font-size="24" font-weight="700" fill="#${C.primary}">${xmlEscape(l)}</text>`).join("")}
    <path d="M70 395h730" stroke="#${C.border}" stroke-width="2"/>
    <path d="M80 430c75-55 138-25 195-58 65-38 98 36 170 6 73-31 118-62 195-18 38 22 78 28 122 11" fill="none" stroke="#${C.secondary}" stroke-width="9" stroke-linecap="round"/>
  </svg>`;
}

function addBg(slide) {
  slide.background = { color: C.bg };
  slide.addShape(SHAPE.arc, { x: 10.6, y: -0.45, w: 3.7, h: 3.7, line: { color: C.tealSoft, transparency: 100 }, fill: { color: C.tealSoft, transparency: 26 }, adjustPoint: 0.25, rotate: 0 });
  slide.addShape(SHAPE.arc, { x: -0.9, y: 5.65, w: 3.2, h: 3.2, line: { color: C.blueSoft, transparency: 100 }, fill: { color: C.blueSoft, transparency: 30 }, adjustPoint: 0.25, rotate: 180 });
}

function addText(slide, text, x, y, w, h, opts = {}) {
  slide.addText(text, {
    x, y, w, h,
    fontFace: FONT,
    fontSize: opts.size || 16,
    bold: !!opts.bold,
    color: opts.color || C.text,
    breakLine: false,
    valign: opts.valign || "mid",
    margin: opts.margin || 0,
    fit: "shrink",
    align: opts.align || "left",
  });
}

function addFooter(slide, idx) {
  slide.addShape(SHAPE.line, { x: 0.6, y: 7.04, w: 12.13, h: 0, line: { color: C.border, width: 1 } });
  addText(slide, "Hospital Operations Portal | โรงพยาบาลนาหมื่น", 0.6, 7.09, 5.4, 0.2, { size: 14, color: C.muted });
  addText(slide, String(idx).padStart(2, "0"), 12.2, 7.09, 0.5, 0.2, { size: 14, color: C.muted, align: "right" });
}

function addHeader(slide, spec, idx) {
  addText(slide, spec.title, 0.72, 0.52, 7.8, 0.38, { size: 20, bold: true, color: C.primary });
  addText(slide, spec.subtitle, 0.72, 0.94, 9.4, 0.34, { size: 18, color: C.muted });
  slide.addShape(SHAPE.line, { x: 0.72, y: 1.36, w: 1.35, h: 0, line: { color: spec.accent, width: 3 } });
  slide.addImage({ path: path.join(ASSETS, "icons", `${spec.icon}.svg`), x: 11.78, y: 0.42, w: 0.58, h: 0.58 });
  addFooter(slide, idx);
}

function addBullets(slide, items, x, y, w, h, color = C.text) {
  slide.addText(items.map((item) => ({ text: item, options: { bullet: { type: "bullet" } } })), {
    x, y, w, h,
    fontFace: FONT,
    fontSize: 16,
    color,
    breakLine: false,
    fit: "shrink",
    paraSpaceAfterPt: 7,
    margin: 0.08,
  });
}

function card(slide, x, y, w, h, title, body, color = C.primary) {
  slide.addShape(SHAPE.roundRect, { x, y, w, h, rectRadius: 0.08, line: { color: C.border, width: 1 }, fill: { color: C.white }, shadow: { type: "outer", color: "CBD5E1", opacity: 0.16, blur: 1, angle: 45, distance: 1 } });
  slide.addShape(SHAPE.rect, { x, y, w, h: 0.07, line: { color, transparency: 100 }, fill: { color } });
  addText(slide, title, x + 0.22, y + 0.22, w - 0.44, 0.3, { size: 18, bold: true, color });
  addText(slide, body, x + 0.22, y + 0.62, w - 0.44, h - 0.78, { size: 16, color: C.text, valign: "top" });
}

function metric(slide, x, y, w, h, value, label, color) {
  slide.addShape(SHAPE.roundRect, { x, y, w, h, rectRadius: 0.06, line: { color: C.border, width: 1 }, fill: { color: C.white } });
  slide.addShape(SHAPE.line, { x: x + 0.18, y: y + 0.16, w: w - 0.36, h: 0, line: { color, width: 3 } });
  addText(slide, value, x + 0.18, y + 0.38, w - 0.36, 0.4, { size: 24, bold: true, color });
  addText(slide, label, x + 0.18, y + 0.86, w - 0.36, 0.34, { size: 16, color: C.muted });
}

function workflow(slide, labels, colors) {
  labels.forEach((label, i) => {
    const x = 0.9 + i * 3.0;
    slide.addShape(SHAPE.roundRect, { x, y: 2.35, w: 2.18, h: 1.0, rectRadius: 0.08, line: { color: colors[i], width: 1.5 }, fill: { color: C.white } });
    addText(slide, label, x + 0.15, 2.55, 1.88, 0.58, { size: 16, bold: true, color: colors[i], align: "center" });
    if (i < labels.length - 1) {
      slide.addShape(SHAPE.chevron, { x: x + 2.24, y: 2.62, w: 0.44, h: 0.44, line: { color: C.border, transparency: 100 }, fill: { color: colors[i], transparency: 8 } });
    }
  });
}

function renderContent(slide, spec, idx) {
  addBg(slide);
  if (spec.layout === "cover") {
    slide.addShape(SHAPE.roundRect, { x: 0, y: 0, w: 5.15, h: 7.5, rectRadius: 0, fill: { color: C.primary }, line: { color: C.primary, transparency: 100 } });
    slide.addImage({ path: path.join(ASSETS, "logo", "hop-logo.svg"), x: 0.72, y: 0.62, w: 2.7, h: 0.98 });
    addText(slide, spec.title, 0.72, 2.05, 4.1, 0.64, { size: 26, bold: true, color: C.white });
    addText(slide, spec.subtitle, 0.72, 2.78, 3.85, 0.72, { size: 18, color: C.tealSoft, valign: "top" });
    addBullets(slide, spec.body, 0.9, 4.15, 3.65, 1.5, C.white);
    slide.addImage({ path: path.join(ASSETS, "illustrations", "hospital-portal-illustration.svg"), x: 5.75, y: 1.25, w: 6.7, h: 3.85 });
    addText(slide, "Executive Presentation | Phase 1", 5.95, 5.55, 4.0, 0.35, { size: 18, bold: true, color: C.primary });
    addText(slide, "Modern Hospital Operations • Digital Workflow • Real-time Tracking", 5.95, 5.95, 5.5, 0.3, { size: 14, color: C.muted });
    addFooter(slide, idx);
    return;
  }
  addHeader(slide, spec, idx);
  switch (spec.layout) {
    case "metrics":
      metric(slide, 0.82, 1.82, 2.65, 1.3, "Phase 1", "Dashboard • User • Leave", C.primary);
      metric(slide, 3.75, 1.82, 2.65, 1.3, "Real-time", "ติดตามสถานะได้ทันที", C.secondary);
      metric(slide, 6.68, 1.82, 2.65, 1.3, "Secure", "Role & Permission", C.success);
      metric(slide, 9.61, 1.82, 2.65, 1.3, "LINE OA", "แจ้งเตือนเฉพาะผู้เกี่ยวข้อง", C.warning);
      addBullets(slide, spec.body, 1.0, 3.65, 10.9, 1.9);
      break;
    case "pain":
      spec.body.forEach((b, i) => card(slide, 0.82 + (i % 2) * 5.9, 1.78 + Math.floor(i / 2) * 1.65, 5.45, 1.28, `Pain Point ${i + 1}`, b, [C.warning, C.danger, C.primary, C.secondary][i]));
      break;
    case "workflow-before":
      workflow(slide, ["แบบฟอร์มกระดาษ", "ส่งต่อด้วยคน", "บันทึกซ้ำ", "รวบรวมรายงาน"], [C.warning, C.warning, C.danger, C.muted]);
      addBullets(slide, spec.body, 1.1, 4.15, 10.4, 1.25);
      break;
    case "workflow-after":
      workflow(slide, ["ส่งผ่านระบบ", "ตรวจสิทธิ์", "แจ้งเตือน", "Dashboard"], [C.primary, C.secondary, C.success, C.primary]);
      addBullets(slide, spec.body, 1.1, 4.15, 10.4, 1.25);
      break;
    case "statement":
      slide.addShape(SHAPE.roundRect, { x: 1.25, y: 1.9, w: 10.85, h: 2.25, rectRadius: 0.08, line: { color: C.border, width: 1 }, fill: { color: C.white } });
      addText(slide, "“ระบบกลางที่ทำให้งานบริหารภายในโปร่งใส รวดเร็ว และพร้อมต่อยอดสู่ Digital Hospital”", 1.75, 2.4, 9.8, 0.75, { size: 22, bold: true, color: C.primary, align: "center" });
      addBullets(slide, spec.body, 1.7, 4.65, 9.85, 1.1);
      break;
    case "objectives":
    case "scope":
    case "cards":
      spec.body.forEach((b, i) => card(slide, 0.85 + (i % 2) * 5.85, 1.78 + Math.floor(i / 2) * 1.55, 5.35, 1.18, ["เป้าหมาย", "ขอบเขต", "ความสามารถ", "การต่อยอด"][i] || `หัวข้อ ${i + 1}`, b, [C.primary, C.secondary, C.success, C.warning][i % 4]));
      break;
    case "architecture":
      slide.addImage({ path: path.join(ASSETS, "diagrams", "architecture-overview.svg"), x: 1.0, y: 1.7, w: 7.4, h: 3.45 });
      addBullets(slide, spec.body, 8.75, 1.95, 3.4, 2.8);
      break;
    case "tech":
      ["React", ".NET 9", "PostgreSQL", "Ubuntu", "Docker", "JWT", "Role Permission", "LINE OA"].forEach((t, i) => {
        const x = 0.9 + (i % 4) * 3.05;
        const y = 1.88 + Math.floor(i / 4) * 1.18;
        slide.addShape(SHAPE.roundRect, { x, y, w: 2.55, h: 0.78, rectRadius: 0.08, line: { color: C.border }, fill: { color: i % 2 ? C.blueSoft : C.tealSoft } });
        addText(slide, t, x + 0.16, y + 0.18, 2.23, 0.32, { size: 18, bold: true, color: C.primary, align: "center" });
      });
      addBullets(slide, spec.body, 1.05, 4.5, 10.8, 1.05);
      break;
    case "security":
      card(slide, 0.9, 1.72, 3.7, 3.4, "Access Control", "JWT + Refresh Token\nRole & Permission\nBackend Enforcement", C.primary);
      card(slide, 4.82, 1.72, 3.7, 3.4, "Governance", "Audit Log\nPermission Denied\nApproval History", C.secondary);
      card(slide, 8.74, 1.72, 3.7, 3.4, "Privacy", "เห็นข้อมูลตามสิทธิ์\nแจ้งเตือนเฉพาะผู้เกี่ยวข้อง\nไม่เปิดเผย token", C.success);
      break;
    case "mockup-dashboard":
    case "mockup-leave":
    case "mockup-inventory":
    case "mockup-repair":
    case "mockup-executive":
      {
        const file = {
          "mockup-dashboard": "dashboard-mockup.svg",
          "mockup-leave": "leave-mockup.svg",
          "mockup-inventory": "inventory-mockup.svg",
          "mockup-repair": "repair-mockup.svg",
          "mockup-executive": "executive-dashboard-mockup.svg",
        }[spec.layout];
        slide.addImage({ path: path.join(ASSETS, "mockups", file), x: 0.82, y: 1.62, w: 6.65, h: 3.86 });
        addBullets(slide, spec.body, 7.85, 1.85, 4.15, 3.1);
      }
      break;
    case "notification":
      card(slide, 0.95, 1.72, 3.6, 3.6, "In-app", "Badge\nNotification Center\nAction Required", C.primary);
      card(slide, 4.85, 1.72, 3.6, 3.6, "LINE OA", "Flex Message\nSecure action URL\nDelivery logs", C.secondary);
      card(slide, 8.75, 1.72, 3.6, 3.6, "Policy", "แจ้งเฉพาะผู้เกี่ยวข้อง\nไม่แจ้งล่วงหน้า\nตรวจสอบย้อนหลังได้", C.warning);
      break;
    case "dataflow":
      workflow(slide, ["Frontend", "Backend API", "PostgreSQL", "Notification"], [C.secondary, C.primary, C.warning, C.success]);
      addBullets(slide, spec.body, 1.05, 4.15, 10.8, 1.2);
      break;
    case "database":
      spec.body.forEach((b, i) => card(slide, 0.85 + (i % 2) * 5.85, 1.78 + Math.floor(i / 2) * 1.55, 5.35, 1.18, ["Core Identity", "Leave Domain", "Notification", "Operations"][i], b, [C.primary, C.success, C.secondary, C.warning][i]));
      break;
    case "roadmap":
      slide.addShape(SHAPE.line, { x: 1.15, y: 3.2, w: 10.9, h: 0, line: { color: C.secondary, width: 5 } });
      [
        ["2569", "Dashboard\nUser Management\nLeave Management"],
        ["1 ต.ค. 2569", "เริ่มปีงบประมาณ 2570"],
        ["2570", "Inventory\nRepair Request\nExecutive Report"],
        ["ต่อยอด", "Mobile Responsive\nLINE Integration"],
      ].forEach(([year, txt], i) => {
        const x = 1.05 + i * 3.05;
        slide.addShape(SHAPE.ellipse, { x, y: 2.93, w: 0.54, h: 0.54, fill: { color: i === 0 ? C.primary : C.secondary }, line: { color: C.white, width: 2 } });
        addText(slide, year, x - 0.45, 2.18, 1.45, 0.34, { size: 18, bold: true, color: C.primary, align: "center" });
        addText(slide, txt, x - 0.72, 3.72, 2.0, 1.0, { size: 16, color: C.text, align: "center", valign: "top" });
      });
      break;
    case "timeline":
      spec.body.forEach((b, i) => card(slide, 0.82 + i * 2.45, 2.05, 2.05, 2.2, `Step ${i + 1}`, b.replace(/^\d\.\s*/, ""), [C.primary, C.secondary, C.success, C.warning, C.primary][i]));
      break;
    case "kpi":
      metric(slide, 0.9, 1.8, 2.65, 1.25, "-40%", "เวลาอนุมัติเฉลี่ย", C.success);
      metric(slide, 3.85, 1.8, 2.65, 1.25, "-60%", "เอกสารกระดาษ", C.secondary);
      metric(slide, 6.8, 1.8, 2.65, 1.25, "95%", "ติดตามผ่านระบบ", C.primary);
      metric(slide, 9.75, 1.8, 2.65, 1.25, "100%", "Audit สำคัญ", C.warning);
      addBullets(slide, spec.body, 1.0, 3.55, 10.9, 1.3);
      break;
    case "benefits":
    case "risk":
    case "roi":
    case "future":
    case "conclusion":
      addBullets(slide, spec.body, 1.0, 1.95, 11.0, 3.1);
      slide.addShape(SHAPE.roundRect, { x: 1.0, y: 5.36, w: 11.0, h: 0.75, rectRadius: 0.08, fill: { color: C.white }, line: { color: C.border } });
      addText(slide, spec.layout === "risk" ? "แนวทางหลัก: Pilot ก่อน ขยายเมื่อ workflow และข้อมูลตั้งต้นพร้อม" : "Key message: HOP ช่วยให้ข้อมูลพร้อมใช้ งานเร็วขึ้น และตรวจสอบได้", 1.35, 5.55, 10.3, 0.28, { size: 18, bold: true, color: spec.accent, align: "center" });
      break;
    case "qa":
    case "thanks":
      slide.addShape(SHAPE.roundRect, { x: 2.05, y: 2.05, w: 9.25, h: 2.85, rectRadius: 0.1, fill: { color: C.white }, line: { color: C.border } });
      addText(slide, spec.layout === "qa" ? "Q&A" : "Thank You", 2.5, 2.55, 8.35, 0.58, { size: 30, bold: true, color: C.primary, align: "center" });
      addText(slide, spec.body.join("\n"), 2.65, 3.35, 8.05, 0.98, { size: 18, color: C.muted, align: "center" });
      break;
    default:
      addBullets(slide, spec.body, 1.0, 1.9, 11.0, 3.0);
  }
}

async function buildPptx() {
  const pptx = new pptxgen();
  pptx.layout = "LAYOUT_WIDE";
  pptx.author = "Hospital Operations Portal";
  pptx.subject = "HOP Executive Presentation";
  pptx.title = "Hospital Operations Portal Executive Presentation";
  pptx.company = "Nan Muen Hospital";
  pptx.lang = "th-TH";
  pptx.theme = {
    headFontFace: FONT,
    bodyFontFace: FONT,
    lang: "th-TH",
    themeColors: [
      { name: "accent1", color: C.primary },
      { name: "accent2", color: C.secondary },
      { name: "accent3", color: C.success },
      { name: "accent4", color: C.warning },
      { name: "accent5", color: C.danger },
      { name: "accent6", color: C.muted },
    ],
  };
  pptx.defineLayout({ name: "HOP_WIDE", width: SLIDE_W, height: SLIDE_H });
  pptx.layout = "HOP_WIDE";
  pptx.defineSlideMaster({
    title: "HOP Executive Master",
    background: { color: C.bg },
    objects: [
      { line: { x: 0.6, y: 7.04, w: 12.13, h: 0, line: { color: C.border, width: 1 } } },
      { text: { text: "Hospital Operations Portal | โรงพยาบาลนาหมื่น", options: { x: 0.6, y: 7.09, w: 5.4, h: 0.2, fontFace: FONT, fontSize: 14, color: C.muted } } },
    ],
    slideNumber: { x: 12.2, y: 7.09, color: C.muted },
  });
  slides.forEach((spec, i) => {
    const slide = pptx.addSlide("HOP Executive Master");
    slide.addNotes(spec.note);
    slide.transition = { type: "fade" };
    renderContent(slide, spec, i + 1);
  });
  await pptx.writeFile({ fileName: PPTX });
}

function svgSlide(spec, idx) {
  const bulletRows = spec.body.map((b, i) => `<text x="118" y="${240 + i * 44}" font-family="TH Sarabun New, Arial" font-size="30" fill="#${C.text}">• ${xmlEscape(b)}</text>`).join("");
  return `<svg xmlns="http://www.w3.org/2000/svg" width="1280" height="720" viewBox="0 0 1280 720">
    <rect width="1280" height="720" fill="#${C.bg}"/>
    <circle cx="1130" cy="90" r="250" fill="#${C.tealSoft}" opacity=".65"/>
    <circle cx="60" cy="675" r="230" fill="#${C.blueSoft}" opacity=".55"/>
    <rect x="70" y="58" width="72" height="72" rx="20" fill="#${C.tealSoft}"/>
    <text x="106" y="107" text-anchor="middle" font-family="Arial" font-size="26" font-weight="700" fill="#${C.primary}">${xmlEscape(String(idx).padStart(2, "0"))}</text>
    <text x="166" y="88" font-family="TH Sarabun New, Arial" font-size="36" font-weight="700" fill="#${C.primary}">${xmlEscape(spec.title)}</text>
    <text x="166" y="125" font-family="TH Sarabun New, Arial" font-size="28" fill="#${C.muted}">${xmlEscape(spec.subtitle)}</text>
    <line x1="166" y1="148" x2="292" y2="148" stroke="#${spec.accent}" stroke-width="5" stroke-linecap="round"/>
    <rect x="78" y="190" width="1124" height="396" rx="28" fill="#FFFFFF" stroke="#${C.border}"/>
    ${bulletRows}
    <line x1="70" y1="676" x2="1210" y2="676" stroke="#${C.border}" stroke-width="2"/>
    <text x="70" y="704" font-family="TH Sarabun New, Arial" font-size="22" fill="#${C.muted}">Hospital Operations Portal | โรงพยาบาลนาหมื่น</text>
    <text x="1210" y="704" text-anchor="end" font-family="TH Sarabun New, Arial" font-size="22" fill="#${C.muted}">${String(idx).padStart(2, "0")}</text>
  </svg>`;
}

async function buildPreviews() {
  const manifest = [];
  for (let i = 0; i < slides.length; i++) {
    const svg = svgSlide(slides[i], i + 1);
    const png = path.join(PREVIEW, `slide-${String(i + 1).padStart(2, "0")}.png`);
    const pdfPng = path.join(PDF_IMAGES, `slide-${String(i + 1).padStart(2, "0")}.png`);
    await sharp(Buffer.from(svg)).png().toFile(png);
    await sharp(Buffer.from(svg.replace('width="1280" height="720"', 'width="1920" height="1080"'))).png().resize(1920, 1080).toFile(pdfPng);
    manifest.push({ slide: i + 1, title: slides[i].title, subtitle: slides[i].subtitle, bodyItems: slides[i].body.length, speakerNotes: !!slides[i].note });
  }
  await fsp.writeFile(path.join(QA, "slide-manifest.json"), JSON.stringify(manifest, null, 2), "utf8");
  const thumbs = await Promise.all(Array.from({ length: 30 }, async (_, i) => sharp(path.join(PREVIEW, `slide-${String(i + 1).padStart(2, "0")}.png`)).resize(320, 180).toBuffer()));
  const composite = thumbs.map((input, i) => ({ input, left: (i % 5) * 320, top: Math.floor(i / 5) * 180 }));
  await sharp({ create: { width: 1600, height: 1080, channels: 4, background: "#F8FAFC" } }).composite(composite).png().toFile(path.join(QA, "deck-montage.png"));
}

async function writeSourceDocs() {
  const src = path.join(OUT, "source");
  const content = [
    "# Hospital Operations Portal (HOP) - Executive Presentation Content",
    "",
    "เอกสารต้นฉบับสำหรับ PowerPoint ระดับผู้บริหาร โรงพยาบาลนาหมื่น",
    "",
    ...slides.flatMap((s, i) => [
      `## Slide ${i + 1}: ${s.title}`,
      "",
      "### Slide Title",
      s.title,
      "",
      "### Subtitle",
      s.subtitle,
      "",
      "### Bullet Content",
      ...s.body.map((b) => `- ${b}`),
      "",
      "### Speaker Note",
      s.note,
      "",
      "### Visual Suggestion",
      `ใช้ layout แบบ ${s.layout} พร้อม icon ${s.icon} และ accent color #${s.accent}`,
      "",
      "### Layout Suggestion",
      "ใช้ grid system 12 columns, พื้นหลังขาว/เทาอ่อน, card radius และ spacing สม่ำเสมอ",
      "",
      "### Icon Suggestion",
      `ใช้ SVG icon จาก assets/icons/${s.icon}.svg`,
      "",
    ]),
  ].join("\n");

  const outline = [
    "# Hospital Operations Portal (HOP) - Executive Presentation Outline",
    "",
    "## Storyline",
    "นำเสนอจากปัญหาปัจจุบัน → workflow ใหม่ → ความมั่นคงของ architecture → โมดูล Phase 1 → roadmap และผลลัพธ์เชิงบริหาร",
    "",
    "## Slide List",
    ...slides.map((s, i) => `${i + 1}. ${s.title} — ${s.subtitle}`),
    "",
    "## Recommended Timing",
    "- 30 slides ใช้เวลาประมาณ 25-35 นาที",
    "- สไลด์ 1-8: ภาพรวมและเหตุผลของโครงการ",
    "- สไลด์ 9-20: ระบบและโมดูลหลัก",
    "- สไลด์ 21-30: แผนดำเนินงาน ผลลัพธ์ ความเสี่ยง และข้อสรุป",
    "",
    "## Key Messages",
    "- HOP เป็นระบบกลาง ไม่ใช่เพียงระบบลา",
    "- Phase 1 เลือกโมดูลที่จำเป็นและพร้อมใช้งานจริง",
    "- ระบบเน้นความปลอดภัย ตรวจสอบย้อนหลังได้ และต่อยอดได้",
  ].join("\n");

  const notes = [
    "# Hospital Operations Portal (HOP) - Speaker Notes",
    "",
    ...slides.flatMap((s, i) => [
      `## Slide ${i + 1}: ${s.title}`,
      "",
      s.note,
      "",
      "ประเด็นเสริม:",
      ...s.body.map((b) => `- ${b}`),
      "",
    ]),
  ].join("\n");

  const guide = [
    "# Hospital Operations Portal (HOP) - Presentation Design Guide",
    "",
    "## Theme",
    "- Premium Healthcare",
    "- Modern",
    "- Cozy Minimal",
    "- Scandinavian",
    "- Professional Corporate",
    "",
    "## Color Palette",
    `| Token | Hex | Usage |`,
    `|---|---|---|`,
    `| Primary | #${C.primary} | หัวข้อ, เส้นเน้น, sidebar visual |`,
    `| Secondary | #${C.secondary} | accent, icon, connector |`,
    `| Background | #${C.bg} | พื้นหลังหลัก |`,
    `| Success | #${C.success} | approved, benefit, positive KPI |`,
    `| Warning | #${C.warning} | risk, attention, timeline marker |`,
    `| Danger | #${C.danger} | rejected, high risk |`,
    "",
    "## Typography",
    "- Font: TH Sarabun New",
    "- Title: 20 pt",
    "- Subtitle: 18 pt",
    "- Content: 16 pt",
    "- Caption/Footer: 14 pt",
    "",
    "## Layout Rules",
    "- ใช้ 12-column grid",
    "- ใช้ card สีขาวบนพื้นหลัง #F8FAFC",
    "- ใช้เส้น accent สั้นใต้หัวข้อเพื่อสร้าง hierarchy",
    "- หลีกเลี่ยงข้อความยาวในสไลด์เดียว ให้กระจายเป็น bullet สั้นและมี speaker note",
    "",
    "## Asset Folders",
    "- assets/icons: SVG icons",
    "- assets/mockups: mockup screens",
    "- assets/diagrams: architecture and data flow diagrams",
    "- assets/backgrounds: theme background",
    "- assets/logo: HOP logo and hospital logo",
  ].join("\n");

  await fsp.writeFile(path.join(src, "HOP_Presentation_Content.md"), content, "utf8");
  await fsp.writeFile(path.join(src, "HOP_Presentation_Outline.md"), outline, "utf8");
  await fsp.writeFile(path.join(src, "HOP_Presentation_Speaker_Notes.md"), notes, "utf8");
  await fsp.writeFile(path.join(src, "HOP_Presentation_Design_Guide.md"), guide, "utf8");
}

async function main() {
  await ensureDirs();
  await writeSvgAssets();
  await writeSourceDocs();
  await buildPptx();
  await buildPreviews();
  console.log(JSON.stringify({ pptx: PPTX, slides: slides.length, preview: PREVIEW, qa: QA }, null, 2));
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
