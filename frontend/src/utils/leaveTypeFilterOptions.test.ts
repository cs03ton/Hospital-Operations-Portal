import { describe, expect, it } from "vitest";
import { getLeaveTypeFilterOptions, withLeaveTypeFilter } from "./leaveTypeFilterOptions";

describe("leave type filter options", () => {
  it("groups identical labels while preserving every leave type ID", () => {
    const options = getLeaveTypeFilterOptions([
      { id: "a", code: "SICK_LEAVE", name: "ลาป่วย" },
      { id: "b", code: "SICK", name: "ลาป่วย" },
      { id: "c", code: "VACATION_LEAVE", name: "ลาพักผ่อน" },
    ]);
    expect(options).toEqual([
      { label: "ลาป่วย", value: "a,b" },
      { label: "ลาพักผ่อน", value: "c" },
    ]);
    expect(withLeaveTypeFilter({ leaveTypeId: options[0].value, page: 1 })).toEqual({ leaveTypeIds: "a,b", page: 1 });
    expect(withLeaveTypeFilter({ leaveTypeId: options[1].value })).toEqual({ leaveTypeId: "c" });
  });
});
