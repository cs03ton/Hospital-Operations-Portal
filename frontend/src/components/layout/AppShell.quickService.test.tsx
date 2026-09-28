// @vitest-environment jsdom
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes, useNavigate } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import { AppShell } from "./AppShell";

vi.mock("./AppHeader", () => ({ AppHeader: () => null }));
vi.mock("./AppSidebar", () => ({ AppSidebar: () => null }));
vi.mock("./AppFooter", () => ({ AppFooter: () => null }));
vi.mock("./QuickServiceFab", () => ({ QuickServiceFab: () => <button type="button">บริการด่วน</button> }));
vi.mock("../../hooks/useSidebarState", () => ({ useSidebarState: () => ({
  isCollapsed: false, isMobileOpen: false, openMobileSidebar: vi.fn(), closeMobileSidebar: vi.fn(),
  expandSidebar: vi.fn(), toggleSidebar: vi.fn(), toggleCollapse: vi.fn(),
}) }));

function Dashboard() {
  const navigate = useNavigate();
  return <button type="button" onClick={() => navigate("/profile")}>ไปข้อมูลส่วนตัว</button>;
}

describe("AppShell quick service placement", () => {
  it("keeps one floating menu available across main-layout pages", async () => {
    render(<MemoryRouter initialEntries={["/dashboard"]}><Routes>
      <Route element={<AppShell />}>
        <Route path="/dashboard" element={<Dashboard />} />
        <Route path="/profile" element={<div>ข้อมูลส่วนตัว</div>} />
      </Route>
    </Routes></MemoryRouter>);

    expect(screen.getAllByRole("button", { name: "บริการด่วน" })).toHaveLength(1);
    await userEvent.click(screen.getByRole("button", { name: "ไปข้อมูลส่วนตัว" }));
    expect(screen.getByText("ข้อมูลส่วนตัว")).toBeInTheDocument();
    expect(screen.getAllByRole("button", { name: "บริการด่วน" })).toHaveLength(1);
  });
});
