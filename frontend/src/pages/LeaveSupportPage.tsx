import CheckCircleOutlineOutlinedIcon from "@mui/icons-material/CheckCircleOutlineOutlined";
import HighlightOffOutlinedIcon from "@mui/icons-material/HighlightOffOutlined";
import ManageSearchOutlinedIcon from "@mui/icons-material/ManageSearchOutlined";
import VisibilityOutlinedIcon from "@mui/icons-material/VisibilityOutlined";
import { Alert, Button, Card, CardContent, Chip, Dialog, DialogActions, DialogContent, DialogTitle, Grid, MenuItem, Stack, Table, TableBody, TableCell, TableHead, TableRow, TextField, Typography } from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { getDepartments, getUsers } from "../api/adminApi";
import { getLeaveSupportDetail, getLeaveSupportRequests, overrideApproveLeaveRequest, overrideRejectLeaveRequest, type LeaveSupportQuery, type LeaveSupportRequest } from "../api/leaveApi";
import { AppDatePicker } from "../components/common/AppDatePicker";
import { PageHeader } from "../components/PageHeader";
import { PermissionGuard } from "../context/PermissionContext";
import { useNotification } from "../hooks/useNotification";
import { formatThaiDate, formatThaiDateTime } from "../utils/dateFormat";
import { getLeaveStatusColor, getLeaveStatusLabel, getLeaveTypeWithDurationLabel } from "../utils/leaveLabels";

export function LeaveSupportPage() {
  const queryClient = useQueryClient();
  const { showSuccess } = useNotification();
  const [filters, setFilters] = useState({ search: "", departmentId: "", status: "", currentApproverId: "", fromDate: "", toDate: "" });
  const [selected, setSelected] = useState<LeaveSupportRequest | null>(null);
  const [overrideAction, setOverrideAction] = useState<"approve" | "reject" | null>(null);
  const [reason, setReason] = useState("");
  const queryParams = useMemo<LeaveSupportQuery>(() => ({
    search: filters.search || undefined,
    departmentId: filters.departmentId || undefined,
    status: filters.status || undefined,
    currentApproverId: filters.currentApproverId || undefined,
    fromDate: filters.fromDate || undefined,
    toDate: filters.toDate || undefined,
  }), [filters]);
  const { data: requests = [], isLoading } = useQuery({ queryKey: ["leave-support", queryParams], queryFn: () => getLeaveSupportRequests(queryParams) });
  const { data: departments = [] } = useQuery({ queryKey: ["departments"], queryFn: getDepartments });
  const { data: users = [] } = useQuery({ queryKey: ["users"], queryFn: getUsers });
  const { data: detail } = useQuery({ queryKey: ["leave-support", selected?.id], queryFn: () => getLeaveSupportDetail(selected!.id), enabled: Boolean(selected) });
  const invalidate = async () => {
    await queryClient.invalidateQueries({ queryKey: ["leave-support"] });
    setOverrideAction(null);
    setReason("");
  };
  const overrideApprove = useMutation({
    mutationFn: () => overrideApproveLeaveRequest(selected!.id, reason),
    onSuccess: async () => {
      showSuccess("Override อนุมัติคำขอลาเรียบร้อยแล้ว");
      await invalidate();
    },
  });
  const overrideReject = useMutation({
    mutationFn: () => overrideRejectLeaveRequest(selected!.id, reason),
    onSuccess: async () => {
      showSuccess("Override ไม่อนุมัติคำขอลาเรียบร้อยแล้ว");
      await invalidate();
    },
  });

  return (
    <>
      <PageHeader title="มุมมองช่วยเหลือระบบลา" subtitle="ค้นหาและตรวจสอบคำขอลาทั้งหมดสำหรับงาน support โดยไม่ปะปนกับคิวอนุมัติปกติ" />
      <Stack spacing={2}>
        {(overrideApprove.isError || overrideReject.isError) && <Alert severity="error">ดำเนินการ override ไม่สำเร็จ กรุณาตรวจสอบเหตุผลและสิทธิ์</Alert>}
        <Card>
          <CardContent>
            <Grid container spacing={1.5}>
              <Grid item xs={12} md={3}>
                <TextField fullWidth size="small" label="ค้นหาเลขที่คำขอ/ผู้ขอลา" InputLabelProps={{ shrink: true }} value={filters.search} onChange={(event) => setFilters({ ...filters, search: event.target.value })} />
              </Grid>
              <Grid item xs={12} md={2}>
                <TextField fullWidth size="small" select label="หน่วยงาน" value={filters.departmentId} onChange={(event) => setFilters({ ...filters, departmentId: event.target.value })}>
                  <MenuItem value="">ทั้งหมด</MenuItem>
                  {departments.map((item) => <MenuItem key={item.id} value={item.id}>{item.name}</MenuItem>)}
                </TextField>
              </Grid>
              <Grid item xs={12} md={2}>
                <TextField fullWidth size="small" select label="สถานะ" value={filters.status} onChange={(event) => setFilters({ ...filters, status: event.target.value })}>
                  <MenuItem value="">ทั้งหมด</MenuItem>
                  {["Draft", "Pending", "Approved", "Rejected", "Cancelled"].map((item) => <MenuItem key={item} value={item}>{getLeaveStatusLabel(item)}</MenuItem>)}
                </TextField>
              </Grid>
              <Grid item xs={12} md={2}>
                <TextField fullWidth size="small" select label="ผู้อนุมัติปัจจุบัน" value={filters.currentApproverId} onChange={(event) => setFilters({ ...filters, currentApproverId: event.target.value })}>
                  <MenuItem value="">ทั้งหมด</MenuItem>
                  {users.map((item) => <MenuItem key={item.id} value={item.id}>{item.fullname}</MenuItem>)}
                </TextField>
              </Grid>
              <Grid item xs={12} sm={6} md={1.5}>
                <AppDatePicker label="จากวันที่" value={filters.fromDate} onChange={(value) => setFilters({ ...filters, fromDate: value })} />
              </Grid>
              <Grid item xs={12} sm={6} md={1.5}>
                <AppDatePicker label="ถึงวันที่" value={filters.toDate} onChange={(value) => setFilters({ ...filters, toDate: value })} />
              </Grid>
            </Grid>
          </CardContent>
        </Card>

        <Card>
          <CardContent>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>เลขที่คำขอ</TableCell>
                  <TableCell>ผู้ขอลา</TableCell>
                  <TableCell>ประเภทลา</TableCell>
                  <TableCell>ช่วงวันที่</TableCell>
                  <TableCell>ผู้อนุมัติปัจจุบัน</TableCell>
                  <TableCell>สถานะ</TableCell>
                  <TableCell>ค้างอนุมัติ</TableCell>
                  <TableCell align="right">ดูรายละเอียด</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {isLoading ? (
                  <TableRow><TableCell colSpan={8}>กำลังโหลดข้อมูล...</TableCell></TableRow>
                ) : requests.length ? requests.map((item) => (
                  <TableRow key={item.id}>
                    <TableCell>{item.requestNumber}</TableCell>
                    <TableCell>{item.fullname ?? "-"}</TableCell>
                    <TableCell>{getLeaveTypeWithDurationLabel(item.leaveTypeName, item.durationType)}</TableCell>
                    <TableCell>{formatThaiDate(item.startDate)} - {formatThaiDate(item.endDate)}</TableCell>
                    <TableCell>{item.currentApproverName ?? "-"}</TableCell>
                    <TableCell><Chip size="small" label={getLeaveStatusLabel(item.status)} color={getLeaveStatusColor(item.status)} /></TableCell>
                    <TableCell>{item.isOverdue ? <Chip size="small" color="warning" label={item.blockingReason ?? "ค้างอนุมัติ"} /> : "-"}</TableCell>
                    <TableCell align="right">
                      <Button size="small" variant="outlined" startIcon={<VisibilityOutlinedIcon />} onClick={() => setSelected(item)}>รายละเอียด</Button>
                    </TableCell>
                  </TableRow>
                )) : (
                  <TableRow><TableCell colSpan={8}>ไม่พบคำขอลาที่ตรงกับเงื่อนไข</TableCell></TableRow>
                )}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      </Stack>

      <Dialog open={Boolean(selected)} onClose={() => setSelected(null)} fullWidth maxWidth="lg">
        <DialogTitle>รายละเอียดช่วยเหลือระบบลา {selected?.requestNumber}</DialogTitle>
        <DialogContent dividers>
          {detail && (
            <Stack spacing={2}>
              <Alert severity="warning" icon={<ManageSearchOutlinedIcon />}>
                มุมมองนี้ใช้สำหรับช่วยเหลือระบบเท่านั้น การ Override ต้องระบุเหตุผลและจะถูกบันทึกใน Audit Log
              </Alert>
              <Grid container spacing={2}>
                <Grid item xs={12} md={4}><Typography color="text.secondary">ผู้ขอลา</Typography><Typography fontWeight={700}>{detail.request.fullname ?? "-"}</Typography></Grid>
                <Grid item xs={12} md={4}><Typography color="text.secondary">ผู้อนุมัติปัจจุบัน</Typography><Typography fontWeight={700}>{detail.request.currentApproverName ?? "-"}</Typography></Grid>
                <Grid item xs={12} md={4}><Typography color="text.secondary">สถานะ</Typography><Chip size="small" label={getLeaveStatusLabel(detail.request.status)} color={getLeaveStatusColor(detail.request.status)} /></Grid>
              </Grid>
              <Table size="small">
                <TableHead><TableRow><TableCell>ขั้น</TableCell><TableCell>ผู้อนุมัติ</TableCell><TableCell>สถานะ</TableCell><TableCell>วันที่ดำเนินการ</TableCell><TableCell>หมายเหตุ</TableCell></TableRow></TableHead>
                <TableBody>{detail.approvals.map((item) => <TableRow key={item.id}><TableCell>{item.stepName ?? item.stepOrder}</TableCell><TableCell>{item.approverName ?? "-"}</TableCell><TableCell>{getLeaveStatusLabel(item.status)}</TableCell><TableCell>{item.actionAt ? formatThaiDateTime(item.actionAt) : "-"}</TableCell><TableCell>{item.remark ?? "-"}</TableCell></TableRow>)}</TableBody>
              </Table>
              <Typography variant="h6">Override Log</Typography>
              <Table size="small">
                <TableBody>{detail.overrideLogs.length ? detail.overrideLogs.map((item) => <TableRow key={item.id}><TableCell>{formatThaiDateTime(item.createdAt)}</TableCell><TableCell>{item.overrideByName ?? "-"}</TableCell><TableCell>{item.action}</TableCell><TableCell>{item.reason}</TableCell></TableRow>) : <TableRow><TableCell>ยังไม่มี override log</TableCell></TableRow>}</TableBody>
              </Table>
              <Typography variant="h6">Audit Trail</Typography>
              <Table size="small">
                <TableBody>{detail.auditLogs.length ? detail.auditLogs.map((item) => <TableRow key={item.id}><TableCell>{formatThaiDateTime(item.timestamp)}</TableCell><TableCell>{item.fullname ?? item.username ?? "-"}</TableCell><TableCell>{item.action}</TableCell><TableCell>{item.result}</TableCell><TableCell>{item.detail ?? "-"}</TableCell></TableRow>) : <TableRow><TableCell>ยังไม่มี audit trail</TableCell></TableRow>}</TableBody>
              </Table>
            </Stack>
          )}
        </DialogContent>
        <DialogActions sx={{ justifyContent: "space-between", flexWrap: "wrap", gap: 1 }}>
          <PermissionGuard permission="LeaveApproval.Override">
            <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
              <Button color="success" variant="contained" startIcon={<CheckCircleOutlineOutlinedIcon />} disabled={selected?.status !== "Pending"} onClick={() => setOverrideAction("approve")}>Override อนุมัติ</Button>
              <Button color="error" variant="outlined" startIcon={<HighlightOffOutlinedIcon />} disabled={selected?.status !== "Pending"} onClick={() => setOverrideAction("reject")}>Override ไม่อนุมัติ</Button>
            </Stack>
          </PermissionGuard>
          <Button onClick={() => setSelected(null)}>ปิด</Button>
        </DialogActions>
      </Dialog>

      <Dialog open={Boolean(overrideAction)} onClose={() => setOverrideAction(null)} fullWidth maxWidth="sm">
        <DialogTitle>ยืนยันการดำเนินการแทน</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            <Alert severity="warning">การดำเนินการนี้เป็นการอนุมัติแทนและจะถูกบันทึกใน Audit Log</Alert>
            <TextField multiline minRows={3} label="เหตุผลการดำเนินการแทน" InputLabelProps={{ shrink: true }} value={reason} onChange={(event) => setReason(event.target.value)} />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOverrideAction(null)}>ยกเลิก</Button>
          <Button
            variant="contained"
            color={overrideAction === "approve" ? "success" : "error"}
            disabled={!reason.trim() || overrideApprove.isPending || overrideReject.isPending}
            onClick={() => overrideAction === "approve" ? overrideApprove.mutate() : overrideReject.mutate()}
          >
            ยืนยัน
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}
