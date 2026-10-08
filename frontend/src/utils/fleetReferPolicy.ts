export const REFER_MISSION_TYPE = "ส่งต่อผู้ป่วย (Refer)";
export function applyReferPolicy<T extends { missionType: string; isUrgent: boolean; urgentReason: string }>(form: T): T {
  return form.missionType === REFER_MISSION_TYPE
    ? { ...form, isUrgent: true, urgentReason: form.urgentReason.trim() ? form.urgentReason : REFER_MISSION_TYPE }
    : form;
}
