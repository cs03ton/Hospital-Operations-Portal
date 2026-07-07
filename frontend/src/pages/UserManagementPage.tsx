import AddOutlinedIcon from "@mui/icons-material/AddOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import PersonOffOutlinedIcon from "@mui/icons-material/PersonOffOutlined";
import RuleOutlinedIcon from "@mui/icons-material/RuleOutlined";
import {
  Button,
  Card,
  CardContent,
  Chip,
  IconButton,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import { deactivateUser, getUsers } from "../api/adminApi";
import { resolveApprovalRulePreview, type ApprovalRulePreview } from "../api/leaveApi";
import { ActionTooltip } from "../components/common/ActionTooltip";
import { ApprovalRulePreviewDialog } from "../components/leave/ApprovalRulePreviewDialog";
import { PageHeader } from "../components/PageHeader";
import { PermissionGuard } from "../context/PermissionContext";
import { useNotification } from "../hooks/useNotification";
import { getRoleLabels } from "../utils/roleLabels";
import { UserManagementGridPage } from "./UserManagementGridPage";

export function UserManagementPage() {
  return <UserManagementGridPage />;
}

function LegacyUserManagementPage() {
  const queryClient = useQueryClient();
  const { showSuccess } = useNotification();
  const [preview, setPreview] = useState<ApprovalRulePreview | null>(null);
  const { data = [], isLoading } = useQuery({
    queryKey: ["users"],
    queryFn: getUsers,
  });

  const deactivateMutation = useMutation({
    mutationFn: deactivateUser,
    onSuccess: () => {
      showSuccess("ปิดการใช้งานผู้ใช้งานเรียบร้อยแล้ว");
      return queryClient.invalidateQueries({ queryKey: ["users"] });
    },
  });
  const previewMutation = useMutation({
    mutationFn: (userId: string) => resolveApprovalRulePreview({ userId }),
    onSuccess: setPreview,
  });

  return (
    <>
      <Stack direction={{ xs: "column", sm: "row" }} justifyContent="space-between" spacing={2}>
        <PageHeader title="จัดการผู้ใช้" subtitle="เพิ่ม แก้ไข และปิดใช้งานบัญชีผู้ใช้" />
        <PermissionGuard permission="UserManagement.Create">
          <Button
            component={RouterLink}
            to="/admin/users/create"
            variant="contained"
            startIcon={<AddOutlinedIcon />}
            sx={{ alignSelf: { sm: "center" } }}
          >
            เพิ่มผู้ใช้
          </Button>
        </PermissionGuard>
      </Stack>
      <Card>
        <CardContent>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>ชื่อ-สกุล</TableCell>
                <TableCell>ชื่อผู้ใช้</TableCell>
                <TableCell>บทบาท</TableCell>
                <TableCell>หน่วยงาน</TableCell>
                <TableCell>กฎการอนุมัติวันลา</TableCell>
                <TableCell>สถานะ</TableCell>
                <TableCell align="right">จัดการ</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {isLoading ? (
                <TableRow>
                  <TableCell colSpan={7}>กำลังโหลดข้อมูลผู้ใช้...</TableCell>
                </TableRow>
              ) : (
                data.map((user) => (
                  <TableRow key={user.id}>
                    <TableCell>{user.fullname}</TableCell>
                    <TableCell>{user.username}</TableCell>
                    <TableCell>{getRoleLabels(user.roles)}</TableCell>
                    <TableCell>{user.department ?? "-"}</TableCell>
                    <TableCell>{user.leaveApprovalRuleName ?? "-"}</TableCell>
                    <TableCell>
                      <Chip
                        size="small"
                        label={user.isActive ? "ใช้งาน" : "ปิดใช้งาน"}
                        color={user.isActive ? "success" : "default"}
                      />
                    </TableCell>
                    <TableCell align="right">
                      <PermissionGuard permission="LeaveAdmin.ManageApprovalChains">
                        <ActionTooltip title="ทดสอบกฎการอนุมัติของผู้ใช้นี้">
                          <IconButton
                            aria-label="ทดสอบกฎการอนุมัติของผู้ใช้นี้"
                            disabled={previewMutation.isPending}
                            onClick={() => previewMutation.mutate(user.id)}
                          >
                            <RuleOutlinedIcon />
                          </IconButton>
                        </ActionTooltip>
                      </PermissionGuard>
                      <PermissionGuard permission="UserManagement.Edit">
                        <ActionTooltip title="แก้ไขข้อมูลผู้ใช้งาน">
                          <IconButton
                            component={RouterLink}
                            to={`/admin/users/${user.id}/edit`}
                            aria-label="แก้ไขข้อมูลผู้ใช้งาน"
                          >
                            <EditOutlinedIcon />
                          </IconButton>
                        </ActionTooltip>
                      </PermissionGuard>
                      <PermissionGuard permission="UserManagement.Delete">
                        <ActionTooltip title="ปิดใช้งานผู้ใช้งาน">
                          <IconButton
                            aria-label="ปิดใช้งานผู้ใช้งาน"
                            disabled={!user.isActive || deactivateMutation.isPending}
                            onClick={() => deactivateMutation.mutate(user.id)}
                          >
                            <PersonOffOutlinedIcon />
                          </IconButton>
                        </ActionTooltip>
                      </PermissionGuard>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
      <ApprovalRulePreviewDialog open={Boolean(preview)} preview={preview} onClose={() => setPreview(null)} />
    </>
  );
}
