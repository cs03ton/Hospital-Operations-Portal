export const repairStatusLabels: Record<string, string> = {
  Submitted: "แจ้งใหม่",
  InProgress: "ดำเนินการ",
  WaitingParts: "รออะไหล่",
  Resolved: "รอตรวจรับ",
  Closed: "ปิดงาน",
  Returned: "ส่งกลับผู้แจ้ง",
  Cancelled: "ยกเลิก",
};
export type RepairTone = { foreground: string; background: string; border: string; accent: string };
const neutral: RepairTone = { foreground: '#53616D', background: '#EFF2F4', border: '#CCD3D8', accent: '#75828C' };
const orange: RepairTone = { foreground: '#8A4B08', background: '#FFF3E2', border: '#E9C99B', accent: '#C87915' };
export const repairStatusTones: Record<string, RepairTone> = {
  Submitted: { foreground: '#2456A6', background: '#EDF4FF', border: '#C1D5F1', accent: '#3478CE' },
  InProgress: { foreground: '#145C7E', background: '#E9F4FA', border: '#BCD9E7', accent: '#21779D' },
  WaitingParts: orange,
  Resolved: { foreground: '#643B9B', background: '#F3EDFC', border: '#D6C4ED', accent: '#8053B4' },
  Closed: { foreground: '#236640', background: '#EAF5EE', border: '#BDDBC8', accent: '#328257' },
  Returned: { foreground: '#9C4027', background: '#FFF0EA', border: '#EBC4B7', accent: '#C55D3D' },
  Cancelled: neutral,
};
export const repairPriorityTones: Record<string, RepairTone> = {
  Normal: { foreground: '#415F75', background: '#EDF3F7', border: '#C7D5DE', accent: '#628198' },
  Urgent: orange,
  Emergency: { foreground: '#A12D32', background: '#FDEEF0', border: '#EABFC4', accent: '#C4444A' },
};
export const getRepairStatusTone = (status: string): RepairTone => repairStatusTones[status] ?? neutral;
export const getRepairPriorityTone = (priority?: string): RepairTone => repairPriorityTones[priority ?? ''] ?? neutral;
export const repairPriorityLabels: Record<string, string> = {
  Normal: "ปกติ",
  Urgent: "เร่งด่วน",
  Emergency: "ฉุกเฉิน",
};
export const repairActionLabels: Record<string, string> = {
  "category-change": "ผู้ดูแลเปลี่ยนประเภทงาน",
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
