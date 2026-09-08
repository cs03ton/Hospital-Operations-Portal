// @vitest-environment jsdom
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { getFleetDashboard } from "../api/fleetApi";
import { FleetPermissionGuard } from "./FleetPermissionGuard";

const permissionState = vi.hoisted(() => ({ direct: [] as string[] }));
vi.mock("../context/PermissionContext", () => ({
  usePermission: () => ({ hasAnyPermission: (items: string[]) => items.some(item => permissionState.direct.includes(item)) }),
}));
vi.mock("../api/fleetApi", () => ({ FLEET_DASHBOARD_QUERY_KEY: ["fleet-dashboard"], getFleetDashboard: vi.fn() }));

function renderGuard(denyMode: "redirect" | "hide" = "redirect") {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<QueryClientProvider client={client}><MemoryRouter initialEntries={["/fleet/approvals"]}><Routes><Route path="/fleet/approvals" element={<><div>landing content</div><FleetPermissionGuard denyMode={denyMode} permissions={["FleetDirector.Approve"]}><div>approval content</div></FleetPermissionGuard></>} /><Route path="/unauthorized" element={<div>permission denied</div>} /></Routes></MemoryRouter></QueryClientProvider>);
}

describe("FleetPermissionGuard", () => {
  beforeEach(() => { permissionState.direct = []; vi.resetAllMocks(); });

  it("allows a direct permission without requesting delegated capabilities", () => {
    permissionState.direct = ["FleetDirector.Approve"];
    renderGuard();
    expect(screen.getByText("approval content")).toBeInTheDocument();
    expect(getFleetDashboard).not.toHaveBeenCalled();
  });

  it("allows an effective delegated permission returned by the server", async () => {
    vi.mocked(getFleetDashboard).mockResolvedValue({ capabilities: { delegatedPermissions: ["FleetDirector.Approve"] } } as never);
    renderGuard();
    expect(await screen.findByText("approval content")).toBeInTheDocument();
  });

  it("redirects after the server no longer returns an expired delegation", async () => {
    vi.mocked(getFleetDashboard).mockResolvedValue({ capabilities: { delegatedPermissions: [] } } as never);
    renderGuard();
    expect(await screen.findByText("permission denied")).toBeInTheDocument();
  });

  it("hides a denied section without redirecting its composite landing page", async () => {
    vi.mocked(getFleetDashboard).mockResolvedValue({ capabilities: { delegatedPermissions: [] } } as never);
    renderGuard("hide");
    expect(await screen.findByText("landing content")).toBeInTheDocument();
    expect(screen.queryByText("approval content")).not.toBeInTheDocument();
    expect(screen.queryByText("permission denied")).not.toBeInTheDocument();
  });
});
