import AddOutlinedIcon from "@mui/icons-material/AddOutlined";
import BlockOutlinedIcon from "@mui/icons-material/BlockOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
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
import { Link as RouterLink } from "react-router-dom";
import { deactivateDepartment, getDepartments } from "../api/adminApi";
import { ActionTooltip } from "../components/common/ActionTooltip";
import { PageHeader } from "../components/PageHeader";
import { PermissionGuard } from "../context/PermissionContext";

export function DepartmentManagementPage() {
  const queryClient = useQueryClient();
  const { data = [], isLoading } = useQuery({
    queryKey: ["departments"],
    queryFn: getDepartments,
  });

  const deactivateMutation = useMutation({
    mutationFn: deactivateDepartment,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["departments"] }),
  });

  return (
    <>
      <Stack direction={{ xs: "column", sm: "row" }} justifyContent="space-between" spacing={2}>
        <PageHeader title="จัดการหน่วยงาน" subtitle="เพิ่ม แก้ไข และปิดใช้งานหน่วยงาน" />
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
                        <ActionTooltip title="ปิดใช้งานหน่วยงาน">
                          <IconButton
                            aria-label="ปิดใช้งานหน่วยงาน"
                            disabled={!department.isActive || deactivateMutation.isPending}
                            onClick={() => deactivateMutation.mutate(department.id)}
                          >
                            <BlockOutlinedIcon />
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
    </>
  );
}
