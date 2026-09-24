import { getStatusLabel, getStatusTone } from "./statusLabels";

export const leaveTypeLabels: Record<string, string> = {
  annual: "ลาพักผ่อน",
  vacation_leave: "ลาพักผ่อน",
  "vacation leave": "ลาพักผ่อน",
  "annual leave": "ลาพักผ่อน",
  sick: "ลาป่วย",
  sick_leave: "ลาป่วย",
  "sick leave": "ลาป่วย",
  personal: "ลากิจ",
  personal_leave: "ลากิจส่วนตัว",
  "personal leave": "ลากิจ",
  maternity: "ลาคลอด",
  maternity_leave: "ลาคลอดบุตร",
  "maternity leave": "ลาคลอด",
  ordination: "ลาอุปสมบท",
  ordination_leave: "ลาบวช",
  "ordination leave": "ลาอุปสมบท",
  study: "ลาศึกษาต่อ",
  study_leave: "ลาศึกษาต่อ",
  "study leave": "ลาศึกษาต่อ",
  other: "อื่น ๆ",
  other_leave: "อื่น ๆ",
  "other leave": "อื่น ๆ",
};

export const leaveStatusLabels: Record<string, string> = {
  Draft: "แบบร่าง",
  Pending: "รออนุมัติ",
  ReturnedForRevision: "ตีกลับรอแก้ไข",
  Approved: "อนุมัติแล้ว",
  Rejected: "ไม่อนุมัติ",
  Cancelled: "ยกเลิก",
  CancelledAfterApproval: "ยกเลิกหลังอนุมัติ",
};

export const leaveDurationTypeLabels: Record<string, string> = {
  FULL_DAY: "เต็มวัน",
  HALF_DAY_AM: "ครึ่งวัน (เช้า)",
  HALF_DAY_PM: "ครึ่งวัน (บ่าย)",
};

export function getLeaveTypeLabel(type?: string | null) {
  if (!type) {
    return "-";
  }

  const normalized = type.trim().toLowerCase();
  return leaveTypeLabels[normalized] ?? type;
}

export function getLeaveStatusLabel(status?: string | null) {
  return getStatusLabel("leave", status);
}

export function getLeaveDurationTypeLabel(durationType?: string | null) {
  if (!durationType) {
    return "เต็มวัน";
  }

  return leaveDurationTypeLabels[durationType] ?? durationType;
}

export function isHalfDayLeave(durationType?: string | null) {
  return durationType === "HALF_DAY_AM" || durationType === "HALF_DAY_PM";
}

export function getLeaveTypeWithDurationLabel(leaveType?: string | null, durationType?: string | null) {
  const leaveTypeLabel = getLeaveTypeLabel(leaveType);
  if (durationType === "HALF_DAY_AM") {
    return `${leaveTypeLabel} (เช้า)`;
  }

  if (durationType === "HALF_DAY_PM") {
    return `${leaveTypeLabel} (บ่าย)`;
  }

  return leaveTypeLabel;
}

export function getLeaveStatusColor(status?: string | null): "default" | "warning" | "success" | "error" | "info" {
  return getStatusTone("leave", status);
}

export function getLeaveTypeColor(type?: string | null) {
  const normalized = type?.trim().toLowerCase();
  switch (normalized) {
    case "sick":
    case "sick_leave":
    case "ลาป่วย":
      return "#DBEAFE";
    case "annual":
    case "vacation_leave":
    case "annual leave":
    case "vacation leave":
    case "ลาพักผ่อน":
      return "#DCFCE7";
    case "personal":
    case "personal_leave":
    case "ลากิจ":
    case "ลากิจส่วนตัว":
      return "#FEF3C7";
    case "maternity":
    case "maternity_leave":
    case "ลาคลอด":
    case "ลาคลอดบุตร":
      return "#FCE7F3";
    case "ordination":
    case "ordination_leave":
    case "ลาอุปสมบท":
    case "ลาบวช":
      return "#EDE9FE";
    case "study":
    case "study_leave":
    case "ลาศึกษาต่อ":
      return "#E0F2FE";
    default:
      return "#F1F5F9";
  }
}

export function getLeaveTypeAccentColor(type?: string | null) {
  switch (getLeaveTypeColor(type)) {
    case "#DBEAFE": return "#3B82F6";
    case "#DCFCE7": return "#22C55E";
    case "#FEF3C7": return "#D97706";
    case "#FCE7F3": return "#EC4899";
    case "#EDE9FE": return "#8B5CF6";
    case "#E0F2FE": return "#0284C7";
    default: return "#64748B";
  }
}
