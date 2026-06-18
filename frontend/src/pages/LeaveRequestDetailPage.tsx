import UploadFileOutlinedIcon from "@mui/icons-material/UploadFileOutlined";
import DownloadOutlinedIcon from "@mui/icons-material/DownloadOutlined";
import { Alert, Button, Card, CardContent, Chip, Divider, Stack, Table, TableBody, TableCell, TableHead, TableRow, TextField, Typography } from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { isAxiosError } from "axios";
import dayjs from "dayjs";
import { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  approveLeaveRequest,
  cancelLeaveRequest,
  downloadLeaveAttachment,
  getLeaveApprovals,
  getLeaveAttachments,
  getLeaveRequest,
  rejectLeaveRequest,
  submitLeaveRequest,
  uploadLeaveAttachment,
} from "../api/leaveApi";
import { PageHeader } from "../components/PageHeader";
import { PermissionGuard } from "../context/PermissionContext";

const statusLabels: Record<string, string> = {
  Draft: "แบบร่าง",
  Pending: "รออนุมัติ",
  Approved: "อนุมัติแล้ว",
  Rejected: "ไม่อนุมัติ",
  Cancelled: "ยกเลิก",
};

export function LeaveRequestDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [remark, setRemark] = useState("");
  const [file, setFile] = useState<File | null>(null);
  const maxUploadMb = Number(import.meta.env.VITE_MAX_UPLOAD_SIZE_MB ?? 5);

  const { data: request } = useQuery({ queryKey: ["leave-requests", id], queryFn: () => getLeaveRequest(id!), enabled: Boolean(id) });
  const { data: attachments = [] } = useQuery({ queryKey: ["leave-requests", id, "attachments"], queryFn: () => getLeaveAttachments(id!), enabled: Boolean(id) });
  const { data: approvals = [] } = useQuery({ queryKey: ["leave-requests", id, "approvals"], queryFn: () => getLeaveApprovals(id!), enabled: Boolean(id) });

  const invalidate = async () => {
    await queryClient.invalidateQueries({ queryKey: ["leave-requests"] });
    await queryClient.invalidateQueries({ queryKey: ["leave-requests", id] });
    await queryClient.invalidateQueries({ queryKey: ["leave-requests", id, "attachments"] });
    await queryClient.invalidateQueries({ queryKey: ["leave-requests", id, "approvals"] });
  };

  const submitMutation = useMutation({ mutationFn: () => submitLeaveRequest(id!), onSuccess: invalidate });
  const cancelMutation = useMutation({ mutationFn: () => cancelLeaveRequest(id!), onSuccess: invalidate });
  const approveMutation = useMutation({ mutationFn: () => approveLeaveRequest(id!, remark), onSuccess: invalidate });
  const rejectMutation = useMutation({ mutationFn: () => rejectLeaveRequest(id!, remark), onSuccess: invalidate });
  const uploadMutation = useMutation({
    mutationFn: () => uploadLeaveAttachment(id!, file!),
    onSuccess: async () => {
      setFile(null);
      await invalidate();
    },
  });

  async function handleDownloadAttachment(attachmentId: string, fileName: string) {
    const blob = await downloadLeaveAttachment(attachmentId);
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);
  }

  if (!request) {
    return <PageHeader title="รายละเอียดคำขอลา" subtitle="กำลังโหลดข้อมูลคำขอลา..." />;
  }

  const canSubmit = request.status === "Draft";
  const canCancel = request.status === "Draft" || request.status === "Pending";
  const canDecide = request.status === "Pending";

  return (
    <>
      <Stack direction={{ xs: "column", sm: "row" }} justifyContent="space-between" spacing={2}>
        <PageHeader title="รายละเอียดคำขอลา" subtitle={`${request.leaveTypeName ?? "-"} · ${statusLabels[request.status] ?? request.status}`} />
        <Button variant="outlined" onClick={() => navigate("/leave")}>กลับรายการคำขอลา</Button>
      </Stack>

      <Stack spacing={2}>
        {(submitMutation.isError || cancelMutation.isError || approveMutation.isError || rejectMutation.isError) && (
          <Alert severity="error">
            {getApiErrorMessage(
              submitMutation.error ?? cancelMutation.error ?? approveMutation.error ?? rejectMutation.error,
              "ดำเนินการคำขอลาไม่สำเร็จ",
            )}
          </Alert>
        )}
        <Card>
          <CardContent>
            <Stack spacing={1.5}>
              <Typography variant="h6">ข้อมูลคำขอ</Typography>
              <Typography>ผู้ขอ: {request.fullname ?? "-"}</Typography>
              <Typography>ช่วงวันที่ลา: {dayjs(request.startDate).format("DD/MM/YYYY")} - {dayjs(request.endDate).format("DD/MM/YYYY")}</Typography>
              <Typography>จำนวนวัน: {request.totalDays}</Typography>
              <Typography>เหตุผล: {request.reason}</Typography>
              <Typography>ผู้อนุมัติปัจจุบัน: {request.currentApproverName ?? "-"}</Typography>
              <Chip sx={{ width: "fit-content" }} label={statusLabels[request.status] ?? request.status} />
              <Stack direction="row" spacing={1.5} flexWrap="wrap">
                <PermissionGuard permission="LeaveManagement.Create">
                  <Button variant="contained" disabled={!canSubmit || submitMutation.isPending} onClick={() => submitMutation.mutate()}>
                    ส่งคำขออนุมัติ
                  </Button>
                </PermissionGuard>
                <PermissionGuard permission="LeaveManagement.Edit">
                  <Button variant="outlined" color="warning" disabled={!canCancel || cancelMutation.isPending} onClick={() => cancelMutation.mutate()}>
                    ยกเลิกคำขอ
                  </Button>
                </PermissionGuard>
              </Stack>
            </Stack>
          </CardContent>
        </Card>

        <PermissionGuard permission="LeaveManagement.Approve">
          <Card>
            <CardContent>
              <Stack spacing={2}>
                <Typography variant="h6">การอนุมัติ</Typography>
                <TextField label="หมายเหตุ" value={remark} onChange={(event) => setRemark(event.target.value)} />
                <Stack direction="row" spacing={1.5}>
                  <Button variant="contained" color="success" disabled={!canDecide || approveMutation.isPending} onClick={() => approveMutation.mutate()}>
                    อนุมัติ
                  </Button>
                  <Button variant="outlined" color="error" disabled={!canDecide || rejectMutation.isPending} onClick={() => rejectMutation.mutate()}>
                    ไม่อนุมัติ
                  </Button>
                </Stack>
              </Stack>
            </CardContent>
          </Card>
        </PermissionGuard>

        <Card>
          <CardContent>
            <Stack spacing={2}>
              <Typography variant="h6">ไฟล์แนบ</Typography>
              {uploadMutation.isError && <Alert severity="error">อัปโหลดไฟล์ไม่สำเร็จ กรุณาตรวจสอบชนิดไฟล์และขนาดไม่เกิน {maxUploadMb} MB</Alert>}
              <PermissionGuard permission="LeaveManagement.Edit">
                <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5}>
                  <Button component="label" variant="outlined" startIcon={<UploadFileOutlinedIcon />}>
                    เลือกไฟล์
                    <input hidden type="file" accept=".pdf,.jpg,.jpeg,.png" onChange={(event) => setFile(event.target.files?.[0] ?? null)} />
                  </Button>
                  <Typography sx={{ alignSelf: "center" }}>{file?.name ?? "ยังไม่ได้เลือกไฟล์"}</Typography>
                  <Button variant="contained" disabled={!file || uploadMutation.isPending} onClick={() => uploadMutation.mutate()}>
                    อัปโหลด
                  </Button>
                </Stack>
              </PermissionGuard>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>ชื่อไฟล์</TableCell>
                    <TableCell>ขนาด</TableCell>
                    <TableCell>ผู้อัปโหลด</TableCell>
                    <TableCell>วันที่</TableCell>
                    <TableCell align="right">ดาวน์โหลด</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {attachments.length ? attachments.map((item) => (
                    <TableRow key={item.id}>
                      <TableCell>{item.fileName}</TableCell>
                      <TableCell>{Math.ceil(item.fileSizeBytes / 1024)} KB</TableCell>
                      <TableCell>{item.uploadedByName ?? "-"}</TableCell>
                      <TableCell>{dayjs(item.createdAt).format("DD/MM/YYYY HH:mm")}</TableCell>
                      <TableCell align="right">
                        <PermissionGuard permission="LeaveAttachment.Download">
                          <Button size="small" variant="outlined" startIcon={<DownloadOutlinedIcon />} onClick={() => handleDownloadAttachment(item.id, item.fileName)}>
                            ดาวน์โหลด
                          </Button>
                        </PermissionGuard>
                      </TableCell>
                    </TableRow>
                  )) : (
                    <TableRow><TableCell colSpan={5}>ยังไม่มีไฟล์แนบ</TableCell></TableRow>
                  )}
                </TableBody>
              </Table>
            </Stack>
          </CardContent>
        </Card>

        <Card>
          <CardContent>
            <Typography variant="h6">ประวัติการอนุมัติ</Typography>
            <Divider sx={{ my: 2 }} />
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>ลำดับ</TableCell>
                  <TableCell>ขั้นอนุมัติ</TableCell>
                  <TableCell>ผู้อนุมัติ</TableCell>
                  <TableCell>สถานะ</TableCell>
                  <TableCell>หมายเหตุ</TableCell>
                  <TableCell>วันที่ดำเนินการ</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {approvals.length ? approvals.map((item) => (
                  <TableRow key={item.id}>
                    <TableCell>{item.stepOrder}</TableCell>
                    <TableCell>{item.stepName ?? `ขั้นที่ ${item.stepOrder}`}</TableCell>
                    <TableCell>{item.approverName ?? "-"}</TableCell>
                    <TableCell>{statusLabels[item.status] ?? item.status}</TableCell>
                    <TableCell>{item.remark ?? "-"}</TableCell>
                    <TableCell>{item.actionAt ? dayjs(item.actionAt).format("DD/MM/YYYY HH:mm") : "-"}</TableCell>
                  </TableRow>
                )) : (
                  <TableRow><TableCell colSpan={6}>ยังไม่มีประวัติการอนุมัติ</TableCell></TableRow>
                )}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      </Stack>
    </>
  );
}

function getApiErrorMessage(error: unknown, fallback: string) {
  if (isAxiosError<{ message?: string }>(error)) {
    return error.response?.data?.message ?? fallback;
  }

  return fallback;
}
