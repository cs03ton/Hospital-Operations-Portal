import CalendarMonthOutlinedIcon from "@mui/icons-material/CalendarMonthOutlined";
import DescriptionOutlinedIcon from "@mui/icons-material/DescriptionOutlined";
import EventRepeatOutlinedIcon from "@mui/icons-material/EventRepeatOutlined";
import SaveOutlinedIcon from "@mui/icons-material/SaveOutlined";
import SendOutlinedIcon from "@mui/icons-material/SendOutlined";
import UndoOutlinedIcon from "@mui/icons-material/UndoOutlined";
import { alpha } from "@mui/material/styles";
import { Alert, Box, Button, Card, CardContent, Chip, MenuItem, Stack, TextField, Typography } from "@mui/material";
import { useMutation, useQuery } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { createLeaveCancellationRequest, getLeaveCancellationEligibility, getLeaveRequest, getLeaveRequestsPaged } from "../api/leaveApi";
import { PageHeader } from "../components/PageHeader";
import { useAuth } from "../context/AuthContext";
import { useNotification } from "../hooks/useNotification";
import { formatThaiDate } from "../utils/dateFormat";
import { getLeaveTypeLabel } from "../utils/leaveLabels";

export function LeaveCancellationCreatePage() {
  const [searchParams] = useSearchParams();
  const initialLeaveRequestId = searchParams.get("leaveRequestId") ?? "";
  const navigate = useNavigate();
  const { user } = useAuth();
  const { showSuccess, showError } = useNotification();
  const [selectedLeaveRequestId, setSelectedLeaveRequestId] = useState(initialLeaveRequestId);
  const [reason, setReason] = useState("");

  const { data: approvedRequests, isLoading: isLoadingApprovedRequests } = useQuery({
    queryKey: ["leave-requests", "cancellation-options", user?.id],
    queryFn: () => getLeaveRequestsPaged({ scope: "mine", status: "Approved", page: 1, pageSize: 100 }),
  });
  const { data: originalFromParam } = useQuery({
    queryKey: ["leave-requests", selectedLeaveRequestId],
    queryFn: () => getLeaveRequest(selectedLeaveRequestId),
    enabled: Boolean(selectedLeaveRequestId) && !approvedRequests?.items.some((item) => item.id === selectedLeaveRequestId),
  });
  const { data: eligibility } = useQuery({
    queryKey: ["leave-cancellation-eligibility", selectedLeaveRequestId],
    queryFn: () => getLeaveCancellationEligibility(selectedLeaveRequestId),
    enabled: Boolean(selectedLeaveRequestId),
  });

  const original = approvedRequests?.items.find((item) => item.id === selectedLeaveRequestId) ?? originalFromParam;

  const mutation = useMutation({
    mutationFn: (submit: boolean) => createLeaveCancellationRequest({ originalLeaveRequestId: selectedLeaveRequestId, reason, submit }),
    onSuccess: (data, submit) => {
      showSuccess(submit ? "ส่งคำขอยกเลิกใบลาเรียบร้อยแล้ว" : "บันทึกแบบร่างคำขอยกเลิกใบลาเรียบร้อยแล้ว");
      navigate(`/leave/cancellations/${data.id}`);
    },
    onError: (error: unknown) => showError(getErrorMessage(error, "ไม่สามารถบันทึกคำขอยกเลิกใบลาได้")),
  });

  const invalid = !selectedLeaveRequestId || !eligibility?.canCreate || !reason.trim();

  return (
    <Stack spacing={3}>
      <PageHeader title="สร้างคำขอยกเลิกใบลา" subtitle="คำขอยกเลิกใบลาจะเข้าสู่สายอนุมัติเดิม และคืนยอดเมื่ออนุมัติครบทุกขั้น" />
      {!selectedLeaveRequestId && <Alert severity="info">กรุณาเลือกใบลาเดิมของตนเองที่ต้องการขอยกเลิก</Alert>}
      {eligibility && !eligibility.canCreate && <Alert severity="warning">{eligibility.message}</Alert>}
      <Card
        sx={(theme) => ({
          borderTop: `5px solid ${theme.palette.warning.main}`,
          boxShadow: `0 18px 38px ${alpha(theme.palette.primary.dark, 0.08)}`,
        })}
      >
        <CardContent sx={{ p: { xs: 2.25, md: 3 }, overflow: "hidden" }}>
          <Stack spacing={3} sx={{ minWidth: 0 }}>
            <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5} alignItems={{ xs: "flex-start", sm: "center" }} justifyContent="space-between">
              <Box sx={{ minWidth: 0 }}>
                <Typography variant="h5" color="primary" fontWeight={800}>
                  ใบลาอ้างอิง
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  เลือกใบลาเดิมที่ได้รับอนุมัติแล้ว ระบบจะใช้สายอนุมัติเดิมและคืนยอดเมื่ออนุมัติครบทุกขั้น
                </Typography>
              </Box>
              <Chip
                icon={<UndoOutlinedIcon />}
                label={eligibility?.canCreate ? "พร้อมส่งคำขอ" : "รอเลือกใบลา"}
                color={eligibility?.canCreate ? "success" : "default"}
                variant={eligibility?.canCreate ? "filled" : "outlined"}
                sx={{ flexShrink: 0 }}
              />
            </Stack>

          <TextField
            select
            label="เลือกใบลาเดิมของตนเอง"
            value={selectedLeaveRequestId}
            onChange={(event) => setSelectedLeaveRequestId(event.target.value)}
            helperText={isLoadingApprovedRequests ? "กำลังโหลดรายการใบลาที่อนุมัติแล้ว..." : "แสดงเฉพาะใบลาของคุณที่อนุมัติแล้วและสามารถนำมาขอยกเลิกได้"}
            fullWidth
            required
            sx={{
              "& .MuiSelect-select": {
                display: "block",
                overflow: "hidden",
                textOverflow: "ellipsis",
                whiteSpace: "nowrap",
              },
            }}
          >
            {(approvedRequests?.items ?? []).map((item) => (
              <MenuItem key={item.id} value={item.id} sx={{ whiteSpace: "normal" }}>
                {item.requestNumber ?? "-"} · {getLeaveTypeLabel(item.leaveTypeName)} · {formatThaiDate(item.startDate)} ถึง {formatThaiDate(item.endDate)} · {formatDays(item.totalDays)}
              </MenuItem>
            ))}
          </TextField>

          <Box
            sx={{
              display: "grid",
              gridTemplateColumns: {
                xs: "1fr",
                sm: "repeat(2, minmax(0, 1fr))",
                lg: "repeat(4, minmax(0, 1fr))",
              },
              gap: 2,
              width: "100%",
            }}
          >
            <Box sx={{ minWidth: 0 }}>
              <ReferenceInfoCard icon={<DescriptionOutlinedIcon />} label="เลขที่คำขอเดิม" value={original?.requestNumber ?? "-"} />
            </Box>
            <Box sx={{ minWidth: 0 }}>
              <ReferenceInfoCard icon={<EventRepeatOutlinedIcon />} label="ประเภทลา" value={original ? getLeaveTypeLabel(original.leaveTypeName) : "-"} />
            </Box>
            <Box sx={{ minWidth: 0 }}>
              <ReferenceInfoCard icon={<CalendarMonthOutlinedIcon />} label="วันที่ลา" value={original ? `${formatThaiDate(original.startDate)} ถึง ${formatThaiDate(original.endDate)}` : "-"} />
            </Box>
            <Box sx={{ minWidth: 0 }}>
              <Box
                sx={(theme) => ({
                  height: "100%",
                  minHeight: 112,
                  border: `1px solid ${alpha(theme.palette.warning.main, 0.42)}`,
                  borderRadius: 3,
                  p: 2,
                  background: `linear-gradient(135deg, ${alpha(theme.palette.warning.light, 0.18)}, ${alpha(theme.palette.background.paper, 0.96)})`,
                })}
              >
                <Typography variant="caption" color="text.secondary">
                  จำนวนวันที่จะคืนยอด
                </Typography>
                <Typography variant="h4" color="warning.dark" fontWeight={900} sx={{ mt: 0.5 }}>
                  {formatDays(eligibility?.originalLeaveDays ?? original?.totalDays ?? 0)}
                </Typography>
              </Box>
            </Box>
          </Box>

          <TextField
            label="เหตุผลการขอยกเลิกใบลา"
            value={reason}
            onChange={(event) => setReason(event.target.value)}
            multiline
            minRows={4}
            fullWidth
            required
            sx={{ "& .MuiInputBase-root": { alignItems: "flex-start" } }}
          />
          <Stack
            direction={{ xs: "column", sm: "row" }}
            spacing={1.5}
            justifyContent="flex-end"
            sx={(theme) => ({
              pt: 2,
              borderTop: `1px solid ${alpha(theme.palette.divider, 0.8)}`,
            })}
          >
            <Button variant="outlined" disabled={invalid || mutation.isPending} startIcon={<SaveOutlinedIcon />} onClick={() => mutation.mutate(false)}>
              บันทึกแบบร่าง
            </Button>
            <Button variant="contained" disabled={invalid || mutation.isPending} startIcon={<SendOutlinedIcon />} onClick={() => mutation.mutate(true)}>
              ส่งคำขอ
            </Button>
          </Stack>
          </Stack>
        </CardContent>
      </Card>
    </Stack>
  );
}

export function formatDays(value: number) {
  return `${Number(value || 0).toLocaleString("th-TH", { maximumFractionDigits: 1 })} วัน`;
}

function getErrorMessage(error: unknown, fallback: string) {
  const maybeError = error as { response?: { data?: { message?: string } } };
  return maybeError.response?.data?.message ?? fallback;
}

function ReferenceInfoCard({ icon, label, value }: { icon: ReactNode; label: string; value: string }) {
  return (
    <Box
      sx={(theme) => ({
        height: "100%",
        minHeight: 112,
        border: `1px solid ${theme.palette.divider}`,
        borderRadius: 3,
        p: 2,
        bgcolor: alpha(theme.palette.background.paper, 0.92),
      })}
    >
      <Stack direction="row" spacing={1.25} alignItems="flex-start">
        <Box
          sx={(theme) => ({
            width: 38,
            height: 38,
            borderRadius: "50%",
            display: "grid",
            placeItems: "center",
            bgcolor: alpha(theme.palette.primary.main, 0.08),
            color: "primary.main",
            flex: "0 0 auto",
          })}
        >
          {icon}
        </Box>
        <Box sx={{ minWidth: 0 }}>
          <Typography variant="caption" color="text.secondary">
            {label}
          </Typography>
          <Typography fontWeight={800} sx={{ overflowWrap: "anywhere" }}>
            {value}
          </Typography>
        </Box>
      </Stack>
    </Box>
  );
}
