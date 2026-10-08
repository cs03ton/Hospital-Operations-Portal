import { describe, expect, it } from "vitest";
import { applyReferPolicy, REFER_MISSION_TYPE } from "./fleetReferPolicy";

describe("Refer mission", () => {
  it("forces urgency and supplies a reason for selection or draft reload", () => {
    expect(applyReferPolicy({ missionType: REFER_MISSION_TYPE, isUrgent: false, urgentReason: "" })).toEqual({ missionType: REFER_MISSION_TYPE, isUrgent: true, urgentReason: REFER_MISSION_TYPE });
  });
  it("preserves a custom reason", () => {
    expect(applyReferPolicy({ missionType: REFER_MISSION_TYPE, isUrgent: false, urgentReason: "ส่งต่อเพื่อรักษา" }).urgentReason).toBe("ส่งต่อเพื่อรักษา");
  });
  it("preserves urgency and reason when leaving Refer", () => {
    const form = { missionType: "ทั่วไป", isUrgent: true, urgentReason: "เหตุผลเดิม" };
    expect(applyReferPolicy(form)).toEqual(form);
  });
});
