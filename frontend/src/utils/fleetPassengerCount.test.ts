import { describe, expect, it } from "vitest";
import { countFleetPassengers } from "./fleetPassengerCount";

describe("fleet request passenger count", () => {
  it("counts a requester travelling alone as one passenger", () => {
    expect(countFleetPassengers(true, 0, 0)).toBe(1);
  });

  it("adds selected personnel and preserved external passengers", () => {
    expect(countFleetPassengers(true, 2, 1)).toBe(4);
    expect(countFleetPassengers(false, 1, 0)).toBe(1);
  });

  it("returns zero when the requester does not travel and nobody else is selected", () => {
    expect(countFleetPassengers(false, 0, 0)).toBe(0);
  });
});
