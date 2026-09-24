import { createFleetRequest, transitionFleetRequest, updateFleetRequest, uploadFleetRequestAttachment, type FleetRequest, type SaveFleetRequest } from "../api/fleetApi";

type Operations = {
  create: typeof createFleetRequest;
  update: typeof updateFleetRequest;
  upload: typeof uploadFleetRequestAttachment;
  submit: typeof transitionFleetRequest;
};

const defaults: Operations = { create: createFleetRequest, update: updateFleetRequest, upload: uploadFleetRequestAttachment, submit: transitionFleetRequest };

export async function persistFleetRequestWithDocuments({ requestId, payload, files, submit, onSaved, onUploaded, onUploadError, operations = defaults }: {
  requestId?: string | null;
  payload: SaveFleetRequest;
  files: File[];
  submit: boolean;
  onSaved: (request: FleetRequest) => void;
  onUploaded: (file: File, requestId: string) => void | Promise<void>;
  onUploadError: () => void;
  operations?: Operations;
}) {
  const saved = requestId ? await operations.update(requestId, payload) : await operations.create(payload);
  onSaved(saved);
  for (const file of files) {
    try {
      await operations.upload(saved.id, file);
      await onUploaded(file, saved.id);
    } catch (error) {
      onUploadError();
      throw error;
    }
  }
  return submit ? operations.submit(saved.id, "submit", saved.concurrencyToken) : saved;
}
