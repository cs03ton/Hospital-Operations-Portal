import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { getContrastRatio } from "@mui/material/styles";
import { RepairPriority, RepairStatus, RepairTeam } from "./RepairUi";
import { getRepairPriorityTone, getRepairStatusTone, repairPriorityLabels, repairPriorityTones, repairStatusLabels, repairStatusTones } from "../../utils/repairPresentation";

describe("Repair status and priority colors", () => {
  it("keeps every badge text readable against its background", () => {
    for (const tone of [...Object.values(repairStatusTones), ...Object.values(repairPriorityTones), getRepairPriorityTone()])
      expect(getContrastRatio(tone.foreground, tone.background)).toBeGreaterThanOrEqual(4.5);
  });
  it("renders all status labels with their shared tone", () => {
    render(<>{Object.keys(repairStatusLabels).map(value => <RepairStatus key={value} value={value} />)}</>);
    for (const [value, label] of Object.entries(repairStatusLabels)) {
      expect(screen.getByText(label).parentElement).toHaveStyle({ color: getRepairStatusTone(value).foreground, backgroundColor: getRepairStatusTone(value).background });
    }
    expect(getRepairStatusTone("Resolved").accent).not.toBe(getRepairStatusTone("Closed").accent);
    expect(new Set(Object.values(repairStatusTones).map(tone => tone.accent)).size).toBe(7);
  });
  it("separates unassessed priority from normal and preserves team appearance", () => {
    render(<><RepairPriority /><RepairPriority value="Normal" /><RepairPriority value="Urgent" /><RepairPriority value="Emergency" /><RepairTeam value="IT" /></>);
    for (const label of ["ยังไม่ประเมิน", ...Object.values(repairPriorityLabels)]) expect(screen.getByText(label)).toBeInTheDocument();
    expect(getRepairPriorityTone().background).not.toBe(getRepairPriorityTone("Normal").background);
    expect(screen.getByText("IT").parentElement).toHaveStyle({ backgroundColor: "#FFF3D6" });
  });
  it("uses neutral colors for unknown values while keeping their text", () => {
    render(<><RepairStatus value="FutureStatus" /><RepairPriority value="FuturePriority" /></>);
    expect(screen.getByText("FutureStatus").parentElement).toHaveStyle({ color: getRepairStatusTone("Cancelled").foreground });
    expect(screen.getByText("FuturePriority")).toBeInTheDocument();
    expect(getRepairPriorityTone("FuturePriority")).toEqual(getRepairPriorityTone());
  });
});
