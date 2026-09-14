import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import { RepairListPage } from "./RepairPages";
import * as api from "../api/repairApi";

vi.mock("../context/PermissionContext", () => ({
  usePermission: () => ({
    hasAnyPermission: () => true,
    hasPermission: () => true,
  }),
}));
vi.mock("../api/repairApi", async (original) => ({
  ...(await original<typeof api>()),
  repairList: vi
    .fn()
    .mockResolvedValue({
      items: [
        {
          id: "1",
          number: 1,
          title: "เครื่องพิมพ์",
          location: "ห้องทำงาน",
          teamCode: "IT",
          status: "WaitingParts",
          currentRound: 2,
          updatedAt: "2026-09-11T00:00:00Z",
        },
      ],
      total: 1,
      pageSize: 20,
    }),
  repairSummary: vi
    .fn()
    .mockResolvedValue({
      generatedAtUtc: "2026-09-11T00:00:00Z",
      counts: [],
      teamPending: 1,
    }),
}));
describe("repair team queue", () => {
  it("resets pagination with filters and refreshes without navigation", async () => {
    vi.mocked(api.repairList).mockResolvedValue({
      items: [],
      total: 41,
      pageSize: 20,
    });
    const client = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    const view = render(
      <QueryClientProvider client={client}>
        <MemoryRouter>
          <RepairListPage />
        </MemoryRouter>
      </QueryClientProvider>,
    );
    fireEvent.click(
      await screen.findByRole("button", { name: "Go to page 2" }),
    );
    await waitFor(() =>
      expect(api.repairList).toHaveBeenLastCalledWith(
        expect.objectContaining({ page: 2 }),
      ),
    );
    fireEvent.change(screen.getByLabelText("ค้นหาหัวข้อหรือสถานที่"), {
      target: { value: "printer" },
    });
    await waitFor(() =>
      expect(api.repairList).toHaveBeenLastCalledWith(
        expect.objectContaining({ page: 1, search: "printer" }),
      ),
    );
    fireEvent.click(screen.getByRole("button", { name: "ล้างตัวกรอง" }));
    expect(screen.getByLabelText("ค้นหาหัวข้อหรือสถานที่")).toHaveValue("");
    await waitFor(() =>
      expect(
        screen.getByRole("button", { name: "โหลดข้อมูลล่าสุด" }),
      ).toBeEnabled(),
    );
    const calls = vi.mocked(api.repairList).mock.calls.length;
    fireEvent.click(screen.getByRole("button", { name: "โหลดข้อมูลล่าสุด" }));
    await waitFor(() =>
      expect(vi.mocked(api.repairList).mock.calls.length).toBeGreaterThan(
        calls,
      ),
    );
    view.unmount();
    client.clear();
  });
  it("shows team work, status, detail and create actions", async () => {
    vi.mocked(api.repairList).mockResolvedValue({
      items: [
        {
          id: "1",
          number: 1,
          title: "เครื่องพิมพ์",
          location: "ห้องทำงาน",
          teamCode: "IT",
          status: "WaitingParts",
          currentRound: 2,
          updatedAt: "2026-09-11T00:00:00Z",
        } as api.Repair,
      ],
      total: 1,
      pageSize: 20,
    });
    const client = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    const view = render(
      <QueryClientProvider client={client}>
        <MemoryRouter>
          <RepairListPage />
        </MemoryRouter>
      </QueryClientProvider>,
    );
    expect(await screen.findByText(/เครื่องพิมพ์/)).toBeInTheDocument();
    expect(screen.getByText("รออะไหล่")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "ดูรายละเอียด" })).toHaveAttribute(
      "href",
      "/repairs/1",
    );
    expect(screen.getByRole("link", { name: "แจ้งซ่อม" })).toHaveAttribute(
      "href",
      "/repairs/new",
    );
    expect(api.repairList).toHaveBeenCalledWith(
      expect.objectContaining({ scope: "team" }),
    );
    const options = client
      .getQueryCache()
      .find({ queryKey: ["repairs", "list", "team", "", "", 1] })?.options as {
      refetchInterval?: number;
      refetchIntervalInBackground?: boolean;
    };
    expect(options.refetchInterval).toBe(15000);
    expect(options.refetchIntervalInBackground).toBe(false);
    view.unmount();
    client.clear();
  });
});
