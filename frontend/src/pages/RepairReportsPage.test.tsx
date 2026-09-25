import { cleanup, render, screen } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { RepairReportsPage } from "./RepairReportsPage";
import { repairReportItems, repairReportSummary } from "../api/repairApi";
import { navigationModules } from "../config/menuConfig";

vi.mock("../api/repairApi", () => ({ repairReportItems: vi.fn(), repairReportSummary: vi.fn(), repairReportExport: vi.fn() }));
afterEach(cleanup);

describe("RepairReportsPage", () => {
  it("shows the real status breakdown and only ViewAll can see the menu", async () => {
    vi.mocked(repairReportSummary).mockResolvedValue({
      from: "2026-09-01", to: "2026-09-30", total: 3, acceptanceRate: 50,
      counts: [{ status: "Closed", count: 1 }, { status: "Resolved", count: 1 }, { status: "Cancelled", count: 1 }],
      months: [{ month: "2026-09", total: 3, closed: 1, inProgress: 1, acceptanceRate: 50 }],
      byCategory: [{ id: "a", name: "ไฟฟ้า", count: 3 }], byDepartment: [{ id: "b", name: "IT", count: 3 }],
      categories: [{ id: "a", name: "ไฟฟ้า" }], departments: [{ id: "b", name: "IT" }],
    });
    vi.mocked(repairReportItems).mockResolvedValue({ items: [{ id: "request-1", number: 1, title: "ไฟฟ้าขัดข้อง", categoryName: "ไฟฟ้า", departmentName: "IT", requesterName: "ผู้แจ้ง", createdAt: "2026-09-01T01:00:00Z", closedAt: "2026-09-03T01:00:00Z", status: "Closed", priority: "Normal" }], total: 1, page: 1, pageSize: 10 });
    const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    render(<QueryClientProvider client={client}><MemoryRouter><RepairReportsPage /></MemoryRouter></QueryClientProvider>);
    expect(await screen.findByText("ไฟฟ้าขัดข้อง")).toBeInTheDocument();
    expect(screen.getByText("แก้ไขแล้วรอตรวจรับ")).toBeInTheDocument();
    expect(screen.getAllByText("ตรวจรับแล้ว").length).toBeGreaterThan(0);
    expect(screen.getByRole("link", { name: "ดู" })).toHaveAttribute("href", "/repairs/request-1");
    const item = navigationModules.find(x => x.moduleId === "RepairManagement")?.children.find(x => x.path === "/repairs/reports");
    expect(item?.permission).toBe("RepairManagement.ViewAll");
    client.clear();
  });
});
