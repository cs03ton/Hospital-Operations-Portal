import DownloadOutlinedIcon from "@mui/icons-material/DownloadOutlined";
import ClearOutlinedIcon from "@mui/icons-material/ClearOutlined";
import { Button, Card, CardContent, Grid, MenuItem, Stack, Table, TableBody, TableCell, TableHead, TableRow, TextField, Typography } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { getDepartments } from "../api/adminApi";
import { downloadLeaveReportExcel, downloadLeaveReportPdf, getLeaveReport, getLeaveTypes, type LeaveReportQuery } from "../api/leaveApi";
import { PageHeader } from "../components/PageHeader";
import { ActionTooltip } from "../components/common/ActionTooltip";
import { AppDatePicker } from "../components/common/AppDatePicker";
import { PermissionGuard } from "../context/PermissionContext";
import { formatThaiDate } from "../utils/dateFormat";
import { getLeaveStatusLabel, getLeaveTypeLabel } from "../utils/leaveLabels";

export function LeaveReportsPage() {
  const [filters, setFilters] = useState<LeaveReportQuery>({});
  const { data: departments = [] } = useQuery({ queryKey: ["departments"], queryFn: getDepartments });
  const { data: leaveTypes = [] } = useQuery({ queryKey: ["leave-types"], queryFn: getLeaveTypes });
  const { data } = useQuery({ queryKey: ["leave-report", filters], queryFn: () => getLeaveReport(filters) });

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
          <Grid container spacing={1.5} alignItems="center">
            <Grid item xs={12} sm={6} md={2}>
              <AppDatePicker label="ตั้งแต่วันที่" value={filters.from ?? ""} onChange={(value) => setFilters({ ...filters, from: value || undefined })} />
            </Grid>
            <Grid item xs={12} sm={6} md={2}>
              <AppDatePicker label="ถึงวันที่" value={filters.to ?? ""} onChange={(value) => setFilters({ ...filters, to: value || undefined })} />
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <TextField fullWidth size="small" select label="หน่วยงาน" value={filters.departmentId ?? ""} onChange={(event) => setFilters({ ...filters, departmentId: event.target.value || undefined })}>
                <MenuItem value="">ทุกหน่วยงาน</MenuItem>
                {departments.map((item) => <MenuItem key={item.id} value={item.id}>{item.name}</MenuItem>)}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <TextField fullWidth size="small" select label="ประเภทลา" value={filters.leaveTypeId ?? ""} onChange={(event) => setFilters({ ...filters, leaveTypeId: event.target.value || undefined })}>
                <MenuItem value="">ทุกประเภทลา</MenuItem>
                {leaveTypes.map((item) => <MenuItem key={item.id} value={item.id}>{getLeaveTypeLabel(item.name || item.code)}</MenuItem>)}
              </TextField>
            </Grid>
            <Grid item xs={12} md={2}>
              <Stack direction="row" spacing={1} justifyContent={{ xs: "flex-start", md: "flex-end" }} flexWrap="wrap" useFlexGap>
                <Button variant="outlined" startIcon={<ClearOutlinedIcon />} onClick={() => setFilters({})}>ล้าง</Button>
                <PermissionGuard permission="ReportManagement.Export">
                  <ActionTooltip title="ส่งออกรายงานการลาเป็น Excel">
                    <Button variant="outlined" startIcon={<DownloadOutlinedIcon />} onClick={() => download(downloadLeaveReportExcel(filters), "leave-report.xlsx")}>Excel</Button>
                  </ActionTooltip>
                </PermissionGuard>
                <PermissionGuard permission="ReportManagement.Export">
                  <ActionTooltip title="ส่งออกรายงานการลาเป็น PDF">
                    <Button variant="outlined" startIcon={<DownloadOutlinedIcon />} onClick={() => download(downloadLeaveReportPdf(filters), "leave-report.pdf")}>PDF</Button>
                  </ActionTooltip>
                </PermissionGuard>
              </Stack>
            </Grid>
          </Grid>
        </CardContent>
      </Card>
      <Card>
        <CardContent>
          <Typography variant="h6" sx={{ mb: 2 }}>รายการคำขอลา</Typography>
          <Table size="small">
            <TableHead><TableRow><TableCell>ชื่อ</TableCell><TableCell>หน่วยงาน</TableCell><TableCell>ประเภทลา</TableCell><TableCell>ช่วงวันที่</TableCell><TableCell>วัน</TableCell><TableCell>สถานะ</TableCell></TableRow></TableHead>
            <TableBody>
              {(data?.leaveRequests ?? []).length ? (data?.leaveRequests ?? []).map((item) => (
                <TableRow key={item.id}>
                  <TableCell>{item.fullname ?? "-"}</TableCell>
                  <TableCell>{item.departmentName ?? "-"}</TableCell>
                  <TableCell>{getLeaveTypeLabel(item.leaveTypeName)}</TableCell>
                  <TableCell>{formatThaiDate(item.startDate)} - {formatThaiDate(item.endDate)}</TableCell>
                  <TableCell>{item.totalDays}</TableCell>
                  <TableCell>{getLeaveStatusLabel(item.status)}</TableCell>
                </TableRow>
              )) : (
                <TableRow><TableCell colSpan={6}>ไม่พบข้อมูลรายงานการลา</TableCell></TableRow>
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </>
  );
}
