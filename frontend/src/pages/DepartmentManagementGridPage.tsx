import AddOutlinedIcon from "@mui/icons-material/AddOutlined";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import { Box, Button, IconButton, MenuItem, Stack, TextField } from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import {
  deleteDepartment,
  getDepartmentsPaged,
  type DepartmentSummary,
  type DeleteResult,
} from "../api/adminApi";
import { ActionTooltip } from "../components/common/ActionTooltip";
import { ConfirmDeleteDialog } from "../components/common/ConfirmDeleteDialog";
import { ManagementDataGrid, type GridSortDirection, type ManagementDataGridColumn } from "../components/common/ManagementDataGrid";
import { StatusBadge } from "../components/common/StatusBadge";
import { PageHeader } from "../components/PageHeader";
import { PermissionGuard } from "../context/PermissionContext";
import { useNotification } from "../hooks/useNotification";
import { formatThaiDateTime } from "../utils/dateFormat";

export function DepartmentManagementGridPage() {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useNotification();
  const [deletingDepartment, setDeletingDepartment] = useState<DepartmentSummary | null>(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("all");
  const [sort, setSort] = useState("name");
  const [direction, setDirection] = useState<GridSortDirection>("asc");

  const queryParams = { page, pageSize, search: search || undefined, status, sort, direction };
  const departmentsQuery = useQuery({
    queryKey: ["departments", "grid", queryParams],
    queryFn: () => getDepartmentsPaged(queryParams),
  });

  const deleteMutation = useMutation({
    mutationFn: deleteDepartment,
    onSuccess: async (result?: DeleteResult) => {
      await queryClient.invalidateQueries({ queryKey: ["departments"] });
      setDeletingDepartment(null);
      showSuccess(result?.message ?? "ลบหน่วยงานเรียบร้อยแล้ว");
    },
    onError: (error: unknown) => showError(extractErrorMessage(error, "ไม่สามารถลบหน่วยงานได้")),
  });

  const columns = useMemo<ManagementDataGridColumn<DepartmentSummary>[]>(() => [
    { key: "code", label: "Department Code", sortable: true, render: (department) => department.id.slice(0, 8).toUpperCase() },
    { key: "name", label: "Department Name", sortable: true, render: (department) => department.name },
    { key: "manager", label: "Manager", render: () => "-" },
    { key: "usersCount", label: "Users Count", sortable: true, render: (department) => department.usersCount.toLocaleString("th-TH") },
    {
      key: "status",
      label: "Status",
      render: (department) => (
        <StatusBadge domain="active" status={department.isActive ? "active" : "inactive"} />
      ),
    },
    { key: "createdAt", label: "Created", sortable: true, render: (department) => formatThaiDateTime(department.createdAt) },
    {
      key: "action",
      label: "Action",
      align: "right",
      render: (department) => (
        <Stack direction="row" justifyContent="flex-end" spacing={0.5}>
          <PermissionGuard permission="DepartmentManagement.Edit">
            <ActionTooltip title="แก้ไขข้อมูลหน่วยงาน">
              <IconButton component={RouterLink} to={`/admin/departments/${department.id}/edit`}>
                <EditOutlinedIcon />
              </IconButton>
            </ActionTooltip>
          </PermissionGuard>
          <PermissionGuard permission="DepartmentManagement.Delete">
            <ActionTooltip title="ลบหน่วยงาน">
              <IconButton color="error" disabled={deleteMutation.isPending} onClick={() => setDeletingDepartment(department)}>
                <DeleteOutlineIcon />
              </IconButton>
            </ActionTooltip>
          </PermissionGuard>
        </Stack>
      ),
    },
  ], [deleteMutation.isPending]);

  function resetToFirstPage() {
    setPage(1);
  }

  return (
    <>
      <Stack direction={{ xs: "column", sm: "row" }} justifyContent="space-between" spacing={2}>        
        <Box sx={{ minWidth: 0 }}>
          <PageHeader title="จัดการหน่วยงาน" subtitle="ค้นหา เรียงลำดับ และลบหน่วยงานตามเงื่อนไขอ้างอิง" />
        </Box>
        <PermissionGuard permission="DepartmentManagement.Create">
          <Button component={RouterLink} to="/admin/departments/create" variant="contained" startIcon={<AddOutlinedIcon />} sx={{ alignSelf: { sm: "center" } }}>
            เพิ่มหน่วยงาน
          </Button>
        </PermissionGuard>
      </Stack>
      <ManagementDataGrid
        title="รายการหน่วยงาน"
        rows={departmentsQuery.data?.items ?? []}
        columns={columns}
        getRowId={(department) => department.id}
        isLoading={departmentsQuery.isLoading}
        emptyMessage="ไม่พบหน่วยงานตามเงื่อนไข"
        page={page}
        pageSize={pageSize}
        totalItems={departmentsQuery.data?.totalItems ?? 0}
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
            <TextField size="small" label="ค้นหาชื่อหรือรายละเอียดหน่วยงาน" value={search} onChange={(event) => { setSearch(event.target.value); resetToFirstPage(); }} fullWidth />
            <TextField select size="small" label="สถานะ" value={status} onChange={(event) => { setStatus(event.target.value); resetToFirstPage(); }} sx={{ minWidth: 160 }}>
              <MenuItem value="all">ทั้งหมด</MenuItem>
              <MenuItem value="active">ใช้งาน</MenuItem>
              <MenuItem value="inactive">ปิดใช้งาน</MenuItem>
            </TextField>
          </Stack>
        }
      />
      <ConfirmDeleteDialog
        open={Boolean(deletingDepartment)}
        title="ยืนยันการลบหน่วยงาน"
        itemName={deletingDepartment?.name}
        description="ระบบจะไม่อนุญาตให้ลบถ้าหน่วยงานมีผู้ใช้ กฎอนุมัติ คำขอลา หรือ audit log อ้างอิงอยู่"
        confirmLabel="ลบหน่วยงาน"
        isLoading={deleteMutation.isPending}
        onClose={() => setDeletingDepartment(null)}
        onConfirm={() => deletingDepartment && deleteMutation.mutate(deletingDepartment.id)}
      />
    </>
  );
}

function extractErrorMessage(error: unknown, fallback: string) {
  const maybeError = error as { response?: { data?: { message?: string; data?: { message?: string } } } };
  return maybeError.response?.data?.data?.message ?? maybeError.response?.data?.message ?? fallback;
}
