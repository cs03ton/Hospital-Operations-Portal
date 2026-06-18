import DownloadOutlinedIcon from "@mui/icons-material/DownloadOutlined";
import { Button, Card, CardContent, MenuItem, Stack, Table, TableBody, TableCell, TableHead, TableRow, TextField, Typography } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import dayjs from "dayjs";
import { useState } from "react";
import { getDepartments } from "../api/adminApi";
import { downloadLeaveReportExcel, downloadLeaveReportPdf, getLeaveReport, getLeaveTypes, type LeaveReportQuery } from "../api/leaveApi";
import { PageHeader } from "../components/PageHeader";
import { PermissionGuard } from "../context/PermissionContext";

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
          <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
            <TextField type="date" label="ตั้งแต่วันที่" InputLabelProps={{ shrink: true }} value={filters.from ?? ""} onChange={(event) => setFilters({ ...filters, from: event.target.value || undefined })} />
            <TextField type="date" label="ถึงวันที่" InputLabelProps={{ shrink: true }} value={filters.to ?? ""} onChange={(event) => setFilters({ ...filters, to: event.target.value || undefined })} />
            <TextField select label="หน่วยงาน" value={filters.departmentId ?? ""} onChange={(event) => setFilters({ ...filters, departmentId: event.target.value || undefined })}>
              <MenuItem value="">ทุกหน่วยงาน</MenuItem>
              {departments.map((item) => <MenuItem key={item.id} value={item.id}>{item.name}</MenuItem>)}
            </TextField>
            <TextField select label="ประเภทลา" value={filters.leaveTypeId ?? ""} onChange={(event) => setFilters({ ...filters, leaveTypeId: event.target.value || undefined })}>
              <MenuItem value="">ทุกประเภทลา</MenuItem>
              {leaveTypes.map((item) => <MenuItem key={item.id} value={item.id}>{item.name}</MenuItem>)}
            </TextField>
            <PermissionGuard permission="ReportManagement.Export">
              <Button variant="outlined" startIcon={<DownloadOutlinedIcon />} onClick={() => download(downloadLeaveReportExcel(filters), "leave-report.xls")}>ส่งออก Excel</Button>
            </PermissionGuard>
            <PermissionGuard permission="ReportManagement.Export">
              <Button variant="outlined" startIcon={<DownloadOutlinedIcon />} onClick={() => download(downloadLeaveReportPdf(filters), "leave-report.pdf")}>ส่งออก PDF</Button>
            </PermissionGuard>
          </Stack>
        </CardContent>
      </Card>
      <Card>
        <CardContent>
          <Typography variant="h6" sx={{ mb: 2 }}>รายการคำขอลา</Typography>
          <Table size="small">
            <TableHead><TableRow><TableCell>ชื่อ</TableCell><TableCell>หน่วยงาน</TableCell><TableCell>ประเภทลา</TableCell><TableCell>ช่วงวันที่</TableCell><TableCell>วัน</TableCell><TableCell>สถานะ</TableCell></TableRow></TableHead>
            <TableBody>
              {(data?.leaveRequests ?? []).map((item) => (
                <TableRow key={item.id}>
                  <TableCell>{item.fullname ?? "-"}</TableCell>
                  <TableCell>{item.departmentName ?? "-"}</TableCell>
                  <TableCell>{item.leaveTypeName ?? "-"}</TableCell>
                  <TableCell>{dayjs(item.startDate).format("DD/MM/YYYY")} - {dayjs(item.endDate).format("DD/MM/YYYY")}</TableCell>
                  <TableCell>{item.totalDays}</TableCell>
                  <TableCell>{item.status}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </>
  );
}
