// @vitest-environment jsdom
import { AxiosError, CanceledError } from "axios";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { httpClient } from "./httpClient";
import { notifyGlobal } from "../contexts/NotificationContext";
vi.mock("../contexts/NotificationContext", () => ({ notifyGlobal: vi.fn() }));
describe("HTTP failure notifications", () => {
  beforeEach(() => { vi.clearAllMocks(); });
  it("does not report canceled requests as connection errors", async () => {
    await expect(httpClient.get("/api/test", { adapter: async () => { throw new CanceledError("canceled"); } })).rejects.toMatchObject({ code: "ERR_CANCELED" });
    expect(notifyGlobal).not.toHaveBeenCalled();
  });
  it("still reports actual connection failures", async () => {
    await expect(httpClient.get("/api/test", { adapter: async config => { throw new AxiosError("offline", "ERR_NETWORK", config); } })).rejects.toMatchObject({ code: "ERR_NETWORK" });
    expect(notifyGlobal).toHaveBeenCalledWith("error", "ไม่สามารถเชื่อมต่อเซิร์ฟเวอร์ได้");
  });
  it("distinguishes timeouts from connection failures", async () => {
    await expect(httpClient.get("/api/test", { adapter: async config => { throw new AxiosError("timeout", "ECONNABORTED", config); } })).rejects.toMatchObject({ code: "ECONNABORTED" });
    expect(notifyGlobal).toHaveBeenCalledWith("error", "เซิร์ฟเวอร์ใช้เวลาตอบกลับนานเกินไป กรุณาลองอีกครั้ง");
  });
});
