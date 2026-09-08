export type AppStatusDomain = "leave" | "fleet" | "backup" | "diagnostics" | "notificationPriority" | "notificationType" | "lineBinding" | "active" | "announcement" | "announcementPriority";

export type StatusTone = "default" | "success" | "warning" | "error" | "info";

type StatusMeta = {
  label: string;
  tone: StatusTone;
};

const statusMaps: Record<AppStatusDomain, Record<string, StatusMeta>> = {
  fleet: {
    DRAFT: { label: "แบบร่าง", tone: "default" },
    PENDING_DISPATCH: { label: "รอจัดรถและคนขับ", tone: "warning" },
    PENDING_ADMIN_REVIEW: { label: "รอหัวหน้าฝ่ายบริหารตรวจสอบ", tone: "warning" },
    PENDING_DIRECTOR: { label: "รอผู้อำนวยการอนุมัติ", tone: "warning" },
    PENDING_DIRECTOR_APPROVAL: { label: "รอผู้อำนวยการอนุมัติ", tone: "warning" },
    APPROVED: { label: "อนุมัติแล้ว", tone: "success" },
    PENDING_DRIVER_ACK: { label: "รอคนขับรับทราบ", tone: "warning" },
    DRIVER_ACKNOWLEDGED: { label: "คนขับรับทราบแล้ว", tone: "info" },
    READY: { label: "พร้อมเดินทาง", tone: "info" },
    IN_PROGRESS: { label: "กำลังปฏิบัติงาน", tone: "info" },
    COMPLETED: { label: "เสร็จสิ้น", tone: "success" },
    CANCELLATION_PENDING: { label: "รอพิจารณายกเลิก", tone: "warning" },
    RETURNED: { label: "ส่งกลับแก้ไข", tone: "warning" },
    REJECTED: { label: "ไม่รับคำขอ", tone: "error" },
    CANCELLED: { label: "ยกเลิก", tone: "default" },
    ABORTED: { label: "ยุติการเดินทาง", tone: "error" },
    ASSIGNED: { label: "จัดรถและคนขับแล้ว", tone: "success" },
    REPLACED: { label: "เปลี่ยนรถหรือคนขับแล้ว", tone: "info" },
    AVAILABLE: { label: "พร้อมใช้งาน", tone: "success" },
    RESERVED: { label: "จองแล้ว", tone: "warning" },
    IN_USE: { label: "กำลังใช้งาน", tone: "info" },
    MAINTENANCE: { label: "อยู่ระหว่างบำรุงรักษา", tone: "warning" },
    TEMPORARILY_UNAVAILABLE: { label: "ไม่พร้อมใช้งานชั่วคราว", tone: "warning" },
    DECOMMISSIONED: { label: "เลิกใช้งาน", tone: "default" },
    UNAVAILABLE: { label: "ไม่พร้อมใช้งาน", tone: "error" },
    SUSPENDED: { label: "ระงับการใช้งาน", tone: "error" },
    ACTIVE: { label: "กำลังดำเนินการ", tone: "info" },
    MATCH: { label: "เหมาะสม", tone: "success" },
    PARTIAL_MATCH: { label: "เหมาะสมบางส่วน", tone: "warning" },
    NOT_MATCH: { label: "ไม่เหมาะสม", tone: "error" },
    OVERRIDDEN: { label: "อนุมัติยกเว้นแล้ว", tone: "info" },
    NORMAL: { label: "ปกติ", tone: "default" },
    URGENT: { label: "เร่งด่วน", tone: "warning" },
    EMERGENCY: { label: "ฉุกเฉิน", tone: "error" },
  },
  leave: {
    Draft: { label: "แบบร่าง", tone: "default" },
    Submitted: { label: "ส่งคำขอแล้ว", tone: "warning" },
    InApproval: { label: "อยู่ระหว่างอนุมัติ", tone: "warning" },
    Pending: { label: "รออนุมัติ", tone: "warning" },
    ReturnedForRevision: { label: "ตีกลับรอแก้ไข", tone: "warning" },
    Approved: { label: "อนุมัติแล้ว", tone: "success" },
    Rejected: { label: "ไม่อนุมัติ", tone: "error" },
    Cancelled: { label: "ยกเลิก", tone: "default" },
    CancelledAfterApproval: { label: "ยกเลิกหลังอนุมัติ", tone: "info" },
  },
  backup: {
    Running: { label: "กำลังทำงาน", tone: "warning" },
    Success: { label: "สำเร็จ", tone: "success" },
    Healthy: { label: "ปกติ", tone: "success" },
    Failed: { label: "ล้มเหลว", tone: "error" },
    Verified: { label: "ตรวจสอบแล้ว", tone: "success" },
    Deleted: { label: "ถูกลบ", tone: "default" },
  },
  diagnostics: {
    Healthy: { label: "ปกติ", tone: "success" },
    Warning: { label: "ควรตรวจสอบ", tone: "warning" },
    Unhealthy: { label: "ผิดปกติ", tone: "error" },
    Failed: { label: "ล้มเหลว", tone: "error" },
    Unknown: { label: "ไม่ทราบสถานะ", tone: "warning" },
    Running: { label: "กำลังทำงาน", tone: "warning" },
    Available: { label: "พร้อมดาวน์โหลด", tone: "success" },
    Expired: { label: "หมดอายุ", tone: "error" },
  },
  notificationPriority: {
    Critical: { label: "วิกฤต", tone: "error" },
    High: { label: "สูง", tone: "warning" },
    Medium: { label: "ปานกลาง", tone: "warning" },
    Normal: { label: "ปกติ", tone: "info" },
    Information: { label: "ข้อมูล", tone: "info" },
    Success: { label: "สำเร็จ", tone: "success" },
  },
  notificationType: {
    ActionRequired: { label: "ต้องดำเนินการ", tone: "warning" },
    Information: { label: "ข้อมูล", tone: "default" },
  },
  lineBinding: {
    Pending: { label: "รอผูกบัญชี", tone: "warning" },
    Bound: { label: "เชื่อมต่อแล้ว", tone: "success" },
    Unbound: { label: "ยกเลิกการเชื่อมต่อ", tone: "default" },
  },
  active: {
    active: { label: "ใช้งาน", tone: "success" },
    inactive: { label: "ปิดใช้งาน", tone: "default" },
  },
  announcement: {
    Draft: { label: "แบบร่าง", tone: "default" },
    Scheduled: { label: "ตั้งเวลา", tone: "info" },
    Published: { label: "เผยแพร่แล้ว", tone: "success" },
    Expired: { label: "หมดอายุ", tone: "warning" },
    Archived: { label: "จัดเก็บแล้ว", tone: "default" },
    Cancelled: { label: "ยกเลิก", tone: "error" },
  },
  announcementPriority: {
    Normal: { label: "ปกติ", tone: "info" },
    Important: { label: "สำคัญ", tone: "warning" },
    Critical: { label: "เร่งด่วน", tone: "error" },
  },
};

export function getStatusMeta(domain: AppStatusDomain, status?: string | null): StatusMeta {
  if (!status) {
    return { label: "-", tone: "default" };
  }

  return statusMaps[domain][status] ?? { label: status, tone: "default" };
}

export function getStatusLabel(domain: AppStatusDomain, status?: string | null) {
  return getStatusMeta(domain, status).label;
}

export function getStatusTone(domain: AppStatusDomain, status?: string | null) {
  return getStatusMeta(domain, status).tone;
}
