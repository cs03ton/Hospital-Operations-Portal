/* eslint-disable react-refresh/only-export-components */
import AddCircleOutlineOutlinedIcon from "@mui/icons-material/AddCircleOutlineOutlined";
import VisibilityOutlinedIcon from "@mui/icons-material/VisibilityOutlined";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Grid,
  IconButton,
  MenuItem,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TablePagination,
  TableRow,
  TextField,
  Typography,
} from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { useEffect, useMemo, useState } from "react";
import { Link as RouterLink, useSearchParams } from "react-router-dom";
import { getLeaveCancellationRequests, getLeaveTypes } from "../api/leaveApi";
import { AppDatePicker } from "../components/common/AppDatePicker";
import { PageHeader } from "../components/PageHeader";
import { PermissionGuard, usePermission } from "../context/PermissionContext";
import { getLeaveTypeLabel } from "../utils/leaveLabels";
import { formatDays } from "./LeaveCancellationCreatePage";

type CancellationListFilters = {
  leaveTypeId: string;
  status: string;
  scope: string;
  requesterId: string;
  fromDate: string;
  toDate: string;
};

export function LeaveCancellationListPage() {
  const { hasAnyPermission } = usePermission();
  const [searchParams, setSearchParams] = useSearchParams();
  const canUseDepartmentScope = hasAnyPermission(["LeaveCancellation.ViewDepartment", "LeaveCancellation.ViewAll", "LeaveCancellation.Manage"]);
  const [filters, setFilters] = useState<CancellationListFilters>(() => readFiltersFromSearchParams(searchParams));
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(10);

  useEffect(() => {
    setFilters(readFiltersFromSearchParams(searchParams));
    setPage(0);
  }, [searchParams]);

  const queryFilters = useMemo(
    () => ({
      leaveTypeId: filters.leaveTypeId || undefined,
      status: filters.status || undefined,
      scope: filters.scope || undefined,
      requesterId: filters.requesterId || undefined,
      fromDate: filters.fromDate || undefined,
      toDate: filters.toDate || undefined,
      page: page + 1,
      pageSize,
    }),
    [filters, page, pageSize],
  );

  const { data, isLoading, isError } = useQuery({
    queryKey: ["leave-cancellations", queryFilters],
    queryFn: () => getLeaveCancellationRequests(queryFilters),
  });
  const { data: leaveTypes = [] } = useQuery({ queryKey: ["leave-types"], queryFn: getLeaveTypes });

  const visibleData = useMemo(() => data?.items ?? [], [data?.items]);
  const requesterOptions = useMemo(() => {
    const map = new Map<string, string>();
    visibleData.forEach((item) => {
      map.set(item.requesterUserId, item.requesterName ?? "-");
    });
    return Array.from(map, ([id, fullname]) => ({ id, fullname }));
  }, [visibleData]);

  function updateFilters(nextFilters: CancellationListFilters) {
    setFilters(nextFilters);
    setPage(0);
    setSearchParams(buildSearchParams(nextFilters), { replace: false });
  }

  return (
    <>
      <Stack direction={{ xs: "column", sm: "row" }} justifyContent="space-between" alignItems={{ xs: "stretch", sm: "flex-start" }} spacing={2}>
        <Box sx={{ minWidth: 0 }}>
          <PageHeader title="คำขอยกเลิกใบลา" subtitle="ติดตามคำขอยกเลิกใบลาที่ได้รับอนุมัติแล้วและรอคืนยอดวันลา" />
        </Box>
        <PermissionGuard permission="LeaveCancellation.Create">
          <Button
            component={RouterLink}
            to="/leave/cancellations/create"
            variant="contained"
            size="medium"
            startIcon={<AddCircleOutlineOutlinedIcon />}
            sx={{
              alignSelf: { xs: "stretch", sm: "center" },
              px: 2,
              minWidth: { xs: "auto", sm: 220 },
              whiteSpace: "nowrap",
            }}
          >
            เลือกใบลาที่ต้องการยกเลิก
          </Button>
        </PermissionGuard>
      </Stack>

      {isError && <Alert severity="error">ไม่สามารถโหลดคำขอยกเลิกใบลาได้</Alert>}

      <Card sx={{ mb: 2 }}>
        <CardContent sx={{ py: 2 }}>
          <Grid container spacing={1.5}>
            <Grid item xs={12} sm={6} md={2}>
              <TextField select fullWidth size="small" label="ประเภทลา" value={filters.leaveTypeId} onChange={(event) => updateFilters({ ...filters, leaveTypeId: event.target.value })}>
                <MenuItem value="">ทุกประเภทลา</MenuItem>
                {leaveTypes.map((item) => (
                  <MenuItem key={item.id} value={item.id}>
                    {getLeaveTypeLabel(item.name || item.code)}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={6} md={2}>
              <TextField select fullWidth size="small" label="สถานะคำขอ" value={filters.status} onChange={(event) => updateFilters({ ...filters, status: event.target.value })}>
                <MenuItem value="">ทุกสถานะ</MenuItem>
                <MenuItem value="Draft">แบบร่าง</MenuItem>
                <MenuItem value="Pending">รออนุมัติ</MenuItem>
                <MenuItem value="ReturnedForRevision">ตีกลับรอแก้ไข</MenuItem>
                <MenuItem value="Approved">อนุมัติยกเลิกแล้ว</MenuItem>
                <MenuItem value="Rejected">ไม่อนุมัติ</MenuItem>
                <MenuItem value="Cancelled">ยกเลิก</MenuItem>
              </TextField>
            </Grid>
            <Grid item xs={12} sm={6} md={2}>
              <TextField select fullWidth size="small" label="ขอบเขตรายการ" value={filters.scope} onChange={(event) => updateFilters({ ...filters, scope: event.target.value })}>
                <MenuItem value="">ตามสิทธิ์ของฉัน</MenuItem>
                <MenuItem value="mine">คำขอของฉัน</MenuItem>
                <MenuItem value="pending-approval">รอฉันอนุมัติ</MenuItem>
                {canUseDepartmentScope && <MenuItem value="department">คำขอของหน่วยงาน</MenuItem>}
                {!canUseDepartmentScope && filters.scope === "department" && <MenuItem value="department" disabled>คำขอของหน่วยงาน</MenuItem>}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={6} md={2}>
              <TextField select fullWidth size="small" label="ผู้ขอลา" value={filters.requesterId} onChange={(event) => updateFilters({ ...filters, requesterId: event.target.value })}>
                <MenuItem value="">ทุกคน</MenuItem>
                {filters.requesterId && !requesterOptions.some((item) => item.id === filters.requesterId) && (
                  <MenuItem value={filters.requesterId} disabled>
                    ผู้ขอที่เลือก
                  </MenuItem>
                )}
                {requesterOptions.map((item) => (
                  <MenuItem key={item.id} value={item.id}>
                    {item.fullname}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={6} md={2}>
              <AppDatePicker label="ตั้งแต่วันที่" value={filters.fromDate} onChange={(value) => updateFilters({ ...filters, fromDate: value })} />
            </Grid>
            <Grid item xs={12} sm={6} md={2}>
              <AppDatePicker label="ถึงวันที่" value={filters.toDate} onChange={(value) => updateFilters({ ...filters, toDate: value })} />
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      <Card>
        <CardContent>
          <Stack sx={{ mb: 2 }}>
            <Typography variant="h6" color="primary" fontWeight={700}>
              รายการคำขอยกเลิกใบลา
            </Typography>
            <Typography variant="body2" color="text.secondary">
              ระบบจะคืนยอดวันลาเมื่อคำขอยกเลิกอนุมัติครบทุกขั้นเท่านั้น
            </Typography>
          </Stack>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>เลขที่คำขอยกเลิก</TableCell>
                <TableCell>ใบลาเดิม</TableCell>
                <TableCell>ประเภทลา</TableCell>
                <TableCell>จำนวนวันคืนยอด</TableCell>
                <TableCell>สถานะ</TableCell>
                <TableCell>ผู้อนุมัติปัจจุบัน</TableCell>
                <TableCell align="right">จัดการ</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {isLoading ? (
                <TableRow>
                  <TableCell colSpan={7}>กำลังโหลดคำขอยกเลิกใบลา...</TableCell>
                </TableRow>
              ) : visibleData.length ? (
                visibleData.map((item) => (
                  <TableRow key={item.id} hover>
                    <TableCell>{item.cancellationRequestNumber}</TableCell>
                    <TableCell>{item.originalRequestNumber ?? "-"}</TableCell>
                    <TableCell>{item.leaveTypeName ?? "-"}</TableCell>
                    <TableCell>{formatDays(item.originalLeaveDays)}</TableCell>
                    <TableCell><Chip size="small" label={getCancellationStatusLabel(item.status)} color={getCancellationStatusColor(item.status)} /></TableCell>
                    <TableCell>{item.currentApproverName ?? "-"}</TableCell>
                    <TableCell align="right">
                      <IconButton component={RouterLink} to={`/leave/cancellations/${item.id}`} aria-label="ดูรายละเอียดคำขอยกเลิกใบลา">
                        <VisibilityOutlinedIcon />
                      </IconButton>
                    </TableCell>
                  </TableRow>
                ))
              ) : (
                <TableRow>
                  <TableCell colSpan={7} align="center">ยังไม่มีคำขอยกเลิกใบลา</TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
          <TablePagination
            component="div"
            count={data?.totalItems ?? 0}
            page={page}
            rowsPerPage={pageSize}
            rowsPerPageOptions={[10, 20, 50]}
            labelRowsPerPage="จำนวนรายการต่อหน้า"
            labelDisplayedRows={({ from, to, count }) => `${from}-${to} จาก ${count !== -1 ? count : `มากกว่า ${to}`}`}
            getItemAriaLabel={(type) => {
              if (type === "first") return "ไปหน้าแรก";
              if (type === "last") return "ไปหน้าสุดท้าย";
              if (type === "next") return "ไปหน้าถัดไป";
              return "ไปหน้าก่อนหน้า";
            }}
            onPageChange={(_, nextPage) => setPage(nextPage)}
            onRowsPerPageChange={(event) => {
              setPageSize(Number(event.target.value));
              setPage(0);
            }}
          />
        </CardContent>
      </Card>
    </>
  );
}

export function getCancellationStatusLabel(status: string) {
  return {
    Draft: "แบบร่าง",
    Pending: "รออนุมัติ",
    Approved: "อนุมัติยกเลิกใบลาแล้ว",
    Rejected: "ไม่อนุมัติ",
    Cancelled: "ยกเลิก",
    ReturnedForRevision: "ตีกลับรอแก้ไข",
  }[status] ?? status;
}

function getCancellationStatusColor(status: string): "default" | "primary" | "success" | "error" | "warning" {
  if (status === "Approved") return "success";
  if (status === "Rejected") return "error";
  if (status === "Pending") return "warning";
  if (status === "ReturnedForRevision") return "primary";
  return "default";
}

function readFiltersFromSearchParams(searchParams: URLSearchParams): CancellationListFilters {
  return {
    leaveTypeId: searchParams.get("leaveTypeId") ?? "",
    status: normalizeStatusParam(searchParams.get("status")),
    scope: normalizeScopeParam(searchParams.get("scope")),
    requesterId: searchParams.get("requesterId") ?? searchParams.get("userId") ?? "",
    fromDate: searchParams.get("fromDate") ?? "",
    toDate: searchParams.get("toDate") ?? "",
  };
}

function buildSearchParams(filters: CancellationListFilters) {
  const next = new URLSearchParams();
  setIfPresent(next, "leaveTypeId", filters.leaveTypeId);
  setIfPresent(next, "status", toStatusQueryValue(filters.status));
  setIfPresent(next, "scope", filters.scope);
  setIfPresent(next, "requesterId", filters.requesterId);
  setIfPresent(next, "fromDate", filters.fromDate);
  setIfPresent(next, "toDate", filters.toDate);
  return next;
}

function setIfPresent(params: URLSearchParams, key: string, value: string) {
  if (value) {
    params.set(key, value);
  }
}

function normalizeStatusParam(value: string | null) {
  if (!value) return "";
  switch (value.toLowerCase()) {
    case "draft":
      return "Draft";
    case "pending":
      return "Pending";
    case "returned":
    case "returnedforrevision":
    case "returned_for_revision":
      return "ReturnedForRevision";
    case "approved":
      return "Approved";
    case "rejected":
      return "Rejected";
    case "cancelled":
    case "canceled":
      return "Cancelled";
    default:
      return value;
  }
}

function toStatusQueryValue(value: string) {
  switch (value) {
    case "Draft":
      return "draft";
    case "Pending":
      return "pending";
    case "ReturnedForRevision":
      return "returned";
    case "Approved":
      return "approved";
    case "Rejected":
      return "rejected";
    case "Cancelled":
      return "cancelled";
    default:
      return value;
  }
}

function normalizeScopeParam(value: string | null) {
  if (!value) return "";
  const normalized = value.toLowerCase();
  return ["mine", "department", "pending-approval"].includes(normalized) ? normalized : "";
}
