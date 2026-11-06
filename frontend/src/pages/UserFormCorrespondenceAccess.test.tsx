// @vitest-environment jsdom
import { cleanup, fireEvent, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { renderFleet } from "../test/renderFleet";
import { UserFormPage } from "./UserFormPage";
import { updateUser } from "../api/adminApi";
const actor = vi.hoisted(() => ({ role: "Admin" }));
vi.mock("../context/AuthContext", () => ({ useAuth: () => ({ user: actor }) }));
vi.mock("../hooks/useSaveFeedback", () => ({ useSaveFeedback: () => ({ showSaveError: vi.fn(), showSuccessAndRedirect: vi.fn() }) }));
vi.mock("../api/adminApi", () => ({
  getDepartments: async () => [],
  getRoles: async () => [{ id: "staff", name: "Staff", isActive: true }, { id: "memo", name: "ผู้ใช้งานสารบรรณ", isActive: true, requiresSuperAdmin: true }],
  getUser: async () => ({ fullname: "User Test", username: "test", roleIds: ["staff", "memo"], isActive: true }),
  createUser: vi.fn(), updateUser: vi.fn().mockResolvedValue({}),
}));
vi.mock("../api/leaveApi", () => ({ getApprovalChains: async () => [] }));
describe("correspondence role assignment", () => {
  beforeEach(() => { actor.role = "Admin"; vi.clearAllMocks(); });
  afterEach(cleanup);
  it("locks the existing memo role for Admin while preserving it on profile save", async () => {
    renderFleet(<UserFormPage />, "/admin/users/u/edit", "/admin/users/:id/edit");
    await screen.findByDisplayValue("User Test");
    fireEvent.mouseDown(screen.getByRole("combobox", { name: "บทบาท" }));
    expect(await screen.findByRole("option", { name: "ผู้ใช้งานสารบรรณ" })).toHaveAttribute("aria-disabled", "true");
    fireEvent.keyDown(screen.getByRole("listbox"), { key: "Escape" });
    fireEvent.change(screen.getByLabelText("ชื่อ-สกุล"), { target: { value: "Updated User" } });
    fireEvent.submit(screen.getByRole("button", { name: /บันทึก/ }).closest("form")!);
    await waitFor(() => expect(updateUser).toHaveBeenCalledWith("u", expect.objectContaining({ fullname: "Updated User", roleIds: ["staff", "memo"] })));
  });
  it("lets SuperAdmin select the memo role", async () => {
    actor.role = "SuperAdmin";
    renderFleet(<UserFormPage />, "/admin/users/u/edit", "/admin/users/:id/edit");
    await screen.findByDisplayValue("User Test"); fireEvent.mouseDown(screen.getByRole("combobox", { name: "บทบาท" }));
    expect(await screen.findByRole("option", { name: "ผู้ใช้งานสารบรรณ" })).not.toHaveAttribute("aria-disabled", "true");
  });
});
