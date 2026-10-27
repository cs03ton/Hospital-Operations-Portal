import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { useLocation } from "react-router-dom";
import { renderFleet } from "../../test/renderFleet";
import { RepairCategoryChangeButton } from "./RepairCategoryChangeButton";
import * as api from "../../api/repairApi";

const permission = vi.hoisted(() => ({ manage: true }));
vi.mock("../../context/PermissionContext", () => ({ usePermission: () => ({ hasPermission: () => permission.manage }) }));
vi.mock("../../api/repairApi", () => ({ repairOptions: vi.fn(), repairChangeCategory: vi.fn() }));
const request: api.Repair = { id: "repair", number: 3, requesterId: "requester", categoryId: "old", teamCode: "IT", title: "งานทดสอบ", description: "อาการเดิม", location: "ห้องทำงาน", contact: "ผู้แจ้ง", status: "Submitted", currentRound: 1, hasStarted: false, createdAt: "2026-10-27T01:00:00Z", updatedAt: "2026-10-27T01:00:00Z", concurrencyToken: "original-token" };
const categories: api.RepairCategory[] = [
  { id: "old", name: "ประเภทเดิม", teamCode: "IT", isActive: true, concurrencyToken: "1" },
  { id: "new", name: "ประเภทใหม่", teamCode: "IT", isActive: true, concurrencyToken: "2" },
  { id: "general", name: "ไฟฟ้า", teamCode: "GENERAL", isActive: true, concurrencyToken: "3" },
  { id: "inactive", name: "เลิกใช้", teamCode: "IT", isActive: false, concurrencyToken: "4" },
];
function Location() { return <div data-testid="location">{useLocation().pathname}</div>; }
describe("Admin repair category change", () => {
  beforeEach(() => {
    vi.resetAllMocks(); permission.manage = true;
    vi.mocked(api.repairOptions).mockResolvedValue({ categories, teams: [] });
    vi.mocked(api.repairChangeCategory).mockResolvedValue({ request: { ...request, categoryId: "new" }, canView: true });
  });
  async function open(started = false, status = "Submitted") {
    const changed = vi.fn().mockResolvedValue(undefined);
    renderFleet(<><RepairCategoryChangeButton request={{ ...request, hasStarted: started, status }} categoryName="ประเภทเดิม" allowed onChanged={changed} /><Location /></>);
    fireEvent.click(screen.getByRole("button", { name: "เปลี่ยนประเภทงาน" }));
    await waitFor(() => expect(screen.getByRole("combobox", { name: /ประเภทงานใหม่/ })).not.toHaveAttribute("aria-disabled", "true"));
    return changed;
  }
  async function choose(label: RegExp) {
    fireEvent.mouseDown(screen.getByRole("combobox", { name: /ประเภทงานใหม่/ }));
    fireEvent.click(await screen.findByRole("option", { name: label }));
  }
  it("requires a new category and reason, warns on transfer and sends the opened token", async () => {
    const changed = await open();
    expect(screen.getByRole("button", { name: "ยืนยันเปลี่ยนประเภท" })).toBeDisabled();
    await choose(/ไฟฟ้า/);
    expect(screen.getByText(/ใบงานจะย้ายจากทีม IT ไปยังทีม ช่างทั่วไป/)).toBeInTheDocument();
    fireEvent.change(screen.getByRole("textbox", { name: /เหตุผลการเปลี่ยนประเภท/ }), { target: { value: "เลือกประเภทผิด" } });
    fireEvent.click(screen.getByRole("button", { name: "ยืนยันเปลี่ยนประเภท" }));
    await waitFor(() => expect(api.repairChangeCategory).toHaveBeenCalledWith("repair", { categoryId: "general", reason: "เลือกประเภทผิด", concurrencyToken: "original-token" }));
    await waitFor(() => expect(changed).toHaveBeenCalled());
  });
  it("offers only active categories within the original team once work has started", async () => {
    await open(true); fireEvent.mouseDown(screen.getByRole("combobox", { name: /ประเภทงานใหม่/ }));
    expect(await screen.findByRole("option", { name: /ประเภทใหม่/ })).toBeInTheDocument();
    expect(screen.queryByRole("option", { name: /ไฟฟ้า|ประเภทเดิม|เลิกใช้/ })).not.toBeInTheDocument();
  });
  it("navigates to the list when the admin loses visibility after moving teams", async () => {
    vi.mocked(api.repairChangeCategory).mockResolvedValue({ request: { ...request, teamCode: "GENERAL" }, canView: false });
    await open(); await choose(/ไฟฟ้า/);
    fireEvent.change(screen.getByRole("textbox", { name: /เหตุผลการเปลี่ยนประเภท/ }), { target: { value: "ส่งทีมใหม่" } });
    fireEvent.click(screen.getByRole("button", { name: "ยืนยันเปลี่ยนประเภท" }));
    await waitFor(() => expect(screen.getByTestId("location")).toHaveTextContent("/repairs"));
  });

  it("allows a closed request with the Backend capability and warns about historical reports", async () => {
    await open(true, "Closed");
    expect(screen.getByText(/การเปลี่ยนประเภทจะปรับการจัดหมวดในรายงานย้อนหลัง/)).toBeInTheDocument();
    await choose(/ประเภทใหม่/);
    fireEvent.change(screen.getByRole("textbox", { name: /เหตุผลการเปลี่ยนประเภท/ }), { target: { value: "แก้หมวดหลังปิดงาน" } });
    fireEvent.click(screen.getByRole("button", { name: "ยืนยันเปลี่ยนประเภท" }));
    await waitFor(() => expect(api.repairChangeCategory).toHaveBeenCalledWith("repair", { categoryId: "new", reason: "แก้หมวดหลังปิดงาน", concurrencyToken: "original-token" }));
  });
  it("refreshes after a conflict and keeps the reason for review", async () => {
    vi.mocked(api.repairChangeCategory).mockRejectedValue(new Error("ข้อมูลเปลี่ยนแล้ว"));
    const changed = await open(); await choose(/ประเภทใหม่/);
    fireEvent.change(screen.getByRole("textbox", { name: /เหตุผลการเปลี่ยนประเภท/ }), { target: { value: "เหตุผลเดิม" } });
    fireEvent.click(screen.getByRole("button", { name: "ยืนยันเปลี่ยนประเภท" }));
    await waitFor(() => expect(changed).toHaveBeenCalled());
    expect(screen.getByRole("textbox", { name: /เหตุผลการเปลี่ยนประเภท/ })).toHaveValue("เหตุผลเดิม");
  });
  it("does not expose the action without management permission or Backend capability", () => {
    permission.manage = false;
    const view = renderFleet(<RepairCategoryChangeButton request={request} allowed categoryName="เดิม" onChanged={async () => {}} />);
    expect(screen.queryByRole("button", { name: "เปลี่ยนประเภทงาน" })).not.toBeInTheDocument();
    expect(api.repairOptions).not.toHaveBeenCalled(); view.unmount();
    permission.manage = true; renderFleet(<RepairCategoryChangeButton request={request} allowed={false} onChanged={async () => {}} />);
    expect(screen.queryByRole("button", { name: "เปลี่ยนประเภทงาน" })).not.toBeInTheDocument();
  });
});
