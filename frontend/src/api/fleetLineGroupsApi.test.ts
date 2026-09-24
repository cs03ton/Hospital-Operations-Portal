import { beforeEach, describe, expect, it, vi } from "vitest";
import { httpClient } from "./httpClient";
import {
  confirmFleetLineGroup,
  createFleetLineGroupEndpoint,
  disableFleetLineGroup,
  getFleetLineGroupDeliveries,
  getFleetLineGroups,
  migrateRepairLineGroup,
  testFleetLineGroup,
  updateFleetLineGroupEndpoint,
  updateFleetLineGroupSubscriptions,
} from "./fleetApi";

vi.mock("./httpClient", () => ({
  httpClient: { get: vi.fn(), post: vi.fn(), put: vi.fn() },
}));

const configuration = {
  displayName: "กลุ่มทดสอบ",
  groupId: "group-id",
  endpointUrl: "https://example.org/endpoint",
  clientId: "client-id",
  clientSecret: "secret",
};

describe("central LINE Groups API paths", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    const stopped = new Error("request intercepted");
    vi.mocked(httpClient.get).mockRejectedValue(stopped);
    vi.mocked(httpClient.post).mockRejectedValue(stopped);
    vi.mocked(httpClient.put).mockRejectedValue(stopped);
  });

  it.each([
    ["get", "/api/admin/line-groups", () => getFleetLineGroups({ status: "Active" })],
    ["post", "/api/admin/line-groups", () => createFleetLineGroupEndpoint(configuration)],
    ["put", "/api/admin/line-groups/group-1/configuration", () => updateFleetLineGroupEndpoint("group-1", configuration)],
    ["post", "/api/admin/line-groups/group-1/confirm", () => confirmFleetLineGroup("group-1", "token")],
    ["post", "/api/admin/line-groups/group-1/disable", () => disableFleetLineGroup("group-1", "token", "test")],
    ["post", "/api/admin/line-groups/group-1/migrate-repair", () => migrateRepairLineGroup({ id: "group-1", displayName: "แจ้งซ่อม IT", groupIdMasked: "C1234...7890", status: "Active", concurrencyToken: "token" } as Parameters<typeof migrateRepairLineGroup>[0], "IT")],
    ["put", "/api/admin/line-groups/group-1/subscriptions", () => updateFleetLineGroupSubscriptions("group-1", "token", { "Fleet.RequestSubmitted": true })],
    ["post", "/api/admin/line-groups/group-1/test", () => testFleetLineGroup("group-1")],
    ["get", "/api/admin/line-groups/group-1/deliveries", () => getFleetLineGroupDeliveries("group-1")],
  ] as const)("%s %s reaches the backend API", async (method, path, call) => {
    await expect(call()).rejects.toThrow("request intercepted");
    expect(httpClient[method]).toHaveBeenCalledWith(path, expect.anything());
  });
});
