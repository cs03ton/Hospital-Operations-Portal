// @vitest-environment jsdom
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, useLocation } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { getFleetRolloutAccess } from "../../api/fleetApi";
import { quickServiceRoutes } from "../../config/quickServices";
import { QuickServiceFab } from "./QuickServiceFab";

const auth = vi.hoisted(() => ({ role: "Staff", permissions: [] as string[] }));
vi.mock("../../context/AuthContext", () => ({ useAuth: () => ({ user: auth }) }));
vi.mock("../../api/fleetApi", () => ({ getFleetRolloutAccess: vi.fn() }));

function LocationDisplay() {
  const location = useLocation();
  return <span data-testid="location">{location.pathname}</span>;
}

function setup() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<QueryClientProvider client={queryClient}><MemoryRouter initialEntries={["/dashboard"]}><QuickServiceFab /><LocationDisplay /><button type="button">นอกเมนู</button></MemoryRouter></QueryClientProvider>);
}

describe("QuickServiceFab", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    auth.role = "Staff";
    auth.permissions = ["LeaveRequest.Create", "FleetRequest.Create", "RepairManagement.Create", "MeetingRoom.Booking.Create"];
    vi.mocked(getFleetRolloutAccess).mockResolvedValue({ isAllowed: true, mode: "Enabled" } as Awaited<ReturnType<typeof getFleetRolloutAccess>>);
  });

  it("starts collapsed and shows permitted services in the requested order", async () => {
    setup();
    expect(screen.getByRole("button", { name: "บริการด่วน" })).toHaveAttribute("aria-expanded", "false");
    expect(screen.queryByRole("menu")).not.toBeInTheDocument();
    await waitFor(() => expect(getFleetRolloutAccess).toHaveBeenCalled());
    await userEvent.click(screen.getByRole("button", { name: "บริการด่วน" }));
    const labels = within(screen.getByRole("menu")).getAllByRole("menuitem").map((item) => item.getAttribute("aria-label"));
    expect(labels).toEqual(["จองห้องประชุม", "แจ้งซ่อม", "เพิ่มคำขอรถ", "เพิ่มคำขอลา"]);
    await waitFor(() => expect(screen.getByRole("menuitem", { name: "จองห้องประชุม" })).toHaveFocus());
    fireEvent.keyDown(screen.getByRole("menu"), { key: "ArrowDown" });
    expect(screen.getByRole("menuitem", { name: "แจ้งซ่อม" })).toHaveFocus();
  });

  it("closes on Escape, outside click, the close button, and selection", async () => {
    setup();
    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: "บริการด่วน" }));
    fireEvent.keyDown(document, { key: "Escape" });
    await waitFor(() => expect(screen.queryByRole("menu")).not.toBeInTheDocument());
    expect(screen.getByRole("button", { name: "บริการด่วน" })).toHaveFocus();

    await user.click(screen.getByRole("button", { name: "บริการด่วน" }));
    await user.click(screen.getByRole("button", { name: "นอกเมนู" }));
    await waitFor(() => expect(screen.queryByRole("menu")).not.toBeInTheDocument());

    await user.click(screen.getByRole("button", { name: "บริการด่วน" }));
    await user.click(screen.getByRole("button", { name: "ปิดบริการด่วน" }));
    await waitFor(() => expect(screen.queryByRole("menu")).not.toBeInTheDocument());

    await user.click(screen.getByRole("button", { name: "บริการด่วน" }));
    await user.click(screen.getByRole("menuitem", { name: "แจ้งซ่อม" }));
    expect(screen.getByTestId("location")).toHaveTextContent(quickServiceRoutes.repair);
    await waitFor(() => expect(screen.queryByRole("menu")).not.toBeInTheDocument());
  });

  it("hides actions blocked by role, permission, or Fleet rollout", async () => {
    auth.role = "Admin";
    auth.permissions = ["LeaveRequest.Create", "FleetRequest.Create", "RepairManagement.Create"];
    vi.mocked(getFleetRolloutAccess).mockResolvedValue({ isAllowed: false, mode: "Disabled" } as Awaited<ReturnType<typeof getFleetRolloutAccess>>);
    setup();
    await userEvent.click(screen.getByRole("button", { name: "บริการด่วน" }));
    expect(within(screen.getByRole("menu")).getAllByRole("menuitem")).toHaveLength(1);
    expect(screen.getByRole("menuitem", { name: "แจ้งซ่อม" })).toBeInTheDocument();
  });

  it("does not render when no service is available", () => {
    auth.permissions = [];
    setup();
    expect(screen.queryByRole("button", { name: "บริการด่วน" })).not.toBeInTheDocument();
  });
});
