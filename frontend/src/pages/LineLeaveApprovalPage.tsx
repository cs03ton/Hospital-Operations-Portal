import ArrowBackOutlinedIcon from "@mui/icons-material/ArrowBackOutlined";
import CheckCircleOutlineOutlinedIcon from "@mui/icons-material/CheckCircleOutlineOutlined";
import HighlightOffOutlinedIcon from "@mui/icons-material/HighlightOffOutlined";
import { Alert, Box, Button, Card, CardContent, Chip, Grid, Stack, TextField, Typography } from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams, useSearchParams } from "react-router-dom";
import {
  approveLeaveRequest,
  getLeaveApprovals,
  getLeaveRequest,
  recordLineApprovalActionOpened,
  rejectLeaveRequest,
} from "../api/leaveApi";
import { LoadingState } from "../components/common/LoadingState";
import { ApprovalWorkflowTimeline } from "../components/leave/ApprovalWorkflowTimeline";
import { PageHeader } from "../components/PageHeader";
import { useAuth } from "../context/AuthContext";
import { useNotification } from "../hooks/useNotification";
import { formatThaiDate } from "../utils/dateFormat";
import { getLeaveDurationTypeLabel, getLeaveStatusColor, getLeaveStatusLabel, getLeaveTypeLabel } from "../utils/leaveLabels";
import { getLeaveRequestCode } from "../utils/leaveTrackingLabels";

export function LineLeaveApprovalPage() {
  const { id } = useParams();
  const [searchParams] = useSearchParams();
  const action = searchParams.get("action") === "reject" ? "reject" : "approve";
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { user } = useAuth();
  const { showError, showSuccess } = useNotification();
  const [remark, setRemark] = useState("");

  const { data: request, isLoading } = useQuery({ queryKey: ["leave-requests", id], queryFn: () => getLeaveRequest(id!), enabled: Boolean(id) });
  const { data: approvals = [] } = useQuery({ queryKey: ["leave-requests", id, "approvals"], queryFn: () => getLeaveApprovals(id!), enabled: Boolean(id) });

  useEffect(() => {
    if (id) {
      recordLineApprovalActionOpened(id, action).catch(() => undefined);
    }
  }, [action, id]);

  const currentApproval = useMemo(
    () => approvals.find((item) => item.status === "Pending"),
    [approvals],
  );

  const invalidate = async () => {
    await queryClient.invalidateQueries({ queryKey: ["leave-requests"] });
    await queryClient.invalidateQueries({ queryKey: ["leave-requests", id] });
    await queryClient.invalidateQueries({ queryKey: ["leave-requests", id, "approvals"] });
    await queryClient.invalidateQueries({ queryKey: ["approvals", "my-pending"] });
    await queryClient.invalidateQueries({ queryKey: ["notifications"] });
    await queryClient.invalidateQueries({ queryKey: ["dashboard-summary"] });
  };

  const approveMutation = useMutation({
    mutationFn: async () => approveLeaveRequest(id!, remark),
    onSuccess: async () => {
      await recordLineApprovalActionOpened(id!, "approve-completed").catch(() => undefined);
      showSuccess("อนุมัติคำขอลาเรียบร้อยแล้ว");
      await invalidate();
      navigate(`/leave/${id}`, { replace: true });
    },
    onError: () => showError("อนุมัติคำขอลาไม่สำเร็จ"),
  });

  const rejectMutation = useMutation({
    mutationFn: async () => rejectLeaveRequest(id!, remark),
    onSuccess: async () => {
      await recordLineApprovalActionOpened(id!, "reject-completed").catch(() => undefined);
      showSuccess("ไม่อนุมัติคำขอลาเรียบร้อยแล้ว");
      await invalidate();
      navigate(`/leave/${id}`, { replace: true });
    },
    onError: () => showError("ไม่อนุมัติคำขอลาไม่สำเร็จ"),
  });

  if (isLoading || !request) {
    return (
      <>
        <PageHeader title="อนุมัติคำขอลา" subtitle="กำลังโหลดข้อมูลจาก LINE..." />
        <LoadingState message="กำลังโหลดข้อมูลคำขอลา..." />
      </>
    );
  }

  const requestCode = getLeaveRequestCode(request.requestNumber, request.id);
  const isCurrentApprover = request.status === "Pending" && request.currentApproverId === user?.id;
  const alreadyDone = request.status !== "Pending";
  const rejectRemarkMissing = action === "reject" && !remark.trim();

  return (
    <>
      <PageHeader title="อนุมัติคำขอลาจาก LINE" subtitle={`เลขที่คำขอ ${requestCode}`} />
      <Stack spacing={2}>
        {!isCurrentApprover && !alreadyDone && (
          <Alert severity="error">คุณไม่มีสิทธิ์อนุมัติรายการนี้ หรือยังไม่ถึงคิวอนุมัติของคุณ</Alert>
        )}
        {alreadyDone && (
          <Alert severity="info">รายการนี้ถูกดำเนินการแล้ว สถานะปัจจุบัน: {getLeaveStatusLabel(request.status)}</Alert>
        )}

        <Card sx={{ borderTop: (theme) => `4px solid ${theme.palette.secondary.main}` }}>
          <CardContent>
            <Stack spacing={2}>
              <Stack direction={{ xs: "column", md: "row" }} justifyContent="space-between" spacing={1}>
                <Box>
                  <Typography variant="h5" color="primary">รายละเอียดคำขอลา</Typography>
                  <Typography color="text.secondary">ผู้อนุมัติปัจจุบัน: {request.currentApproverName ?? "-"}</Typography>
                </Box>
                <Chip color={getLeaveStatusColor(request.status)} label={getLeaveStatusLabel(request.status)} />
              </Stack>
              <Grid container spacing={2}>
                <Info label="เลขที่คำขอ" value={requestCode} />
                <Info label="ผู้ขอ" value={request.fullname ?? "-"} />
                <Info label="ประเภทลา" value={getLeaveTypeLabel(request.leaveTypeName)} />
                <Info label="วันที่ลา" value={`${formatThaiDate(request.startDate)} - ${formatThaiDate(request.endDate)}`} />
                <Info label="จำนวนวัน" value={`${request.totalDays} วัน`} />
                <Info label="ช่วงเวลา" value={getLeaveDurationTypeLabel(request.durationType)} />
                <Info label="ขั้นอนุมัติปัจจุบัน" value={request.currentStepName ?? currentApproval?.stepName ?? "-"} />
                <Grid item xs={12}>
                  <Typography variant="caption" color="text.secondary">เหตุผล</Typography>
                  <Typography sx={{ whiteSpace: "pre-wrap" }}>{request.reason || "-"}</Typography>
                </Grid>
              </Grid>
            </Stack>
          </CardContent>
        </Card>

        <ApprovalWorkflowTimeline approvals={approvals} />

        <Card>
          <CardContent>
            <Stack spacing={2}>
              <Typography variant="h6">{action === "approve" ? "ยืนยันการอนุมัติ" : "ยืนยันไม่อนุมัติ"}</Typography>
              {action === "reject" && (
                <TextField
                  multiline
                  minRows={3}
                  label="เหตุผลไม่อนุมัติ"
                  value={remark}
                  onChange={(event) => setRemark(event.target.value)}
                  required
                />
              )}
              {action === "approve" && (
                <TextField
                  multiline
                  minRows={2}
                  label="หมายเหตุ (ไม่บังคับ)"
                  value={remark}
                  onChange={(event) => setRemark(event.target.value)}
                />
              )}
              <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                <Button variant="outlined" startIcon={<ArrowBackOutlinedIcon />} onClick={() => navigate(`/leave/${id}`)}>
                  กลับไปรายละเอียด
                </Button>
                {action === "approve" ? (
                  <Button
                    variant="contained"
                    color="success"
                    startIcon={<CheckCircleOutlineOutlinedIcon />}
                    disabled={!isCurrentApprover || approveMutation.isPending}
                    onClick={() => approveMutation.mutate()}
                  >
                    ยืนยันอนุมัติ
                  </Button>
                ) : (
                  <Button
                    variant="contained"
                    color="error"
                    startIcon={<HighlightOffOutlinedIcon />}
                    disabled={!isCurrentApprover || rejectRemarkMissing || rejectMutation.isPending}
                    onClick={() => rejectMutation.mutate()}
                  >
                    ยืนยันไม่อนุมัติ
                  </Button>
                )}
              </Stack>
            </Stack>
          </CardContent>
        </Card>
      </Stack>
    </>
  );
}

function Info({ label, value }: { label: string; value: string }) {
  return (
    <Grid item xs={12} md={6}>
      <Box sx={{ p: 1.5, border: (theme) => `1px solid ${theme.palette.divider}`, borderRadius: 2 }}>
        <Typography variant="caption" color="text.secondary">{label}</Typography>
        <Typography fontWeight={700}>{value}</Typography>
      </Box>
    </Grid>
  );
}
