import { describe, expect, it, vi } from "vitest";
import { persistFleetRequestWithDocuments } from "./fleetRequestDocumentFlow";
import type { FleetRequest, SaveFleetRequest } from "../api/fleetApi";

describe("persistFleetRequestWithDocuments", () => {
  it("keeps the draft and does not submit when upload fails, then retries without creating again", async () => {
    const saved = { id: "draft-1", concurrencyToken: "token-1" } as FleetRequest;
    const submitted = { ...saved, status: "PENDING_DISPATCH" } as FleetRequest;
    const create = vi.fn().mockResolvedValue(saved);
    const update = vi.fn().mockResolvedValue(saved);
    const upload = vi.fn().mockRejectedValueOnce(new Error("upload failed")).mockResolvedValueOnce({});
    const submit = vi.fn().mockResolvedValue(submitted);
    const operations = { create, update, upload, submit } as unknown as NonNullable<Parameters<typeof persistFleetRequestWithDocuments>[0]["operations"]>;
    const file = new File(["%PDF-"], "invite.pdf", { type: "application/pdf" });
    const payload = { concurrencyToken: "token-1" } as SaveFleetRequest;
    let draftId: string | null = null;
    const onSaved = (request: FleetRequest) => { draftId = request.id; };
    const onUploaded = vi.fn();
    const onUploadError = vi.fn();

    await expect(persistFleetRequestWithDocuments({ payload, files: [file], submit: true, onSaved, onUploaded, onUploadError, operations })).rejects.toThrow("upload failed");
    expect(draftId).toBe("draft-1");
    expect(submit).not.toHaveBeenCalled();
    expect(onUploadError).toHaveBeenCalledOnce();

    await expect(persistFleetRequestWithDocuments({ requestId: draftId, payload, files: [file], submit: true, onSaved, onUploaded, onUploadError, operations })).resolves.toEqual(submitted);
    expect(create).toHaveBeenCalledOnce();
    expect(update).toHaveBeenCalledOnce();
    expect(onUploaded).toHaveBeenCalledOnce();
    expect(submit).toHaveBeenCalledWith("draft-1", "submit", "token-1");
  });

  it("saves a draft without requiring an attachment", async () => {
    const saved = { id: "draft-2", concurrencyToken: "token-2" } as FleetRequest;
    const create = vi.fn().mockResolvedValue(saved);
    const submit = vi.fn();
    const operations = { create, update: vi.fn(), upload: vi.fn(), submit } as unknown as NonNullable<Parameters<typeof persistFleetRequestWithDocuments>[0]["operations"]>;
    await expect(persistFleetRequestWithDocuments({ payload: {} as SaveFleetRequest, files: [], submit: false, onSaved: vi.fn(), onUploaded: vi.fn(), onUploadError: vi.fn(), operations })).resolves.toEqual(saved);
    expect(submit).not.toHaveBeenCalled();
  });
});
