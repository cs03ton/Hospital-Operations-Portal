import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { ChangePasswordPage } from "./ChangePasswordPage";
import { getPasswordPolicy } from "../api/authApi";

vi.mock("../api/authApi", () => ({ getPasswordPolicy: vi.fn(), changePassword: vi.fn() }));
vi.mock("../context/AuthContext", () => ({
  useAuth: () => ({ user: { username: "staff01" }, clearSession: vi.fn() }),
}));
vi.mock("../hooks/useNotification", () => ({
  useNotification: () => ({ showError: vi.fn(), showSuccess: vi.fn() }),
}));

afterEach(cleanup);

describe("ChangePasswordPage password policy", () => {
  it("uses the API minimum of eight and rejects seven while keeping the other checks", async () => {
    vi.mocked(getPasswordPolicy).mockResolvedValue({
      minimumLength: 8, requireUppercase: true, requireLowercase: true,
      requireDigit: true, requireSpecialCharacter: true, disallowUsername: true,
    });
    const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    render(<QueryClientProvider client={client}><MemoryRouter><ChangePasswordPage /></MemoryRouter></QueryClientProvider>);

    await waitFor(() => expect(screen.getByText(/ความยาวอย่างน้อย 8 ตัวอักษร/)).toBeInTheDocument());
    expect(screen.getByText(/อย่างน้อย 8 ตัวอักษร และต้องเป็นไปตาม Password Policy/)).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText("รหัสผ่านปัจจุบัน"), { target: { value: "OldPass@123" } });
    fireEvent.change(screen.getByLabelText("รหัสผ่านใหม่", { exact: true }), { target: { value: "Ab1@cde" } });
    fireEvent.change(screen.getByLabelText("ยืนยันรหัสผ่านใหม่"), { target: { value: "Ab1@cde" } });
    expect(screen.getByRole("button", { name: "เปลี่ยนรหัสผ่าน" })).toBeDisabled();

    fireEvent.change(screen.getByLabelText("รหัสผ่านใหม่", { exact: true }), { target: { value: "Ab1@cdef" } });
    fireEvent.change(screen.getByLabelText("ยืนยันรหัสผ่านใหม่"), { target: { value: "Ab1@cdef" } });
    expect(screen.getByRole("button", { name: "เปลี่ยนรหัสผ่าน" })).toBeEnabled();
    client.clear();
  });
});
