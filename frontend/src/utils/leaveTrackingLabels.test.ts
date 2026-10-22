import { describe, expect, it } from "vitest";
import { getTrackingStepLabel } from "./leaveTrackingLabels";

describe("getTrackingStepLabel", () => {
  it("shows a simple progress label while approval is pending and after completion", () => {
    expect(getTrackingStepLabel({ status: "Pending", currentStepName: "หัวหน้าหน่วยงานสูงสุด" })).toBe("กำลังดำเนินการ");
    expect(getTrackingStepLabel({ status: "Approved", currentStepName: "Director" })).toBe("เสร็จสิ้น");
  });

  it("keeps draft and rejected requests distinguishable from completed approvals", () => {
    expect(getTrackingStepLabel({ status: "Draft", currentStepName: null })).toBe("ยังไม่ส่งคำขอ");
    expect(getTrackingStepLabel({ status: "Rejected", currentStepName: null })).toBe("สิ้นสุดด้วยการไม่อนุมัติ");
  });
});
