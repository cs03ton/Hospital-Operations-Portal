import { useState } from "react";
import { fireEvent, render, screen, cleanup } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { RepairFilePicker } from "./RepairUi";
import { formatThaiBuddhistDateTime } from "../../utils/dateFormat";

function Picker() {
  const [files, setFiles] = useState<File[]>([]);
  return <RepairFilePicker files={files} setFiles={setFiles} />;
}
afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});
describe("repair photos and dates", () => {
  it("previews and removes selected photos, releasing object URLs", () => {
    const revoke = vi.fn();
    vi.stubGlobal("URL", {
      createObjectURL: vi.fn(() => "blob:preview"),
      revokeObjectURL: revoke,
    });
    render(<Picker />);
    fireEvent.change(screen.getByLabelText("เลือกรูปภาพแนบ"), {
      target: {
        files: [new File(["png"], "repair.png", { type: "image/png" })],
      },
    });
    expect(screen.getByAltText("ภาพตัวอย่าง repair.png")).toBeInTheDocument();
    fireEvent.click(
      screen.getByRole("button", { name: "นำรูป repair.png ออก" }),
    );
    expect(
      screen.queryByAltText("ภาพตัวอย่าง repair.png"),
    ).not.toBeInTheDocument();
    expect(revoke).toHaveBeenCalledWith("blob:preview");
  });
  it("rejects unsupported files and more than five photos", () => {
    render(<Picker />);
    const input = screen.getByLabelText("เลือกรูปภาพแนบ");
    fireEvent.change(input, {
      target: {
        files: [new File(["pdf"], "file.pdf", { type: "application/pdf" })],
      },
    });
    expect(screen.getByRole("alert")).toHaveTextContent("ไม่เกิน 5 รูป");
    fireEvent.change(input, {
      target: {
        files: Array.from(
          { length: 6 },
          (_, i) => new File(["png"], `${i}.png`, { type: "image/png" }),
        ),
      },
    });
    expect(screen.queryAllByRole("img")).toHaveLength(0);
  });
  it("renders Buddhist years without changing existing date utilities", () => {
    expect(formatThaiBuddhistDateTime("2026-09-11T08:00:00Z")).toContain(
      "2569",
    );
    expect(formatThaiBuddhistDateTime("bad date")).toBe("-");
  });
});
