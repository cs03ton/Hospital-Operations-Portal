import { useEffect, useState, useSyncExternalStore } from "react";
import { Stack, TextField, Typography } from "@mui/material";
import { ActionDialog } from "./ActionDialog";

type Request = { message: string; input: boolean; settle: (answer: boolean | string | null) => void };
let current: Request | null = null;
const listeners = new Set<() => void>();
const publish = () => listeners.forEach(listener => listener());
function request(message: string, input: boolean, signal?: AbortSignal): Promise<boolean | string | null> {
  if (current || signal?.aborted) return Promise.resolve(input ? null : false);
  return new Promise(resolve => {
    const abort = () => entry.settle(input ? null : false);
    const entry: Request = { message, input, settle: answer => {
      if (current !== entry) return;
      current = null; signal?.removeEventListener("abort", abort); publish(); resolve(answer);
    } };
    current = entry; signal?.addEventListener("abort", abort, { once: true }); publish();
  });
}
export const confirmSystem = (message: string, signal?: AbortSignal) => request(message, false, signal) as Promise<boolean>;
export const promptSystem = (message: string) => request(message, true) as Promise<string | null>;
const subscribe = (listener: () => void) => { listeners.add(listener); return () => { listeners.delete(listener); }; };
export function SystemDialogHost() {
  const active = useSyncExternalStore(subscribe, () => current, () => null);
  const [value, setValue] = useState("");
  useEffect(() => { setValue(""); }, [active]);
  useEffect(() => () => current?.settle(current.input ? null : false), []);
  return <ActionDialog open={!!active} title={active?.input ? "ระบุเหตุผล" : "ยืนยันการดำเนินการ"}
    onClose={() => active?.settle(active.input ? null : false)} onConfirm={() => active?.settle(active.input ? value.trim() : true)}
    isConfirmDisabled={!!active?.input && !value.trim()}>
    <Stack spacing={2}>
      <Typography sx={{ whiteSpace: "pre-wrap", overflowWrap: "anywhere" }}>{active?.message}</Typography>
      {active?.input && <TextField autoFocus fullWidth multiline minRows={3} label="เหตุผล" value={value} onChange={e => setValue(e.target.value)} inputProps={{ maxLength: 2000 }} />}
    </Stack>
  </ActionDialog>;
}
