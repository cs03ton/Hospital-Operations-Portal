export type AppStatusDomain = "leave" | "backup" | "diagnostics" | "notificationPriority" | "notificationType" | "lineBinding" | "active";

export type StatusTone = "default" | "success" | "warning" | "error" | "info";

type StatusMeta = {
  label: string;
  tone: StatusTone;
};

const statusMaps: Record<AppStatusDomain, Record<string, StatusMeta>> = {
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
