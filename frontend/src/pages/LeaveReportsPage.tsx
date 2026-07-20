import DownloadOutlinedIcon from "@mui/icons-material/DownloadOutlined";
import ClearOutlinedIcon from "@mui/icons-material/ClearOutlined";
import VisibilityOutlinedIcon from "@mui/icons-material/VisibilityOutlined";
import { Box, Button, Card, CardContent, MenuItem, Stack, Table, TableBody, TableCell, TableHead, TablePagination, TableRow, TextField, Typography } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import { downloadLeaveReportExcel, downloadLeaveReportPdf, getLeaveAnalyticsOptions, getLeaveCancellationRequests, getLeaveReport, type LeaveReportQuery } from "../api/leaveApi";
import { PageHeader } from "../components/PageHeader";
import { ActionTooltip } from "../components/common/ActionTooltip";
import { AppDatePicker } from "../components/common/AppDatePicker";
import { PermissionGuard } from "../context/PermissionContext";
import { formatThaiDate } from "../utils/dateFormat";
import { getLeaveStatusLabel, getLeaveTypeLabel, getLeaveTypeWithDurationLabel } from "../utils/leaveLabels";
import { getCancellationStatusLabel } from "./LeaveCancellationListPage";
import { formatDays as formatLeaveDays } from "./LeaveCancellationCreatePage";

export function LeaveReportsPage() {
  const [filters, setFilters] = useState<LeaveReportQuery>({});
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(10);
  const { data: reportOptions } = useQuery({ queryKey: ["leave-report", "options"], queryFn: getLeaveAnalyticsOptions });
  const departments = reportOptions?.departments ?? [];
  const leaveTypes = reportOptions?.leaveTypes ?? [];
  const { data } = useQuery({ queryKey: ["leave-report", filters], queryFn: () => getLeaveReport(filters) });
  const { data: cancellationReport } = useQuery({
    queryKey: ["leave-cancellation-report-preview"],
    queryFn: () => getLeaveCancellationRequests({ page: 1, pageSize: 10 }),
  });
  const visibleLeaveRequests = useMemo(
    () => (data?.leaveRequests ?? []).slice(page * pageSize, page * pageSize + pageSize),
    [data?.leaveRequests, page, pageSize],
  );

  function updateFilters(nextFilters: LeaveReportQuery) {
    setFilters(nextFilters);
    setPage(0);
  }

  async function download(blobPromise: Promise<Blob>, fileName: string) {
    const blob = await blobPromise;
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);
  }

  return (
    <>
      <PageHeader title="รายงานการลา" subtitle="ค้นหารายการลา ยอดวันลา และส่งออก Excel/PDF" />
      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Box
            sx={{
              display: "grid",
              gridTemplateColumns: {
                xs: "1fr",
                sm: "repeat(2, minmax(0, 1fr))",
                lg: "minmax(160px, 1fr) minmax(160px, 1fr) minmax(220px, 1.45fr) minmax(220px, 1.45fr) auto",
              },
              gap: 1.5,
              alignItems: "center",
            }}
          >
            <Box sx={{ minWidth: 0 }}>
              <AppDatePicker label="ตั้งแต่วันที่" value={filters.from ?? ""} onChange={(value) => updateFilters({ ...filters, from: value || undefined })} />
            </Box>
            <Box sx={{ minWidth: 0 }}>
              <AppDatePicker label="ถึงวันที่" value={filters.to ?? ""} onChange={(value) => updateFilters({ ...filters, to: value || undefined })} />
            </Box>
            <Box sx={{ minWidth: 0 }}>
              <TextField fullWidth size="small" select label="หน่วยงาน" value={filters.departmentId ?? ""} onChange={(event) => updateFilters({ ...filters, departmentId: event.target.value || undefined })}>
                <MenuItem value="">ทุกหน่วยงาน</MenuItem>
                {departments.map((item) => <MenuItem key={item.id} value={item.id}>{item.name}</MenuItem>)}
              </TextField>
            </Box>
            <Box sx={{ minWidth: 0 }}>
              <TextField fullWidth size="small" select label="ประเภทลา" value={filters.leaveTypeId ?? ""} onChange={(event) => updateFilters({ ...filters, leaveTypeId: event.target.value || undefined })}>
                <MenuItem value="">ทุกประเภทลา</MenuItem>
                {leaveTypes.map((item) => <MenuItem key={item.id} value={item.id}>{getLeaveTypeLabel(item.name || item.code)}</MenuItem>)}
              </TextField>
            </Box>
            <Stack
              direction="row"
              spacing={1}
              justifyContent={{ xs: "flex-start", sm: "flex-end" }}
              flexWrap="nowrap"
              sx={{ minWidth: { xs: "100%", sm: "auto" }, gridColumn: { xs: "1", sm: "1 / -1", lg: "auto" } }}
            >
                <Button variant="outlined" startIcon={<ClearOutlinedIcon />} onClick={() => updateFilters({})} sx={{ whiteSpace: "nowrap", minWidth: 96 }}>ล้าง</Button>
                <PermissionGuard permission="ReportManagement.Export">
                  <ActionTooltip title="ส่งออกรายงานการลาเป็น Excel">
                    <Button variant="outlined" startIcon={<DownloadOutlinedIcon />} onClick={() => download(downloadLeaveReportExcel(filters), "leave-report.xlsx")} sx={{ whiteSpace: "nowrap", minWidth: 96 }}>Excel</Button>
                  </ActionTooltip>
                </PermissionGuard>
                <PermissionGuard permission="ReportManagement.Export">
                  <ActionTooltip title="ส่งออกรายงานการลาเป็น PDF">
                    <Button variant="outlined" startIcon={<DownloadOutlinedIcon />} onClick={() => download(downloadLeaveReportPdf(filters), "leave-report.pdf")} sx={{ whiteSpace: "nowrap", minWidth: 96 }}>PDF</Button>
                  </ActionTooltip>
                </PermissionGuard>
            </Stack>
          </Box>
        </CardContent>
      </Card>
      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Stack direction={{ xs: "column", md: "row" }} justifyContent="space-between" alignItems={{ xs: "stretch", md: "flex-start" }} spacing={2} sx={{ mb: 2 }}>
            <Stack spacing={0.5}>
              <Typography variant="h6">รายงานคำขอยกเลิกใบลา</Typography>
              <Typography variant="body2" color="text.secondary">
                แสดงคำขอยกเลิกใบลาล่าสุดและจำนวนวันที่คืนยอด เพื่อช่วยตรวจสอบ Leave Restoration
              </Typography>
            </Stack>
            <Button component={RouterLink} to="/leave/cancellations" variant="outlined" startIcon={<VisibilityOutlinedIcon />} sx={{ alignSelf: { xs: "stretch", md: "center" }, whiteSpace: "nowrap", minWidth: 132 }}>
              ดูทั้งหมด
            </Button>
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
              </TableRow>
            </TableHead>
            <TableBody>
              {(cancellationReport?.items ?? []).length ? (
                cancellationReport!.items.map((item) => (
                  <TableRow key={item.id}>
                    <TableCell>{item.cancellationRequestNumber}</TableCell>
                    <TableCell>{item.originalRequestNumber ?? "-"}</TableCell>
                    <TableCell>{getLeaveTypeLabel(item.leaveTypeName ?? "-")}</TableCell>
                    <TableCell>{formatLeaveDays(item.originalLeaveDays)}</TableCell>
                    <TableCell>{getCancellationStatusLabel(item.status)}</TableCell>
                    <TableCell>{item.currentApproverName ?? "-"}</TableCell>
                  </TableRow>
                ))
              ) : (
                <TableRow><TableCell colSpan={6}>ยังไม่มีข้อมูลคำขอยกเลิกใบลา</TableCell></TableRow>
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
      <Card>
        <CardContent>
          <Stack direction={{ xs: "column", md: "row" }} justifyContent="space-between" alignItems={{ xs: "stretch", md: "flex-start" }} spacing={2} sx={{ mb: 2 }}>
            <Stack spacing={0.5}>
              <Typography variant="h6">รายการคำขอลา</Typography>
              <Typography variant="body2" color="text.secondary">
                รายการคำขอลาตามช่วงวันที่ หน่วยงาน และประเภทลาที่เลือก
              </Typography>
            </Stack>
            <Button component={RouterLink} to="/leave" variant="outlined" startIcon={<VisibilityOutlinedIcon />} sx={{ alignSelf: { xs: "stretch", md: "center" }, whiteSpace: "nowrap", minWidth: 132 }}>
              ดูทั้งหมด
            </Button>
          </Stack>
          <Table size="small">
            <TableHead><TableRow><TableCell>ชื่อ</TableCell><TableCell>หน่วยงาน</TableCell><TableCell>ประเภทลา</TableCell><TableCell>ช่วงวันที่</TableCell><TableCell>วัน</TableCell><TableCell>สถานะ</TableCell></TableRow></TableHead>
            <TableBody>
              {visibleLeaveRequests.length ? visibleLeaveRequests.map((item) => (
                <TableRow key={item.id}>
                  <TableCell>{item.fullname ?? "-"}</TableCell>
                  <TableCell>{item.departmentName ?? "-"}</TableCell>
                  <TableCell>{getLeaveTypeWithDurationLabel(item.leaveTypeName, item.durationType)}</TableCell>
                  <TableCell>{formatThaiDate(item.startDate)} - {formatThaiDate(item.endDate)}</TableCell>
                  <TableCell>{item.totalDays}</TableCell>
                  <TableCell>{getLeaveStatusLabel(item.status)}</TableCell>
                </TableRow>
              )) : (
                <TableRow><TableCell colSpan={6}>ไม่พบข้อมูลรายงานการลา</TableCell></TableRow>
              )}
            </TableBody>
          </Table>
          <TablePagination
            component="div"
            count={data?.leaveRequests.length ?? 0}
            page={page}
            onPageChange={(_, nextPage) => setPage(nextPage)}
            rowsPerPage={pageSize}
            onRowsPerPageChange={(event) => {
              setPageSize(Number(event.target.value));
              setPage(0);
            }}
            rowsPerPageOptions={[10, 20, 50]}
            labelRowsPerPage="จำนวนรายการต่อหน้า"
            labelDisplayedRows={({ from, to, count }) => `${from}-${to} จาก ${count}`}
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
