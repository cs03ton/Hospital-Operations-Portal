// @vitest-environment jsdom
import DirectionsCarOutlinedIcon from "@mui/icons-material/DirectionsCarOutlined";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import { ModuleMenuItem } from "./ModuleMenuItem";

describe("ModuleMenuItem task badge", () => {
  it("hides zero and caps large counts at 99+", () => {
    const { rerender } = render(<MemoryRouter><ModuleMenuItem item={{ label: "คิวจัดรถ", path: "/fleet/dispatch", icon: DirectionsCarOutlinedIcon, badgeCount: 0 }} isActive={false} isCollapsed={false} onClick={vi.fn()} /></MemoryRouter>);
    expect(screen.queryByText("0")).not.toBeInTheDocument();
    rerender(<MemoryRouter><ModuleMenuItem item={{ label: "คิวจัดรถ", path: "/fleet/dispatch", icon: DirectionsCarOutlinedIcon, badgeCount: 120 }} isActive={false} isCollapsed={false} onClick={vi.fn()} /></MemoryRouter>);
    expect(screen.getByText("99+")).toBeInTheDocument();
  });
});
