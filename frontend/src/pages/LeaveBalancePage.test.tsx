import { screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { getMyLeaveBalances } from "../api/leaveApi";
import { getMyProfile } from "../api/profileApi";
import { renderFleet } from "../test/renderFleet";
import { LeaveBalancePage } from "./LeaveBalancePage";

vi.mock("../api/leaveApi", () => ({ getMyLeaveBalances: vi.fn() }));
vi.mock("../api/profileApi", () => ({ getMyProfile: vi.fn() }));

describe("LeaveBalancePage", () => {
  it("shows fiscal-year and calendar-year balances together", async () => {
    vi.mocked(getMyProfile).mockResolvedValue({ employmentType: "CIVIL_SERVANT", departmentName: "IT" } as never);
    vi.mocked(getMyLeaveBalances).mockResolvedValue([
      balance("ลาพักผ่อน", 2027, true),
      balance("ลาป่วย", 2026, false),
    ] as never);

    renderFleet(<LeaveBalancePage />, "/leave/balances", "/leave/balances");

    expect(await screen.findByText(/ปีงบประมาณ 2570/)).toBeInTheDocument();
    expect(screen.getAllByText(/ลาพักผ่อน/).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/ลาป่วย/).length).toBeGreaterThan(0);
    expect(screen.getByText("ปีปฏิทิน 2569")).toBeInTheDocument();
  });
});

function balance(leaveTypeName: string, year: number, useFiscalYear: boolean) {
  return {
    id: crypto.randomUUID(), userId: crypto.randomUUID(), leaveTypeId: crypto.randomUUID(), leaveTypeName,
    year, useFiscalYear, entitledDays: 30, carriedOverDays: 0, adjustedDays: 0,
    usedDays: 1, pendingDays: 0, availableDays: 29, remainingDays: 29,
  };
}
