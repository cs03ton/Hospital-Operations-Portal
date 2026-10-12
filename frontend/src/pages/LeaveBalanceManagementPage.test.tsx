import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { getDepartments, getUsers } from "../api/adminApi";
import { getLeaveBalances, getLeaveTypes, previewLeaveBalanceRolloverBatch, type LeaveBalanceRolloverBatch } from "../api/leaveApi";
import { renderFleet } from "../test/renderFleet";
import { LeaveBalanceManagementPage } from "./LeaveBalanceManagementPage";

vi.mock("../api/adminApi", () => ({ getDepartments: vi.fn(), getUsers: vi.fn() }));
vi.mock("../api/leaveApi", async (importOriginal) => ({
  ...await importOriginal<typeof import("../api/leaveApi")>(),
  getLeaveBalances: vi.fn(), getLeaveTypes: vi.fn(), previewLeaveBalanceRolloverBatch: vi.fn(),
}));
vi.mock("../hooks/useNotification", () => ({ useNotification: () => ({ showSuccess: vi.fn(), showWarning: vi.fn() }) }));

const preview: LeaveBalanceRolloverBatch = {
  fromFiscalYear: 2026, toFiscalYear: 2027, totalUsers: 1, created: 1, updated: 0, skipped: 0, blocked: 0,
  items: [{
    userId: "user-1", employeeName: "พนักงานทดสอบ", departmentName: "ไอที", employmentType: "MOPH_EMPLOYEE",
    employmentTypeName: "พนักงานกระทรวง", leaveTypeId: "vacation", leaveTypeName: "ลาพักผ่อน",
    fromFiscalYear: 2026, toFiscalYear: 2027, entitlementDays: 10, carriedOverDays: 0,
    adjustedDays: 0, usedDays: 0, pendingDays: 0, endYearRemaining: 10, carryOverCap: 15,
    carryOverDays: 4, forfeitedDays: 6, newEntitlementDays: 10, newAvailableDays: -8,
    targetUsedDays: 20, targetPendingDays: 2, maximumTotalAvailableDays: 30,
    action: "Created", reason: "สร้างยอดวันลาปีงบประมาณใหม่", warnings: [],
  }],
};

describe("LeaveBalanceManagementPage rollover preview", () => {
  beforeEach(() => {
    vi.mocked(getDepartments).mockResolvedValue([]);
    vi.mocked(getUsers).mockResolvedValue([]);
    vi.mocked(getLeaveTypes).mockResolvedValue([{
      id: "vacation", code: "VACATION_LEAVE", name: "ลาพักผ่อน", defaultDaysPerYear: 10,
      requiresBalance: true, allowCarryOver: true, carryOverMaxDays: 15, useFiscalYear: true,
      requiresAttachment: false, isPaid: true, isActive: true,
    }]);
    vi.mocked(getLeaveBalances).mockResolvedValue([]);
    vi.mocked(previewLeaveBalanceRolloverBatch).mockResolvedValue(preview);
  });

  it("shows current target-year usage and signed remaining after rollover", async () => {
    renderFleet(<LeaveBalanceManagementPage />);
    fireEvent.click(screen.getByRole("button", { name: "คำนวณ Preview" }));

    const row = await screen.findByRole("row", { name: /พนักงานทดสอบ/ });
    expect(screen.getByRole("columnheader", { name: "ใช้ไปปีปลายทาง" })).toBeInTheDocument();
    expect(screen.getByRole("columnheader", { name: "รออนุมัติปีปลายทาง" })).toBeInTheDocument();
    expect(within(row).getByText("20")).toBeInTheDocument();
    expect(within(row).getByText("2")).toBeInTheDocument();
    expect(within(row).getByText("-8")).toBeInTheDocument();
    const input = await screen.findByRole("spinbutton", { name: "ยอดยกมาลาพักผ่อน พนักงานทดสอบ" });
    fireEvent.change(input, { target: { value: "14" } });
    expect(screen.queryByText(/ยอดยกมาต้องไม่ติดลบ/)).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "คำนวณ Preview" }));
    await waitFor(() => expect(previewLeaveBalanceRolloverBatch).toHaveBeenLastCalledWith(expect.objectContaining({
      overrides: [{ userId: "user-1", leaveTypeId: "vacation", carryOverDays: 14 }],
    })));
  });
});
