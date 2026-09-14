import { cleanup, render, screen } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { AppSidebar } from "./AppSidebar";
import type { NavigationModule } from "../../config/menuConfig";

const state = vi.hoisted(() => ({
  role: "Staff",
  permissions: [] as string[],
  fleet: true,
}));
vi.mock("../../context/AuthContext", () => ({
  useAuth: () => ({ user: { role: state.role } }),
}));
vi.mock("../../context/PermissionContext", () => ({
  usePermission: () => ({
    permissions: state.permissions,
    hasPermission: (p: string) => state.permissions.includes(p),
    hasAnyPermission: (ps: string[]) =>
      ps.some((p) => state.permissions.includes(p)),
  }),
}));
vi.mock("../../api/fleetApi", () => ({
  FLEET_DASHBOARD_QUERY_KEY: ["fleet-dashboard"],
  getFleetRolloutAccess: async () => ({ isAllowed: state.fleet }),
  getFleetDashboard: async () => ({}),
}));
vi.mock("../../api/repairApi", () => ({
  repairWorkPermissions: [
    "RepairManagement.WorkIT",
    "RepairManagement.WorkGeneral",
    "RepairManagement.ViewAll",
  ],
  repairSummary: async () => ({ teamPending: 1 }),
}));
vi.mock("./ModuleMenuGroup", () => ({
  isItemActive: () => false,
  ModuleMenuGroup: ({ module }: { module: NavigationModule }) => (
    <section data-testid="module" aria-label={module.moduleId}>
      {module.children.map((item) => (
        <a key={item.path} href={item.path}>
          {item.label}
        </a>
      ))}
    </section>
  ),
}));
afterEach(cleanup);

async function sidebar() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  const view = render(
    <QueryClientProvider client={client}>
      <MemoryRouter>
        <AppSidebar
          drawerWidth={280}
          expandedDrawerWidth={280}
          isCollapsed={false}
          mobileOpen
          onClose={() => {}}
          onExpandSidebar={() => {}}
          onToggleCollapse={() => {}}
        />
      </MemoryRouter>
    </QueryClientProvider>,
  );
  await screen.findByRole("link", { name: "Dashboard แจ้งซ่อม" });
  return () => {
    view.unmount();
    client.clear();
  };
}

describe("sidebar repair permission filtering", () => {
  it("renders repairs immediately after Fleet when both are allowed", async () => {
    state.role = "Staff";
    state.fleet = true;
    state.permissions = ["RepairManagement.ViewOwn", "FleetRequest.ViewOwn"];
    const dispose = await sidebar();
    await screen.findByRole("link", { name: "Dashboard รถ" });
    const modules = screen
      .getAllByTestId("module")
      .map((element) => element.getAttribute("aria-label"));
    expect(modules[modules.indexOf("VehicleBooking") + 1]).toBe(
      "RepairManagement",
    );
    dispose();
  });
  it.each([
    ["Staff", []],
    ["IT", ["WorkIT"]],
    ["General", ["WorkGeneral"]],
    ["Dual", ["WorkIT", "WorkGeneral"]],
    ["Admin", ["ViewAll", "Manage"]],
    ["SuperAdmin", ["ViewAll", "Manage"]],
  ])(
    "keeps repair routes scoped for %s without Fleet access",
    async (role, extra) => {
      state.role = role as string;
      state.fleet = false;
      state.permissions = ["ViewOwn", "Create", ...(extra as string[])].map(
        (p) => `RepairManagement.${p}`,
      );
      const dispose = await sidebar();
      expect(
        screen.getByRole("link", { name: "Dashboard แจ้งซ่อม" }),
      ).toHaveAttribute("href", "/dashboard/repair");
      expect(
        screen.queryByRole("region", { name: "VehicleBooking" }),
      ).not.toBeInTheDocument();
      const settings = document.querySelector('a[href="/repairs/settings"]');
      expect(!!settings).toBe((extra as string[]).includes("Manage"));
      dispose();
    },
  );
});
