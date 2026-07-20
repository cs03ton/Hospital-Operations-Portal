import AddOutlinedIcon from "@mui/icons-material/AddOutlined";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import RuleOutlinedIcon from "@mui/icons-material/RuleOutlined";
import { Avatar, Box, Button, Chip, IconButton, MenuItem, Stack, TextField } from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import {
  deleteUser,
  getDepartments,
  getRoles,
  getUsersPaged,
  type DeleteResult,
  type UserSummary,
} from "../api/adminApi";
import { resolveApprovalRulePreview, type ApprovalRulePreview } from "../api/leaveApi";
import { ActionTooltip } from "../components/common/ActionTooltip";
import { ConfirmDeleteDialog } from "../components/common/ConfirmDeleteDialog";
import { ManagementDataGrid, type GridSortDirection, type ManagementDataGridColumn } from "../components/common/ManagementDataGrid";
import { StatusBadge } from "../components/common/StatusBadge";
import { ApprovalRulePreviewDialog } from "../components/leave/ApprovalRulePreviewDialog";
import { PageHeader } from "../components/PageHeader";
import { PermissionGuard } from "../context/PermissionContext";
import { useNotification } from "../hooks/useNotification";
import { formatThaiDateTime } from "../utils/dateFormat";
import { getEmploymentTypeLabel } from "../utils/employmentLabels";
import { getRoleLabels } from "../utils/roleLabels";

export function UserManagementGridPage() {
  const queryClient = useQueryClient();
  const { showSuccess, showWarning, showError } = useNotification();
  const [preview, setPreview] = useState<ApprovalRulePreview | null>(null);
  const [deletingUser, setDeletingUser] = useState<UserSummary | null>(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [search, setSearch] = useState("");
  const [departmentId, setDepartmentId] = useState("");
  const [roleId, setRoleId] = useState("");
  const [employmentType, setEmploymentType] = useState("");
  const [status, setStatus] = useState("all");
  const [hasLine, setHasLine] = useState("all");
  const [sort, setSort] = useState("fullname");
  const [direction, setDirection] = useState<GridSortDirection>("asc");

  const queryParams = {
    page,
    pageSize,
    search: search || undefined,
    sort,
    direction,
    departmentId: departmentId || undefined,
    roleId: roleId || undefined,
    employmentType: employmentType || undefined,
    status,
    hasLine: hasLine === "all" ? undefined : hasLine === "yes",
  };

  const usersQuery = useQuery({
    queryKey: ["users", "grid", queryParams],
    queryFn: () => getUsersPaged(queryParams),
  });
  const { data: departments = [] } = useQuery({ queryKey: ["departments"], queryFn: getDepartments });
  const { data: roles = [] } = useQuery({ queryKey: ["roles"], queryFn: getRoles });

  const deleteMutation = useMutation({
    mutationFn: deleteUser,
    onSuccess: async (result?: DeleteResult) => {
      await queryClient.invalidateQueries({ queryKey: ["users"] });
      setDeletingUser(null);
      if (result?.action === "SoftDeleted") {
        showWarning(result.message);
      } else {
        showSuccess(result?.message ?? "ลบผู้ใช้งานเรียบร้อยแล้ว");
      }
    },
    onError: (error: unknown) => showError(extractErrorMessage(error, "ไม่สามารถลบผู้ใช้งานได้")),
  });

  const previewMutation = useMutation({
    mutationFn: (userId: string) => resolveApprovalRulePreview({ userId }),
    onSuccess: setPreview,
  });

  const columns = useMemo<ManagementDataGridColumn<UserSummary>[]>(() => [
    {
      key: "avatar",
      label: "Avatar",
      width: 72,
      render: (user) => <Avatar src={undefined}>{getInitials(user.fullname || user.username)}</Avatar>,
    },
    { key: "username", label: "Username", sortable: true, render: (user) => user.username },
    { key: "fullname", label: "Full Name", sortable: true, render: (user) => user.fullname },
    { key: "department", label: "Department", sortable: true, render: (user) => user.department ?? "-" },
    { key: "employmentType", label: "Employment Type", render: (user) => getEmploymentTypeLabel(user.employmentType) },
    { key: "role", label: "Role", render: (user) => getRoleLabels(user.roles) },
    {
      key: "status",
      label: "Status",
      render: (user) => (
        <StatusBadge domain="active" status={user.isActive ? "active" : "inactive"} />
      ),
    },
    {
      key: "line",
      label: "LINE",
      render: (user) => <Chip size="small" label={user.lineUserId ? "เชื่อมต่อแล้ว" : "ยังไม่เชื่อม"} color={user.lineUserId ? "success" : "default"} variant="outlined" />,
    },
    { key: "createdAt", label: "Created Date", sortable: true, render: (user) => formatThaiDateTime(user.createdAt) },
    { key: "lastLogin", label: "Last Login", sortable: true, render: (user) => formatThaiDateTime(user.lastLoginAt) },
    {
      key: "action",
      label: "Action",
      align: "right",
      render: (user) => (
        <Stack direction="row" spacing={0.5} justifyContent="flex-end">
          <PermissionGuard permission="LeaveAdmin.ManageApprovalChains">
            <ActionTooltip title="ทดสอบกฎการอนุมัติของผู้ใช้นี้">
              <IconButton disabled={previewMutation.isPending} onClick={() => previewMutation.mutate(user.id)}>
                <RuleOutlinedIcon />
              </IconButton>
            </ActionTooltip>
          </PermissionGuard>
          <PermissionGuard permission="UserManagement.Edit">
            <ActionTooltip title="แก้ไขข้อมูลผู้ใช้งาน">
              <IconButton component={RouterLink} to={`/admin/users/${user.id}/edit`}>
                <EditOutlinedIcon />
              </IconButton>
            </ActionTooltip>
          </PermissionGuard>
          <PermissionGuard permission="UserManagement.Delete">
            <ActionTooltip title="ลบหรือปิดใช้งานผู้ใช้งาน">
              <IconButton color="error" disabled={!user.isActive || deleteMutation.isPending} onClick={() => setDeletingUser(user)}>
                <DeleteOutlineIcon />
              </IconButton>
            </ActionTooltip>
          </PermissionGuard>
        </Stack>
      ),
    },
  ], [deleteMutation.isPending, previewMutation]);

  function resetToFirstPage() {
    setPage(1);
  }

  return (
    <>
      <Stack direction={{ xs: "column", sm: "row" }} justifyContent="space-between" spacing={2}>
        <Box sx={{ minWidth: 0 }}>
          <PageHeader title="จัดการผู้ใช้" subtitle="ค้นหา กรอง เรียงลำดับ และลบ/ปิดใช้งานบัญชีผู้ใช้" />
        </Box>        
        <PermissionGuard permission="UserManagement.Create">
          <Button component={RouterLink} to="/admin/users/create" variant="contained" startIcon={<AddOutlinedIcon />} sx={{ alignSelf: { sm: "center" } }}>
            เพิ่มผู้ใช้
          </Button>
        </PermissionGuard>
      </Stack>
      <ManagementDataGrid
        title="รายการผู้ใช้งาน"
        subtitle="รองรับ Pagination, Sorting, Filtering และ Search"
        rows={usersQuery.data?.items ?? []}
        columns={columns}
        getRowId={(user) => user.id}
        isLoading={usersQuery.isLoading}
        emptyMessage="ไม่พบผู้ใช้งานตามเงื่อนไข"
        page={page}
        pageSize={pageSize}
        totalItems={usersQuery.data?.totalItems ?? 0}
        sort={sort}
        direction={direction}
        onSortChange={(nextSort, nextDirection) => {
          setSort(nextSort);
          setDirection(nextDirection);
          resetToFirstPage();
        }}
        onPageChange={setPage}
        onPageSizeChange={(value) => {
          setPageSize(value);
          resetToFirstPage();
        }}
        toolbar={
          <Stack direction={{ xs: "column", md: "row" }} spacing={1.5}>
            <TextField size="small" label="ค้นหา Username / Name / Department / Role" value={search} onChange={(event) => { setSearch(event.target.value); resetToFirstPage(); }} fullWidth />
            <TextField select size="small" label="หน่วยงาน" value={departmentId} onChange={(event) => { setDepartmentId(event.target.value); resetToFirstPage(); }} sx={{ minWidth: 180 }}>
              <MenuItem value="">ทั้งหมด</MenuItem>
              {departments.map((department) => <MenuItem key={department.id} value={department.id}>{department.name}</MenuItem>)}
            </TextField>
            <TextField select size="small" label="บทบาท" value={roleId} onChange={(event) => { setRoleId(event.target.value); resetToFirstPage(); }} sx={{ minWidth: 160 }}>
              <MenuItem value="">ทั้งหมด</MenuItem>
              {roles.map((role) => <MenuItem key={role.id} value={role.id}>{role.name}</MenuItem>)}
            </TextField>
            <TextField size="small" label="Employment Type" value={employmentType} onChange={(event) => { setEmploymentType(event.target.value); resetToFirstPage(); }} sx={{ minWidth: 160 }} />
            <TextField select size="small" label="สถานะ" value={status} onChange={(event) => { setStatus(event.target.value); resetToFirstPage(); }} sx={{ minWidth: 140 }}>
              <MenuItem value="all">ทั้งหมด</MenuItem>
              <MenuItem value="active">ใช้งาน</MenuItem>
              <MenuItem value="inactive">ปิดใช้งาน</MenuItem>
            </TextField>
            <TextField select size="small" label="LINE" value={hasLine} onChange={(event) => { setHasLine(event.target.value); resetToFirstPage(); }} sx={{ minWidth: 150 }}>
              <MenuItem value="all">ทั้งหมด</MenuItem>
              <MenuItem value="yes">เชื่อมต่อแล้ว</MenuItem>
              <MenuItem value="no">ยังไม่เชื่อม</MenuItem>
            </TextField>
          </Stack>
        }
      />
      <ConfirmDeleteDialog
        open={Boolean(deletingUser)}
        title="ยืนยันการลบผู้ใช้งาน"
        itemName={deletingUser?.fullname}
        description="ถ้าผู้ใช้งานมีข้อมูลอ้างอิง ระบบจะปิดการใช้งานแทนการลบถาวร"
        confirmLabel="ยืนยัน"
        isLoading={deleteMutation.isPending}
        onClose={() => setDeletingUser(null)}
        onConfirm={() => deletingUser && deleteMutation.mutate(deletingUser.id)}
      />
      <ApprovalRulePreviewDialog open={Boolean(preview)} preview={preview} onClose={() => setPreview(null)} />
    </>
  );
}

function getInitials(value: string) {
  return value.trim().slice(0, 2).toUpperCase() || "U";
}

function extractErrorMessage(error: unknown, fallback: string) {
  const maybeError = error as { response?: { data?: { message?: string; data?: { message?: string } } } };
  return maybeError.response?.data?.data?.message ?? maybeError.response?.data?.message ?? fallback;
}
