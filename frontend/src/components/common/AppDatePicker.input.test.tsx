import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { AppDatePicker } from "./AppDatePicker";

describe("Buddhist date input", () => {
  it.each(["24/09/2569", "24/09/2026", "29/02/2567"])("accepts typed %s", input => {
    const change = vi.fn();
    const { unmount } = render(<AppDatePicker label="วันที่ลา" value="" onChange={change} />);
    fireEvent.change(screen.getByRole("textbox"), { target: { value: input } });
    expect(change).toHaveBeenLastCalledWith(input.includes("2567") ? "2024-02-29" : "2026-09-24");
    unmount();
  });
  it("shows a persisted Gregorian date as Buddhist without modifying it", () => {
    const change = vi.fn();
    const { unmount } = render(<AppDatePicker label="วันที่ลา" value="2026-09-24" onChange={change} />);
    expect((screen.getByRole("textbox") as HTMLInputElement).value).toContain("2569");
    expect(change).not.toHaveBeenCalled();
    unmount();
  });
});
