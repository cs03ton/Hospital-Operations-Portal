import { act, render, screen, waitFor } from "@testing-library/react";
import {
  QueryClient,
  QueryClientProvider,
  focusManager,
} from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import { RepairDashboardPage } from "./RepairDashboardPage";
import { RepairListPage } from "./RepairPages";
import * as api from "../api/repairApi";

vi.mock("../context/PermissionContext", () => ({
  usePermission: () => ({
    hasPermission: () => true,
    hasAnyPermission: () => true,
  }),
}));
vi.mock("../api/repairApi", async (original) => ({
  ...(await original<typeof api>()),
  repairSummary: vi.fn().mockResolvedValue({
    generatedAtUtc: "2026-09-14T00:00:00Z",
    counts: [{ status: "Submitted", count: 12 }],
    teamPending: 12,
  }),
  repairList: vi.fn().mockResolvedValue({
    items: Array.from({ length: 8 }, (_, i) => ({
      id: String(i),
      number: i + 1,
      title: `Repair ${i}`,
      status: "Submitted",
      teamCode: "IT",
      updatedAt: "2026-09-14T00:00:00Z",
    })),
    total: 8,
    pageSize: 5,
  }),
}));

function mount(element: React.ReactNode, route = "/dashboard/repair") {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  const view = render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={[route]}>{element}</MemoryRouter>
    </QueryClientProvider>,
  );
  return () => {
    view.unmount();
    client.clear();
  };
}

describe("repair dashboard and filtered navigation", () => {
  it("polls both data sources and pauses while the tab is hidden", async () => {
    vi.useFakeTimers();
    focusManager.setFocused(true);
    vi.mocked(api.repairList).mockClear();
    vi.mocked(api.repairSummary).mockClear();
    const cleanup = mount(<RepairDashboardPage />);
    try {
      await act(async () => {
        await vi.advanceTimersByTimeAsync(15000);
      });
      expect(api.repairList).toHaveBeenCalledTimes(2);
      expect(api.repairSummary).toHaveBeenCalledTimes(2);
      focusManager.setFocused(false);
      await act(async () => {
        await vi.advanceTimersByTimeAsync(30000);
      });
      expect(api.repairList).toHaveBeenCalledTimes(2);
      await act(async () => {
        focusManager.setFocused(true);
        await Promise.resolve();
      });
      expect(api.repairList).toHaveBeenCalledTimes(3);
      expect(api.repairSummary).toHaveBeenCalledTimes(3);
    } finally {
      cleanup();
      focusManager.setFocused(undefined);
      vi.useRealTimers();
    }
  });
  it("requests five visible jobs and links status counts to the same scope", async () => {
    const cleanup = mount(<RepairDashboardPage />);
    expect(await screen.findByText("Repair 0")).toBeInTheDocument();
    expect(screen.getAllByRole("link", { name: "ดูรายละเอียด" })).toHaveLength(
      5,
    );
    expect(screen.queryByText("Repair 5")).not.toBeInTheDocument();
    expect(screen.getByText("12", { selector: "h4" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "ดูงานแจ้งใหม่" })).toHaveAttribute(
      "href",
      "/repairs?scope=all&status=Submitted",
    );
    expect(api.repairList).toHaveBeenCalledWith(
      expect.objectContaining({ scope: "all", page: 1, pageSize: 5 }),
    );
    expect(
      screen.queryByLabelText("ค้นหาหัวข้อหรือสถานที่"),
    ).not.toBeInTheDocument();
    cleanup();
  });
  it.each([
    ["/repairs?scope=all&status=Resolved", "all", "Resolved"],
    ["/repairs?scope=unknown&status=unknown", "team", ""],
  ])("validates list parameters: %s", async (route, scope, status) => {
    const cleanup = mount(<RepairListPage />, route);
    await waitFor(() =>
      expect(api.repairList).toHaveBeenLastCalledWith(
        expect.objectContaining({ scope, status, page: 1 }),
      ),
    );
    cleanup();
  });
});
