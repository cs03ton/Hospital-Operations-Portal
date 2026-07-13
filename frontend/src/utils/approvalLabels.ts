export type ApprovalStatusIcon = "check" | "clock" | "close" | "cancel" | "skip" | "pending";

const approvalStatusLabels: Record<string, string> = {
  approved: "อนุมัติแล้ว",
  approve: "อนุมัติแล้ว",
  pending: "รออนุมัติ",
  returnedforrevision: "ตีกลับรอแก้ไข",
  rejected: "ไม่อนุมัติ",
  reject: "ไม่อนุมัติ",
  cancelled: "ยกเลิกแล้ว",
  canceled: "ยกเลิกแล้ว",
  skipped: "ข้ามขั้น",
  waiting: "ยังไม่ถึงขั้น",
  draft: "ยังไม่ถึงขั้น",
};

export function normalizeApprovalStatus(status?: string | null) {
  return status?.trim().toLowerCase() ?? "";
}

export function getApprovalStatusLabel(status?: string | null) {
  const normalized = normalizeApprovalStatus(status);
  if (!normalized) {
    return "-";
  }

  return approvalStatusLabels[normalized] ?? status ?? "-";
}

export function getApprovalStatusColor(status?: string | null): "default" | "warning" | "success" | "error" {
  switch (normalizeApprovalStatus(status)) {
    case "approved":
    case "approve":
      return "success";
    case "pending":
    case "returnedforrevision":
      return "warning";
    case "rejected":
    case "reject":
      return "error";
    case "cancelled":
    case "canceled":
    case "skipped":
    case "waiting":
    case "draft":
    default:
      return "default";
  }
}

export function getApprovalStatusIcon(status?: string | null): ApprovalStatusIcon {
  switch (normalizeApprovalStatus(status)) {
    case "approved":
    case "approve":
      return "check";
    case "pending":
    case "returnedforrevision":
      return "clock";
    case "rejected":
    case "reject":
      return "close";
    case "cancelled":
    case "canceled":
      return "cancel";
    case "skipped":
      return "skip";
    case "waiting":
    case "draft":
    default:
      return "pending";
  }
}
