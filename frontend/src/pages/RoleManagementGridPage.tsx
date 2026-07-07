import BlockOutlinedIcon from "@mui/icons-material/BlockOutlined";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import RuleOutlinedIcon from "@mui/icons-material/RuleOutlined";
import {
  Alert,
  Button,
  Card,
  CardContent,
  Checkbox,
  Chip,
  FormControlLabel,
  IconButton,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { Link as RouterLink } from "react-router-dom";
import {
  createRole,
  deletePermission,
  deleteRole,
  getPermissionsPaged,
  getRolesPaged,
  updateRole,
  type DeleteResult,
  type PermissionSummary,
  type RoleSummary,
  type SaveRoleRequest,
} from "../api/adminApi";
import { ActionTooltip } from "../components/common/ActionTooltip";
import { ConfirmDeleteDialog } from "../components/common/ConfirmDeleteDialog";
import { ManagementDataGrid, type GridSortDirection, type ManagementDataGridColumn } from "../components/common/ManagementDataGrid";
import { PageHeader } from "../components/PageHeader";
import { PermissionGuard, usePermission } from "../context/PermissionContext";
import { useNotification } from "../hooks/useNotification";
import { formatThaiDateTime } from "../utils/dateFormat";
import { getRoleLabel } from "../utils/roleLabels";

type RoleFormValues = {
  name: string;
  description: string;
  isActive: boolean;
};

export function RoleManagementGridPage() {
  const queryClient = useQueryClient();
  const { hasPermission } = usePermission();
  const { showSuccess, showError } = useNotification();
  const [editingRole, setEditingRole] = useState<RoleSummary | null>(null);
  const [deletingRole, setDeletingRole] = useState<RoleSummary | null>(null);
  const [deletingPermission, setDeletingPermission] = useState<PermissionSummary | null>(null);
  const [rolePage, setRolePage] = useState(1);
  const [rolePageSize, setRolePageSize] = useState(20);
  const [roleSearch, setRoleSearch] = useState("");
  const [roleStatus, setRoleStatus] = useState("all");
  const [roleSort, setRoleSort] = useState("name");
  const [roleDirection, setRoleDirection] = useState<GridSortDirection>("asc");
  const [permissionPage, setPermissionPage] = useState(1);
  const [permissionPageSize, setPermissionPageSize] = useState(20);
  const [permissionSearch, setPermissionSearch] = useState("");
  const [permissionModule, setPermissionModule] = useState("");
  const [permissionStatus, setPermissionStatus] = useState("all");
  const [permissionSort, setPermissionSort] = useState("code");
  const [permissionDirection, setPermissionDirection] = useState<GridSortDirection>("asc");

  const roleQueryParams = {
    page: rolePage,
    pageSize: rolePageSize,
    search: roleSearch || undefined,
    status: roleStatus,
    sort: roleSort,
    direction: roleDirection,
  };
  const permissionQueryParams = {
    page: permissionPage,
    pageSize: permissionPageSize,
    search: permissionSearch || undefined,
    module: permissionModule || undefined,
    status: permissionStatus,
    sort: permissionSort,
    direction: permissionDirection,
  };

  const rolesQuery = useQuery({
    queryKey: ["roles", "grid", roleQueryParams],
    queryFn: () => getRolesPaged(roleQueryParams),
  });
  const permissionsQuery = useQuery({
    queryKey: ["permissions", "grid", permissionQueryParams],
    queryFn: () => getPermissionsPaged(permissionQueryParams),
  });

  const {
    control,
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<RoleFormValues>({
    defaultValues: { name: "", description: "", isActive: true },
  });

  const saveMutation = useMutation({
    mutationFn: (values: SaveRoleRequest) => editingRole ? updateRole(editingRole.id, values) : createRole(values),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["roles"] });
      showSuccess(editingRole ? "บันทึกบทบาทเรียบร้อยแล้ว" : "เพิ่มบทบาทเรียบร้อยแล้ว");
      setEditingRole(null);
      reset({ name: "", description: "", isActive: true });
    },
    onError: () => showError("บันทึกบทบาทไม่สำเร็จ"),
  });

  const deleteRoleMutation = useMutation({
    mutationFn: deleteRole,
    onSuccess: async (result?: DeleteResult) => {
      await queryClient.invalidateQueries({ queryKey: ["roles"] });
      setDeletingRole(null);
      showSuccess(result?.message ?? "ปิดใช้งานบทบาทเรียบร้อยแล้ว");
    },
    onError: (error: unknown) => showError(extractErrorMessage(error, "ไม่สามารถลบบทบาทได้")),
  });

  const deletePermissionMutation = useMutation({
    mutationFn: deletePermission,
    onSuccess: async (result?: DeleteResult) => {
      await queryClient.invalidateQueries({ queryKey: ["permissions"] });
      setDeletingPermission(null);
      showSuccess(result?.message ?? "ปิดใช้งานสิทธิ์เรียบร้อยแล้ว");
    },
    onError: (error: unknown) => showError(extractErrorMessage(error, "ไม่สามารถลบสิทธิ์ได้")),
  });

  const roleColumns = useMemo<ManagementDataGridColumn<RoleSummary>[]>(() => [
    { key: "name", label: "Role", sortable: true, render: (role) => getRoleLabel(role.name) },
    { key: "description", label: "Description", render: (role) => role.description ?? "-" },
    { key: "usersCount", label: "Users", sortable: true, render: (role) => role.usersCount.toLocaleString("th-TH") },
    { key: "permissionsCount", label: "Permissions", sortable: true, render: (role) => role.permissionsCount.toLocaleString("th-TH") },
    {
      key: "status",
      label: "Status",
      render: (role) => (
        <Chip size="small" label={role.isActive ? "ใช้งาน" : "ปิดใช้งาน"} color={role.isActive ? "success" : "default"} />
      ),
    },
    { key: "createdAt", label: "Created", sortable: true, render: (role) => formatThaiDateTime(role.createdAt) },
    {
      key: "action",
      label: "Action",
      align: "right",
      render: (role) => (
        <Stack direction="row" justifyContent="flex-end" spacing={0.5}>
          <PermissionGuard permission="RoleManagement.Edit">
            <ActionTooltip title="แก้ไขข้อมูลบทบาท">
              <IconButton onClick={() => editRole(role)}><EditOutlinedIcon /></IconButton>
            </ActionTooltip>
          </PermissionGuard>
          <PermissionGuard permission="RoleManagement.Manage">
            <ActionTooltip title="จัดการสิทธิ์ของบทบาทนี้">
              <IconButton component={RouterLink} to={`/admin/roles/${role.id}/permissions`}><RuleOutlinedIcon /></IconButton>
            </ActionTooltip>
          </PermissionGuard>
          <PermissionGuard permission="RoleManagement.Delete">
            <ActionTooltip title="ลบบทบาท">
              <IconButton color="error" disabled={role.isSystemRole || !role.isActive || deleteRoleMutation.isPending} onClick={() => setDeletingRole(role)}>
                {role.isSystemRole ? <BlockOutlinedIcon /> : <DeleteOutlineIcon />}
              </IconButton>
            </ActionTooltip>
          </PermissionGuard>
        </Stack>
      ),
    },
  ], [deleteRoleMutation.isPending]);

  const permissionColumns = useMemo<ManagementDataGridColumn<PermissionSummary>[]>(() => [
    { key: "code", label: "Permission Code", sortable: true, render: (permission) => permission.code },
    { key: "module", label: "Module", sortable: true, render: (permission) => permission.group },
    { key: "description", label: "Description", render: (permission) => permission.name },
    { key: "rolesCount", label: "Roles Count", sortable: true, render: (permission) => permission.rolesCount.toLocaleString("th-TH") },
    {
      key: "status",
      label: "Status",
      render: (permission) => <Chip size="small" label={permission.isActive ? "ใช้งาน" : "ปิดใช้งาน"} color={permission.isActive ? "success" : "default"} />,
    },
    { key: "createdAt", label: "Created", sortable: true, render: (permission) => formatThaiDateTime(permission.createdAt) },
    {
      key: "action",
      label: "Action",
      align: "right",
      render: (permission) => (
        <PermissionGuard permission="RoleManagement.Delete">
          <ActionTooltip title="ลบหรือปิดใช้งานสิทธิ์">
            <IconButton color="error" disabled={!permission.isActive || permission.rolesCount > 0 || deletePermissionMutation.isPending} onClick={() => setDeletingPermission(permission)}>
              <DeleteOutlineIcon />
            </IconButton>
          </ActionTooltip>
        </PermissionGuard>
      ),
    },
  ], [deletePermissionMutation.isPending]);

  const permissionModules = useMemo(() => {
    const groups = new Set((permissionsQuery.data?.items ?? []).map((permission) => permission.group));
    return Array.from(groups).sort();
  }, [permissionsQuery.data?.items]);

  function editRole(role: RoleSummary) {
    if (!hasPermission("RoleManagement.Edit")) return;
    setEditingRole(role);
    reset({ name: role.name, description: role.description ?? "", isActive: role.isActive });
  }

  function onSubmit(values: RoleFormValues) {
    saveMutation.mutate({ name: values.name, description: values.description || null, isActive: values.isActive });
  }

  const canSaveRole = editingRole ? "RoleManagement.Edit" : "RoleManagement.Create";

  return (
    <>
      <PageHeader title="บทบาทและสิทธิ์" subtitle="จัดการบทบาท สิทธิ์ และความสัมพันธ์ของสิทธิ์ในระบบ" />
      <Stack spacing={2}>
        <PermissionGuard permission={canSaveRole}>
          <Card>
            <CardContent>
              <Typography variant="h6" sx={{ mb: 2 }}>{editingRole ? "แก้ไขบทบาท" : "เพิ่มบทบาท"}</Typography>
              <Stack component="form" spacing={2} onSubmit={handleSubmit(onSubmit)}>
                {saveMutation.isError && <Alert severity="error">บันทึกบทบาทไม่สำเร็จ</Alert>}
                <TextField fullWidth label="ชื่อบทบาท" InputLabelProps={{ shrink: true }} error={Boolean(errors.name)} helperText={errors.name?.message} {...register("name", { required: "กรุณากรอกชื่อบทบาท" })} />
                <TextField fullWidth label="รายละเอียด" InputLabelProps={{ shrink: true }} {...register("description")} />
                <Controller name="isActive" control={control} render={({ field }) => <FormControlLabel control={<Checkbox checked={field.value} onChange={(event) => field.onChange(event.target.checked)} />} label="เปิดใช้งาน" />} />
                <Stack direction="row" spacing={1.5}>
                  <Button type="submit" variant="contained" disabled={saveMutation.isPending}>บันทึกข้อมูล</Button>
                  {editingRole && (
                    <Button variant="outlined" onClick={() => { setEditingRole(null); reset({ name: "", description: "", isActive: true }); }}>
                      ยกเลิก
                    </Button>
                  )}
                </Stack>
              </Stack>
            </CardContent>
          </Card>
        </PermissionGuard>

        <ManagementDataGrid
          title="Role Grid"
          rows={rolesQuery.data?.items ?? []}
          columns={roleColumns}
          getRowId={(role) => role.id}
          isLoading={rolesQuery.isLoading}
          emptyMessage="ไม่พบบทบาทตามเงื่อนไข"
          page={rolePage}
          pageSize={rolePageSize}
          totalItems={rolesQuery.data?.totalItems ?? 0}
          sort={roleSort}
          direction={roleDirection}
          onSortChange={(nextSort, nextDirection) => { setRoleSort(nextSort); setRoleDirection(nextDirection); setRolePage(1); }}
          onPageChange={setRolePage}
          onPageSizeChange={(value) => { setRolePageSize(value); setRolePage(1); }}
          toolbar={
            <Stack direction={{ xs: "column", md: "row" }} spacing={1.5}>
              <TextField size="small" label="ค้นหาบทบาท" value={roleSearch} onChange={(event) => { setRoleSearch(event.target.value); setRolePage(1); }} fullWidth />
              <TextField select size="small" label="สถานะ" value={roleStatus} onChange={(event) => { setRoleStatus(event.target.value); setRolePage(1); }} sx={{ minWidth: 160 }}>
                <MenuItem value="all">ทั้งหมด</MenuItem>
                <MenuItem value="active">ใช้งาน</MenuItem>
                <MenuItem value="inactive">ปิดใช้งาน</MenuItem>
              </TextField>
            </Stack>
          }
        />

        <ManagementDataGrid
          title="Permission Grid"
          rows={permissionsQuery.data?.items ?? []}
          columns={permissionColumns}
          getRowId={(permission) => permission.id}
          isLoading={permissionsQuery.isLoading}
          emptyMessage="ไม่พบสิทธิ์ตามเงื่อนไข"
          page={permissionPage}
          pageSize={permissionPageSize}
          totalItems={permissionsQuery.data?.totalItems ?? 0}
          sort={permissionSort}
          direction={permissionDirection}
          onSortChange={(nextSort, nextDirection) => { setPermissionSort(nextSort); setPermissionDirection(nextDirection); setPermissionPage(1); }}
          onPageChange={setPermissionPage}
          onPageSizeChange={(value) => { setPermissionPageSize(value); setPermissionPage(1); }}
          toolbar={
            <Stack direction={{ xs: "column", md: "row" }} spacing={1.5}>
              <TextField size="small" label="ค้นหาสิทธิ์ / Module" value={permissionSearch} onChange={(event) => { setPermissionSearch(event.target.value); setPermissionPage(1); }} fullWidth />
              <TextField select size="small" label="Module" value={permissionModule} onChange={(event) => { setPermissionModule(event.target.value); setPermissionPage(1); }} sx={{ minWidth: 180 }}>
                <MenuItem value="">ทั้งหมด</MenuItem>
                {permissionModules.map((module) => <MenuItem key={module} value={module}>{module}</MenuItem>)}
              </TextField>
              <TextField select size="small" label="สถานะ" value={permissionStatus} onChange={(event) => { setPermissionStatus(event.target.value); setPermissionPage(1); }} sx={{ minWidth: 160 }}>
                <MenuItem value="all">ทั้งหมด</MenuItem>
                <MenuItem value="active">ใช้งาน</MenuItem>
                <MenuItem value="inactive">ปิดใช้งาน</MenuItem>
              </TextField>
            </Stack>
          }
        />
      </Stack>
      <ConfirmDeleteDialog
        open={Boolean(deletingRole)}
        title="ยืนยันการลบบทบาท"
        itemName={deletingRole ? getRoleLabel(deletingRole.name) : ""}
        description="บทบาทระบบหรือบทบาทที่มีผู้ใช้งานอยู่จะไม่สามารถลบได้"
        confirmLabel="ลบบทบาท"
        isLoading={deleteRoleMutation.isPending}
        onClose={() => setDeletingRole(null)}
        onConfirm={() => deletingRole && deleteRoleMutation.mutate(deletingRole.id)}
      />
      <ConfirmDeleteDialog
        open={Boolean(deletingPermission)}
        title="ยืนยันการลบสิทธิ์"
        itemName={deletingPermission?.code}
        description="สิทธิ์ที่ถูกใช้งานกับบทบาทอยู่จะไม่สามารถลบได้"
        confirmLabel="ลบสิทธิ์"
        isLoading={deletePermissionMutation.isPending}
        onClose={() => setDeletingPermission(null)}
        onConfirm={() => deletingPermission && deletePermissionMutation.mutate(deletingPermission.id)}
      />
    </>
  );
}

function extractErrorMessage(error: unknown, fallback: string) {
  const maybeError = error as { response?: { data?: { message?: string; data?: { message?: string } } } };
  return maybeError.response?.data?.data?.message ?? maybeError.response?.data?.message ?? fallback;
}
