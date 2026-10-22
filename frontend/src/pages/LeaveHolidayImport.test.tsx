import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { LeaveHolidayManagementPage } from "./LeaveHolidayManagementPage";
import { renderFleet } from "../test/renderFleet";
import * as api from "../api/leaveApi";

vi.mock("../api/leaveApi", () => ({ getLeaveHolidaysPaged: vi.fn(), previewLeaveHolidayImport: vi.fn(), confirmLeaveHolidayImport: vi.fn(),
  createLeaveHoliday: vi.fn(), updateLeaveHoliday: vi.fn(), deleteLeaveHoliday: vi.fn(), downloadLeaveHolidayTemplate: vi.fn() }));
vi.mock("../hooks/useSaveFeedback", () => ({ useSaveFeedback: () => ({ showSaveError: vi.fn(), showSuccessAndRedirect: vi.fn() }) }));

const rows = [13, 14, 15].map((day, index) => ({ rowNumber: index + 2, holidayDate: `2027-04-${day}`, name: "วันสงกรานต์", holidayType: "National", isValid: true, errors: [] as string[] }));
describe("Holiday import preview", () => {
  beforeEach(() => {
    vi.resetAllMocks();
    vi.mocked(api.getLeaveHolidaysPaged).mockResolvedValue({ items: [], page: 1, pageSize: 20, totalItems: 0, totalPages: 0 });
    vi.mocked(api.previewLeaveHolidayImport).mockResolvedValue({ totalRows: 3, validRows: 3, invalidRows: 0, rows });
    vi.mocked(api.confirmLeaveHolidayImport).mockResolvedValue({ addedCount: 3, failedRows: [] });
  });
  async function openPreview() {
    renderFleet(<LeaveHolidayManagementPage />);
    fireEvent.click(screen.getByRole("button", { name: "Import วันหยุดราชการ" }));
    fireEvent.change(screen.getByLabelText("ไฟล์นำเข้าวันหยุด"), { target: { files: [new File(["วันที่,ชื่อวันหยุด\n13/4/2027,วันสงกรานต์"], "holidays.csv", { type: "text/csv" })] } });
    fireEvent.click(screen.getByRole("button", { name: "Preview ก่อน Import" }));
    await screen.findByText("ทั้งหมด 3 รายการ");
  }
  it("shows two-column instructions, Buddhist dates and repeated holiday names before confirming the whole set", async () => {
    await openPreview();
    expect(screen.getByText(/6\/4\/2027 หมายถึง 6 เมษายน 2570/)).toBeInTheDocument();
    expect(screen.getByText("13/04/2570")).toBeInTheDocument();
    expect(screen.getAllByText("วันสงกรานต์")).toHaveLength(3);
    expect(api.confirmLeaveHolidayImport).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: "Confirm Import" }));
    await waitFor(() => expect(api.confirmLeaveHolidayImport).toHaveBeenCalledWith({ rows: rows.map(({ holidayDate, name, holidayType }) => ({ holidayDate, name, holidayType })) }));
  });
  it("disables confirmation for the entire file when any row has an error and displays its row number", async () => {
    vi.mocked(api.previewLeaveHolidayImport).mockResolvedValue({ totalRows: 3, validRows: 2, invalidRows: 1,
      rows: [rows[0], { ...rows[1], isValid: false, errors: ["วันที่ซ้ำ"] }, rows[2]] });
    await openPreview();
    expect(screen.getByText("วันที่ซ้ำ")).toBeInTheDocument();
    expect(screen.getByText("3")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Confirm Import" })).toBeDisabled();
    expect(api.confirmLeaveHolidayImport).not.toHaveBeenCalled();
  });
  it("requires a new preview after selecting a different file", async () => {
    await openPreview();
    fireEvent.change(screen.getByLabelText("ไฟล์นำเข้าวันหยุด"), { target: { files: [new File(["วันที่,ชื่อวันหยุด"], "another.xlsx")] } });
    expect(screen.getByRole("button", { name: "Confirm Import" })).toBeDisabled();
    expect(screen.queryByText("13/04/2570")).not.toBeInTheDocument();
  });
});
