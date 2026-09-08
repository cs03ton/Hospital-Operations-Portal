import AddCircleOutlineOutlinedIcon from "@mui/icons-material/AddCircleOutlineOutlined";
import CheckCircleOutlineOutlinedIcon from "@mui/icons-material/CheckCircleOutlineOutlined";
import RefreshOutlinedIcon from "@mui/icons-material/RefreshOutlined";
import RateReviewOutlinedIcon from "@mui/icons-material/RateReviewOutlined";
import SearchOutlinedIcon from "@mui/icons-material/SearchOutlined";
import VisibilityOutlinedIcon from "@mui/icons-material/VisibilityOutlined";
import { Alert, Box, Button, Card, CardContent, Chip, Grid, IconButton, InputAdornment, MenuItem, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, TextField, Typography } from "@mui/material";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { getMyFleetFeedbackTrips, getMyFleetRequestsPaged, type FleetRequestQuery } from "../api/fleetApi";
import { AppDatePicker } from "../components/common/AppDatePicker";
import { EmptyState } from "../components/common/EmptyState";
import { ListPagination } from "../components/common/ListPagination";
import { LoadingState } from "../components/common/LoadingState";
import { StatusBadge } from "../components/common/StatusBadge";
import { PageHeader } from "../components/PageHeader";
import { useAuth } from "../context/AuthContext";
import { formatThaiDateTime } from "../utils/dateFormat";

const statuses = ["DRAFT", "PENDING_DISPATCH", "PENDING_ADMIN_REVIEW", "PENDING_DIRECTOR_APPROVAL", "APPROVED", "DRIVER_ACKNOWLEDGED", "IN_PROGRESS", "COMPLETED", "RETURNED", "REJECTED", "CANCELLED"];

export function FleetRequestsPage() {
  const { user } = useAuth();
  const canViewAll = user?.permissions.includes("FleetRequest.ViewAll") ?? false;
  const [searchParams, setSearchParams] = useSearchParams();
  const [searchDraft, setSearchDraft] = useState(searchParams.get("search") ?? "");
  const page = positiveNumber(searchParams.get("page"), 1);
  const pageSize = positiveNumber(searchParams.get("pageSize"), 10);
  const status = searchParams.get("status") ?? "";
  const dateFrom = searchParams.get("dateFrom") ?? "";
  const dateTo = searchParams.get("dateTo") ?? "";
  const search = searchParams.get("search") ?? "";

  const updateParams = useCallback((values: Record<string, string>) => {
    const next = new URLSearchParams(searchParams);
    Object.entries(values).forEach(([key, value]) => value ? next.set(key, value) : next.delete(key));
    setSearchParams(next, { replace: false });
  }, [searchParams, setSearchParams]);

  useEffect(() => setSearchDraft(search), [search]);
  useEffect(() => {
    const timer = window.setTimeout(() => {
      if (searchDraft !== search) updateParams({ search: searchDraft, page: "1" });
    }, 350);
    return () => window.clearTimeout(timer);
  }, [searchDraft, search, updateParams]);

  const query = useMemo<FleetRequestQuery>(() => ({ page, pageSize, search: search || undefined, status: status || undefined, dateFrom: dateFrom || undefined, dateTo: dateTo || undefined, sortBy: "createdAt", sortDirection: "desc" }), [page, pageSize, search, status, dateFrom, dateTo]);
  const requests = useQuery({ queryKey: ["fleet", "mine", "paged", query], queryFn: () => getMyFleetRequestsPaged(query), placeholderData: keepPreviousData });
  const feedbackTrips = useQuery({
    queryKey: ["fleet", "my-feedback-trips", user?.id],
    queryFn: getMyFleetFeedbackTrips,
    enabled: user?.permissions.includes("FleetFeedback.ViewOwn") ?? false,
    staleTime: 60_000,
    retry: false,
  });
  const feedbackByRequestNo = useMemo(
    () => new Map((feedbackTrips.data ?? []).map((trip) => [trip.requestNo, trip])),
    [feedbackTrips.data],
  );
  const filtered = Boolean(search || status || dateFrom || dateTo);

  useEffect(() => {
    if (requests.data && page > Math.max(1, requests.data.totalPages)) updateParams({ page: String(Math.max(1, requests.data.totalPages)) });
  }, [page, requests.data, updateParams]);

  function clearFilters() {
    setSearchDraft("");
    const next = new URLSearchParams();
    next.set("page", "1");
    next.set("pageSize", String(pageSize));
    setSearchParams(next);
  }

  return (
    <Stack spacing={2.5}>
      <Stack direction={{ xs: "column", sm: "row" }} justifyContent="space-between" alignItems={{ xs: "stretch", sm: "flex-start" }} spacing={2}>
        <Box sx={{ minWidth: 0, flex: 1 }}><PageHeader title={canViewAll ? "คำขอใช้รถทั้งหมด" : "คำขอใช้รถของฉัน"} subtitle={canViewAll ? "ดูและติดตามคำขอใช้รถของผู้ใช้ทั้งหมด" : "สร้าง ติดตามสถานะ และดูรายละเอียดคำขอใช้รถราชการ"} /></Box>
        <Button component={Link} to="/fleet/requests/create" variant="contained" startIcon={<AddCircleOutlineOutlinedIcon />} sx={{ alignSelf: { xs: "stretch", sm: "center" }, minWidth: 148 }}>สร้างคำขอ</Button>
      </Stack>

      <Card><CardContent sx={{ py: 2 }}><Grid container spacing={1.5}>
        <Grid item xs={12} md={4}><TextField fullWidth size="small" label="ค้นหา" placeholder="เลขที่คำขอ ภารกิจ หรือปลายทาง" value={searchDraft} onChange={(event) => setSearchDraft(event.target.value)} InputProps={{ startAdornment: <InputAdornment position="start"><SearchOutlinedIcon fontSize="small" /></InputAdornment> }} /></Grid>
        <Grid item xs={12} sm={6} md={2}><TextField select fullWidth size="small" label="สถานะ" value={status} onChange={(event) => updateParams({ status: event.target.value, page: "1" })}><MenuItem value="">ทุกสถานะ</MenuItem>{statuses.map((item) => <MenuItem key={item} value={item}><StatusBadge domain="fleet" status={item} /></MenuItem>)}</TextField></Grid>
        <Grid item xs={12} sm={6} md={2}><AppDatePicker label="เดินทางตั้งแต่" value={dateFrom} onChange={(value) => updateParams({ dateFrom: value, page: "1" })} /></Grid>
        <Grid item xs={12} sm={6} md={2}><AppDatePicker label="เดินทางถึง" value={dateTo} onChange={(value) => updateParams({ dateTo: value, page: "1" })} /></Grid>
        <Grid item xs={12} sm={6} md={2}><Button fullWidth variant="outlined" onClick={clearFilters} disabled={!filtered} sx={{ height: 40 }}>ล้างตัวกรอง</Button></Grid>
      </Grid></CardContent></Card>

      <Card><CardContent>
        <Stack direction={{ xs: "column", sm: "row" }} justifyContent="space-between" alignItems={{ xs: "stretch", sm: "center" }} spacing={1} sx={{ mb: 1.5 }}>
          <Typography fontWeight={900}>รายการคำขอ <Typography component="span" color="text.secondary" fontWeight={500}>({requests.data?.totalItems ?? 0} รายการ)</Typography></Typography>
          <Button size="small" startIcon={<RefreshOutlinedIcon />} onClick={() => void requests.refetch()} disabled={requests.isFetching}>รีเฟรช</Button>
        </Stack>
        {requests.isError && <Alert severity="error" action={<Button color="inherit" onClick={() => void requests.refetch()}>ลองใหม่</Button>}>โหลดรายการคำขอไม่สำเร็จ กรุณาลองใหม่อีกครั้ง</Alert>}
        {requests.isLoading && <LoadingState message="กำลังโหลดคำขอใช้รถ..." />}
        {!requests.isLoading && !requests.isError && (requests.data?.items.length ?? 0) === 0 && <EmptyState title={filtered ? "ไม่พบคำขอ" : "ยังไม่มีคำขอใช้รถ"} description={filtered ? "ลองเปลี่ยนหรือล้างตัวกรองเพื่อค้นหาอีกครั้ง" : "เริ่มต้นสร้างคำขอใช้รถรายการแรกของคุณ"} action={!filtered ? <Button component={Link} to="/fleet/requests/create">สร้างคำขอ</Button> : <Button onClick={clearFilters}>ล้างตัวกรอง</Button>} />}
        {(requests.data?.items.length ?? 0) > 0 && <>
          <TableContainer sx={{ overflowX: "auto" }}><Table size="small" aria-label="รายการคำขอใช้รถ"><TableHead><TableRow><TableCell>เลขที่คำขอ</TableCell>{canViewAll && <TableCell>ผู้ขอ</TableCell>}<TableCell>ภารกิจและปลายทาง</TableCell><TableCell>วันเวลาเดินทาง</TableCell><TableCell align="center">ผู้ร่วมเดินทาง</TableCell><TableCell>สถานะ</TableCell><TableCell align="right">จัดการ</TableCell></TableRow></TableHead><TableBody>{requests.data!.items.map((item) => {
            const feedbackTrip = feedbackByRequestNo.get(item.requestNo);
            return <TableRow key={item.id} hover><TableCell sx={{ fontWeight: 800, whiteSpace: "nowrap" }}>{item.requestNo}</TableCell>{canViewAll && <TableCell>{item.requesterName}</TableCell>}<TableCell><Typography variant="body2" fontWeight={700}>{item.purpose}</Typography><Typography variant="body2" color="text.secondary">{item.destination}</Typography></TableCell><TableCell sx={{ whiteSpace: "nowrap" }}>{formatThaiDateTime(item.departureAt)}</TableCell><TableCell align="center">{item.passengerCount}</TableCell><TableCell><StatusBadge domain="fleet" status={item.status} /></TableCell><TableCell align="right"><Stack direction="row" spacing={0.5} justifyContent="flex-end" alignItems="center" sx={{ whiteSpace: "nowrap" }}>{feedbackTrip?.feedbackStatus === "AVAILABLE" && <Button component={Link} to={`/fleet/trips/${feedbackTrip.tripId}/feedback`} size="small" variant="contained" color="success" startIcon={<RateReviewOutlinedIcon />} aria-label={`ให้ Feedback ${item.requestNo}`}>ให้ Feedback</Button>}{feedbackTrip?.feedbackStatus === "SUBMITTED" && <Chip size="small" color="success" variant="outlined" icon={<CheckCircleOutlineOutlinedIcon />} label="ส่ง Feedback แล้ว" aria-label={`ส่ง Feedback แล้ว ${item.requestNo}`} />}<IconButton component={Link} to={`/fleet/requests/${item.id}`} aria-label={`ดูรายละเอียด ${item.requestNo}`}><VisibilityOutlinedIcon /></IconButton></Stack></TableCell></TableRow>;
          })}</TableBody></Table></TableContainer>
          <ListPagination page={page} pageSize={pageSize} totalItems={requests.data?.totalItems ?? 0} onPageChange={(next) => updateParams({ page: String(next) })} onPageSizeChange={(next) => updateParams({ pageSize: String(next), page: "1" })} pageSizeOptions={[10, 20, 50]} disabled={requests.isFetching} />
        </>}
      </CardContent></Card>
    </Stack>
  );
}

function positiveNumber(value: string | null, fallback: number) {
  const parsed = Number(value);
  return Number.isInteger(parsed) && parsed > 0 ? parsed : fallback;
}
