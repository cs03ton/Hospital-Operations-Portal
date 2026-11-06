// @vitest-environment jsdom
import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { renderFleet } from "../../test/renderFleet";
import { AppSidebar } from "./AppSidebar";
const state = vi.hoisted(() => ({ permissions: [] as string[], role: "Staff", fleet: true, desktop: true }));
vi.mock("@mui/material", async original => ({ ...await original<typeof import("@mui/material")>(), useMediaQuery: () => state.desktop }));
vi.mock("../../context/AuthContext", () => ({ useAuth: () => ({ user: { role: state.role } }) }));
vi.mock("../../context/PermissionContext", () => ({ usePermission: () => ({ permissions: state.permissions, hasPermission: (p: string) => state.permissions.includes(p), hasAnyPermission: (ps: string[]) => ps.some(p => state.permissions.includes(p)) }) }));
vi.mock("../../api/fleetApi", () => ({ FLEET_DASHBOARD_QUERY_KEY: ["fleet-dashboard"], getFleetRolloutAccess: async () => ({ isAllowed: state.fleet }), getFleetDashboard: async () => ({ badges: { dispatchQueue: 2 } }) }));
vi.mock("../../api/leaveApi", () => ({ getMyPendingApprovalCount: async () => ({ total: 3 }) }));
vi.mock("../../api/repairApi", () => ({ repairWorkPermissions: ["RepairManagement.ViewAll"], repairSummary: async () => ({ teamPending: 4 }) }));
const close = vi.fn(); const expand = vi.fn();
function renderSidebar(path = "/dashboard", collapsed = false) {
  return renderFleet(<AppSidebar drawerWidth={collapsed ? 72 : 292} expandedDrawerWidth={292} isCollapsed={collapsed} mobileOpen onClose={close} onExpandSidebar={expand} onToggleCollapse={vi.fn()} />, path);
}
describe("portal sidebar structure", () => {
  beforeEach(() => { localStorage.clear(); vi.clearAllMocks(); state.role = "Staff"; state.desktop = true; state.fleet = true; state.permissions = ["Dashboard.View", "Correspondence.Use"]; });
  it("shows authenticated correspondence menus and keeps settings hidden for regular staff", async () => {
    renderSidebar("/correspondence/create");
    expect(screen.getByText("Hospital Operations Portal")).toBeInTheDocument();
    expect(screen.getByText("ระบบบริหารงานโรงพยาบาลนาหมื่น")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "หน้าหลัก" })).toHaveAttribute("href", "/dashboard");
    expect(await screen.findByRole("link", { name: "สร้างบันทึกข้อความ" })).toHaveAttribute("aria-current", "page");
    expect(screen.getByRole("link", { name: "สร้างบันทึกข้อความ" })).toHaveAttribute("href", "/correspondence/create");
    expect(screen.getByRole("link", { name: "สร้างเอกสาร ฉ.11" })).toHaveAttribute("href", "/correspondence/cho11");
    expect(screen.queryByRole("link", { name: "รายงานผลการปฏิบัติงานนอกเวลา" })).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "บริการเจ้าหน้าที่" })).toHaveAttribute("aria-expanded", "true");
    expect(screen.getByRole("button", { name: "งานสารบรรณ" })).toHaveAttribute("aria-expanded", "true");
    expect(screen.getByText("ใหม่")).toBeInTheDocument(); expect(screen.queryByRole("button", { name: "ตั้งค่าระบบ" })).not.toBeInTheDocument();
  });
  it("hides correspondence when the user has no permission", () => {
    state.permissions = ["Dashboard.View"];
    renderSidebar();
    expect(screen.queryByRole("button", { name: "งานสารบรรณ" })).not.toBeInTheDocument();
  });
  it("orders staff modules and opens the active system while preserving approval badges", async () => {
    state.permissions.push("LeaveRequest.ViewOwn", "LeaveRequest.ViewPendingApproval", "FleetDispatch.View", "FleetDispatch.Assign", "MeetingRoom.Calendar.View", "RepairManagement.ViewOwn", "Announcement.View");
    renderSidebar("/leave/pending-approvals");
    await screen.findByRole("button", { name: "จองรถราชการ" });
    const names = screen.getAllByRole("button").map(el => el.getAttribute("aria-label"));
    expect(names.indexOf("ระบบลา")).toBeLessThan(names.indexOf("จองรถราชการ"));
    expect(names.indexOf("จองรถราชการ")).toBeLessThan(names.indexOf("จองห้องประชุม"));
    expect(names.indexOf("จองห้องประชุม")).toBeLessThan(names.indexOf("แจ้งซ่อม"));
    expect(names.indexOf("ข่าวสาร")).toBeLessThan(names.indexOf("ระบบลา"));
    expect(names.indexOf("แจ้งซ่อม")).toBeLessThan(names.indexOf("งานสารบรรณ"));
    expect(screen.getByRole("button", { name: "บริการเจ้าหน้าที่" })).toHaveAttribute("aria-expanded", "true");
    const link = await screen.findByRole("link", { name: /งานรออนุมัติของฉัน/ }); expect(within(link).getByText("3")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "บริการเจ้าหน้าที่" }));
    await waitFor(() => expect(screen.queryByRole("button", { name: "ระบบลา" })).not.toBeInTheDocument());
  });
  it("keeps Fleet rollout restrictions and admin routes inside settings", async () => {
    state.role = "SuperAdmin"; state.fleet = false; state.permissions.push("FleetDispatch.View", "SystemSettings.View");
    renderSidebar("/admin/dashboard");
    expect(screen.getByRole("button", { name: "ตั้งค่าระบบ" })).toHaveAttribute("aria-expanded", "true");
    expect(await screen.findByRole("link", { name: "Admin Dashboard" })).toHaveAttribute("href", "/admin/dashboard");
    expect(screen.queryByRole("button", { name: "จองรถราชการ" })).not.toBeInTheDocument();
  });
  it("expands from collapsed mode and closes the mobile drawer on navigation", async () => {
    renderSidebar("/correspondence/create", true);
    expect(screen.queryByRole("link", { name: "สร้างบันทึกข้อความ" })).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "บริการเจ้าหน้าที่" })); expect(expand).toHaveBeenCalled();
  });
  it("uses mobile navigation links with the same routes", async () => {
    state.desktop = false; renderSidebar("/correspondence/create");
    fireEvent.click(await screen.findByRole("link", { name: "สร้างบันทึกข้อความ" })); expect(close).toHaveBeenCalled();
    expect(screen.queryByRole("button", { name: "พับเมนู" })).not.toBeInTheDocument();
  });
});
