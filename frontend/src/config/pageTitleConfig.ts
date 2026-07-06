type PageTitleEntry = {
  path: string;
  title: string;
  subtitle?: string;
  exact?: boolean;
  breadcrumbs?: PageBreadcrumb[];
};

export type PageTitle = {
  title: string;
  subtitle?: string;
};

export type PageBreadcrumb = {
  label: string;
  path?: string;
};

const pageTitleEntries: PageTitleEntry[] = [
  { path: "/dashboard/leave", title: "แดชบอร์ดระบบลา", subtitle: "ภาพรวมคำขอลา งานอนุมัติ และปฏิทินการลา", exact: true, breadcrumbs: [{ label: "Dashboard", path: "/dashboard" }, { label: "ระบบลา" }] },
  { path: "/dashboard/vehicle", title: "แดชบอร์ดระบบจองรถ/ยืมรถ", subtitle: "ระบบอยู่ระหว่างเตรียมเปิดใช้งาน", exact: true, breadcrumbs: [{ label: "Dashboard", path: "/dashboard" }, { label: "ระบบจองรถ/ยืมรถ" }] },
  { path: "/dashboard/repair", title: "แดชบอร์ดระบบแจ้งซ่อม", subtitle: "ระบบอยู่ระหว่างเตรียมเปิดใช้งาน", exact: true, breadcrumbs: [{ label: "Dashboard", path: "/dashboard" }, { label: "ระบบแจ้งซ่อม" }] },
  { path: "/dashboard/inventory", title: "แดชบอร์ด Inventory", subtitle: "ระบบอยู่ระหว่างเตรียมเปิดใช้งาน", exact: true, breadcrumbs: [{ label: "Dashboard", path: "/dashboard" }, { label: "Inventory" }] },
  { path: "/dashboard/executive", title: "Executive Dashboard", subtitle: "ภาพรวมเชิงบริหารและ KPI สำคัญ", exact: true, breadcrumbs: [{ label: "Dashboard", path: "/dashboard" }, { label: "Executive Dashboard" }] },
  { path: "/dashboard", title: "Dashboard Hub", subtitle: "ศูนย์กลางแดชบอร์ดของระบบงานโรงพยาบาล", exact: true, breadcrumbs: [{ label: "Dashboard" }] },
  { path: "/notifications", title: "ศูนย์แจ้งเตือน", subtitle: "รายการแจ้งเตือนตามบทบาทและหน้าที่ของคุณ", exact: true, breadcrumbs: [{ label: "ศูนย์แจ้งเตือน" }] },
  { path: "/profile", title: "ข้อมูลส่วนตัวของฉัน", subtitle: "ข้อมูลสำหรับระบบลาและเอกสารใบลา", exact: true, breadcrumbs: [{ label: "ข้อมูลส่วนตัวของฉัน" }] },
  { path: "/admin/users/create", title: "เพิ่มผู้ใช้", subtitle: "สร้างบัญชีผู้ใช้งานระบบ", exact: true, breadcrumbs: [{ label: "จัดการผู้ใช้", path: "/admin/users" }, { label: "เพิ่มผู้ใช้" }] },
  { path: "/admin/users", title: "จัดการผู้ใช้", subtitle: "ดูแลบัญชีเจ้าหน้าที่และสิทธิ์การใช้งาน", breadcrumbs: [{ label: "จัดการผู้ใช้" }] },
  { path: "/admin/departments/create", title: "เพิ่มหน่วยงาน", subtitle: "สร้างข้อมูลหน่วยงานในโรงพยาบาล", exact: true, breadcrumbs: [{ label: "จัดการหน่วยงาน", path: "/admin/departments" }, { label: "เพิ่มหน่วยงาน" }] },
  { path: "/admin/departments", title: "จัดการหน่วยงาน", subtitle: "ดูแลโครงสร้างหน่วยงาน", breadcrumbs: [{ label: "จัดการหน่วยงาน" }] },
  { path: "/admin/roles", title: "บทบาทและสิทธิ์", subtitle: "กำหนดบทบาทและ permission ของผู้ใช้งาน", breadcrumbs: [{ label: "บทบาทและสิทธิ์" }] },
  { path: "/admin/audit-logs/export", title: "ส่งออกบันทึกการใช้งาน", subtitle: "ส่งออกบันทึกเหตุการณ์สำคัญ", exact: true, breadcrumbs: [{ label: "บันทึกการใช้งาน", path: "/admin/audit-logs" }, { label: "ส่งออก" }] },
  { path: "/admin/audit-logs", title: "บันทึกการใช้งาน", subtitle: "ตรวจสอบบันทึกการใช้งานและเหตุการณ์ความปลอดภัย", breadcrumbs: [{ label: "บันทึกการใช้งาน" }] },
  { path: "/admin/health", title: "สถานะระบบ", subtitle: "ตรวจสอบความพร้อมของ API, Database, Storage, LINE และ Backup", exact: true, breadcrumbs: [{ label: "สถานะระบบ" }] },
  { path: "/admin/line-settings", title: "ตั้งค่า LINE", subtitle: "ตรวจสอบและทดสอบ LINE Messaging API", breadcrumbs: [{ label: "ตั้งค่า LINE" }] },
  { path: "/admin/approval-chains/create", title: "เพิ่มกฎการอนุมัติวันลา", subtitle: "กำหนด rule และลำดับผู้อนุมัติใหม่", exact: true, breadcrumbs: [{ label: "กฎการอนุมัติวันลา", path: "/admin/approval-chains" }, { label: "เพิ่มกฎการอนุมัติ" }] },
  { path: "/admin/approval-chains", title: "กฎการอนุมัติวันลา", subtitle: "จัดการ rule ผู้อนุมัติและขั้นตอนการอนุมัติ", breadcrumbs: [{ label: "กฎการอนุมัติวันลา" }] },
  { path: "/admin/approval-delegations", title: "มอบหมายอนุมัติ", subtitle: "จัดการผู้รับมอบหมายอนุมัติแทน", breadcrumbs: [{ label: "มอบหมายอนุมัติ" }] },
  { path: "/admin/leave-holidays", title: "วันหยุดราชการ", subtitle: "จัดการวันหยุดที่ใช้คำนวณวันลา", breadcrumbs: [{ label: "วันหยุดราชการ" }] },
  { path: "/leave/create", title: "สร้างคำขอลา", subtitle: "กรอกข้อมูลและส่งคำขอลา", exact: true, breadcrumbs: [{ label: "รายการคำขอลา", path: "/leave" }, { label: "สร้างคำขอลา" }] },
  { path: "/leave/pending-approvals", title: "งานรออนุมัติของฉัน", subtitle: "คำขอลาที่ถึงคิวอนุมัติของผู้ใช้งานปัจจุบัน", exact: true, breadcrumbs: [{ label: "ระบบลา" }, { label: "งานรออนุมัติของฉัน" }] },
  { path: "/leave/calendar", title: "ปฏิทินการลา", subtitle: "ดูภาพรวมการลาของเจ้าหน้าที่ตามเดือน", breadcrumbs: [{ label: "ปฏิทินการลา" }] },
  { path: "/leave/types", title: "ประเภทการลา", subtitle: "กำหนดประเภทลาและเงื่อนไขเบื้องต้น", breadcrumbs: [{ label: "ประเภทการลา" }] },
  { path: "/leave/balances", title: "วันลาคงเหลือ", subtitle: "ตรวจสอบสิทธิ์วันลาของผู้ใช้งาน", breadcrumbs: [{ label: "วันลาคงเหลือ" }] },
  { path: "/leave", title: "รายการคำขอลา", subtitle: "สร้างคำขอลา ติดตามสถานะ และดำเนินการอนุมัติ", breadcrumbs: [{ label: "รายการคำขอลา" }] },
  { path: "/reports/leaves", title: "รายงานการลา", subtitle: "สรุปและส่งออกข้อมูลการลา", breadcrumbs: [{ label: "รายงานการลา" }] },
];

export function getPageTitle(pathname: string): PageTitle {
  const matched = findPageTitleEntry(pathname);
  return matched ?? { title: "หน้าหลักระบบ", subtitle: "Hospital Operations Portal" };
}

export function getPageBreadcrumbs(pathname: string): PageBreadcrumb[] {
  const matched = findPageTitleEntry(pathname);
  return matched?.breadcrumbs ?? [{ label: matched?.title ?? "หน้าหลักระบบ" }];
}

function findPageTitleEntry(pathname: string): PageTitleEntry | undefined {
  const normalizedPath = pathname === "/" ? "/dashboard" : pathname;
  return pageTitleEntries.find((entry) =>
    entry.exact ? normalizedPath === entry.path : normalizedPath === entry.path || normalizedPath.startsWith(`${entry.path}/`),
  );
}
