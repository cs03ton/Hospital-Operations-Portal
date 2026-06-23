import AddOutlinedIcon from "@mui/icons-material/AddOutlined";
import DeleteOutlineOutlinedIcon from "@mui/icons-material/DeleteOutlineOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import RuleOutlinedIcon from "@mui/icons-material/RuleOutlined";
import {
  Button,
  Card,
  CardContent,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { deactivateApprovalChain, getApprovalChains, resolveApprovalRulePreview, type ApprovalChain, type ApprovalRulePreview } from "../api/leaveApi";
import { ActionTooltip } from "../components/common/ActionTooltip";
import { ApprovalRulePreviewDialog } from "../components/leave/ApprovalRulePreviewDialog";
import { PageHeader } from "../components/PageHeader";
import { PermissionGuard } from "../context/PermissionContext";
import { useNotification } from "../hooks/useNotification";
import { getLeaveTypeLabel } from "../utils/leaveLabels";

export function ApprovalChainManagementPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { showSuccess } = useNotification();
  const [preview, setPreview] = useState<ApprovalRulePreview | null>(null);
  const [deletingChain, setDeletingChain] = useState<ApprovalChain | null>(null);
  const { data = [], isLoading } = useQuery({ queryKey: ["approval-chains"], queryFn: getApprovalChains });
  const activeChains = data.filter((item) => item.isActive);
  const deleteMutation = useMutation({
    mutationFn: deactivateApprovalChain,
    onSuccess: () => {
      showSuccess("ลบกฎการอนุมัติเรียบร้อยแล้ว");
      setDeletingChain(null);
      return queryClient.invalidateQueries({ queryKey: ["approval-chains"] });
    },
  });
  const previewMutation = useMutation({
    mutationFn: (approvalRuleId: string) => resolveApprovalRulePreview({ approvalRuleId }),
    onSuccess: setPreview,
  });

  return (
    <>
      <Stack direction={{ xs: "column", sm: "row" }} justifyContent="space-between" spacing={2}>
        <PageHeader title="กฎการอนุมัติวันลา" subtitle="กำหนด rule และลำดับผู้อนุมัติสำหรับผูกกับผู้ใช้งานแต่ละคน" />
        <PermissionGuard permission="LeaveAdmin.ManageApprovalChains">
          <ActionTooltip title="เพิ่มกฎการอนุมัติวันลา">
            <Button variant="contained" size="medium" startIcon={<AddOutlinedIcon />} onClick={() => navigate("/admin/approval-chains/create")}>
              เพิ่มกฎการอนุมัติ
            </Button>
          </ActionTooltip>
        </PermissionGuard>
      </Stack>
      <Card>
        <CardContent>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>ชื่อกฎการอนุมัติ</TableCell>
                <TableCell>หน่วยงาน</TableCell>
                <TableCell>ประเภทการลา</TableCell>
                <TableCell>จำนวนวันขั้นต่ำ</TableCell>
                <TableCell>ผู้ใช้งานที่ใช้</TableCell>
                <TableCell>สถานะ</TableCell>
                <TableCell align="right">จัดการ</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {isLoading ? (
                <TableRow><TableCell colSpan={7}>กำลังโหลดกฎการอนุมัติ...</TableCell></TableRow>
              ) : activeChains.length ? activeChains.map((item) => (
                <TableRow key={item.id}>
                  <TableCell>{item.name}</TableCell>
                  <TableCell>{item.departmentName ?? "ทุกหน่วยงาน"}</TableCell>
                  <TableCell>{item.leaveTypeName ? getLeaveTypeLabel(item.leaveTypeName) : "ทุกประเภท"}</TableCell>
                  <TableCell>{item.minimumDays}</TableCell>
                  <TableCell>{item.userCount}</TableCell>
                  <TableCell><Chip size="small" label={item.isActive ? "ใช้งาน" : "ปิดใช้งาน"} /></TableCell>
                  <TableCell align="right">
                    <PermissionGuard permission="LeaveAdmin.ManageApprovalChains">
                      <ActionTooltip title="ทดสอบกฎการอนุมัติ">
                        <IconButton aria-label="ทดสอบกฎการอนุมัติ" disabled={previewMutation.isPending} onClick={() => previewMutation.mutate(item.id)}>
                          <RuleOutlinedIcon />
                        </IconButton>
                      </ActionTooltip>
                    </PermissionGuard>
                    <PermissionGuard permission="LeaveAdmin.ManageApprovalChains">
                      <ActionTooltip title="แก้ไขกฎการอนุมัติวันลา">
                        <IconButton aria-label="แก้ไขกฎการอนุมัติวันลา" onClick={() => navigate(`/admin/approval-chains/${item.id}/edit`)}>
                          <EditOutlinedIcon />
                        </IconButton>
                      </ActionTooltip>
                    </PermissionGuard>
                    <PermissionGuard permission="LeaveAdmin.ManageApprovalChains">
                      <ActionTooltip title="ลบกฎการอนุมัติวันลา">
                        <IconButton
                          aria-label="ลบกฎการอนุมัติวันลา"
                          color="error"
                          disabled={!item.isActive || deleteMutation.isPending}
                          onClick={() => setDeletingChain(item)}
                        >
                          <DeleteOutlineOutlinedIcon />
                        </IconButton>
                      </ActionTooltip>
                    </PermissionGuard>
                  </TableCell>
                </TableRow>
              )) : (
                <TableRow><TableCell colSpan={7}>ยังไม่มีกฎการอนุมัติ</TableCell></TableRow>
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
      <ApprovalRulePreviewDialog open={Boolean(preview)} preview={preview} onClose={() => setPreview(null)} />
      <Dialog open={Boolean(deletingChain)} onClose={() => setDeletingChain(null)} fullWidth maxWidth="xs">
        <DialogTitle>ยืนยันการลบกฎการอนุมัติ</DialogTitle>
        <DialogContent>
          <Stack spacing={1.5}>
            <Typography>
              ต้องการลบกฎการอนุมัติ “{deletingChain?.name}” ใช่หรือไม่?
            </Typography>
            <Typography variant="body2" color="text.secondary">
              ระบบจะปิดใช้งานกฎนี้เพื่อรักษาประวัติการอนุมัติเดิมไว้ หากมีผู้ใช้งานผูกกับกฎนี้อยู่ กรุณาเปลี่ยนกฎให้ผู้ใช้งานก่อนใช้งานจริง
            </Typography>
            {(deletingChain?.userCount ?? 0) > 0 && (
              <Chip color="warning" label={`มีผู้ใช้งานที่ใช้กฎนี้ ${deletingChain?.userCount} คน`} />
            )}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeletingChain(null)}>ยกเลิก</Button>
          <Button
            color="error"
            variant="contained"
            disabled={!deletingChain || deleteMutation.isPending}
            onClick={() => deletingChain && deleteMutation.mutate(deletingChain.id)}
          >
            ลบกฎ
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}
