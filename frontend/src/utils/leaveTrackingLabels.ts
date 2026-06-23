import type { LeaveRequest } from "../api/leaveApi";

export function getLeaveRequestCode(requestNumber?: string | null, _id?: string | null) {
  if (requestNumber) {
    return requestNumber;
  }

  return "-";
}

export function getTrackingStatusLabel(request: Pick<LeaveRequest, "status" | "currentStatusLabel" | "currentApproverName" | "currentStepName">) {
  if (request.currentStatusLabel) {
    return request.currentStatusLabel;
  }

  switch (request.status) {
    case "Draft":
      return "แบบร่าง";
    case "Pending":
      return request.currentApproverName ? `รออนุมัติจาก ${request.currentApproverName}` : "ส่งคำขอแล้ว";
    case "Approved":
      return "อนุมัติแล้ว";
    case "Rejected":
      return "ไม่อนุมัติ";
    case "Cancelled":
      return "ยกเลิกแล้ว";
    default:
      return request.status;
  }
}

export function getTrackingStepLabel(request: Pick<LeaveRequest, "status" | "currentStepName">) {
  if (request.status === "Draft") {
    return "ยังไม่ส่งคำขอ";
  }

  if (request.status === "Pending") {
    return request.currentStepName || "รออนุมัติ";
  }

  if (request.status === "Approved") {
    return "เสร็จสิ้น";
  }

  if (request.status === "Rejected") {
    return "สิ้นสุดด้วยการไม่อนุมัติ";
  }

  if (request.status === "Cancelled") {
    return "ยกเลิกคำขอ";
  }

  return request.currentStepName || "-";
}

export function getTrackingMessage(request: Pick<LeaveRequest, "id" | "requestNumber" | "status" | "trackingMessage" | "currentApproverName" | "currentStepName">) {
  if (request.trackingMessage) {
    return request.trackingMessage;
  }

  const requestCode = getLeaveRequestCode(request.requestNumber, request.id);
  if (request.status === "Pending" && request.currentApproverName) {
    return `คำขอลา ${requestCode} รออนุมัติจาก ${request.currentApproverName}`;
  }

  return `คำขอลา ${requestCode} อยู่ในสถานะ ${getTrackingStepLabel(request)}`;
}
