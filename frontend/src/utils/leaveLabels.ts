export const leaveTypeLabels: Record<string, string> = {
  annual: "ลาพักผ่อน",
  "annual leave": "ลาพักผ่อน",
  sick: "ลาป่วย",
  "sick leave": "ลาป่วย",
  personal: "ลากิจ",
  "personal leave": "ลากิจ",
  maternity: "ลาคลอด",
  "maternity leave": "ลาคลอด",
  ordination: "ลาอุปสมบท",
  "ordination leave": "ลาอุปสมบท",
  study: "ลาศึกษาต่อ",
  "study leave": "ลาศึกษาต่อ",
  other: "อื่น ๆ",
  "other leave": "อื่น ๆ",
};

export const leaveStatusLabels: Record<string, string> = {
  Draft: "แบบร่าง",
  Pending: "รออนุมัติ",
  Approved: "อนุมัติแล้ว",
  Rejected: "ไม่อนุมัติ",
  Cancelled: "ยกเลิก",
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
  if (!status) {
    return "-";
  }

  return leaveStatusLabels[status] ?? status;
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

export function getLeaveStatusColor(status?: string | null): "default" | "warning" | "success" | "error" {
  switch (status) {
    case "Pending":
      return "warning";
    case "Approved":
      return "success";
    case "Rejected":
      return "error";
    case "Cancelled":
    case "Draft":
    default:
      return "default";
  }
}

export function getLeaveTypeColor(type?: string | null) {
  const normalized = type?.trim().toLowerCase();
  switch (normalized) {
    case "sick":
      return "#DBEAFE";
    case "annual":
      return "#DCFCE7";
    case "personal":
      return "#FEF3C7";
    case "maternity":
      return "#FCE7F3";
    case "ordination":
      return "#EDE9FE";
    case "study":
      return "#E0F2FE";
    default:
      return "#F1F5F9";
  }
}
