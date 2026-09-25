import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link as RouterLink } from "react-router-dom";
import { Alert, Box, Button, Card, CardContent, Chip, CircularProgress, MenuItem, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TablePagination, TableRow, TextField, Typography } from "@mui/material";
import DownloadOutlinedIcon from "@mui/icons-material/DownloadOutlined";
import SearchOutlinedIcon from "@mui/icons-material/SearchOutlined";
import { PageHeader } from "../components/PageHeader";
import { AppDatePicker } from "../components/common/AppDatePicker";
import { bangkokDayjs, formatThaiDate, formatThaiMonthYear } from "../utils/dateFormat";
import { repairNumber, repairPriorityLabels, repairStatusLabels } from "../utils/repairPresentation";
import { repairReportExport, repairReportItems, repairReportSummary, type RepairReportFilter, type RepairReportGroup } from "../api/repairApi";

const statusCards = [
  { status: "Submitted", label: "แจ้งใหม่", color: "#3b82f6" },
  { status: "InProgress", label: "กำลังดำเนินการ", color: "#d99720" },
  { status: "WaitingParts", label: "รออะไหล่", color: "#a271ce" },
  { status: "Returned", label: "ส่งกลับผู้แจ้ง", color: "#d57960" },
  { status: "Resolved", label: "แก้ไขแล้วรอตรวจรับ", color: "#19a785" },
  { status: "Closed", label: "ตรวจรับแล้ว", color: "#128456" },
  { status: "Cancelled", label: "ยกเลิก", color: "#798696" },
] as const;
const chartColors = ["#198d68", "#438de8", "#f4b649", "#e98aa4", "#9b80d2", "#7fc4bc", "#ddab6b"];
const today = bangkokDayjs();
const initialFilters: RepairReportFilter = { from: today.startOf("month").format("YYYY-MM-DD"), to: today.format("YYYY-MM-DD") };

export function RepairReportsPage() {
  const [filters, setFilters] = useState<RepairReportFilter>(initialFilters);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(10);
  const [exporting, setExporting] = useState<"excel" | "pdf" | null>(null);
  const [exportError, setExportError] = useState("");
  const validRange = Boolean(filters.from && filters.to && filters.from <= filters.to);
  const summary = useQuery({ queryKey: ["repair-report", "summary", filters], queryFn: () => repairReportSummary(filters), enabled: validRange });
  const rows = useQuery({ queryKey: ["repair-report", "items", filters, page, pageSize], queryFn: () => repairReportItems(filters, page + 1, pageSize), enabled: validRange });

  function update(next: RepairReportFilter) { setFilters(next); setPage(0); }
  async function download(type: "excel" | "pdf") {
    setExportError(""); setExporting(type);
    try {
      const blob = await repairReportExport(filters, type);
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a"); link.href = url; link.download = `repair-report-${filters.from}-${filters.to}.${type === "excel" ? "xlsx" : "pdf"}`;
      document.body.appendChild(link); link.click(); link.remove(); URL.revokeObjectURL(url);
    } catch { setExportError("ส่งออกรายงานไม่สำเร็จ กรุณาลองใหม่"); }
    finally { setExporting(null); }
  }

  const report = summary.data;
  const maxMonth = Math.max(1, ...(report?.months ?? []).map(x => x.total));
  return <Stack spacing={2.5} sx={{ maxWidth: 1600, mx: "auto", minWidth: 0 }}>
    <PageHeader title="รายงานการแจ้งซ่อม" subtitle="ภาพรวม สถิติ และรายละเอียดการแจ้งซ่อม เพื่อใช้วางแผนและติดตามการบำรุงรักษา" />
    <Card><CardContent><Stack direction={{ xs: "column", md: "row" }} spacing={1.5} alignItems={{ md: "center" }} justifyContent="space-between">
      <Box><Typography variant="h6" fontWeight={800}>ช่วงวันที่รายงาน</Typography><Typography variant="body2" color="text.secondary">นับตามวันที่แจ้งในเวลาไทย</Typography></Box>
      <Stack direction={{ xs: "column", sm: "row" }} spacing={1} alignItems={{ sm: "center" }}>
        <Box sx={{ minWidth: 185 }}><AppDatePicker label="ตั้งแต่วันที่" value={filters.from} onChange={value => update({ ...filters, from: value })} /></Box>
        <Box sx={{ minWidth: 185 }}><AppDatePicker label="ถึงวันที่" value={filters.to} onChange={value => update({ ...filters, to: value })} /></Box>
        <Button variant="outlined" startIcon={<DownloadOutlinedIcon />} disabled={!validRange || !!exporting} onClick={() => void download("excel")}>Excel</Button>
        <Button variant="contained" startIcon={<DownloadOutlinedIcon />} disabled={!validRange || !!exporting} onClick={() => void download("pdf")}>PDF</Button>
      </Stack>
    </Stack></CardContent></Card>
    {!validRange && <Alert severity="warning">กรุณาเลือกช่วงวันที่ให้ถูกต้อง</Alert>}
    {exportError && <Alert severity="error">{exportError}</Alert>}
    {summary.isError && <Alert severity="error">โหลดสรุปรายงานไม่สำเร็จ</Alert>}
    {summary.isLoading && validRange && <CircularProgress size={28} />}
    {report && <>
      <Box sx={{ display: "grid", gridTemplateColumns: { xs: "repeat(2,minmax(0,1fr))", sm: "repeat(3,minmax(0,1fr))", lg: "repeat(4,minmax(0,1fr))" }, gap: 1.5 }}>
        <Metric label="แจ้งซ่อมทั้งหมด" count={report.total} color="#164d43" detail={`ตรวจรับแล้ว ${report.acceptanceRate}% ของงานที่ไม่ยกเลิก`} />
        {statusCards.map(card => <Metric key={card.status} label={card.label} count={report.counts.find(x => x.status === card.status)?.count ?? 0} color={card.color} />)}
      </Box>
      <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", lg: "1.5fr 1fr 1fr" }, gap: 1.5 }}>
        <Card><CardContent><Typography variant="h6" fontWeight={800} gutterBottom>แนวโน้มการแจ้งซ่อม</Typography><Typography variant="caption" color="text.secondary">สีเขียว: แจ้งทั้งหมด · สีฟ้า: ตรวจรับแล้ว · เส้น: อัตราตรวจรับ</Typography>
          <Box sx={{ overflowX: "auto", pt: 2 }}><Box sx={{ display: "flex", alignItems: "flex-end", gap: 1.2, height: 205, minWidth: Math.max(450, report.months.length * 48), position: "relative" }}>
            {report.months.length > 0 && <svg aria-label="เส้นอัตราตรวจรับรายเดือน" viewBox={`0 0 ${report.months.length * 100} 145`} preserveAspectRatio="none" style={{ position: "absolute", left: 0, right: 0, top: 28, width: "100%", height: 145, pointerEvents: "none", zIndex: 1 }}>
              <polyline fill="none" stroke="#17574b" strokeWidth="2" points={report.months.map((month, index) => `${index * 100 + 50},${145 - month.acceptanceRate * 1.35}`).join(" ")} />
              {report.months.map((month, index) => <circle key={month.month} cx={index * 100 + 50} cy={145 - month.acceptanceRate * 1.35} r="4" fill="#17574b" />)}
            </svg>}
            {report.months.map(month => { const [year, number] = month.month.split("-").map(Number); return <Box key={month.month} sx={{ flex: 1, minWidth: 36, textAlign: "center" }} title={`${formatThaiMonthYear(year, number)}: ${month.total} งาน, ตรวจรับ ${month.closed} งาน (${month.acceptanceRate}%)`}>
              <Typography variant="caption" color="primary.main">{month.acceptanceRate}%</Typography><Box sx={{ height: 145, display: "flex", alignItems: "flex-end", justifyContent: "center", gap: 0.3, borderBottom: "1px solid", borderColor: "divider" }}>
                <Box sx={{ width: 13, height: `${Math.max(2, month.total / maxMonth * 135)}px`, bgcolor: "#20a774", borderRadius: "3px 3px 0 0" }} />
                <Box sx={{ width: 13, height: `${Math.max(2, month.closed / maxMonth * 135)}px`, bgcolor: "#4c91e8", borderRadius: "3px 3px 0 0" }} />
              </Box><Typography variant="caption" noWrap>{formatThaiMonthYear(year, number, "short").split(" ")[0]}</Typography></Box>; })}
          </Box></Box>
        </CardContent></Card>
        <GroupChart title="สัดส่วนประเภทงานซ่อม" groups={report.byCategory} total={report.total} />
        <GroupChart title="จำนวนงานตามหน่วยงาน" groups={report.byDepartment} total={report.total} bars />
      </Box>
    </>}
    <Card><CardContent><Typography variant="h6" fontWeight={800} gutterBottom>รายละเอียดรายการแจ้งซ่อม</Typography>
      <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", sm: "repeat(2,minmax(0,1fr))", lg: "2fr repeat(4,minmax(130px,1fr))" }, gap: 1, mb: 2 }}>
        <TextField size="small" label="ค้นหารหัส หัวข้อ สถานที่ หรือผู้แจ้ง" value={filters.search ?? ""} onChange={e => update({ ...filters, search: e.target.value })} InputProps={{ startAdornment: <SearchOutlinedIcon color="action" sx={{ mr: 1 }} /> }} />
        <TextField select size="small" label="ประเภทงาน" value={filters.categoryId ?? ""} onChange={e => update({ ...filters, categoryId: e.target.value || undefined })}><MenuItem value="">ทั้งหมด</MenuItem>{report?.categories.map(x => <MenuItem key={x.id} value={x.id}>{x.name}</MenuItem>)}</TextField>
        <TextField select size="small" label="หน่วยงาน" value={filters.departmentId ?? ""} onChange={e => update({ ...filters, departmentId: e.target.value || undefined })}><MenuItem value="">ทั้งหมด</MenuItem>{report?.departments.map(x => <MenuItem key={x.id} value={x.id}>{x.name}</MenuItem>)}</TextField>
        <TextField select size="small" label="สถานะ" value={filters.status ?? ""} onChange={e => update({ ...filters, status: e.target.value || undefined })}><MenuItem value="">ทั้งหมด</MenuItem>{statusCards.map(x => <MenuItem key={x.status} value={x.status}>{x.label}</MenuItem>)}</TextField>
        <TextField select size="small" label="ความเร่งด่วน" value={filters.priority ?? ""} onChange={e => update({ ...filters, priority: e.target.value || undefined })}><MenuItem value="">ทั้งหมด</MenuItem><MenuItem value="Unset">ยังไม่กำหนด</MenuItem>{Object.entries(repairPriorityLabels).map(([key, label]) => <MenuItem key={key} value={key}>{label}</MenuItem>)}</TextField>
      </Box>
      {rows.isError && <Alert severity="error">โหลดรายการรายงานไม่สำเร็จ</Alert>}
      {rows.isLoading && <CircularProgress size={24} />}
      {rows.data && <><TableContainer sx={{ overflowX: "auto" }}><Table size="small" sx={{ minWidth: 1040 }}><TableHead><TableRow>{["รหัสแจ้งซ่อม", "หัวข้อ", "ประเภทงาน", "หน่วยงาน", "ผู้แจ้ง", "วันที่แจ้ง", "วันที่ปิดงาน", "สถานะ", "ความเร่งด่วน", "ดู"].map(label => <TableCell key={label}>{label}</TableCell>)}</TableRow></TableHead><TableBody>
        {rows.data.items.map(item => <TableRow key={item.id} hover><TableCell>{repairNumber(item.number)}</TableCell><TableCell sx={{ maxWidth: 240 }}>{item.title}</TableCell><TableCell>{item.categoryName}</TableCell><TableCell>{item.departmentName}</TableCell><TableCell>{item.requesterName}</TableCell><TableCell>{formatThaiDate(item.createdAt)}</TableCell><TableCell>{item.closedAt ? formatThaiDate(item.closedAt) : "–"}</TableCell><TableCell><Chip size="small" label={repairStatusLabels[item.status] ?? item.status} /></TableCell><TableCell>{item.priority ? repairPriorityLabels[item.priority] ?? item.priority : "ยังไม่กำหนด"}</TableCell><TableCell><Button component={RouterLink} to={`/repairs/${item.id}`} size="small">ดู</Button></TableCell></TableRow>)}
        {rows.data.items.length === 0 && <TableRow><TableCell colSpan={10} align="center">ไม่พบรายการตามตัวกรอง</TableCell></TableRow>}
      </TableBody></Table></TableContainer><TablePagination component="div" count={rows.data.total} page={page} rowsPerPage={pageSize} onPageChange={(_, value) => setPage(value)} onRowsPerPageChange={e => { setPageSize(Number(e.target.value)); setPage(0); }} rowsPerPageOptions={[10, 20, 50]} labelRowsPerPage="รายการต่อหน้า" /></>}
    </CardContent></Card>
  </Stack>;
}

function Metric({ label, count, color, detail }: { label: string; count: number; color: string; detail?: string }) { return <Card sx={{ borderTop: `3px solid ${color}` }}><CardContent sx={{ py: 1.5 }}><Typography variant="body2" color="text.secondary">{label}</Typography><Typography variant="h4" fontWeight={900} sx={{ color }}>{count.toLocaleString("th-TH")}</Typography>{detail && <Typography variant="caption" color="text.secondary">{detail}</Typography>}</CardContent></Card>; }
function GroupChart({ title, groups, total, bars = false }: { title: string; groups: RepairReportGroup[]; total: number; bars?: boolean }) {
  const colors = groups.map((_, index) => chartColors[index % chartColors.length]);
  let offset = 0;
  const segments = groups.map((group, index) => { const start = offset; offset += total ? group.count * 100 / total : 0; return `${colors[index]} ${start}% ${offset}%`; });
  return <Card><CardContent><Typography variant="h6" fontWeight={800} gutterBottom>{title}</Typography>{groups.length === 0 ? <Typography color="text.secondary">ยังไม่มีข้อมูล</Typography> : <>
    {!bars && <Box sx={{ width: 110, height: 110, borderRadius: "50%", mx: "auto", my: 1, background: `conic-gradient(${segments.join(",")})`, display: "grid", placeItems: "center" }}><Box sx={{ width: 62, height: 62, bgcolor: "background.paper", borderRadius: "50%", display: "grid", placeItems: "center", fontWeight: 800 }}>{total}</Box></Box>}
    <Stack spacing={0.8} sx={{ maxHeight: 230, overflowY: "auto" }}>{groups.map((group, index) => <Stack key={group.id} direction="row" alignItems="center" spacing={1}><Box sx={{ width: bars ? 110 : 10, flexShrink: 0, color: "text.secondary", fontSize: 12, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap", bgcolor: bars ? "transparent" : colors[index], height: bars ? "auto" : 10, borderRadius: 1 }}>{bars ? group.name : ""}</Box>{bars ? <Box sx={{ flex: 1, height: 13, bgcolor: "action.hover", borderRadius: 1 }}><Box sx={{ width: `${group.count / Math.max(1, groups[0].count) * 100}%`, height: "100%", bgcolor: colors[index], borderRadius: 1 }} /></Box> : <Typography variant="body2" sx={{ flex: 1 }} noWrap>{group.name}</Typography>}<Typography variant="caption" fontWeight={800}>{group.count}</Typography></Stack>)}</Stack>
  </>}</CardContent></Card>;
}
