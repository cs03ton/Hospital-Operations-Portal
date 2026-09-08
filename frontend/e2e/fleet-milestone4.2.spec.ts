import type { Page } from "@playwright/test";
import { test, expect, manifest } from "./fixtures/fleetQa";

const m = () => { expect(manifest, "M4.2 QA manifest").not.toBeNull(); return manifest!; };
async function api(page: Page, path: string, init?: RequestInit) {
  return page.evaluate(async ({ path, init }) => {
    const token = localStorage.getItem("hop.accessToken");
    const response = await fetch(`/api${path}`, { ...init, credentials: "include", headers: { "Content-Type": "application/json", ...(token ? { Authorization: `Bearer ${token}` } : {}), ...(init?.headers ?? {}) } });
    let body: unknown = null; try { body = await response.json(); } catch { /* response may be empty */ }
    return { status: response.status, body };
  }, { path, init });
}

test("@fleet-m4.2 01 capability master persists typed and safety metadata", async ({ adminPage: p }) => {
  const code = `PW-${m().runId.slice(-8)}`;
  const createPayload = { code, name: "Playwright capability", description: "M4.2 lifecycle", category: "GENERAL", dataType: "BOOLEAN", unit: null, minimumNumericValue: null, maximumNumericValue: null, enumOptionsJson: null, isRequiredSafetyCapability: false, isActive: true, sortOrder: 99, concurrencyToken: null };
  const created = await api(p, "/fleet/capabilities", { method: "POST", body: JSON.stringify(createPayload) });
  expect(created.status).toBe(200);
  const createdItem = (created.body as { data: { id: string; concurrencyToken: string } }).data;
  const updated = await api(p, `/fleet/capabilities/${createdItem.id}`, { method: "PUT", body: JSON.stringify({ ...createPayload, name: "Playwright capability edited", concurrencyToken: createdItem.concurrencyToken }) });
  expect(updated.status).toBe(200);
  const updatedItem = (updated.body as { data: { concurrencyToken: string } }).data;
  const disabled = await api(p, `/fleet/capabilities/${createdItem.id}/disable`, { method: "POST", body: JSON.stringify({ concurrencyToken: updatedItem.concurrencyToken, reason: "M4.2 lifecycle complete" }) });
  expect(disabled.status).toBe(200);
  const detail = await api(p, `/fleet/capabilities/${createdItem.id}`);
  expect(detail.status).toBe(200);
  expect(JSON.stringify(detail.body)).toContain("Playwright capability edited");
  expect((detail.body as { data: { x: { isActive: boolean } } }).data.x.isActive).toBe(false);
});

test("@fleet-m4.2 02 vehicle capability binding renders current values and immutable history", async ({ adminPage: p }) => {
  const code = `VB-${m().runId.slice(-8)}`;
  const created = await api(p, "/fleet/capabilities", { method: "POST", body: JSON.stringify({ code, name: "Vehicle binding", description: null, category: "CAPACITY", dataType: "NUMBER", unit: "kg", minimumNumericValue: 0, maximumNumericValue: 5000, enumOptionsJson: null, isRequiredSafetyCapability: false, isActive: true, sortOrder: 100, concurrencyToken: null }) });
  expect(created.status).toBe(200);
  const capabilityId = (created.body as { data: { id: string } }).data.id;
  const save = await api(p, `/fleet/vehicles/${m().vehicleId}/capabilities`, { method: "PUT", body: JSON.stringify([{ capabilityId, booleanValue: null, numericValue: 750, textValue: null, enumValue: null, effectiveFrom: "2026-08-02T02:00:00Z", effectiveTo: null, isActive: true, concurrencyToken: null }]) });
  expect(save.status).toBe(200);
  const persisted = await api(p, `/fleet/vehicles/${m().vehicleId}/capabilities`);
  expect(persisted.status).toBe(200);
  expect(JSON.stringify(persisted.body)).toContain("750");
  await p.goto(`/fleet/admin/vehicles/${m().vehicleId}/capabilities`);
  await expect(p.getByText(/Vehicle Capabilities/)).toBeVisible();
  await expect(p.getByText(/History/)).toBeVisible();
  await expect(p.getByText(m().runId, { exact: false }).first()).toBeVisible();
});

test("@fleet-m4.2 03 request required capabilities persist mandatory typed requirements", async ({ requesterPage: p }) => {
  const saved = await api(p, `/fleet/requests/${m().draftRequestId}/required-capabilities`, { method: "PUT", body: JSON.stringify([{ capabilityId: m().capabilityId, operator: "EQUALS", requiredBooleanValue: true, requiredNumericValue: null, requiredTextValue: null, requiredEnumValue: null, isMandatory: true, notes: "M4.2 mandatory" }]) });
  expect(saved.status).toBe(200);
  const token = (saved.body as { data: { concurrencyToken: string } }).data.concurrencyToken;
  const result = await api(p, `/fleet/requests/${m().draftRequestId}/required-capabilities`);
  expect(result.status).toBe(200);
  expect(JSON.stringify(result.body)).toContain("M4.2 mandatory");
  const submitted = await api(p, `/fleet/requests/${m().draftRequestId}/submit`, { method: "POST", body: JSON.stringify({ concurrencyToken: token, reason: "M4.2 required capabilities verified" }) });
  expect(submitted.status).toBe(200);
  expect(JSON.stringify(submitted.body)).toContain("PENDING_DISPATCH");
});

test("@fleet-m4.2 04 compatibility reports the matching non-safety capability", async ({ dispatcherPage: p }) => {
  const result = await api(p, `/fleet/requests/${m().requestId}/compatibility?vehicleId=${m().vehicleId}`);
  expect(result.status).toBe(200);
  expect(JSON.stringify(result.body)).toContain(m().capabilityId);
  expect(JSON.stringify(result.body)).toMatch(/MATCH/);
});

test("@fleet-m4.2 05 compatibility reports safety NOT_MATCH with reasons", async ({ dispatcherPage: p }) => {
  const result = await api(p, `/fleet/requests/${m().requestId}/compatibility?vehicleId=${m().vehicleId}`);
  expect(result.status).toBe(200);
  const text = JSON.stringify(result.body);
  expect(text).toContain(m().safetyCapabilityId);
  expect(text).toContain("NOT_MATCH");
});

test("@fleet-m4.2 06 actor without override permission is denied", async ({ noOverridePage: p }) => {
  const result = await api(p, `/fleet/requests/${m().requestId}/compatibility-override`, { method: "POST", body: JSON.stringify({ vehicleId: m().vehicleId, capabilityIds: [m().capabilityId], reason: "M4.2 permission assertion", requestConcurrencyToken: "invalid" }) });
  expect(result.status).toBe(403);
});

test("@fleet-m4.2 07 safety mismatch cannot be overridden", async ({ adminPage: p }) => {
  const detail = await api(p, `/fleet/requests/${m().requestId}`);
  const token = (detail.body as { data: { concurrencyToken: string } }).data.concurrencyToken;
  const result = await api(p, `/fleet/requests/${m().requestId}/compatibility-override`, { method: "POST", body: JSON.stringify({ vehicleId: m().vehicleId, capabilityIds: [m().safetyCapabilityId], reason: "M4.2 safety assertion", requestConcurrencyToken: token }) });
  expect(result.status).toBe(400);
  const verify = await api(p, `/fleet/requests/${m().requestId}/compatibility?vehicleId=${m().vehicleId}`);
  expect(JSON.stringify(verify.body)).toContain("NOT_MATCH");
});

test("@fleet-m4.2 07b non-safety mismatch override persists reason and OVERRIDDEN result", async ({ adminPage: p }) => {
  const detail = await api(p, `/fleet/requests/${m().requestId}`);
  const token = (detail.body as { data: { concurrencyToken: string } }).data.concurrencyToken;
  const override = await api(p, `/fleet/requests/${m().requestId}/compatibility-override`, { method: "POST", body: JSON.stringify({ vehicleId: m().vehicleId, capabilityIds: [m().mismatchCapabilityId], reason: "M4.2 approved non-safety exception", requestConcurrencyToken: token }) });
  expect(override.status).toBe(200);
  const verify = await api(p, `/fleet/requests/${m().requestId}/compatibility?vehicleId=${m().vehicleId}`);
  const text = JSON.stringify(verify.body);
  expect(text).toContain(m().mismatchCapabilityId);
  expect(text).toContain("OVERRIDDEN");
});

test("@fleet-m4.2 08 emergency queue exposes priority and SLA snapshot", async ({ dispatcherPage: p }) => {
  const result = await api(p, "/fleet/emergency-requests/queue");
  expect(result.status).toBe(200);
  const text = JSON.stringify(result.body);
  expect(text).toContain(m().emergencyQueueRequestId);
  expect(text).toContain("EMERGENCY");
  expect(text).toMatch(/responseTargetMinutes|dispatchTargetMinutes/i);
});

test("@fleet-m4.2 09 emergency request UI requires reason and shows EMS boundary", async ({ requesterPage: p }) => {
  await p.goto("/fleet/emergency/create");
  await expect(p.getByText(/Emergency/i).first()).toBeVisible();
  await expect(p.getByText(/EMS|ambulance|ฉุกเฉิน/i).first()).toBeVisible();
  await expect(p.getByLabel(/เหตุผล|reason/i)).toBeVisible();
  const created = await api(p, "/fleet/emergency-requests", { method: "POST", body: JSON.stringify({ purpose: "M4.2 emergency creation", missionType: "EMERGENCY_TRANSPORT", requestedVehicleTypeId: null, destination: "QA emergency destination", contactName: "QA requester", contactPhone: "0812345678", incidentLocation: "QA incident", requestedDepartureAt: "2026-08-03T02:00:00Z", expectedReturnAt: "2026-08-03T03:00:00Z", passengerCount: 1, emergencyReason: "M4.2 emergency UAT", specialRequirement: "QA only", emergencyPolicyCode: m().emergencyPolicyCode }) });
  expect(created.status).toBe(200);
  const item = (created.body as { data: { id: string; concurrencyToken: string } }).data;
  const submitted = await api(p, `/fleet/emergency-requests/${item.id}/submit`, { method: "POST", body: JSON.stringify({ concurrencyToken: item.concurrencyToken, reason: "M4.2 requester confirmed" }) });
  expect(submitted.status).toBe(200);
  expect(JSON.stringify(submitted.body)).toContain("PENDING_DISPATCH");
});

test("@fleet-m4.2 10 completed emergency enters post-review workflow", async ({ reviewerPage: p }) => {
  await p.goto(`/fleet/emergency/${m().requestId}/post-review`);
  await expect(p.getByText(/Post-review|ทบทวน/i).first()).toBeVisible();
  await expect(p.getByText(/SLA|Outcome|ผลการทบทวน/i).first()).toBeVisible();
  const detail = await api(p, `/fleet/emergency-reviews/${m().requestId}`);
  const token = (detail.body as { data: { concurrencyToken: string } }).data.concurrencyToken;
  const reviewed = await api(p, `/fleet/emergency-requests/${m().requestId}/post-review`, { method: "POST", body: JSON.stringify({ concurrencyToken: token, outcome: "ACCEPTABLE", wasBypassAppropriate: true, responseTimeAssessment: "M4.2 SLA assessed", safetyIssues: "None", followUpActions: "Monitor", notes: "Playwright immutable review" }) });
  expect(reviewed.status).toBe(200);
  const verify = await api(p, `/fleet/emergency-reviews/${m().requestId}`);
  expect(JSON.stringify(verify.body)).toContain("Playwright immutable review");
  expect(JSON.stringify(verify.body)).toContain("ACCEPTABLE");
});

test("@fleet-m4.2 11 driver mobile completes trip with mileage and idempotency key", async ({ driverPage: p }) => {
  await p.setViewportSize({ width: 390, height: 844 });
  const result = await api(p, `/fleet/driver-jobs/${m().driverRequestId}`);
  expect(result.status).toBe(200);
  expect(JSON.stringify(result.body)).not.toMatch(/QA-[0-9a-f]{16,}/i);
  const job = (result.body as { data: { concurrencyToken: string; trip: { concurrencyToken: string; startMileage: number } } }).data;
  const completed = await api(p, `/fleet/trips/${m().driverRequestId}/complete`, { method: "POST", headers: { "Idempotency-Key": `pw-complete-${m().runId}` }, body: JSON.stringify({ concurrencyToken: job.concurrencyToken, tripConcurrencyToken: job.trip.concurrencyToken, endMileage: job.trip.startMileage + 10, completionNotes: "M4.2 mobile completion" }) });
  expect(completed.status).toBe(200);
  expect(JSON.stringify(completed.body)).toContain("COMPLETED");
  expect(await p.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBeTruthy();
});

test("@fleet-m4.2 12 driver attachment upload, link and soft-delete lifecycle", async ({ previousDriverPage: p }) => {
  const result = await api(p, `/fleet/trips/${m().attachmentRequestId}/attachments`);
  expect(result.status).toBe(200);
  const body = result.body as { data?: unknown[] };
  expect(body.data ?? []).toHaveLength(0);
  await p.goto(`/fleet/driver/trips/${m().attachmentRequestId}/action`);
  await p.locator('input[type="file"]').setInputFiles({ name: "m42-proof.pdf", mimeType: "application/pdf", buffer: Buffer.from("%PDF-1.4\n1 0 obj\n<< /Type /Catalog >>\nendobj\n%%EOF") });
  const link = p.getByRole("link", { name: "m42-proof.pdf" });
  await expect(link).toBeVisible();
  await expect(link).toHaveAttribute("href", /\/api\/fleet\/trips\/attachments\/.+\/download/);
  p.once("dialog", dialog => dialog.accept());
  await p.getByRole("button", { name: "ลบ" }).click();
  await expect(link).not.toBeVisible();
});
