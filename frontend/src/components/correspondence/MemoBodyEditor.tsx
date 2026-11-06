import { useEffect, useRef } from "react";
import { Box, Button, IconButton, MenuItem, Stack, TextField, Tooltip, Typography } from "@mui/material";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import type { MemoParagraph, MemoRun } from "../../api/correspondenceApi";

function readRuns(root: HTMLElement): MemoRun[] {
  const output: MemoRun[] = [];
  function walk(node: Node, marks: Omit<MemoRun, "text">) {
    if (node.nodeType === Node.TEXT_NODE) { if (node.textContent) output.push({ ...marks, text: node.textContent }); return; }
    if (!(node instanceof HTMLElement)) return;
    if (["SCRIPT", "STYLE", "IMG", "TABLE", "IFRAME"].includes(node.tagName)) return;
    if (node.tagName === "BR") { output.push({ ...marks, text: "\n" }); return; }
    const next = { bold: marks.bold || ["B", "STRONG"].includes(node.tagName) || node.style.fontWeight === "bold" || Number(node.style.fontWeight) >= 600,
      italic: marks.italic || ["I", "EM"].includes(node.tagName) || node.style.fontStyle === "italic",
      underline: marks.underline || node.tagName === "U" || node.style.textDecoration.includes("underline") };
    if (["DIV", "P"].includes(node.tagName) && output.length) output.push({ text: "\n" });
    node.childNodes.forEach(child => walk(child, next));
  }
  root.childNodes.forEach(node => walk(node, {}));
  return output.length ? output : [{ text: "" }];
}

function RichParagraph({ value, onChange, label }: { value: MemoParagraph; onChange: (value: MemoParagraph) => void; label: string }) {
  const editor = useRef<HTMLDivElement>(null);
  useEffect(() => {
    const root = editor.current!;
    root.replaceChildren();
    for (const run of value.runs) { const span = document.createElement("span"); span.textContent = run.text; if (run.bold) span.style.fontWeight = "bold"; if (run.italic) span.style.fontStyle = "italic"; if (run.underline) span.style.textDecoration = "underline"; root.appendChild(span); }
    // This editable is intentionally uncontrolled so typing does not reset the caret.
    // Parent reset/removal remounts it; its live value is serialized as safe text runs.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);
  const update = () => onChange({ ...value, runs: readRuns(editor.current!) });
  const format = (command: string) => { editor.current!.focus(); document.execCommand(command); update(); };
  return <Box><Stack direction="row" spacing={.5} role="toolbar" aria-label={`รูปแบบ ${label}`} sx={{ mb: 1 }}>
    {[["bold", "ตัวหนา", "B"], ["italic", "ตัวเอียง", "I"], ["underline", "ขีดเส้นใต้", "U"]].map(([command, title, text]) => <Button key={command} size="small" variant="outlined" aria-label={`${title} ${label}`} onMouseDown={e => e.preventDefault()} onClick={() => format(command)} sx={{ minWidth: 38, fontWeight: command === "bold" ? 900 : 500, fontStyle: command === "italic" ? "italic" : "normal", textDecoration: command === "underline" ? "underline" : "none" }}>{text}</Button>)}
  </Stack><Box ref={editor} contentEditable suppressContentEditableWarning role="textbox" aria-multiline="true" aria-label={label} onInput={update}
    onPaste={e => { e.preventDefault(); document.execCommand("insertText", false, e.clipboardData.getData("text/plain")); update(); }}
    onDrop={e => e.preventDefault()}
    onKeyDown={e => { if (e.key === "Enter") { e.preventDefault(); document.execCommand("insertLineBreak"); update(); } }}
    lang="th" sx={{ p: 1.5, minHeight: 110, border: "1px solid", borderColor: "divider", borderRadius: 2, whiteSpace: "pre-wrap", overflowWrap: "anywhere", textAlign: value.align, textJustify: "auto", textAlignLast: "auto", pl: 1.5 + value.indent, "&:focus": { outline: "2px solid", outlineColor: "primary.main" } }} />
  </Box>;
}

export function MemoBodyEditor({ value, onChange }: { value: MemoParagraph[]; onChange: (value: MemoParagraph[]) => void }) {
  const ids = useRef(value.map((_, i) => i)); const nextId = useRef(value.length);
  const change = (index: number, paragraph: MemoParagraph) => onChange(value.map((old, i) => i === index ? paragraph : old));
  return <Stack spacing={2}>
    <Typography variant="body2" color="text.secondary">เลือกข้อความในย่อหน้าเพื่อจัดรูปแบบ วางข้อความได้โดยไม่คัดลอกรูปแบบหรือรูปภาพจากภายนอก</Typography>
    {value.map((paragraph, index) => <Box key={ids.current[index]} sx={{ p: { xs: 1.25, sm: 2 }, border: "1px solid", borderColor: "divider", borderRadius: 2 }}>
      <Stack direction="row" alignItems="center" justifyContent="space-between"><Typography fontWeight={800}>ย่อหน้า {index + 1}</Typography><Tooltip title="ลบย่อหน้า"><span><IconButton aria-label={`ลบย่อหน้า ${index + 1}`} disabled={value.length === 1} onClick={() => { ids.current.splice(index, 1); onChange(value.filter((_, i) => i !== index)); }}><DeleteOutlineIcon /></IconButton></span></Tooltip></Stack>
      <Stack direction={{ xs: "column", sm: "row" }} spacing={1} sx={{ mb: 1 }}>
        <TextField select size="small" label={`จัดแนวย่อหน้า ${index + 1}`} value={paragraph.align} onChange={e => change(index, { ...paragraph, align: e.target.value as MemoParagraph["align"] })} sx={{ minWidth: 160 }}><MenuItem value="left">ชิดซ้าย</MenuItem><MenuItem value="center">กึ่งกลาง</MenuItem><MenuItem value="right">ชิดขวา</MenuItem><MenuItem value="justify">เต็มบรรทัด (ภาษาไทย)</MenuItem></TextField>
        <TextField select size="small" label={`ย่อหน้าเข้า ${index + 1}`} value={paragraph.indent} onChange={e => change(index, { ...paragraph, indent: Number(e.target.value) })} sx={{ minWidth: 130 }}>{[0, 1, 2, 3].map(v => <MenuItem key={v} value={v}>{v === 0 ? "ไม่ย่อ" : `ระดับ ${v}`}</MenuItem>)}</TextField>
        <Button variant={paragraph.ordered ? "contained" : "outlined"} aria-pressed={paragraph.ordered} onClick={() => change(index, { ...paragraph, ordered: !paragraph.ordered })}>รายการลำดับ</Button>
      </Stack>
      <RichParagraph value={paragraph} label={`เนื้อหาย่อหน้า ${index + 1}`} onChange={updated => change(index, updated)} />
    </Box>)}
    <Button sx={{ alignSelf: "flex-start" }} disabled={value.length >= 300} onClick={() => { ids.current.push(nextId.current++); onChange([...value, { runs: [{ text: "" }], align: "justify", indent: 1, ordered: false }]); }}>เพิ่มย่อหน้า</Button>
    <Typography variant="caption" color="text.secondary">{value.reduce((sum, p) => sum + p.runs.reduce((n, r) => n + r.text.length, 0), 0).toLocaleString("th-TH")} / 30,000 ตัวอักษร</Typography>
  </Stack>;
}
