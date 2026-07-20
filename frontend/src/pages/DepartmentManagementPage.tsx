import AddOutlinedIcon from "@mui/icons-material/AddOutlined";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
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
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import { deleteDepartment, getDepartments, type DepartmentSummary } from "../api/adminApi";
import { ActionTooltip } from "../components/common/ActionTooltip";
import { PageHeader } from "../components/PageHeader";
import { PermissionGuard } from "../context/PermissionContext";
import { useNotification } from "../hooks/useNotification";
import { DepartmentManagementGridPage } from "./DepartmentManagementGridPage";

export function DepartmentManagementPage() {
  return <DepartmentManagementGridPage />;
}

export function LegacyDepartmentManagementPage() {
  const queryClient = useQueryClient();
  const { showSuccess } = useNotification();
  const [deletingDepartment, setDeletingDepartment] = useState<DepartmentSummary | null>(null);
  const { data = [], isLoading } = useQuery({
    queryKey: ["departments"],
    queryFn: getDepartments,
  });

  const deleteMutation = useMutation({
    mutationFn: deleteDepartment,
    onSuccess: () => {
      showSuccess("ลบหน่วยงานเรียบร้อยแล้ว");
      queryClient.invalidateQueries({ queryKey: ["departments"] });
      setDeletingDepartment(null);
    },
  });

  return (
    <>
      <Stack direction={{ xs: "column", sm: "row" }} justifyContent="space-between" spacing={2}>
        <PageHeader title="จัดการหน่วยงาน" subtitle="เพิ่ม แก้ไข และลบหน่วยงานที่ไม่จำเป็น" />
        <PermissionGuard permission="DepartmentManagement.Create">
          <Button
            component={RouterLink}
            to="/admin/departments/create"
            variant="contained"
            startIcon={<AddOutlinedIcon />}
            sx={{ alignSelf: { sm: "center" } }}
          >
            เพิ่มหน่วยงาน
          </Button>
        </PermissionGuard>
      </Stack>
      <Card>
        <CardContent>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>ชื่อหน่วยงาน</TableCell>
                <TableCell>รายละเอียด</TableCell>
                <TableCell>สถานะ</TableCell>
                <TableCell align="right">จัดการ</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {isLoading ? (
                <TableRow>
                  <TableCell colSpan={4}>กำลังโหลดข้อมูลหน่วยงาน...</TableCell>
                </TableRow>
              ) : (
                data.map((department) => (
                  <TableRow key={department.id}>
                    <TableCell>{department.name}</TableCell>
                    <TableCell>{department.description ?? "-"}</TableCell>
                    <TableCell>
                      <Chip
                        size="small"
                        label={department.isActive ? "ใช้งาน" : "ปิดใช้งาน"}
                        color={department.isActive ? "success" : "default"}
                      />
                    </TableCell>
                    <TableCell align="right">
                      <PermissionGuard permission="DepartmentManagement.Edit">
                        <ActionTooltip title="แก้ไขข้อมูลหน่วยงาน">
                          <IconButton
                            component={RouterLink}
                            to={`/admin/departments/${department.id}/edit`}
                            aria-label="แก้ไขข้อมูลหน่วยงาน"
                          >
                            <EditOutlinedIcon />
                          </IconButton>
                        </ActionTooltip>
                      </PermissionGuard>
                      <PermissionGuard permission="DepartmentManagement.Delete">
                        <ActionTooltip title="ลบหน่วยงาน">
                          <IconButton
                            aria-label="ลบหน่วยงาน"
                            color="error"
                            disabled={deleteMutation.isPending}
                            onClick={() => setDeletingDepartment(department)}
                          >
                            <DeleteOutlineIcon />
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
      <Dialog open={Boolean(deletingDepartment)} onClose={() => setDeletingDepartment(null)} fullWidth maxWidth="sm">
        <DialogTitle>ยืนยันการลบหน่วยงาน</DialogTitle>
        <DialogContent>
          ต้องการลบหน่วยงาน “{deletingDepartment?.name}” ใช่หรือไม่?
          <br />
          หากหน่วยงานนี้มีผู้ใช้งานหรือกฎอนุมัติผูกอยู่ ระบบจะไม่อนุญาตให้ลบ
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeletingDepartment(null)}>ยกเลิก</Button>
          <Button
            color="error"
            variant="contained"
            disabled={!deletingDepartment || deleteMutation.isPending}
            onClick={() => deletingDepartment && deleteMutation.mutate(deletingDepartment.id)}
          >
            ลบหน่วยงาน
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}
