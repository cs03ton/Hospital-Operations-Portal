import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { ManagementDataGrid, type ManagementDataGridColumn } from "./ManagementDataGrid";

type Row = { id: string; name: string };

describe("ManagementDataGrid responsive content", () => {
  it("renders complete row details and actions in the mobile representation", () => {
    const columns: ManagementDataGridColumn<Row>[] = [
      { key: "name", label: "ชื่อ", render: (row) => row.name },
      { key: "action", label: "จัดการ", render: () => <button type="button">แก้ไข</button> },
    ];

    render(
      <ManagementDataGrid
        title="รายการทดสอบ"
        columns={columns}
        rows={[{ id: "1", name: "ผู้ใช้งานทดสอบ" }]}
        getRowId={(row) => row.id}
        page={1}
        pageSize={10}
        totalItems={1}
        sort="name"
        direction="asc"
        onSortChange={vi.fn()}
        onPageChange={vi.fn()}
        onPageSizeChange={vi.fn()}
      />,
    );

    const mobile = screen.getByLabelText("รายการทดสอบ สำหรับมือถือ");
    expect(mobile).toHaveTextContent("ชื่อ");
    expect(mobile).toHaveTextContent("ผู้ใช้งานทดสอบ");
    expect(mobile).toHaveTextContent("แก้ไข");
  });
});
