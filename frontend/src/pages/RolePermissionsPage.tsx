import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Checkbox,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  getPermissions,
  getRole,
  getRolePermissions,
  updateRolePermissions,
} from "../api/adminApi";
import { PageHeader } from "../components/PageHeader";

const actionLabels: Record<string, string> = {
  View: "ดูข้อมูล",
  Create: "เพิ่ม",
  Edit: "แก้ไข",
  Delete: "ลบ",
  Approve: "อนุมัติ",
  Export: "ส่งออก",
  Manage: "จัดการ",
};

export function RolePermissionsPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [selectedIds, setSelectedIds] = useState<string[]>([]);

  const { data: role } = useQuery({ queryKey: ["roles", id], queryFn: () => getRole(id!), enabled: Boolean(id) });
  const { data: permissions = [] } = useQuery({ queryKey: ["permissions"], queryFn: getPermissions });
  const { data: rolePermissions = [] } = useQuery({
    queryKey: ["roles", id, "permissions"],
    queryFn: () => getRolePermissions(id!),
    enabled: Boolean(id),
  });

  useEffect(() => {
    setSelectedIds(rolePermissions.map((permission) => permission.id));
  }, [rolePermissions]);

  const groupedPermissions = useMemo(() => {
    return permissions.reduce<Record<string, typeof permissions>>((groups, permission) => {
      groups[permission.group] ??= [];
      groups[permission.group].push(permission);
      return groups;
    }, {});
  }, [permissions]);

  const saveMutation = useMutation({
    mutationFn: () => updateRolePermissions(id!, selectedIds),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["roles", id, "permissions"] });
      navigate("/admin/roles");
    },
  });

  function togglePermission(permissionId: string) {
    setSelectedIds((current) =>
      current.includes(permissionId)
        ? current.filter((idValue) => idValue !== permissionId)
        : [...current, permissionId],
    );
  }

  return (
    <>
      <PageHeader
        title="กำหนดสิทธิ์บทบาท"
        subtitle={`บทบาท: ${role?.name ?? "-"}`}
      />
      <Card>
        <CardContent>
          <Stack spacing={2}>
            {saveMutation.isError && <Alert severity="error">บันทึกสิทธิ์ไม่สำเร็จ</Alert>}
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>กลุ่มสิทธิ์</TableCell>
                  {Object.values(actionLabels).map((label) => (
                    <TableCell align="center" key={label}>
                      {label}
                    </TableCell>
                  ))}
                </TableRow>
              </TableHead>
              <TableBody>
                {Object.entries(groupedPermissions).map(([group, groupPermissions]) => (
                  <TableRow key={group}>
                    <TableCell>{group}</TableCell>
                    {Object.keys(actionLabels).map((action) => {
                      const permission = groupPermissions.find((item) => item.action === action);
                      return (
                        <TableCell align="center" key={action}>
                          {permission ? (
                            <Checkbox
                              checked={selectedIds.includes(permission.id)}
                              onChange={() => togglePermission(permission.id)}
                            />
                          ) : (
                            <Box component="span">-</Box>
                          )}
                        </TableCell>
                      );
                    })}
                  </TableRow>
                ))}
              </TableBody>
            </Table>
            <Stack direction="row" spacing={1.5}>
              <Button variant="contained" onClick={() => saveMutation.mutate()} disabled={saveMutation.isPending}>
                บันทึกสิทธิ์
              </Button>
              <Button variant="outlined" onClick={() => navigate("/admin/roles")}>
                ยกเลิก
              </Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>
    </>
  );
}
