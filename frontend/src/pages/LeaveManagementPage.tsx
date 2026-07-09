import AddCircleOutlineOutlinedIcon from "@mui/icons-material/AddCircleOutlineOutlined";
import VisibilityOutlinedIcon from "@mui/icons-material/VisibilityOutlined";
import { Box, Button, Card, CardContent, Chip, Grid, IconButton, MenuItem, Stack, Table, TableBody, TableCell, TableHead, TablePagination, TableRow, TextField } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import { getDepartments } from "../api/adminApi";
import { getLeaveRequestsPaged, getLeaveTypes, type LeaveRequestQuery } from "../api/leaveApi";
import { AppDatePicker } from "../components/common/AppDatePicker";
import { PageHeader } from "../components/PageHeader";
import { useAuth } from "../context/AuthContext";
import { PermissionGuard, usePermission } from "../context/PermissionContext";
import { formatThaiDate } from "../utils/dateFormat";
import { getLeaveStatusColor, getLeaveStatusLabel, getLeaveTypeLabel, getLeaveTypeWithDurationLabel } from "../utils/leaveLabels";
import { getLeaveRequestCode, getTrackingStatusLabel, getTrackingStepLabel } from "../utils/leaveTrackingLabels";

export function LeaveManagementPage() {
  const { user } = useAuth();
  const { hasAnyPermission } = usePermission();
  const canRequestLeave = user?.role !== "Admin" && user?.role !== "SuperAdmin";
  const canFilterDepartments = hasAnyPermission(["DepartmentManagement.View", "LeaveRequest.ViewAll"]);
  const [filters, setFilters] = useState({
    leaveTypeId: "",
    status: "",
    departmentId: "",
    fromDate: "",
    toDate: "",
    userId: "",
  });
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(10);
  const queryFilters = useMemo<LeaveRequestQuery>(
    () => ({
      leaveTypeId: filters.leaveTypeId || undefined,
      status: filters.status || undefined,
      departmentId: canFilterDepartments ? filters.departmentId || undefined : undefined,
      fromDate: filters.fromDate || undefined,
      toDate: filters.toDate || undefined,
      userId: filters.userId || undefined,
      page: page + 1,
      pageSize,
    }),
    [canFilterDepartments, filters, page, pageSize],
  );
  const { data, isLoading } = useQuery({ queryKey: ["leave-requests", "paged", queryFilters], queryFn: () => getLeaveRequestsPaged(queryFilters) });
  const { data: leaveTypes = [] } = useQuery({ queryKey: ["leave-types"], queryFn: getLeaveTypes });
  const { data: departments = [] } = useQuery({
    queryKey: ["departments"],
    queryFn: getDepartments,
    enabled: canFilterDepartments,
    retry: false,
  });
  const requesterOptions = useMemo(() => {
    const map = new Map<string, string>();
    (data?.items ?? []).forEach((item) => {
      map.set(item.userId, item.fullname ?? "-");
    });
    return Array.from(map, ([id, fullname]) => ({ id, fullname }));
  }, [data]);
  const visibleData = data?.items ?? [];

  function updateFilters(nextFilters: typeof filters) {
    setFilters(nextFilters);
    setPage(0);
  }

  return (
    <>
      <Stack direction={{ xs: "column", sm: "row" }} justifyContent="space-between" alignItems={{ xs: "stretch", sm: "flex-start" }} spacing={2}>
        <Box sx={{ minWidth: 0 }}>
          <PageHeader title="รายการคำขอลา" subtitle="สร้างคำขอลา ติดตามสถานะ และดำเนินการอนุมัติ" />
        </Box>
        {canRequestLeave && (
          <PermissionGuard permission="LeaveRequest.Create">
            <Button
              component={RouterLink}
              to="/leave/create"
              variant="contained"
              size="medium"
              startIcon={<AddCircleOutlineOutlinedIcon />}
              sx={{
                alignSelf: { xs: "stretch", sm: "center" },
                px: 2,
                minWidth: { xs: "auto", sm: 148 },
                whiteSpace: "nowrap",
              }}
            >
              เพิ่มคำขอลา
            </Button>
          </PermissionGuard>
        )}
      </Stack>
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
                <MenuItem value="Approved">อนุมัติแล้ว</MenuItem>
                <MenuItem value="Rejected">ไม่อนุมัติ</MenuItem>
                <MenuItem value="Cancelled">ยกเลิก</MenuItem>
              </TextField>
            </Grid>
            {canFilterDepartments && (
              <Grid item xs={12} sm={6} md={2}>
                <TextField select fullWidth size="small" label="หน่วยงาน" value={filters.departmentId} onChange={(event) => updateFilters({ ...filters, departmentId: event.target.value })}>
                  <MenuItem value="">ทุกหน่วยงาน</MenuItem>
                  {departments.map((item) => (
                    <MenuItem key={item.id} value={item.id}>
                      {item.name}
                    </MenuItem>
                  ))}
                </TextField>
              </Grid>
            )}
            <Grid item xs={12} sm={6} md={2}>
              <TextField select fullWidth size="small" label="ผู้ขอลา" value={filters.userId} onChange={(event) => updateFilters({ ...filters, userId: event.target.value })}>
                <MenuItem value="">ทุกคน</MenuItem>
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
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>ผู้ขอ</TableCell>
                <TableCell>เลขที่คำขอ</TableCell>
                <TableCell>ประเภทลา</TableCell>
                <TableCell>วันที่ลา</TableCell>
                <TableCell>จำนวนวัน</TableCell>
                <TableCell>สถานะ</TableCell>
                <TableCell>สถานะปัจจุบัน</TableCell>
                <TableCell>ผู้อนุมัติปัจจุบัน</TableCell>
                <TableCell>ขั้นตอนปัจจุบัน</TableCell>
                <TableCell align="right">จัดการ</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {isLoading ? (
                <TableRow><TableCell colSpan={10}>กำลังโหลดคำขอลา...</TableCell></TableRow>
              ) : visibleData.length ? (
                visibleData.map((item) => {
                  return (
                    <TableRow key={item.id}>
                      <TableCell>{item.fullname ?? "-"}</TableCell>
                      <TableCell>{getLeaveRequestCode(item.requestNumber, item.id)}</TableCell>
                      <TableCell>{getLeaveTypeWithDurationLabel(item.leaveTypeName, item.durationType)}</TableCell>
                      <TableCell>{formatThaiDate(item.startDate)} - {formatThaiDate(item.endDate)}</TableCell>
                      <TableCell>{item.totalDays}</TableCell>
                      <TableCell><Chip size="small" label={getLeaveStatusLabel(item.status)} color={getLeaveStatusColor(item.status)} /></TableCell>
                      <TableCell>{getTrackingStatusLabel(item)}</TableCell>
                      <TableCell>
                        {item.currentApproverName ?? "-"}
                        {item.currentApproverRole && (
                          <Box sx={{ color: "text.secondary", fontSize: 12 }}>
                            บทบาท: {item.currentApproverRole}
                          </Box>
                        )}
                      </TableCell>
                      <TableCell>{getTrackingStepLabel(item)}</TableCell>
                      <TableCell align="right">
                        <IconButton component={RouterLink} to={`/leave/${item.id}`} aria-label="ดูรายละเอียดคำขอลา">
                          <VisibilityOutlinedIcon />
                        </IconButton>
                      </TableCell>
                    </TableRow>
                  );
                })
              ) : (
                <TableRow><TableCell colSpan={10}>ไม่พบคำขอลา</TableCell></TableRow>
              )}
            </TableBody>
          </Table>
          <TablePagination
            component="div"
            count={data?.totalItems ?? 0}
            page={page}
            onPageChange={(_, nextPage) => setPage(nextPage)}
            rowsPerPage={pageSize}
            onRowsPerPageChange={(event) => {
              setPageSize(Number(event.target.value));
              setPage(0);
            }}
            rowsPerPageOptions={[10, 20, 50]}
            labelRowsPerPage="จำนวนรายการต่อหน้า"
            labelDisplayedRows={({ from, to, count }) => `${from}-${to} จาก ${count !== -1 ? count : `มากกว่า ${to}`}`}
            getItemAriaLabel={(type) => {
              if (type === "first") return "ไปหน้าแรก";
              if (type === "last") return "ไปหน้าสุดท้าย";
              if (type === "next") return "ไปหน้าถัดไป";
              return "ไปหน้าก่อนหน้า";
            }}
          />
        </CardContent>
      </Card>
    </>
  );
}
