import AddOutlinedIcon from "@mui/icons-material/AddOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import PersonOffOutlinedIcon from "@mui/icons-material/PersonOffOutlined";
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
import { deactivateUser, getUsers } from "../api/adminApi";
import { PageHeader } from "../components/PageHeader";
import { PermissionGuard } from "../context/PermissionContext";

export function UserManagementPage() {
  const queryClient = useQueryClient();
  const { data = [], isLoading } = useQuery({
    queryKey: ["users"],
    queryFn: getUsers,
  });

  const deactivateMutation = useMutation({
    mutationFn: deactivateUser,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["users"] }),
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
                <TableCell>สถานะ</TableCell>
                <TableCell align="right">จัดการ</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {isLoading ? (
                <TableRow>
                  <TableCell colSpan={6}>กำลังโหลดข้อมูลผู้ใช้...</TableCell>
                </TableRow>
              ) : (
                data.map((user) => (
                  <TableRow key={user.id}>
                    <TableCell>{user.fullname}</TableCell>
                    <TableCell>{user.username}</TableCell>
                    <TableCell>{user.roles.join(", ") || "-"}</TableCell>
                    <TableCell>{user.department ?? "-"}</TableCell>
                    <TableCell>
                      <Chip
                        size="small"
                        label={user.isActive ? "ใช้งาน" : "ปิดใช้งาน"}
                        color={user.isActive ? "success" : "default"}
                      />
                    </TableCell>
                    <TableCell align="right">
                      <PermissionGuard permission="UserManagement.Edit">
                        <IconButton
                          component={RouterLink}
                          to={`/admin/users/${user.id}/edit`}
                          aria-label="แก้ไขผู้ใช้"
                        >
                          <EditOutlinedIcon />
                        </IconButton>
                      </PermissionGuard>
                      <PermissionGuard permission="UserManagement.Delete">
                        <IconButton
                          aria-label="ปิดใช้งานผู้ใช้"
                          disabled={!user.isActive || deactivateMutation.isPending}
                          onClick={() => deactivateMutation.mutate(user.id)}
                        >
                          <PersonOffOutlinedIcon />
                        </IconButton>
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
