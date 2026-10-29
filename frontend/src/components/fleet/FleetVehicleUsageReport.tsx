import { useState } from "react";
import DirectionsCarOutlinedIcon from "@mui/icons-material/DirectionsCarOutlined";
import ArrowForwardRoundedIcon from "@mui/icons-material/ArrowForwardRounded";
import { useQuery } from "@tanstack/react-query";
import { Alert, Box, Button, Card, CardContent, Chip, Grid, MenuItem, Pagination, Stack, Table, TableBody, TableCell, TableHead, TableRow, TextField, Typography } from "@mui/material";
import { Link } from "react-router-dom";
import { exportFleetVehicleUsage, getFleetVehicleUsage, type FleetVehicleUsageRow } from "../../api/fleetApi";
import { usePermission } from "../../context/PermissionContext";
import { formatThaiDateTime } from "../../utils/dateFormat";
import { getFleetStatusLabel } from "../../utils/fleetLabels";

const number = (value: number | null) => value == null ? "ยังไม่บันทึก" : value.toLocaleString("th-TH", { maximumFractionDigits: 2 });
export function FleetVehicleUsageReport({ startDate, endDate, status }: { startDate: string; endDate: string; status: string }) {
  const { hasPermission } = usePermission();
  const [vehicle, setVehicle] = useState(""); const [page, setPage] = useState(1);
  const [exportError, setExportError] = useState(false); const [exporting, setExporting] = useState(false);
  const params = new URLSearchParams({ startDate, endDate, status, vehicleId: vehicle, page: String(page), pageSize: "10" });
  const report = useQuery({ queryKey: ["fleet-vehicle-usage", params.toString()], queryFn: () => getFleetVehicleUsage(params) });
  const select = (value: string) => { setVehicle(value); setPage(1); };
  const download = async () => {
    setExportError(false); setExporting(true);
    try {
      const blob = await exportFleetVehicleUsage(params); const url = URL.createObjectURL(blob);
      const anchor = document.createElement("a"); anchor.href = url; anchor.download = `การใช้รถรายคัน-${startDate}-${endDate}.csv`; anchor.click(); URL.revokeObjectURL(url);
    } catch { setExportError(true); } finally { setExporting(false); }
  };
  const details = (row: FleetVehicleUsageRow) => hasPermission("FleetRequest.ViewAll") ? <Button component={Link} to={`/fleet/requests/${row.requestId}`} size="small">รายละเอียด</Button> : null;
  return <Card><CardContent><Stack spacing={2}>
    <Stack direction={{ xs: "column", sm: "row" }} justifyContent="space-between" spacing={2}>
      <Box><Typography variant="h6" fontWeight={900}>การใช้รถรายคัน</Typography><Typography variant="body2" color="text.secondary">ยึดวันออกเดินทางตามคำขอ · รวมทุกคำขอตามสถานะที่เลือก</Typography></Box>
      {hasPermission("FleetReport.Export") && <Button variant="outlined" disabled={exporting || report.isLoading || report.isError} onClick={download}>ส่งออก CSV รายคัน</Button>}
    </Stack>
    <TextField select size="small" label="รถในรายงาน" value={vehicle} onChange={e => select(e.target.value)} SelectProps={{ displayEmpty: true }} InputLabelProps={{ shrink: true }} sx={{ maxWidth: 420 }}>
      <MenuItem value="">ทั้งหมด</MenuItem><MenuItem value="unassigned">ยังไม่จัดรถ</MenuItem>
      {(report.data?.vehicles ?? []).filter(x => x.vehicleId).map(x => <MenuItem key={x.vehicleId} value={x.vehicleId!}>{x.vehicleCode} · {x.registrationNumber}</MenuItem>)}
      {vehicle && vehicle !== "unassigned" && !report.data?.vehicles.some(x => x.vehicleId === vehicle) && <MenuItem value={vehicle}>รถที่เลือก (ไม่มีรายการในช่วงนี้)</MenuItem>}
    </TextField>
    {report.isLoading && <Typography role="status">กำลังโหลดการใช้รถรายคัน...</Typography>}
    {report.isError && <Alert severity="error">โหลดการใช้รถรายคันไม่สำเร็จ <Button onClick={() => report.refetch()}>ลองใหม่</Button></Alert>}
    {exportError && <Alert severity="error">ส่งออก CSV ไม่สำเร็จ กรุณาลองใหม่</Alert>}
    {report.data && <>
      <Grid container spacing={2}>{report.data.vehicles.filter(x => x.vehicleId && (!vehicle || x.vehicleId === vehicle)).map(x => <Grid item xs={12} sm={vehicle ? 12 : 6} lg={vehicle ? 12 : 4} key={x.vehicleId ?? "unassigned"} sx={{ display: "flex" }}><Box sx={{ width: "100%", minWidth: 0, p: { xs: 2, md: 2.5 }, borderRadius: 3, border: "1px solid", borderColor: "divider", borderTop: "4px solid", borderTopColor: "primary.main", bgcolor: "background.paper", boxShadow: "0 5px 20px rgba(26,80,66,.06)", display: "flex", flexDirection: "column", gap: 2 }}>
        <Stack direction="row" spacing={1.5} alignItems="center"><Box sx={{ p: 1.25, bgcolor: "#EDF5F1", color: "primary.main", borderRadius: 2, display: "flex", flexShrink: 0 }}><DirectionsCarOutlinedIcon aria-hidden="true" /></Box><Box minWidth={0}><Typography variant="subtitle1" fontWeight={900} sx={{ overflowWrap: "anywhere" }}>{x.vehicleCode}</Typography><Typography variant="body2" color="text.secondary">{x.registrationNumber}</Typography></Box></Stack>
        <Box sx={{ display: "grid", gridTemplateColumns: vehicle && x.vehicleId ? { xs: "1fr", md: "1fr 1fr" } : "1fr", gap: 2.5, flex: 1 }}>
        <Box minWidth={0}><Typography variant="body2" fontWeight={700} color="text.secondary" sx={{ mb: 1 }}>ตามตัวกรองรายงาน</Typography>
        <Box sx={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 1 }}>
          <SummaryMetric label="จำนวนคำขอ" value={number(x.requestCount)} unit="คำขอ" />
          <SummaryMetric label="ทริปจริง" value={number(x.tripCount)} unit="ทริป" />
          <Box sx={{ gridColumn: "1 / -1" }}><SummaryMetric label="ระยะทางรวม" value={x.distanceKm == null ? "ยังไม่มีระยะทางที่คำนวณได้" : number(x.distanceKm)} unit={x.distanceKm == null ? undefined : "กม."} /></Box>
        </Box>
        <Typography variant="caption" color="text.secondary" display="block" sx={{ mt: 1 }}>คำนวณจากทริปที่จบและเลขไมล์ครบ {x.calculatedTripCount} ทริป</Typography></Box>
        {x.vehicleId && <Box sx={{ minWidth: 0, borderTop: { xs: "1px solid", md: vehicle ? "none" : "1px solid" }, borderLeft: { md: vehicle ? "1px solid" : "none" }, borderColor: "divider", pt: { xs: 2, md: vehicle ? 0 : 2 }, pl: { md: vehicle ? 2.5 : 0 } }}>
          <Typography variant="body2" fontWeight={700} color="text.secondary" sx={{ mb: 1 }}>เลขไมล์จากประวัติทั้งหมดของรถ</Typography>
          <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", sm: vehicle ? "1fr 1fr" : "1fr" }, gap: 1 }}>
          <MileageReading label="เลขไมล์เริ่มต้นที่บันทึก" value={x.firstRecordedMileage} at={x.firstRecordedMileageAt} />
          <MileageReading label="เลขไมล์ล่าสุดที่บันทึก" value={x.latestRecordedMileage} at={x.latestRecordedMileageAt} latest />
          </Box>
        </Box>}
        </Box>
        <Button variant="outlined" endIcon={<ArrowForwardRoundedIcon aria-hidden="true" />} sx={{ alignSelf: "flex-start", mt: "auto" }} onClick={() => select(x.vehicleId ?? "unassigned")}>ดูรายการคันนี้</Button>
      </Box></Grid>)}</Grid>
      {!report.data.items.length && <Typography color="text.secondary">ไม่พบรายการตามตัวกรอง</Typography>}
      {!!report.data.items.length && <>
        <Box sx={{ display: { xs: "none", md: "block" } }}><Table size="small" sx={{ tableLayout: "fixed", "& td, & th": { overflowWrap: "anywhere" } }}><TableHead><TableRow>{["รถ / ทะเบียน", "คำขอ / เดินทาง", "จุดหมาย / สถานะ", "คนขับ", "ไมล์เริ่มต้น", "ไมล์สิ้นสุด", "ระยะทาง (กม.)", ""].map((x, i) => <TableCell key={i}>{x}</TableCell>)}</TableRow></TableHead><TableBody>{report.data.items.map(row => <TableRow key={row.tripId ?? row.requestId}>
          <TableCell>{row.vehicleCode}<br />{row.registrationNumber}</TableCell><TableCell>{row.requestNo}<Typography variant="caption" display="block">{formatThaiDateTime(row.departureAt)} ถึง {formatThaiDateTime(row.expectedReturnAt)}</Typography></TableCell>
          <TableCell>{row.destination}<Typography variant="caption" display="block">{getFleetStatusLabel(row.status)}{row.isAborted ? " · ทริปถูกยุติ" : ""}</Typography></TableCell><TableCell>{row.driver}</TableCell><TableCell>{number(row.startMileage)}</TableCell><TableCell>{number(row.endMileage)}</TableCell><TableCell>{row.distanceKm == null ? "ไม่รวมคำนวณ" : number(row.distanceKm)}</TableCell><TableCell>{details(row)}</TableCell>
        </TableRow>)}</TableBody></Table></Box>
        <Stack spacing={1.5} sx={{ display: { xs: "flex", md: "none" } }}>{report.data.items.map(row => <Box key={row.tripId ?? row.requestId} sx={{ p: 2, border: "1px solid", borderColor: "divider", borderRadius: 2, overflowWrap: "anywhere" }}>
          <Typography fontWeight={900}>{row.requestNo}</Typography><Typography>{row.vehicleCode} · {row.registrationNumber}</Typography><Chip size="small" label={getFleetStatusLabel(row.status)} sx={{ my: 1, height: "auto", "& .MuiChip-label": { whiteSpace: "normal" } }} />
          <Typography variant="body2">เดินทาง: {formatThaiDateTime(row.departureAt)}</Typography><Typography variant="body2">กำหนดกลับ: {formatThaiDateTime(row.expectedReturnAt)}</Typography><Typography>จุดหมาย: {row.destination}</Typography><Typography>คนขับ: {row.driver}</Typography>
          <Typography>ไมล์เริ่มต้น: {number(row.startMileage)}</Typography><Typography>ไมล์สิ้นสุด: {number(row.endMileage)}</Typography><Typography>ระยะทาง: {row.distanceKm == null ? "ไม่รวมคำนวณ" : `${number(row.distanceKm)} กม.`}</Typography>{row.isAborted && <Typography color="warning.main">ทริปถูกยุติ</Typography>}{details(row)}
        </Box>)}</Stack>
        <Typography variant="caption" color="text.secondary">ระยะทางรวมเฉพาะทริปจบจริงที่เลขไมล์ถูกต้อง ไม่รวมทริปยุติหรือคำขอยกเลิก</Typography>
        <Stack alignItems="center" spacing={1}><Typography variant="body2">ทั้งหมด {report.data.totalItems} รายการ</Typography><Pagination page={page} count={Math.max(1, report.data.totalPages)} onChange={(_, value) => setPage(value)} /></Stack>
      </>}
    </>}
  </Stack></CardContent></Card>;
}

function SummaryMetric({ label, value, unit }: { label: string; value: string; unit?: string }) {
  return <Box sx={{ p: 1.5, bgcolor: "#F7F9F8", borderRadius: 2, minWidth: 0 }}><Typography variant="caption" color="text.secondary">{label}</Typography>
    <Typography fontSize={unit ? "1.4rem" : "1rem"} fontWeight={900} sx={{ overflowWrap: "anywhere", fontVariantNumeric: "tabular-nums" }}>{value}{unit && <Typography component="span" variant="body2" color="text.secondary" sx={{ ml: .75 }}>{unit}</Typography>}</Typography>
  </Box>;
}
function MileageReading({ label, value, at, latest = false }: { label: string; value?: number | null; at?: string | null; latest?: boolean }) {
  return <Box sx={{ p: 1.5, borderRadius: 2, border: "1px solid", borderColor: latest ? "#C9DED4" : "divider", bgcolor: latest ? "#EDF6F1" : "background.paper", overflowWrap: "anywhere" }}><Typography variant="caption" color="text.secondary">{label}</Typography>
    <Typography fontWeight={900} color={latest ? "primary.main" : "text.primary"} sx={{ fontSize: value == null ? "1rem" : "1.25rem", fontVariantNumeric: "tabular-nums", mt: .5 }}>{value == null ? "ยังไม่บันทึก" : `${number(value)} กม.`}</Typography>
    {value != null && at && <Typography variant="caption" color="text.secondary" display="block" sx={{ mt: .5 }}>{formatThaiDateTime(at)}</Typography>}
  </Box>;
}
