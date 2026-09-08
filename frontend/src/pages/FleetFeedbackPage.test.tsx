import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as api from "../api/fleetApi";
import { renderFleet } from "../test/renderFleet";
import { FleetFeedbackPage } from "./FleetFeedbackPage";

vi.mock("../api/fleetApi", () => ({ getFleetFeedbackContext: vi.fn(), submitFleetFeedback: vi.fn() }));
vi.mock("../hooks/useNotification", () => ({ useNotification: () => ({ showSuccess: vi.fn(), showError: vi.fn() }) }));

const context: api.FleetFeedbackContext = { tripId: "trip-1", requestNo: "VH-202608-0021", tripDate: "2026-08-15T01:00:00Z", destination: "สสจ.น่าน", vehicleDisplay: "VAN-01 · นข 1234", driverDisplay: "นายคนขับ", completedAt: "2026-08-15T08:00:00Z", feedbackDeadline: "2026-08-22T08:00:00Z", canSubmitFeedback: true, feedbackStatus: "AVAILABLE" };

describe("FleetFeedbackPage", () => {
  beforeEach(() => { vi.resetAllMocks(); vi.mocked(api.getFleetFeedbackContext).mockResolvedValue(context); });

  it("renders participant-safe context and validates all ratings", async () => {
    renderFleet(<FleetFeedbackPage />, "/fleet/trips/trip-1/feedback", "/fleet/trips/:tripId/feedback");
    expect(await screen.findByText("VH-202608-0021")).toBeInTheDocument();
    expect(screen.getByText(/รถ VAN-01/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "ส่ง Feedback" }));
    expect(screen.getByText("กรุณาให้คะแนนครบทุกหัวข้อ")).toBeInTheDocument();
    expect(api.submitFleetFeedback).not.toHaveBeenCalled();
  });

  it("prevents double submit while request is pending", async () => {
    vi.mocked(api.submitFleetFeedback).mockImplementation(() => new Promise(() => undefined));
    renderFleet(<FleetFeedbackPage />, "/fleet/trips/trip-1/feedback", "/fleet/trips/:tripId/feedback");
    await screen.findByText("VH-202608-0021");
    for (const label of ["ความตรงต่อเวลา", "ความปลอดภัยในการขับขี่", "มารยาทและการให้บริการ", "ความพึงพอใจโดยรวม", "สภาพรถ", "ความสะอาด"]) {
      const group = screen.getByLabelText(label);
      const inputs = group.querySelectorAll("input");
      fireEvent.click(inputs[4]);
    }
    fireEvent.click(screen.getByRole("button", { name: "ส่ง Feedback" }));
    await waitFor(() => expect(screen.getByRole("button", { name: "กำลังส่ง..." })).toBeDisabled());
    expect(api.submitFleetFeedback).toHaveBeenCalledTimes(1);
  });

  it("shows expired state without submit action", async () => {
    vi.mocked(api.getFleetFeedbackContext).mockResolvedValue({ ...context, canSubmitFeedback: false, feedbackStatus: "EXPIRED" });
    renderFleet(<FleetFeedbackPage />, "/fleet/trips/trip-1/feedback", "/fleet/trips/:tripId/feedback");
    expect(await screen.findByText("หมดระยะเวลาให้ Feedback การเดินทางแล้ว")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "ส่ง Feedback" })).not.toBeInTheDocument();
  });
});
