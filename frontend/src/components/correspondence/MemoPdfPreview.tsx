import { useEffect, useRef, useState } from "react";
import { Alert, Box, Button, Stack, Typography } from "@mui/material";
import workerUrl from "pdfjs-dist/build/pdf.worker.min.mjs?worker&url";
import type { PDFDocumentProxy, RenderTask } from "pdfjs-dist";

export function MemoPdfPreview({ url, documentLabel = "บันทึกข้อความ" }: { url: string; documentLabel?: string }) {
  const host = useRef<HTMLDivElement>(null); const canvas = useRef<HTMLCanvasElement>(null);
  const [pdf, setPdf] = useState<PDFDocumentProxy | null>(null); const [page, setPage] = useState(1);
  const [width, setWidth] = useState(300); const [busy, setBusy] = useState(true); const [error, setError] = useState(false);
  useEffect(() => {
    const observer = new ResizeObserver(entries => setWidth(Math.max(100, entries[0].contentRect.width)));
    observer.observe(host.current!); return () => observer.disconnect();
  }, []);
  useEffect(() => {
    let active = true; let task: ReturnType<typeof import("pdfjs-dist")["getDocument"]> | undefined;
    setPdf(null); setPage(1); setError(false); setBusy(true);
    import("pdfjs-dist").then(library => {
      if (!active) return;
      library.GlobalWorkerOptions.workerSrc = workerUrl;
      task = library.getDocument({ url, isEvalSupported: false });
      return task.promise.then(document => { if (active) setPdf(document); });
    }).catch(() => { if (active) { setError(true); setBusy(false); } });
    return () => { active = false; void task?.destroy(); };
  }, [url]);
  useEffect(() => {
    if (!pdf) return;
    let active = true; let task: RenderTask | undefined; setBusy(true);
    pdf.getPage(page).then(item => {
      if (!active || !canvas.current) return;
      const scale = width / item.getViewport({ scale: 1 }).width;
      const ratio = Math.min(window.devicePixelRatio || 1, 2); const viewport = item.getViewport({ scale: scale * ratio });
      const element = canvas.current; element.width = Math.ceil(viewport.width); element.height = Math.ceil(viewport.height);
      element.style.width = `${width}px`; element.style.height = `${viewport.height / ratio}px`;
      task = item.render({ canvas: element, viewport });
      return task.promise;
    }).then(() => { if (active) setBusy(false); }).catch(() => { if (active) { setError(true); setBusy(false); } });
    return () => { active = false; task?.cancel(); };
  }, [pdf, page, width]);
  return <Stack spacing={1} title={`ตัวอย่าง${documentLabel} PDF`}>
    <Stack direction="row" spacing={1} alignItems="center" justifyContent="center">
      <Button disabled={!pdf || page === 1} onClick={() => setPage(n => n - 1)}>ก่อนหน้า</Button>
      <Typography variant="body2">หน้า {page} / {pdf?.numPages ?? "…"}</Typography>
      <Button disabled={!pdf || page >= pdf.numPages} onClick={() => setPage(n => n + 1)}>ถัดไป</Button>
    </Stack>
    {busy && <Typography role="status">กำลังแสดงหน้าเอกสาร...</Typography>}
    {error && <Alert severity="warning">แสดงตัวอย่างไม่สำเร็จ กรุณาเปิด PDF เต็มจอหรือดาวน์โหลดไฟล์</Alert>}
    <Box ref={host} sx={{ width: "100%", minWidth: 0, bgcolor: "white", border: "1px solid", borderColor: "divider", overflow: "hidden" }}>
      <canvas ref={canvas} role="img" aria-label={`เอกสาร${documentLabel} หน้า ${page}`} style={{ display: "block", maxWidth: "100%" }} />
    </Box>
  </Stack>;
}
