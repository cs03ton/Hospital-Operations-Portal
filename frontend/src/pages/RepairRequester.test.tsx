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
import { getMyProfile, type UserProfile } from "../api/profileApi";

vi.mock("../context/AuthContext", () => ({
  useAuth: () => ({ user: { id: "requester", department: "Old department" } }),
}));
vi.mock("../api/profileApi", () => ({ getMyProfile: vi.fn() }));
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
describe("repair requester profile", () => {
  it("shows fresh department, name and phone disabled and preserves form on refetch", async () => {
    vi.mocked(getMyProfile).mockResolvedValue({
      id: "requester",
      fullname: "First Name",
      phoneNumber: "0812345678",
      departmentName: "Current department",
    } as UserProfile);
    const { client, dispose } = renderForm();
    const input = await screen.findByDisplayValue("Current department");
    expect(input).toBeDisabled();
    expect(screen.getByDisplayValue("First Name")).toBeDisabled();
    expect(screen.getByDisplayValue("0812345678")).toBeDisabled();
    fireEvent.change(screen.getByLabelText(/หัวข้อ/), {
      target: { value: "Keep my title" },
    });
    vi.mocked(getMyProfile).mockResolvedValue({
      id: "requester",
      fullname: "Updated Name",
      phoneNumber: "0899999999",
      departmentName: "New department",
    } as UserProfile);
    await client.invalidateQueries({ queryKey: ["repair-requester-profile"] });
    expect(await screen.findByDisplayValue("New department")).toBeDisabled();
    expect(screen.getByDisplayValue("Updated Name")).toBeDisabled();
    expect(screen.getByDisplayValue("0899999999")).toBeDisabled();
    expect(screen.getByLabelText(/หัวข้อ/)).toHaveValue("Keep my title");
    dispose();
  });
  it("does not display stale account data on error and supports retry without a department", async () => {
    vi.mocked(getMyProfile).mockRejectedValue(new Error("offline"));
    const { dispose } = renderForm();
    expect(
      await screen.findByText("โหลดข้อมูลส่วนตัวผู้แจ้งไม่สำเร็จ"),
    ).toBeInTheDocument();
    expect(
      screen.queryByDisplayValue("Old department"),
    ).not.toBeInTheDocument();
    vi.mocked(getMyProfile).mockResolvedValue({
      id: "requester",
      fullname: "Requesting User",
      phoneNumber: null,
      departmentName: null,
    } as UserProfile);
    fireEvent.click(screen.getByRole("button", { name: "ลองใหม่" }));
    await waitFor(() =>
      expect(
        screen.getByDisplayValue("ยังไม่ระบุหน่วยงานในบัญชี"),
      ).toBeDisabled(),
    );
    expect(screen.getByDisplayValue("Requesting User")).toBeDisabled();
    expect(screen.getByRole("button", { name: "ส่งแจ้งซ่อม" })).toBeDisabled();
    expect(screen.getByText(/ยังไม่มีเบอร์โทรในข้อมูลส่วนตัว/)).toBeInTheDocument();
    dispose();
  });
});
