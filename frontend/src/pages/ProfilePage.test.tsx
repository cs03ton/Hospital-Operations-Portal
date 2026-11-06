// @vitest-environment jsdom
import { cleanup, fireEvent, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { renderFleet } from "../test/renderFleet";
import { ProfilePage } from "./ProfilePage";
import { getMyProfile, getMyLineBinding, updateMyProfile, type UserProfile } from "../api/profileApi";
vi.mock("../api/profileApi", async importOriginal => ({ ...await importOriginal<typeof import("../api/profileApi")>(), getMyProfile: vi.fn(), getMyLineBinding: vi.fn(), updateMyProfile: vi.fn() }));
vi.mock("../context/AuthContext", () => ({ useAuth: () => ({ refreshUser: vi.fn() }) }));
vi.mock("../hooks/useNotification", () => ({ useNotification: () => ({ showError: vi.fn(), showSuccess: vi.fn() }) }));
const profile = { id: "user", fullname: "ทดสอบ ชื่อเดิม", namePrefix: "นาย", username: "prefix", gender: "Male", hasProfileImage: false, roles: [], isActive: true, permissions: [] } satisfies UserProfile;
beforeEach(() => {
  vi.mocked(getMyProfile).mockResolvedValue({ ...profile });
  vi.mocked(getMyLineBinding).mockResolvedValue({ isBound: false, status: "NotBound", boundAt: null });
  vi.mocked(updateMyProfile).mockResolvedValue({ ...profile });
});
afterEach(() => { cleanup(); vi.clearAllMocks(); });
it("loads the database prefix separately and sends edits without changing fullname", async () => {
  renderFleet(<ProfilePage />);
  const prefix = await screen.findByRole("textbox", { name: "คำนำหน้า" });
  await waitFor(() => expect(prefix).toHaveValue("นาย"));
  expect(screen.getByRole("textbox", { name: "ชื่อ-นามสกุล" })).toHaveValue(profile.fullname);
  fireEvent.change(prefix, { target: { value: " นพ. " } });
  fireEvent.click(screen.getByRole("button", { name: "บันทึกข้อมูล" }));
  await waitFor(() => expect(updateMyProfile).toHaveBeenCalledWith(expect.objectContaining({ namePrefix: "นพ.", fullname: profile.fullname })));
});
it("supports a missing prefix and restores the database value on cancel", async () => {
  vi.mocked(getMyProfile).mockResolvedValue({ ...profile, namePrefix: null });
  renderFleet(<ProfilePage />);
  const prefix = await screen.findByRole("textbox", { name: "คำนำหน้า" });
  await screen.findByDisplayValue(profile.fullname);
  expect(prefix).toHaveValue("");
  fireEvent.change(prefix, { target: { value: "นางสาว" } });
  fireEvent.click(screen.getByRole("button", { name: "ยกเลิก" }));
  expect(prefix).toHaveValue("");
  fireEvent.click(screen.getByRole("button", { name: "บันทึกข้อมูล" }));
  await waitFor(() => expect(updateMyProfile).toHaveBeenCalledWith(expect.objectContaining({ namePrefix: "" })));
});
