import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { ListPagination } from "./ListPagination";

describe("ListPagination", () => {
  it("uses one-based pages and exposes Thai result text", () => {
    const onPageChange = vi.fn();
    render(<ListPagination page={2} pageSize={10} totalItems={35} onPageChange={onPageChange} onPageSizeChange={vi.fn()} />);

    expect(screen.getByText("แสดง 11-20 จาก 35 รายการ")).toBeInTheDocument();
    fireEvent.click(screen.getByLabelText("ไปหน้าถัดไป"));
    expect(onPageChange).toHaveBeenCalledWith(3);
  });

  it("reports a changed page size", () => {
    const onPageSizeChange = vi.fn();
    render(<ListPagination page={1} pageSize={10} totalItems={35} onPageChange={vi.fn()} onPageSizeChange={onPageSizeChange} />);

    fireEvent.mouseDown(screen.getByRole("combobox"));
    fireEvent.click(screen.getByRole("option", { name: "20" }));
    expect(onPageSizeChange).toHaveBeenCalledWith(20);
  });
});
