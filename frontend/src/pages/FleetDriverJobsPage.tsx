import AccessTimeOutlinedIcon from "@mui/icons-material/AccessTimeOutlined";
import CheckCircleOutlineOutlinedIcon from "@mui/icons-material/CheckCircleOutlineOutlined";
import DirectionsCarOutlinedIcon from "@mui/icons-material/DirectionsCarOutlined";
import LocationOnOutlinedIcon from "@mui/icons-material/LocationOnOutlined";
import PeopleOutlineOutlinedIcon from "@mui/icons-material/PeopleOutlineOutlined";
import RefreshOutlinedIcon from "@mui/icons-material/RefreshOutlined";
import SearchOutlinedIcon from "@mui/icons-material/SearchOutlined";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Grid,
  InputAdornment,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { alpha } from "@mui/material/styles";
import { useQueryClient } from "@tanstack/react-query";
import { useEffect, useMemo, useState } from "react";
import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import {
  driverJobAction,
  FLEET_DASHBOARD_QUERY_KEY,
  getDriverJobs,
} from "../api/fleetApi";
import { EmptyState } from "../components/common/EmptyState";
import { ListPagination } from "../components/common/ListPagination";
import { LoadingState } from "../components/common/LoadingState";
import { PageHeader } from "../components/PageHeader";
import { formatThaiDateTime } from "../utils/dateFormat";
import { getFleetStatusLabel } from "../utils/fleetLabels";

type DriverJob = Awaited<ReturnType<typeof getDriverJobs>>[number];

const priorityLabels: Record<string, string> = {
  EMERGENCY: "ฉุกเฉิน",
  URGENT: "เร่งด่วน",
  NORMAL: "ปกติ",
};

export function FleetDriverJobsPage() {
  const queryClient = useQueryClient();
  const [rows, setRows] = useState<Awaited<ReturnType<typeof getDriverJobs>>>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState<string | null>(null);
  const [keyword, setKeyword] = useState("");
  const [status, setStatus] = useState("");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  const load = async () => {
    setLoading(true);
    setError("");
    try {
      setRows(await getDriverJobs());
    } catch {
      setError("ไม่สามารถโหลดงานได้ กรุณาตรวจสอบเครือข่ายและลองใหม่อีกครั้ง");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const statusOptions = useMemo(
    () => Array.from(new Set(rows.map((item) => item.status))).sort(),
    [rows],
  );
  const filteredRows = useMemo(() => {
    const normalizedKeyword = keyword.trim().toLowerCase();
    return rows.filter((item) => {
      const searchable = [item.requestNo, item.destination, item.origin, item.vehicle, item.status, getFleetStatusLabel(item.status)]
        .filter(Boolean)
        .join(" ")
        .toLowerCase();
      return (!normalizedKeyword || searchable.includes(normalizedKeyword)) && (!status || item.status === status);
    });
  }, [keyword, rows, status]);
  const visibleRows = filteredRows.slice((page - 1) * pageSize, page * pageSize);

  useEffect(() => {
    const lastPage = Math.max(1, Math.ceil(filteredRows.length / pageSize));
    if (page > lastPage) setPage(lastPage);
  }, [filteredRows.length, page, pageSize]);

  const updateKeyword = (value: string) => {
    setKeyword(value);
    setPage(1);
  };
  const updateStatus = (value: string) => {
    setStatus(value);
    setPage(1);
  };

  const act = async (id: string, action: "accept" | "decline", token: string, reason?: string) => {
    if (busy) return;
    setBusy(id);
    setError("");
    try {
      await driverJobAction(id, action, token, reason);
      await queryClient.invalidateQueries({ queryKey: FLEET_DASHBOARD_QUERY_KEY });
      await load();
    } catch {
      setError("ไม่ทราบว่าส่งคำสั่งสำเร็จหรือไม่ กรุณารีเฟรชสถานะล่าสุดก่อนลองอีกครั้ง");
    } finally {
      setBusy(null);
    }
  };

  return (
    <Box sx={{ pb: "calc(72px + env(safe-area-inset-bottom))" }}>
      <PageHeader title="งานขับรถของฉัน" subtitle="ตรวจสอบงาน ตอบรับ และติดตามสถานะการเดินทางที่ได้รับมอบหมาย" />

      {error && (
        <Alert severity="error" sx={{ mb: 2 }} action={<Button onClick={() => void load()}>โหลดล่าสุด</Button>}>
          {error}
        </Alert>
      )}

      <Card sx={{ mb: 2 }}>
        <CardContent sx={{ py: 2 }}>
          <Grid container spacing={1.5} alignItems="center">
            <Grid item xs={12} md={7}>
              <TextField
                fullWidth
                size="small"
                label="ค้นหางานขับรถ"
                placeholder="เลขคำขอ ปลายทาง หรือรถ"
                value={keyword}
                onChange={(event) => updateKeyword(event.target.value)}
                InputProps={{
                  startAdornment: <InputAdornment position="start"><SearchOutlinedIcon fontSize="small" /></InputAdornment>,
                }}
              />
            </Grid>
            <Grid item xs={12} sm={7} md={3}>
              <TextField select fullWidth size="small" label="สถานะ" value={status} onChange={(event) => updateStatus(event.target.value)}>
                <MenuItem value="">ทุกสถานะ</MenuItem>
                {statusOptions.map((value) => <MenuItem key={value} value={value}>{getFleetStatusLabel(value)}</MenuItem>)}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={5} md={2}>
              <Button fullWidth variant="outlined" startIcon={<RefreshOutlinedIcon />} disabled={loading} onClick={() => void load()}>
                รีเฟรช
              </Button>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      <Card>
        <CardContent>
          <Stack direction={{ xs: "column", sm: "row" }} justifyContent="space-between" spacing={1} sx={{ mb: 2 }}>
            <Box>
              <Typography variant="h6" color="primary" fontWeight={800}>รายการงานที่ได้รับมอบหมาย</Typography>
              <Typography variant="body2" color="text.secondary">ทั้งหมด {filteredRows.length.toLocaleString("th-TH")} รายการ</Typography>
            </Box>
            {(keyword || status) && <Button onClick={() => { updateKeyword(""); updateStatus(""); }}>ล้างตัวกรอง</Button>}
          </Stack>

          {loading ? (
            <LoadingState message="กำลังโหลดงานขับรถ..." />
          ) : visibleRows.length ? (
            <Stack spacing={1.5}>
              {visibleRows.map((job) => <DriverJobCard key={job.id} job={job} busy={busy === job.id} onAction={act} />)}
              <ListPagination
                page={page}
                pageSize={pageSize}
                totalItems={filteredRows.length}
                pageSizeOptions={[5, 10, 20, 50]}
                disabled={loading || Boolean(busy)}
                onPageChange={setPage}
                onPageSizeChange={(value) => { setPageSize(value); setPage(1); }}
              />
            </Stack>
          ) : (
            <EmptyState
              icon={DirectionsCarOutlinedIcon}
              title={rows.length ? "ไม่พบงานตามเงื่อนไข" : "ยังไม่มีงานขับรถ"}
              description={rows.length ? "ลองเปลี่ยนคำค้นหาหรือตัวกรองสถานะ" : "งานที่ได้รับมอบหมายจะแสดงในหน้านี้"}
            />
          )}
        </CardContent>
      </Card>
    </Box>
  );
}

function DriverJobCard({ job, busy, onAction }: { job: DriverJob; busy: boolean; onAction: (id: string, action: "accept" | "decline", token: string, reason?: string) => Promise<void> }) {
  const priorityColor = job.priority === "EMERGENCY" ? "error" : job.priority === "URGENT" ? "warning" : "default";
  const isCompleted = job.status === "COMPLETED";
  const statusColor = isCompleted ? "success" : job.status === "IN_PROGRESS" ? "info" : job.status === "CANCELLED" || job.status === "REJECTED" || job.status === "ABORTED" ? "error" : "warning";
  return (
    <Card
      variant="outlined"
      sx={(theme) => ({
        position: "relative",
        overflow: "hidden",
        boxShadow: isCompleted ? `0 8px 24px ${alpha(theme.palette.success.main, 0.08)}` : "none",
        borderLeft: "5px solid",
        borderLeftColor: isCompleted ? "success.main" : job.priority === "EMERGENCY" ? "error.main" : job.priority === "URGENT" ? "warning.main" : "primary.main",
        background: isCompleted
          ? `linear-gradient(135deg, ${alpha(theme.palette.success.main, 0.055)} 0%, ${theme.palette.background.paper} 45%)`
          : job.priority === "EMERGENCY" ? alpha(theme.palette.error.main, 0.025) : "background.paper",
        transition: theme.transitions.create(["transform", "box-shadow", "border-color"], { duration: theme.transitions.duration.shorter }),
        "&:hover": {
          transform: "translateY(-3px)",
          boxShadow: `0 14px 32px ${alpha(isCompleted ? theme.palette.success.main : theme.palette.primary.main, 0.14)}`,
        },
        "&::after": isCompleted ? {
          content: '""',
          position: "absolute",
          width: 120,
          height: 120,
          borderRadius: "50%",
          right: -58,
          top: -62,
          bgcolor: alpha(theme.palette.success.main, 0.08),
        } : undefined,
      })}
    >
      <CardContent sx={{ p: { xs: 1.5, md: 2 }, "&:last-child": { pb: { xs: 1.5, md: 2 } } }}>
        <Stack spacing={1.5}>
          <Stack direction={{ xs: "column", sm: "row" }} justifyContent="space-between" spacing={1}>
            <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
              <Typography variant="h6" color="primary" fontWeight={900}>{job.requestNo}</Typography>
              <Chip size="small" color={priorityColor} label={priorityLabels[job.priority] ?? job.priority} />
              <Chip
                size="small"
                color={statusColor}
                icon={isCompleted ? <CheckCircleOutlineOutlinedIcon /> : undefined}
                label={getFleetStatusLabel(job.status)}
                sx={{ fontWeight: 800, px: 0.25 }}
              />
            </Stack>
            <Stack direction="row" spacing={0.75} alignItems="center" color="text.secondary">
              <AccessTimeOutlinedIcon fontSize="small" />
              <Typography variant="body2" fontWeight={700} whiteSpace="nowrap">{formatThaiDateTime(job.departureAt)}</Typography>
            </Stack>
          </Stack>

          <Grid container spacing={1.25}>
            <Grid item xs={12} md={5}><JobDetail icon={<LocationOnOutlinedIcon />} label="เส้นทาง" value={`${job.origin || "โรงพยาบาลนาหมื่น"} → ${job.destination}`} /></Grid>
            <Grid item xs={6} md={3}><JobDetail icon={<DirectionsCarOutlinedIcon />} label="รถที่ได้รับมอบหมาย" value={job.vehicle || "ยังไม่ระบุ"} /></Grid>
            <Grid item xs={6} md={2}><JobDetail icon={<PeopleOutlineOutlinedIcon />} label="ผู้ร่วมเดินทาง" value={`${job.passengerCount.toLocaleString("th-TH")} คน`} /></Grid>
            <Grid item xs={12} md={2}><JobDetail icon={<AccessTimeOutlinedIcon />} label="คาดว่าจะกลับ" value={formatThaiDateTime(job.expectedReturnAt)} /></Grid>
          </Grid>

          {job.requiredCapabilities?.length > 0 && (
            <Stack direction="row" spacing={0.75} alignItems="center" flexWrap="wrap" useFlexGap>
              <Typography variant="caption" color="text.secondary">ความต้องการ:</Typography>
              {job.requiredCapabilities.map((capability) => <Chip key={capability} size="small" variant="outlined" label={capability} />)}
            </Stack>
          )}

          {(job.status === "READY" || job.status === "IN_PROGRESS") && (
            <Button component={Link} to={`/fleet/driver/trips/${job.id}/action`} variant="contained" sx={{ alignSelf: { sm: "flex-end" }, minHeight: 44 }}>
              {job.status === "READY" ? "เริ่มเดินทาง" : "บันทึกและจบทริป"}
            </Button>
          )}
          {job.status === "PENDING_DRIVER_ACK" && (
            <Stack direction={{ xs: "column", sm: "row" }} spacing={1} justifyContent="flex-end">
              <Button variant="contained" disabled={busy} sx={{ minHeight: 44 }} onClick={() => void onAction(job.id, "accept", job.concurrencyToken)}>ตอบรับงาน</Button>
              <Button color="error" variant="outlined" disabled={busy} sx={{ minHeight: 44 }} onClick={() => {
                const reason = window.prompt("ระบุเหตุผลที่ปฏิเสธ");
                if (reason?.trim()) void onAction(job.id, "decline", job.concurrencyToken, reason.trim());
              }}>ปฏิเสธ</Button>
            </Stack>
          )}
        </Stack>
      </CardContent>
    </Card>
  );
}

function JobDetail({ icon, label, value }: { icon: ReactNode; label: string; value: string }) {
  return (
    <Stack
      direction="row"
      spacing={1}
      alignItems="flex-start"
      sx={(theme) => ({
        height: "100%",
        p: 1.15,
        borderRadius: 2,
        bgcolor: alpha(theme.palette.primary.main, 0.025),
        "& .MuiSvgIcon-root": { color: "primary.main", fontSize: 20, mt: 0.15 },
      })}
    >
      {icon}
      <Box minWidth={0}>
        <Typography variant="caption" color="text.secondary" fontWeight={700}>{label}</Typography>
        <Typography variant="body2" fontWeight={800}>{value}</Typography>
      </Box>
    </Stack>
  );
}
