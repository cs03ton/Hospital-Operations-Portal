import type { LeaveType } from "../api/leaveApi";
import { getLeaveTypeLabel } from "./leaveLabels";

// Keep the underlying IDs so a shared display name still filters every matching leave type.
export function getLeaveTypeFilterOptions(types: Pick<LeaveType, "id" | "name" | "code">[]) {
  const groups = new Map<string, string[]>();
  for (const type of types) {
    const label = getLeaveTypeLabel(type.name || type.code).trim();
    if (!groups.has(label)) groups.set(label, []);
    groups.get(label)!.push(type.id);
  }
  return [...groups].map(([label, ids]) => ({ label, value: ids.join(",") }));
}

export function withLeaveTypeFilter<T extends { leaveTypeId?: string | null }>(params: T) {
  const { leaveTypeId, ...rest } = params;
  return leaveTypeId?.includes(",")
    ? { ...rest, leaveTypeIds: leaveTypeId }
    : { ...rest, leaveTypeId };
}
