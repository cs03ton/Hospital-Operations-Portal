import ArrowBackOutlinedIcon from "@mui/icons-material/ArrowBackOutlined";
import CancelOutlinedIcon from "@mui/icons-material/CancelOutlined";
import CheckCircleOutlineOutlinedIcon from "@mui/icons-material/CheckCircleOutlineOutlined";
import DeleteOutlineOutlinedIcon from "@mui/icons-material/DeleteOutlineOutlined";
import DownloadOutlinedIcon from "@mui/icons-material/DownloadOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import HighlightOffOutlinedIcon from "@mui/icons-material/HighlightOffOutlined";
import ReplayOutlinedIcon from "@mui/icons-material/ReplayOutlined";
import ReplyOutlinedIcon from "@mui/icons-material/ReplyOutlined";
import SendOutlinedIcon from "@mui/icons-material/SendOutlined";
import UploadFileOutlinedIcon from "@mui/icons-material/UploadFileOutlined";
import VisibilityOutlinedIcon from "@mui/icons-material/VisibilityOutlined";
import { Alert, Box, Button, Card, CardContent, Chip, Dialog, DialogActions, DialogContent, DialogTitle, Grid, Stack, TableBody, TableCell, TableHead, TableRow, TextField, Typography } from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { isAxiosError } from "axios";
import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  approveLeaveRequest,
  cancelLeaveRequest,
  deleteLeaveAttachment,
  downloadLeaveAttachment,
  downloadLeaveRequestPdf,
  getLeaveApprovals,
  getLeaveAttachments,
  getLeaveRequest,
  previewLeaveAttachment,
  rejectLeaveRequest,
  resubmitLeaveRequest,
  returnLeaveRequestForRevision,
  submitLeaveRequest,
  uploadLeaveAttachment,
  type LeaveAttachment,
} from "../api/leaveApi";
import { ActionTooltip } from "../components/common/ActionTooltip";
import { DataTableCard } from "../components/common/DataTableCard";
import { EmptyState } from "../components/common/EmptyState";
import { InfoCard } from "../components/common/InfoCard";
import { LoadingState } from "../components/common/LoadingState";
import { ApprovalWorkflowTimeline } from "../components/leave/ApprovalWorkflowTimeline";
import { AttachmentPreviewDialog } from "../components/leave/AttachmentPreviewDialog";
import { LeaveTrackingCard } from "../components/leave/LeaveTrackingCard";
import { PageHeader } from "../components/PageHeader";
import { useAuth } from "../context/AuthContext";
import { PermissionGuard } from "../context/PermissionContext";
import { useNotification } from "../hooks/useNotification";
import { formatThaiDate, formatThaiDateTime } from "../utils/dateFormat";
import { getLeaveDurationTypeLabel, getLeaveStatusColor, getLeaveStatusLabel, getLeaveTypeLabel } from "../utils/leaveLabels";
import { getLeaveRequestCode } from "../utils/leaveTrackingLabels";

export function LeaveRequestDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { user } = useAuth();
  const { showError, showSuccess } = useNotification();
  const [remark, setRemark] = useState("");
  const [revisionReason, setRevisionReason] = useState("");
  const [isReturnDialogOpen, setIsReturnDialogOpen] = useState(false);
  const [file, setFile] = useState<File | null>(null);
  const [previewAttachment, setPreviewAttachment] = useState<LeaveAttachment | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [isDownloadingPdf, setIsDownloadingPdf] = useState(false);
  const maxUploadMb = Number(import.meta.env.VITE_MAX_UPLOAD_SIZE_MB ?? 5);

  const { data: request } = useQuery({ queryKey: ["leave-requests", id], queryFn: () => getLeaveRequest(id!), enabled: Boolean(id) });
  const { data: attachments = [] } = useQuery({ queryKey: ["leave-requests", id, "attachments"], queryFn: () => getLeaveAttachments(id!), enabled: Boolean(id) });
  const { data: approvals = [] } = useQuery({ queryKey: ["leave-requests", id, "approvals"], queryFn: () => getLeaveApprovals(id!), enabled: Boolean(id) });

  const invalidate = async () => {
    await queryClient.invalidateQueries({ queryKey: ["leave-requests"] });
    await queryClient.invalidateQueries({ queryKey: ["leave-requests", id] });
    await queryClient.invalidateQueries({ queryKey: ["leave-requests", id, "attachments"] });
    await queryClient.invalidateQueries({ queryKey: ["leave-requests", id, "approvals"] });
    await queryClient.invalidateQueries({ queryKey: ["approvals", "my-pending"] });
    await queryClient.invalidateQueries({ queryKey: ["notifications"] });
    await queryClient.invalidateQueries({ queryKey: ["dashboard-summary"] });
  };

  const submitMutation = useMutation({ mutationFn: () => submitLeaveRequest(id!), onSuccess: async () => { showSuccess("ส่งคำขอลาเข้าสู่กระบวนการอนุมัติเรียบร้อยแล้ว"); await invalidate(); } });
  const resubmitMutation = useMutation({ mutationFn: () => resubmitLeaveRequest(id!), onSuccess: async () => { showSuccess("ส่งคำขอลาใหม่เข้าสู่กระบวนการอนุมัติเรียบร้อยแล้ว"); await invalidate(); } });
  const cancelMutation = useMutation({ mutationFn: () => cancelLeaveRequest(id!), onSuccess: async () => { showSuccess("ยกเลิกคำขอลาเรียบร้อยแล้ว"); await invalidate(); } });
  const approveMutation = useMutation({ mutationFn: () => approveLeaveRequest(id!, remark), onSuccess: async () => { showSuccess("อนุมัติคำขอลาเรียบร้อยแล้ว"); await invalidate(); } });
  const rejectMutation = useMutation({ mutationFn: () => rejectLeaveRequest(id!, remark), onSuccess: async () => { showSuccess("ไม่อนุมัติคำขอลาเรียบร้อยแล้ว"); await invalidate(); } });
  const returnMutation = useMutation({
    mutationFn: () => returnLeaveRequestForRevision(id!, revisionReason),
    onSuccess: async () => {
      setRevisionReason("");
      setIsReturnDialogOpen(false);
      showSuccess("ตีกลับคำขอรอให้ผู้ขอแก้ไขเรียบร้อยแล้ว");
      await invalidate();
    },
  });
  const deleteAttachmentMutation = useMutation({
    mutationFn: (attachmentId: string) => deleteLeaveAttachment(attachmentId),
    onSuccess: async () => {
      showSuccess("ลบไฟล์แนบเรียบร้อยแล้ว");
      await invalidate();
    },
  });
  const uploadMutation = useMutation({
    mutationFn: () => uploadLeaveAttachment(id!, file!),
    onSuccess: async () => {
      setFile(null);
      showSuccess("อัปโหลดไฟล์แนบเรียบร้อยแล้ว");
      await invalidate();
    },
  });
  const previewMutation = useMutation({
    mutationFn: (attachment: LeaveAttachment) => previewLeaveAttachment(id!, attachment.id),
    onSuccess: (blob) => {
      setPreviewUrl((currentUrl) => {
        if (currentUrl) {
          window.URL.revokeObjectURL(currentUrl);
        }
        return window.URL.createObjectURL(blob);
      });
    },
    onError: (error) => {
      showError(getApiErrorMessage(error, "ไม่สามารถแสดงตัวอย่างไฟล์แนบได้"));
    },
  });

  useEffect(() => {
    return () => {
      if (previewUrl) {
        window.URL.revokeObjectURL(previewUrl);
      }
    };
  }, [previewUrl]);

  function handleClosePreview() {
    if (previewUrl) {
      window.URL.revokeObjectURL(previewUrl);
    }
    setPreviewUrl(null);
    setPreviewAttachment(null);
    previewMutation.reset();
  }

  function handlePreviewAttachment(attachment: LeaveAttachment) {
    if (previewUrl) {
      window.URL.revokeObjectURL(previewUrl);
      setPreviewUrl(null);
    }
    setPreviewAttachment(attachment);
    previewMutation.mutate(attachment);
  }

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

  async function handleDownloadPdf() {
    setIsDownloadingPdf(true);
    try {
      const blob = await downloadLeaveRequestPdf(id!);
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = `leave-request-${request?.requestNumber ?? id}.pdf`;
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
      showSuccess("ดาวน์โหลดเอกสารคำขอลาเรียบร้อยแล้ว");
    } catch {
      showError("ดาวน์โหลดเอกสารคำขอลาไม่สำเร็จ");
    } finally {
      setIsDownloadingPdf(false);
    }
  }

  if (!request) {
    return (
      <>
        <PageHeader title="รายละเอียดคำขอลา" subtitle="กำลังโหลดข้อมูลคำขอลา..." />
        <Card>
          <CardContent>
            <LoadingState message="กำลังโหลดข้อมูลคำขอลา..." />
          </CardContent>
        </Card>
      </>
    );
  }

  const canSubmit = request.status === "Draft";
  const isRequester = Boolean(user?.id && user.id === request.userId);
  const canResubmit = isRequester && request.status === "ReturnedForRevision";
  const canCancel = isRequester && (request.status === "Draft" || request.status === "Pending" || request.status === "ReturnedForRevision");
  const canDecide = request.status === "Pending";
  const canCurrentUserDecide = canDecide && Boolean(user?.id && request.currentApproverId && user.id === request.currentApproverId);
  const canCurrentUserManageAttachments = Boolean(user?.id && user.id === request.userId && (request.status === "Draft" || request.status === "ReturnedForRevision"));
  const statusColor = getLeaveStatusColor(request.status);
  const requestCode = getLeaveRequestCode(request.requestNumber, request.id);

  return (
    <>
      <PageHeader title="รายละเอียดคำขอลา" subtitle={`เลขที่คำขอ ${requestCode} · ${getLeaveTypeLabel(request.leaveTypeName)}`} />

      <Stack spacing={2}>
        <InfoCard
          title={`เลขที่คำขอ ${requestCode}`}
          subtitle={`สร้างเมื่อ ${formatThaiDateTime(request.createdAt)} · ผู้อนุมัติปัจจุบัน ${request.currentApproverName ?? "-"}`}
          actions={
            <Stack direction="row" spacing={1} justifyContent={{ xs: "flex-start", md: "flex-end" }} flexWrap="wrap" useFlexGap>
              <ActionTooltip title="กลับไปหน้ารายการคำขอลา">
                <Button variant="outlined" startIcon={<ArrowBackOutlinedIcon />} onClick={() => navigate("/leave")}>
                  กลับ
                </Button>
              </ActionTooltip>
              <PermissionGuard permissions={["LeaveRequest.ViewOwn", "LeaveRequest.ViewPendingApproval", "LeaveRequest.ViewDepartment", "LeaveRequest.ViewAll"]}>
                <ActionTooltip title="ดาวน์โหลดเอกสารคำขอลา">
                  <Button variant="contained" startIcon={<DownloadOutlinedIcon />} disabled={isDownloadingPdf} onClick={handleDownloadPdf}>
                    {isDownloadingPdf ? "กำลังสร้าง PDF..." : "ดาวน์โหลดเอกสารคำขอลา"}
                  </Button>
                </ActionTooltip>
              </PermissionGuard>
              <PermissionGuard permission="LeaveRequest.Create">
                <ActionTooltip title="ส่งคำขอลาเพื่ออนุมัติ">
                  <Button variant="contained" startIcon={<SendOutlinedIcon />} disabled={!canSubmit || submitMutation.isPending} onClick={() => submitMutation.mutate()}>
                    ส่งคำขอ
                  </Button>
                </ActionTooltip>
              </PermissionGuard>
              {canResubmit && (
                <PermissionGuard permission="LeaveRequest.Create">
                  <ActionTooltip title="ส่งคำขอลาที่แก้ไขแล้วกลับเข้าสู่กระบวนการอนุมัติ">
                    <Button variant="contained" startIcon={<ReplayOutlinedIcon />} disabled={resubmitMutation.isPending} onClick={() => resubmitMutation.mutate()}>
                      ส่งคำขอใหม่
                    </Button>
                  </ActionTooltip>
                </PermissionGuard>
              )}
              {canCurrentUserManageAttachments && (
                <PermissionGuard permission="LeaveRequest.EditOwn">
                  <ActionTooltip title="แก้ไขข้อมูลคำขอลา">
                    <Button variant="outlined" startIcon={<EditOutlinedIcon />} onClick={() => navigate(`/leave/${id}/edit`)}>
                      แก้ไขคำขอ
                    </Button>
                  </ActionTooltip>
                </PermissionGuard>
              )}
              <PermissionGuard permission="LeaveRequest.CancelOwn">
                <ActionTooltip title="ยกเลิกคำขอลา">
                  <Button variant="outlined" color="error" startIcon={<CancelOutlinedIcon />} disabled={!canCancel || cancelMutation.isPending} onClick={() => cancelMutation.mutate()}>
                    ยกเลิกคำขอ
                  </Button>
                </ActionTooltip>
              </PermissionGuard>
            </Stack>
          }
        >
          <Grid container spacing={1.5}>
            <Grid item xs={12} sm={6} md={3}>
              <DetailItem label="สถานะคำขอ" value={getLeaveStatusLabel(request.status)} chipColor={statusColor} />
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <DetailItem label="ประเภทลา" value={getLeaveTypeLabel(request.leaveTypeName)} />
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <DetailItem label="ประเภทช่วงเวลา" value={getLeaveDurationTypeLabel(request.durationType)} />
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <DetailItem label="วันที่เริ่มลา" value={formatThaiDate(request.startDate)} />
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <DetailItem label="วันที่สิ้นสุด" value={formatThaiDate(request.endDate)} />
            </Grid>
          </Grid>
        </InfoCard>

        {(submitMutation.isError || resubmitMutation.isError || cancelMutation.isError || approveMutation.isError || rejectMutation.isError || returnMutation.isError || deleteAttachmentMutation.isError) && (
          <Alert severity="error">
            {getApiErrorMessage(
              submitMutation.error ?? resubmitMutation.error ?? cancelMutation.error ?? approveMutation.error ?? rejectMutation.error ?? returnMutation.error ?? deleteAttachmentMutation.error,
              "ดำเนินการคำขอลาไม่สำเร็จ",
            )}
          </Alert>
        )}

        <LeaveTrackingCard request={request} />

        {request.status === "ReturnedForRevision" && (
          <Alert severity="warning">
            คำขอนี้ถูกตีกลับและรอให้ผู้ขอแก้ไขข้อมูลหรือไฟล์แนบ
            {request.returnedForRevisionByName ? ` โดย ${request.returnedForRevisionByName}` : ""}
            {request.returnedForRevisionAt ? ` เมื่อ ${formatThaiDateTime(request.returnedForRevisionAt)}` : ""}
            {request.revisionReason ? ` เหตุผล: ${request.revisionReason}` : ""}
          </Alert>
        )}

        <InfoCard title="ข้อมูลคำขอลา" subtitle="รายละเอียดผู้ขอลาและข้อมูลการลาของคำขอนี้">
          <Grid container spacing={3} alignItems="flex-start">
            <Grid item xs={12} md={6}>
              <Stack spacing={2}>
                <Typography variant="subtitle1" fontWeight={800}>
                  ข้อมูลผู้ขอลา
                </Typography>
                <Grid container spacing={1.5}>
                  <Grid item xs={12}>
                    <DetailItem label="ชื่อผู้ขอลา" value={request.fullname ?? "-"} />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <DetailItem label="รหัสพนักงาน" value="-" />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <DetailItem label="หน่วยงาน" value="-" />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <DetailItem label="ตำแหน่ง" value="-" />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <DetailItem label="วันที่สร้างคำขอ" value={formatThaiDateTime(request.createdAt)} />
                  </Grid>
                </Grid>
              </Stack>
            </Grid>
            <Grid item xs={12} md={6}>
              <Stack spacing={2} sx={{ borderLeft: { md: 1 }, borderColor: "divider", pl: { md: 3 } }}>
                <Typography variant="subtitle1" fontWeight={800}>
                  ข้อมูลการลา
                </Typography>
                <Grid container spacing={1.5}>
                  <Grid item xs={12} sm={6}>
                    <DetailItem label="ประเภทลา" value={getLeaveTypeLabel(request.leaveTypeName)} />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <DetailItem label="ประเภทช่วงเวลา" value={getLeaveDurationTypeLabel(request.durationType)} />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <DetailItem label="สถานะคำขอ" value={getLeaveStatusLabel(request.status)} chipColor={statusColor} />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <DetailItem label="วันที่เริ่มลา" value={formatThaiDate(request.startDate)} />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <DetailItem label="วันที่สิ้นสุด" value={formatThaiDate(request.endDate)} />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <DetailItem label="จำนวนวัน" value={`${request.totalDays.toLocaleString("th-TH")} วัน`} />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <DetailItem label="ลาครึ่งวัน" value={request.durationType === "FULL_DAY" ? "ไม่ใช่" : getLeaveDurationTypeLabel(request.durationType)} />
                  </Grid>
                  <Grid item xs={12}>
                    <DetailItem label="เหตุผลการลา" value={request.reason || "-"} />
                  </Grid>
                </Grid>
              </Stack>
            </Grid>
          </Grid>
        </InfoCard>

        <InfoCard title="การอนุมัติ" subtitle="แสดงลำดับขั้น ผู้อนุมัติ สถานะ วันที่ดำเนินการ และหมายเหตุของคำขอนี้">
          <Stack spacing={2}>
            {canCurrentUserDecide && (
              <PermissionGuard permission="LeaveApproval.ApproveCurrentStep">
                <Stack
                  direction={{ xs: "column", md: "row" }}
                  spacing={1.5}
                  alignItems={{ xs: "stretch", md: "center" }}
                  sx={{ p: 2, border: 1, borderColor: "divider", borderRadius: 3, bgcolor: "background.default" }}
                >
                  <TextField fullWidth size="small" label="หมายเหตุ" InputLabelProps={{ shrink: true }} value={remark} onChange={(event) => setRemark(event.target.value)} />
                  <Stack direction="row" spacing={1} justifyContent={{ xs: "flex-start", md: "flex-end" }} flexWrap="wrap" useFlexGap>
                    <Button variant="contained" color="success" startIcon={<CheckCircleOutlineOutlinedIcon />} disabled={approveMutation.isPending} onClick={() => approveMutation.mutate()}>
                      อนุมัติ
                    </Button>
                    <Button variant="outlined" color="error" startIcon={<HighlightOffOutlinedIcon />} disabled={rejectMutation.isPending} onClick={() => rejectMutation.mutate()}>
                      ไม่อนุมัติ
                    </Button>
                    <Button variant="outlined" color="warning" startIcon={<ReplyOutlinedIcon />} disabled={returnMutation.isPending} onClick={() => setIsReturnDialogOpen(true)}>
                      ตีกลับรอแก้ไข
                    </Button>
                  </Stack>
                </Stack>
              </PermissionGuard>
            )}
            <ApprovalWorkflowTimeline approvals={approvals} />
          </Stack>
        </InfoCard>

        {uploadMutation.isError && <Alert severity="error">อัปโหลดไฟล์ไม่สำเร็จ กรุณาตรวจสอบชนิดไฟล์และขนาดไม่เกิน {maxUploadMb} MB</Alert>}
        {attachments.length ? (
          <DataTableCard
            title="ไฟล์แนบ"
            subtitle="แนบเอกสารประกอบคำขอลาและดูตัวอย่างไฟล์ที่เกี่ยวข้อง"
            actions={canCurrentUserManageAttachments ? <AttachmentActions file={file} setFile={setFile} disabled={!file || uploadMutation.isPending} onUpload={() => uploadMutation.mutate()} /> : undefined}
          >
                <TableHead>
                  <TableRow>
                    <TableCell>ชื่อไฟล์</TableCell>
                    <TableCell>ขนาด</TableCell>
                    <TableCell>ผู้อัปโหลด</TableCell>
                    <TableCell>วันที่</TableCell>
                    <TableCell align="right">จัดการ</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {attachments.map((item) => (
                    <TableRow key={item.id}>
                      <TableCell>{item.fileName}</TableCell>
                      <TableCell>{Math.ceil(item.fileSizeBytes / 1024)} KB</TableCell>
                      <TableCell>{item.uploadedByName ?? "-"}</TableCell>
                      <TableCell>{formatThaiDateTime(item.createdAt)}</TableCell>
                      <TableCell align="right">
                        <Stack direction="row" spacing={1} justifyContent="flex-end" flexWrap="wrap" useFlexGap>
                        <PermissionGuard permissions={["LeaveRequest.ViewOwn", "LeaveRequest.ViewPendingApproval", "LeaveRequest.ViewDepartment", "LeaveRequest.ViewAll"]}>
                          <ActionTooltip title="ดูตัวอย่างไฟล์แนบ">
                            <Button size="small" variant="contained" startIcon={<VisibilityOutlinedIcon />} onClick={() => handlePreviewAttachment(item)}>
                              ดูตัวอย่าง
                            </Button>
                          </ActionTooltip>
                          <ActionTooltip title="ดาวน์โหลดไฟล์แนบ">
                            <Button size="small" variant="outlined" startIcon={<DownloadOutlinedIcon />} onClick={() => handleDownloadAttachment(item.id, item.fileName)}>
                              ดาวน์โหลด
                            </Button>
                          </ActionTooltip>
                        </PermissionGuard>
                        {canCurrentUserManageAttachments && (
                          <PermissionGuard permission="LeaveRequest.EditOwn">
                            <ActionTooltip title="ลบไฟล์แนบ">
                              <Button size="small" variant="outlined" color="error" startIcon={<DeleteOutlineOutlinedIcon />} disabled={deleteAttachmentMutation.isPending} onClick={() => deleteAttachmentMutation.mutate(item.id)}>
                                ลบ
                              </Button>
                            </ActionTooltip>
                          </PermissionGuard>
                        )}
                        </Stack>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
          </DataTableCard>
        ) : (
          <InfoCard
            title="ไฟล์แนบ"
            subtitle="แนบเอกสารประกอบคำขอลาและดูตัวอย่างไฟล์ที่เกี่ยวข้อง"
            actions={canCurrentUserManageAttachments ? <AttachmentActions file={file} setFile={setFile} disabled={!file || uploadMutation.isPending} onUpload={() => uploadMutation.mutate()} /> : undefined}
          >
            <EmptyState message="ยังไม่มีไฟล์แนบสำหรับคำขอนี้" />
          </InfoCard>
        )}

      </Stack>

      <Dialog open={isReturnDialogOpen} onClose={() => setIsReturnDialogOpen(false)} fullWidth maxWidth="sm">
        <DialogTitle>ตีกลับรอแก้ไข</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            <Alert severity="warning">คำขอนี้จะถูกส่งกลับไปยังผู้ขอ เพื่อให้แก้ไขข้อมูลหรือไฟล์แนบ แล้วส่งคำขอใหม่อีกครั้ง</Alert>
            <TextField
              fullWidth
              multiline
              minRows={3}
              label="เหตุผลการตีกลับ"
              value={revisionReason}
              onChange={(event) => setRevisionReason(event.target.value)}
              required
              error={!revisionReason.trim()}
              helperText={!revisionReason.trim() ? "กรุณาระบุเหตุผล เช่น กรุณาแนบเอกสารเพิ่มเติม" : "เหตุผลนี้จะแสดงให้ผู้ขอเห็น"}
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setIsReturnDialogOpen(false)}>ยกเลิก</Button>
          <Button variant="contained" color="warning" disabled={!revisionReason.trim() || returnMutation.isPending} onClick={() => returnMutation.mutate()}>
            ยืนยันตีกลับรอแก้ไข
          </Button>
        </DialogActions>
      </Dialog>
      <AttachmentPreviewDialog
        open={Boolean(previewAttachment)}
        fileName={previewAttachment?.fileName}
        fileSizeBytes={previewAttachment?.fileSizeBytes}
        contentType={previewAttachment?.contentType}
        previewUrl={previewUrl}
        isLoading={previewMutation.isPending}
        errorMessage={previewMutation.isError ? getApiErrorMessage(previewMutation.error, "ไม่สามารถแสดงตัวอย่างไฟล์แนบได้") : null}
        onClose={handleClosePreview}
        onDownload={previewAttachment ? () => handleDownloadAttachment(previewAttachment.id, previewAttachment.fileName) : undefined}
      />
    </>
  );
}

function AttachmentActions({
  file,
  setFile,
  disabled,
  onUpload,
}: {
  file: File | null;
  setFile: (file: File | null) => void;
  disabled: boolean;
  onUpload: () => void;
}) {
  return (
    <PermissionGuard permission="LeaveRequest.EditOwn">
      <Stack direction={{ xs: "column", sm: "row" }} spacing={1} alignItems={{ xs: "stretch", sm: "center" }} flexWrap="wrap" useFlexGap>
        <Button component="label" variant="outlined" startIcon={<UploadFileOutlinedIcon />}>
          เลือกไฟล์
          <input hidden type="file" accept=".pdf,.jpg,.jpeg,.png,.webp" onChange={(event) => setFile(event.target.files?.[0] ?? null)} />
        </Button>
        <Typography variant="body2" color="text.secondary" sx={{ maxWidth: { sm: 280 }, overflowWrap: "anywhere" }}>
          {file?.name ?? "ยังไม่ได้เลือกไฟล์"}
        </Typography>
        <Button variant="contained" disabled={disabled} onClick={onUpload}>
          อัปโหลด
        </Button>
      </Stack>
    </PermissionGuard>
  );
}

function DetailItem({ label, value, chipColor }: { label: string; value: string; chipColor?: "default" | "warning" | "success" | "error" }) {
  return (
    <Box>
      <Typography variant="caption" color="text.secondary" fontWeight={700}>
        {label}
      </Typography>
      {chipColor ? (
        <Box sx={{ mt: 0.5 }}>
          <Chip size="small" color={chipColor} label={value} />
        </Box>
      ) : (
        <Typography sx={{ mt: 0.25 }}>{value}</Typography>
      )}
    </Box>
  );
}

function getApiErrorMessage(error: unknown, fallback: string) {
  if (isAxiosError<{ message?: string }>(error)) {
    return error.response?.data?.message ?? fallback;
  }

  return fallback;
}
