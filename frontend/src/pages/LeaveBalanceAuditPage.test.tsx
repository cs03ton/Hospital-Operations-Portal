import { fireEvent, screen, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { getAuditLeaveBalances, getLeaveBalances, type LeaveBalance } from "../api/leaveApi";
import { renderFleet } from "../test/renderFleet";
import { LeaveBalanceAuditPage } from "./LeaveBalanceAuditPage";

vi.mock("../api/leaveApi", () => ({ getAuditLeaveBalances: vi.fn(), getLeaveBalances: vi.fn() }));

function balance(year: number, carriedOverDays: number, usedDays: number, availableDays: number): LeaveBalance {
  return {
    id: `balance-${year}`, userId: "user-1", fullname: "พนักงานทดสอบ", departmentId: "department-1", departmentName: "ไอที",
    leaveTypeId: "vacation", leaveTypeName: "ลาพักผ่อน", year, useFiscalYear: true,
    entitledDays: 10, carriedOverDays, adjustedDays: 0, usedDays, pendingDays: 0,
    availableDays, remainingDays: availableDays,
  };
}

describe("LeaveBalanceAuditPage", () => {
  it("shows approved usage, previous balance and flags a carry-forward above the prior remaining", async () => {
    vi.mocked(getAuditLeaveBalances).mockResolvedValue([balance(2027, 8, 2, 16)]);
    vi.mocked(getLeaveBalances).mockImplementation(async ({ year } = {}) => {
      if (year === 2026) return [balance(2026, 0, 5, 5)];
      return [];
    });

    renderFleet(<LeaveBalanceAuditPage />, "/admin/leave-balances/audit", "/admin/leave-balances/audit");
    fireEvent.change(screen.getByLabelText("ปีงบประมาณ (พ.ศ.)"), { target: { value: "2570" } });

    expect(await screen.findByText("ยอดยกมาสูงกว่าปีก่อน")).toBeInTheDocument();
    expect(screen.getByText("พนักงานทดสอบ")).toBeInTheDocument();
    expect(screen.getByLabelText("ปีงบประมาณ (พ.ศ.)")).toHaveValue(2570);
    expect(screen.getByText("คงเหลือปีก่อน")).toBeInTheDocument();
  });

  it("lists staff with no saved balance and filters to one person", async () => {
    const unused = { ...balance(2027, 0, 0, 10), id: null, userId: "user-2", fullname: "ยังไม่เคยลา", employeeCode: "E002" };
    vi.mocked(getAuditLeaveBalances).mockResolvedValue([balance(2027, 0, 2, 8), unused]);
    vi.mocked(getLeaveBalances).mockResolvedValue([]);

    renderFleet(<LeaveBalanceAuditPage />, "/admin/leave-balances/audit", "/admin/leave-balances/audit");
    fireEvent.change(screen.getByLabelText("ปีงบประมาณ (พ.ศ.)"), { target: { value: "2570" } });

    expect(await screen.findByText("ยังไม่เคยลา")).toBeInTheDocument();
    expect(screen.getByText("ยังไม่ตั้งต้นยอด")).toBeInTheDocument();
    fireEvent.mouseDown(screen.getByRole("combobox", { name: "บุคลากร" }));
    fireEvent.click(within(screen.getByRole("listbox")).getByRole("option", { name: "ยังไม่เคยลา (E002)" }));
    expect(screen.getByText("ยังไม่เคยลา")).toBeInTheDocument();
    expect(within(screen.getByRole("table")).queryByText("พนักงานทดสอบ")).not.toBeInTheDocument();
  });
});
