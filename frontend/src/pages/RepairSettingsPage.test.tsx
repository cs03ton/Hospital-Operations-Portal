import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import { RepairSettingsPage } from "./RepairSettingsPage";
import * as api from "../api/repairApi";

vi.mock("../context/PermissionContext", () => ({ usePermission: () => ({ hasPermission: () => true }) }));

const data = {
  categories: [
    {
      id: "1",
      name: "Printer",
      teamCode: "IT",
      isActive: true,
      concurrencyToken: "old",
    },
    {
      id: "2",
      name: "Water",
      teamCode: "GENERAL",
      isActive: false,
      concurrencyToken: "old",
    },
  ],
  deliveries: [
    {
      id: "1",
      requestId: "job-1",
      teamCode: "IT",
      status: "Attention",
      attempts: 3,
      errorCode: "TIMEOUT",
    },
  ],
};
vi.mock("../api/repairApi", async (original) => ({
  ...(await original<typeof api>()),
  repairSettings: vi.fn(),
  saveRepairCategory: vi.fn(),
}));
function mount(settings = data) {
  vi.mocked(api.repairSettings).mockResolvedValue(settings);
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  const view = render(
    <QueryClientProvider client={client}>
      <MemoryRouter>
        <RepairSettingsPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
  return {
    client,
    cleanup: () => {
      view.unmount();
      client.clear();
    },
  };
}
describe("repair settings", () => {
  it("paginates filtered categories for both table and mobile card layouts", async () => {
    const categories = Array.from({ length: 12 }, (_, index) => ({
      id: String(index + 1),
      name: `Category ${String(index + 1).padStart(2, "0")}`,
      teamCode: index % 2 === 0 ? "IT" : "GENERAL",
      isActive: true,
      concurrencyToken: "old",
    }));
    const { cleanup } = mount({ ...data, categories });
    expect(await screen.findByText("Category 01")).toBeInTheDocument();
    expect(screen.getByText("Category 10")).toBeInTheDocument();
    expect(screen.queryByText("Category 11")).not.toBeInTheDocument();
    expect(screen.getByText("แสดง 1-10 จาก 12 รายการ")).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "ไปหน้าถัดไป" }));
    expect(await screen.findByText("Category 11")).toBeInTheDocument();
    expect(screen.queryByText("Category 01")).not.toBeInTheDocument();

    fireEvent.change(screen.getByLabelText("ค้นหาประเภทงาน"), {
      target: { value: "Category 01" },
    });
    expect(screen.getByText("Category 01")).toBeInTheDocument();
    expect(screen.getByText("แสดง 1-1 จาก 1 รายการ")).toBeInTheDocument();
    cleanup();
  });

  it("filters categories and hides legacy deliveries", async () => {
    const { cleanup } = mount();
    await screen.findByText("Printer");
    fireEvent.change(screen.getByLabelText("ค้นหาประเภทงาน"), {
      target: { value: "Water" },
    });
    expect(screen.queryByText("Printer")).not.toBeInTheDocument();
    expect(screen.getByText("Water")).toBeInTheDocument();
    expect(screen.queryByRole("tab", { name: "ผลส่งเดิม" })).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: "จัดการกลุ่มแจ้งเตือนส่วนกลาง" })).toHaveAttribute("href", "/admin/line-groups");
    expect(screen.queryByText(/TIMEOUT/)).not.toBeInTheDocument();
    cleanup();
  });
  it("keeps edited text through a conflict and explicitly adopts the latest token", async () => {
    const { cleanup } = mount();
    vi.mocked(api.saveRepairCategory).mockRejectedValueOnce({
      response: { status: 409 },
    });
    fireEvent.click(
      await screen.findByRole("button", { name: "แก้ไข Printer" }),
    );
    fireEvent.change(screen.getByLabelText(/ชื่อประเภท/), {
      target: { value: "Edited printer" },
    });
    fireEvent.click(screen.getByRole("button", { name: "บันทึก" }));
    const reload = await screen.findByRole("button", {
      name: "โหลดข้อมูลล่าสุดโดยคงข้อความ",
    });
    expect(screen.getByLabelText(/ชื่อประเภท/)).toHaveValue("Edited printer");
    vi.mocked(api.repairSettings).mockResolvedValue({
      ...data,
      categories: data.categories.map((c) => ({
        ...c,
        concurrencyToken: "latest",
      })),
    });
    fireEvent.click(reload);
    await waitFor(() =>
      expect(
        screen.queryByRole("button", { name: "โหลดข้อมูลล่าสุดโดยคงข้อความ" }),
      ).not.toBeInTheDocument(),
    );
    expect(screen.getByLabelText(/ชื่อประเภท/)).toHaveValue("Edited printer");
    vi.mocked(api.saveRepairCategory).mockResolvedValueOnce(
      {} as Awaited<ReturnType<typeof api.saveRepairCategory>>,
    );
    fireEvent.click(screen.getByRole("button", { name: "บันทึก" }));
    await waitFor(() =>
      expect(api.saveRepairCategory).toHaveBeenLastCalledWith(
        expect.objectContaining({
          name: "Edited printer",
          concurrencyToken: "latest",
        }),
      ),
    );
    cleanup();
  });
});
