import {
  cleanup,
  fireEvent,
  render,
  screen,
  waitFor,
} from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { RepairCreatePage } from "./RepairPages";
import { getCurrentUser } from "../api/authApi";
import type { AuthUser } from "../types/auth";

vi.mock("../context/AuthContext", () => ({
  useAuth: () => ({ user: { id: "requester", department: "Old department" } }),
}));
vi.mock("../api/authApi", () => ({ getCurrentUser: vi.fn() }));
vi.mock("../api/repairApi", () => ({
  repairOptions: async () => ({
    categories: [{ id: "it", name: "IT", teamCode: "IT" }],
  }),
}));
afterEach(cleanup);

function renderForm() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  const view = render(
    <QueryClientProvider client={client}>
      <MemoryRouter>
        <RepairCreatePage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
  return {
    client,
    dispose: () => {
      view.unmount();
      client.clear();
    },
  };
}
describe("repair requester department", () => {
  it("shows fresh department disabled and preserves form on refetch", async () => {
    vi.mocked(getCurrentUser).mockResolvedValue({
      id: "requester",
      department: "Current department",
    } as AuthUser);
    const { client, dispose } = renderForm();
    const input = await screen.findByDisplayValue("Current department");
    expect(input).toBeDisabled();
    fireEvent.change(screen.getByLabelText(/หัวข้อ/), {
      target: { value: "Keep my title" },
    });
    vi.mocked(getCurrentUser).mockResolvedValue({
      id: "requester",
      department: "New department",
    } as AuthUser);
    await client.invalidateQueries({ queryKey: ["repair-requester-profile"] });
    expect(await screen.findByDisplayValue("New department")).toBeDisabled();
    expect(screen.getByLabelText(/หัวข้อ/)).toHaveValue("Keep my title");
    dispose();
  });
  it("does not display stale account data on error and supports retry without a department", async () => {
    vi.mocked(getCurrentUser).mockRejectedValue(new Error("offline"));
    const { dispose } = renderForm();
    expect(
      await screen.findByText("โหลดหน่วยงานผู้แจ้งไม่สำเร็จ"),
    ).toBeInTheDocument();
    expect(
      screen.queryByDisplayValue("Old department"),
    ).not.toBeInTheDocument();
    vi.mocked(getCurrentUser).mockResolvedValue({
      id: "requester",
      department: null,
    } as AuthUser);
    fireEvent.click(screen.getByRole("button", { name: "ลองใหม่" }));
    await waitFor(() =>
      expect(
        screen.getByDisplayValue("ยังไม่ระบุหน่วยงานในบัญชี"),
      ).toBeDisabled(),
    );
    dispose();
  });
});
