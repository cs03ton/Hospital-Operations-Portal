import CancelOutlinedIcon from "@mui/icons-material/CancelOutlined";
import CheckCircleOutlineOutlinedIcon from "@mui/icons-material/CheckCircleOutlineOutlined";
import HighlightOffOutlinedIcon from "@mui/icons-material/HighlightOffOutlined";
import ReplyOutlinedIcon from "@mui/icons-material/ReplyOutlined";
import SendOutlinedIcon from "@mui/icons-material/SendOutlined";
import {
  Alert,
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  Paper,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { Link as RouterLink, useParams } from "react-router-dom";
import {
  approveLeaveCancellationRequest,
  cancelLeaveCancellationRequest,
  getLeaveCancellationApprovals,
  getLeaveCancellationRequest,
  rejectLeaveCancellationRequest,
  returnLeaveCancellationForRevision,
  submitLeaveCancellationRequest,
} from "../api/leaveApi";
import { PageHeader } from "../components/PageHeader";
import { ApprovalWorkflowTimeline } from "../components/leave/ApprovalWorkflowTimeline";
import { useAuth } from "../context/AuthContext";
import { useNotification } from "../hooks/useNotification";
import { getCancellationStatusLabel } from "./LeaveCancellationListPage";
import { formatDays } from "./LeaveCancellationCreatePage";

export function LeaveCancellationDetailPage() {
  const { id } = useParams();
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useNotification();
  const [remark, setRemark] = useState("");
  const [revisionReason, setRevisionReason] = useState("");
  const [returnDialogOpen, setReturnDialogOpen] = useState(false);

  const { data: cancellation, isError } = useQuery({ queryKey: ["leave-cancellations", id], queryFn: () => getLeaveCancellationRequest(id!), enabled: Boolean(id) });
  const { data: approvals = [] } = useQuery({ queryKey: ["leave-cancellations", id, "approvals"], queryFn: () => getLeaveCancellationApprovals(id!), enabled: Boolean(id) });

  const invalidate = async () => {
    await queryClient.invalidateQueries({ queryKey: ["leave-cancellations"] });
    await queryClient.invalidateQueries({ queryKey: ["leave-cancellations", id] });
    await queryClient.invalidateQueries({ queryKey: ["leave-requests"] });
    await queryClient.invalidateQueries({ queryKey: ["notifications"] });
  };

  const submitMutation = useMutation({ mutationFn: () => submitLeaveCancellationRequest(id!), onSuccess: async () => { showSuccess("ส่งคำขอยกเลิกใบลาเรียบร้อยแล้ว"); await invalidate(); }, onError: (error) => showError(getErrorMessage(error, "ส่งคำขอยกเลิกใบลาไม่สำเร็จ")) });
  const cancelMutation = useMutation({ mutationFn: () => cancelLeaveCancellationRequest(id!), onSuccess: async () => { showSuccess("ยกเลิกคำขอยกเลิกใบลาเรียบร้อยแล้ว"); await invalidate(); }, onError: (error) => showError(getErrorMessage(error, "ยกเลิกคำขอยกเลิกใบลาไม่สำเร็จ")) });
  const approveMutation = useMutation({ mutationFn: () => approveLeaveCancellationRequest(id!, remark), onSuccess: async () => { showSuccess("อนุมัติคำขอยกเลิกใบลาเรียบร้อยแล้ว"); await invalidate(); }, onError: (error) => showError(getErrorMessage(error, "อนุมัติคำขอยกเลิกใบลาไม่สำเร็จ")) });
  const rejectMutation = useMutation({ mutationFn: () => rejectLeaveCancellationRequest(id!, remark), onSuccess: async () => { showSuccess("ไม่อนุมัติคำขอยกเลิกใบลาเรียบร้อยแล้ว"); await invalidate(); }, onError: (error) => showError(getErrorMessage(error, "ไม่อนุมัติคำขอยกเลิกใบลาไม่สำเร็จ")) });
  const returnMutation = useMutation({
    mutationFn: () => returnLeaveCancellationForRevision(id!, revisionReason),
    onSuccess: async () => {
      showSuccess("ตีกลับคำขอยกเลิกใบลาเรียบร้อยแล้ว");
      setReturnDialogOpen(false);
      setRevisionReason("");
      await invalidate();
    },
    onError: (error) => showError(getErrorMessage(error, "ตีกลับคำขอยกเลิกใบลาไม่สำเร็จ")),
  });

  if (isError) {
    return <Alert severity="error">ไม่สามารถโหลดรายละเอียดคำขอยกเลิกใบลาได้</Alert>;
  }

  const isRequester = cancellation?.requesterUserId === user?.id;
  const isCurrentApprover = cancellation?.currentApproverId === user?.id && cancellation?.status === "Pending";
  const canSubmit = isRequester && cancellation?.status === "Draft";
  const canCancel = isRequester && cancellation && !["Approved", "Cancelled", "Rejected"].includes(cancellation.status);

  return (
    <Stack spacing={3}>
      <PageHeader title="รายละเอียดคำขอยกเลิกใบลา" subtitle={cancellation?.cancellationRequestNumber ?? "กำลังโหลดข้อมูล"} />
      {cancellation && (
        <>
          <Paper className="premium-card" sx={{ p: 3 }}>
            <Stack spacing={2}>
              <Stack direction={{ xs: "column", sm: "row" }} spacing={2} justifyContent="space-between">
                <Box>
                  <Typography variant="h5">{cancellation.cancellationRequestNumber}</Typography>
                  <Typography color="text.secondary">อ้างอิงใบลาเดิม {cancellation.originalRequestNumber ?? "-"}</Typography>
                </Box>
                <Chip label={getCancellationStatusLabel(cancellation.status)} color={cancellation.status === "Approved" ? "success" : cancellation.status === "Rejected" ? "error" : cancellation.status === "Pending" ? "warning" : "default"} />
              </Stack>
              <Divider />
              <Stack direction={{ xs: "column", md: "row" }} spacing={3}>
                <Info label="ผู้ขอ" value={cancellation.requesterName ?? "-"} />
                <Info label="ประเภทลา" value={cancellation.leaveTypeName ?? "-"} />
                <Info label="จำนวนวันที่คืนยอด" value={formatDays(cancellation.originalLeaveDays)} />
                <Info label="ผู้อนุมัติปัจจุบัน" value={cancellation.currentApproverName ?? "-"} />
              </Stack>
              <Typography><strong>เหตุผล:</strong> {cancellation.reason}</Typography>
              {cancellation.balanceRestoredAt && <Alert severity="success">คืนยอดวันลาแล้วเมื่อ {formatDateTime(cancellation.balanceRestoredAt)}</Alert>}
              {cancellation.revisionReason && <Alert severity="warning">เหตุผลตีกลับ: {cancellation.revisionReason}</Alert>}
              <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
                <Button component={RouterLink} to={`/leave/${cancellation.originalLeaveRequestId}`} variant="outlined">ดูใบลาเดิม</Button>
                {canSubmit && <Button variant="contained" startIcon={<SendOutlinedIcon />} disabled={submitMutation.isPending} onClick={() => submitMutation.mutate()}>ส่งคำขอ</Button>}
                {canCancel && <Button color="warning" startIcon={<CancelOutlinedIcon />} disabled={cancelMutation.isPending} onClick={() => cancelMutation.mutate()}>ยกเลิกคำขอ</Button>}
              </Stack>
            </Stack>
          </Paper>

          <Paper className="premium-card" sx={{ p: 3 }}>
            <Stack spacing={2}>
              <Typography variant="h6">การพิจารณา</Typography>
              <Typography variant="body2" color="text.secondary">
                แสดงลำดับขั้น ผู้อนุมัติ สถานะ วันที่ดำเนินการ และหมายเหตุของคำขอนี้
              </Typography>
              {isCurrentApprover && <TextField label="หมายเหตุผู้อนุมัติ" value={remark} onChange={(event) => setRemark(event.target.value)} fullWidth />}
              {isCurrentApprover && (
                <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
                  <Button variant="contained" color="success" startIcon={<CheckCircleOutlineOutlinedIcon />} disabled={approveMutation.isPending} onClick={() => approveMutation.mutate()}>อนุมัติ</Button>
                  <Button variant="outlined" color="error" startIcon={<HighlightOffOutlinedIcon />} disabled={rejectMutation.isPending} onClick={() => rejectMutation.mutate()}>ไม่อนุมัติ</Button>
                  <Button variant="outlined" color="warning" startIcon={<ReplyOutlinedIcon />} onClick={() => setReturnDialogOpen(true)}>ตีกลับรอแก้ไข</Button>
                </Stack>
              )}
              {!isCurrentApprover && cancellation.status === "Pending" && <Alert severity="info">ปุ่มอนุมัติจะแสดงเฉพาะผู้อนุมัติที่ถึงคิวปัจจุบันเท่านั้น</Alert>}
              <ApprovalWorkflowTimeline
                approvals={approvals.map((approval) => ({
                  ...approval,
                  leaveRequestId: approval.leaveCancellationRequestId,
                }))}
              />
            </Stack>
          </Paper>
        </>
      )}

      <Dialog open={returnDialogOpen} onClose={() => setReturnDialogOpen(false)} fullWidth maxWidth="sm">
        <DialogTitle>ตีกลับคำขอยกเลิกใบลา</DialogTitle>
        <DialogContent>
          <TextField label="เหตุผล" value={revisionReason} onChange={(event) => setRevisionReason(event.target.value)} fullWidth multiline minRows={4} sx={{ mt: 1 }} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setReturnDialogOpen(false)}>ยกเลิก</Button>
          <Button variant="contained" color="warning" disabled={!revisionReason.trim() || returnMutation.isPending} onClick={() => returnMutation.mutate()}>ยืนยันตีกลับ</Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
}

function Info({ label, value }: { label: string; value: string }) {
  return (
    <Box sx={{ minWidth: 180 }}>
      <Typography variant="body2" color="text.secondary">{label}</Typography>
      <Typography fontWeight={700}>{value}</Typography>
    </Box>
  );
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat("th-TH", { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));
}

function getErrorMessage(error: unknown, fallback: string) {
  const maybeError = error as { response?: { data?: { message?: string } } };
  return maybeError.response?.data?.message ?? fallback;
}
