import AddOutlinedIcon from "@mui/icons-material/AddOutlined";
import BlockOutlinedIcon from "@mui/icons-material/BlockOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import RuleOutlinedIcon from "@mui/icons-material/RuleOutlined";
import { Button, Card, CardContent, Chip, IconButton, Stack, Table, TableBody, TableCell, TableHead, TableRow } from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { deactivateApprovalChain, getApprovalChains, resolveApprovalRulePreview, type ApprovalRulePreview } from "../api/leaveApi";
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
  const { data = [], isLoading } = useQuery({ queryKey: ["approval-chains"], queryFn: getApprovalChains });
  const deleteMutation = useMutation({
    mutationFn: deactivateApprovalChain,
    onSuccess: () => {
      showSuccess("ปิดใช้งานกฎการอนุมัติเรียบร้อยแล้ว");
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
              ) : data.length ? data.map((item) => (
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
                      <ActionTooltip title="ปิดใช้งานกฎการอนุมัติวันลา">
                        <IconButton aria-label="ปิดใช้งานกฎการอนุมัติวันลา" disabled={!item.isActive || deleteMutation.isPending} onClick={() => deleteMutation.mutate(item.id)}>
                          <BlockOutlinedIcon />
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
    </>
  );
}
