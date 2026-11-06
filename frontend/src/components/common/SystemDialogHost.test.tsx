// @vitest-environment jsdom
import { act, cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";
import { confirmSystem, promptSystem, SystemDialogHost } from "./SystemDialogHost";
afterEach(cleanup);
describe("system dialogs", () => {
  it("waits for confirmation and prevents a second pending action", async () => {
    render(<SystemDialogHost />); let answer!: Promise<boolean>;
    act(() => { answer = confirmSystem("ยืนยันการบันทึก?\nข้อมูลเดิมจะถูกแทนที่"); });
    expect(screen.getByRole("dialog")).toHaveTextContent("ข้อมูลเดิมจะถูกแทนที่");
    expect(await confirmSystem("duplicate")).toBe(false);
    fireEvent.click(screen.getByRole("button", { name: "ยืนยัน" })); expect(await answer).toBe(true);
  });
  it("supports reason input and requires nonblank text", async () => {
    render(<SystemDialogHost />); let answer!: Promise<string | null>;
    act(() => { answer = promptSystem("กรุณาระบุเหตุผล"); });
    expect(screen.getByRole("button", { name: "ยืนยัน" })).toBeDisabled();
    fireEvent.change(screen.getByLabelText("เหตุผล"), { target: { value: "  เหตุผลทดสอบ  " } });
    fireEvent.click(screen.getByRole("button", { name: "ยืนยัน" })); expect(await answer).toBe("เหตุผลทดสอบ");
  });
  it("cancels on Escape and abort without approving an action", async () => {
    render(<SystemDialogHost />); let answer!: Promise<boolean>;
    act(() => { answer = confirmSystem("ยืนยัน?"); });
    fireEvent.keyDown(screen.getByRole("dialog"), { key: "Escape" }); expect(await answer).toBe(false);
    const controller = new AbortController();
    act(() => { answer = confirmSystem("ออกจากหน้า?", controller.signal); });
    act(() => controller.abort()); expect(await answer).toBe(false);
  });
});
