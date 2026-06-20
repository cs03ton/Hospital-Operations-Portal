import AddOutlinedIcon from "@mui/icons-material/AddOutlined";
import BlockOutlinedIcon from "@mui/icons-material/BlockOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import { Button, Card, CardContent, Chip, IconButton, Stack, Table, TableBody, TableCell, TableHead, TableRow } from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { deactivateApprovalChain, getApprovalChains } from "../api/leaveApi";
import { ActionTooltip } from "../components/common/ActionTooltip";
import { PageHeader } from "../components/PageHeader";
import { PermissionGuard } from "../context/PermissionContext";
import { getLeaveTypeLabel } from "../utils/leaveLabels";

export function ApprovalChainManagementPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { data = [], isLoading } = useQuery({ queryKey: ["approval-chains"], queryFn: getApprovalChains });
  const deleteMutation = useMutation({
    mutationFn: deactivateApprovalChain,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["approval-chains"] }),
  });

  return (
    <>
      <Stack direction={{ xs: "column", sm: "row" }} justifyContent="space-between" spacing={2}>
        <PageHeader title="สายอนุมัติวันลา" subtitle="กำหนดลำดับผู้อนุมัติตามหน่วยงาน ประเภทการลา และจำนวนวันลา" />
        <PermissionGuard permission="ApprovalChain.Create">
          <ActionTooltip title="เพิ่มสายอนุมัติวันลา">
            <Button variant="contained" size="medium" startIcon={<AddOutlinedIcon />} onClick={() => navigate("/admin/approval-chains/create")}>
              เพิ่มสายอนุมัติ
            </Button>
          </ActionTooltip>
        </PermissionGuard>
      </Stack>
      <Card>
        <CardContent>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>ชื่อสายอนุมัติ</TableCell>
                <TableCell>หน่วยงาน</TableCell>
                <TableCell>ประเภทการลา</TableCell>
                <TableCell>จำนวนวันขั้นต่ำ</TableCell>
                <TableCell>สถานะ</TableCell>
                <TableCell align="right">จัดการ</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {isLoading ? (
                <TableRow><TableCell colSpan={6}>กำลังโหลดสายอนุมัติ...</TableCell></TableRow>
              ) : data.length ? data.map((item) => (
                <TableRow key={item.id}>
                  <TableCell>{item.name}</TableCell>
                  <TableCell>{item.departmentName ?? "ทุกหน่วยงาน"}</TableCell>
                  <TableCell>{item.leaveTypeName ? getLeaveTypeLabel(item.leaveTypeName) : "ทุกประเภท"}</TableCell>
                  <TableCell>{item.minimumDays}</TableCell>
                  <TableCell><Chip size="small" label={item.isActive ? "ใช้งาน" : "ปิดใช้งาน"} /></TableCell>
                  <TableCell align="right">
                    <PermissionGuard permission="ApprovalChain.Edit">
                      <ActionTooltip title="แก้ไขสายอนุมัติวันลา">
                        <IconButton aria-label="แก้ไขสายอนุมัติวันลา" onClick={() => navigate(`/admin/approval-chains/${item.id}/edit`)}>
                          <EditOutlinedIcon />
                        </IconButton>
                      </ActionTooltip>
                    </PermissionGuard>
                    <PermissionGuard permission="ApprovalChain.Delete">
                      <ActionTooltip title="ปิดใช้งานสายอนุมัติวันลา">
                        <IconButton aria-label="ปิดใช้งานสายอนุมัติวันลา" disabled={!item.isActive || deleteMutation.isPending} onClick={() => deleteMutation.mutate(item.id)}>
                          <BlockOutlinedIcon />
                        </IconButton>
                      </ActionTooltip>
                    </PermissionGuard>
                  </TableCell>
                </TableRow>
              )) : (
                <TableRow><TableCell colSpan={6}>ยังไม่มีสายอนุมัติ</TableCell></TableRow>
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </>
  );
}
