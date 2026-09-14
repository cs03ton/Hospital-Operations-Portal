export const repairStatusLabels: Record<string, string> = {
  Submitted: "แจ้งใหม่",
  InProgress: "ดำเนินการ",
  WaitingParts: "รออะไหล่",
  Resolved: "รอตรวจรับ",
  Closed: "ปิดงาน",
  Returned: "ส่งกลับผู้แจ้ง",
  Cancelled: "ยกเลิก",
};
export const repairStatusColors = {
  Submitted: "info",
  InProgress: "primary",
  WaitingParts: "warning",
  Resolved: "success",
  Closed: "success",
  Returned: "warning",
  Cancelled: "default",
} as const;
export const repairPriorityLabels: Record<string, string> = {
  Normal: "ปกติ",
  Urgent: "เร่งด่วน",
  Emergency: "ฉุกเฉิน",
};
export const repairActionLabels: Record<string, string> = {
  start: "เริ่มดำเนินการ",
  wait: "รออะไหล่",
  resume: "ดำเนินการต่อ",
  return: "ส่งกลับผู้แจ้ง",
  priority: "กำหนดความเร่งด่วน",
  note: "เพิ่มบันทึก",
  solve: "แก้ไขเสร็จ",
  accept: "ตรวจรับและปิดงาน",
  "reject-solution": "ยังแก้ไม่สำเร็จ",
  reopen: "เปิดงานซ้ำ",
  resubmit: "แก้ไขและส่งใหม่",
  cancel: "ยกเลิกงาน",
  submit: "ส่งแจ้งซ่อม",
  images: "แนบรูป",
};
export const repairTeamLabel = (code: string) =>
  code === "IT" ? "IT" : code === "GENERAL" ? "ช่างทั่วไป" : code;
export const repairNumber = (number: number) =>
  `REP-${String(number).padStart(6, "0")}`;
export const repairDeliveryLabels: Record<string, string> = {
  Pending: "รอส่ง",
  Sending: "กำลังส่ง",
  Sent: "ส่งสำเร็จ",
  Attention: "ต้องตรวจสอบ",
  Superseded: "ยกเลิกส่งให้ทีมเดิม",
};
