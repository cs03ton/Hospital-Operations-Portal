import BlockOutlinedIcon from "@mui/icons-material/BlockOutlined";
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
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { Link as RouterLink } from "react-router-dom";
import {
  createRole,
  deactivateRole,
  getRoles,
  updateRole,
  type RoleSummary,
  type SaveRoleRequest,
} from "../api/adminApi";
import { ActionTooltip } from "../components/common/ActionTooltip";
import { PageHeader } from "../components/PageHeader";
import { PermissionGuard, usePermission } from "../context/PermissionContext";
import { useNotification } from "../hooks/useNotification";
import { getRoleLabel } from "../utils/roleLabels";

type RoleFormValues = {
  name: string;
  description: string;
  isActive: boolean;
};

export function RoleManagementPage() {
  const queryClient = useQueryClient();
  const { hasPermission } = usePermission();
  const { showSuccess } = useNotification();
  const [editingRole, setEditingRole] = useState<RoleSummary | null>(null);
  const { data = [], isLoading } = useQuery({ queryKey: ["roles"], queryFn: getRoles });

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
    mutationFn: (values: SaveRoleRequest) =>
      editingRole ? updateRole(editingRole.id, values) : createRole(values),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["roles"] });
      showSuccess(editingRole ? "บันทึกบทบาทเรียบร้อยแล้ว" : "เพิ่มบทบาทเรียบร้อยแล้ว");
      setEditingRole(null);
      reset({ name: "", description: "", isActive: true });
    },
  });

  const deactivateMutation = useMutation({
    mutationFn: deactivateRole,
    onSuccess: () => {
      showSuccess("ปิดใช้งานบทบาทเรียบร้อยแล้ว");
      return queryClient.invalidateQueries({ queryKey: ["roles"] });
    },
  });

  function editRole(role: RoleSummary) {
    if (!hasPermission("RoleManagement.Edit")) {
      return;
    }

    setEditingRole(role);
    reset({
      name: role.name,
      description: role.description ?? "",
      isActive: role.isActive,
    });
  }

  function onSubmit(values: RoleFormValues) {
    saveMutation.mutate({
      name: values.name,
      description: values.description || null,
      isActive: values.isActive,
    });
  }

  const canSaveRole = editingRole ? "RoleManagement.Edit" : "RoleManagement.Create";

  return (
    <>
      <PageHeader title="บทบาทและสิทธิ์" subtitle="จัดการบทบาทและกำหนดสิทธิ์การใช้งานระบบ" />
      <Stack spacing={2}>
        <PermissionGuard permission={canSaveRole}>
          <Card>
            <CardContent>
              <Typography variant="h6" sx={{ mb: 2 }}>
                {editingRole ? "แก้ไขบทบาท" : "เพิ่มบทบาท"}
              </Typography>
              <Stack component="form" spacing={2} onSubmit={handleSubmit(onSubmit)}>
                {saveMutation.isError && <Alert severity="error">บันทึกบทบาทไม่สำเร็จ</Alert>}
                <TextField
                  fullWidth
                  label="ชื่อบทบาท"
                  InputLabelProps={{ shrink: true }}
                  error={Boolean(errors.name)}
                  helperText={errors.name?.message}
                  {...register("name", { required: "กรุณากรอกชื่อบทบาท" })}
                />
                <TextField fullWidth label="รายละเอียด" InputLabelProps={{ shrink: true }} {...register("description")} />
                <Controller
                  name="isActive"
                  control={control}
                  render={({ field }) => (
                    <FormControlLabel
                      control={<Checkbox checked={field.value} onChange={(event) => field.onChange(event.target.checked)} />}
                      label="เปิดใช้งาน"
                    />
                  )}
                />
                <Stack direction="row" spacing={1.5}>
                  <Button type="submit" variant="contained" disabled={saveMutation.isPending}>
                    บันทึกข้อมูล
                  </Button>
                  {editingRole && (
                    <Button
                      variant="outlined"
                      onClick={() => {
                        setEditingRole(null);
                        reset({ name: "", description: "", isActive: true });
                      }}
                    >
                      ยกเลิก
                    </Button>
                  )}
                </Stack>
              </Stack>
            </CardContent>
          </Card>
        </PermissionGuard>

        <Card>
          <CardContent>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>ชื่อบทบาท</TableCell>
                  <TableCell>รายละเอียด</TableCell>
                  <TableCell>ประเภท</TableCell>
                  <TableCell>สถานะ</TableCell>
                  <TableCell align="right">จัดการ</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {isLoading ? (
                  <TableRow>
                    <TableCell colSpan={5}>กำลังโหลดข้อมูลบทบาท...</TableCell>
                  </TableRow>
                ) : (
                  data.map((role) => (
                    <TableRow key={role.id}>
                      <TableCell>{getRoleLabel(role.name)}</TableCell>
                      <TableCell>{role.description ?? "-"}</TableCell>
                      <TableCell>{role.isSystemRole ? "บทบาทระบบ" : "บทบาทกำหนดเอง"}</TableCell>
                      <TableCell>
                        <Chip
                          size="small"
                          label={role.isActive ? "ใช้งาน" : "ปิดใช้งาน"}
                          color={role.isActive ? "success" : "default"}
                        />
                      </TableCell>
                      <TableCell align="right">
                        <PermissionGuard permission="RoleManagement.Edit">
                          <ActionTooltip title="แก้ไขข้อมูลบทบาท">
                            <IconButton aria-label="แก้ไขข้อมูลบทบาท" onClick={() => editRole(role)}>
                              <EditOutlinedIcon />
                            </IconButton>
                          </ActionTooltip>
                        </PermissionGuard>
                        <PermissionGuard permission="RoleManagement.Manage">
                          <ActionTooltip title="จัดการสิทธิ์ของบทบาทนี้">
                            <IconButton
                              component={RouterLink}
                              to={`/admin/roles/${role.id}/permissions`}
                              aria-label="จัดการสิทธิ์ของบทบาทนี้"
                            >
                              <RuleOutlinedIcon />
                            </IconButton>
                          </ActionTooltip>
                        </PermissionGuard>
                        <PermissionGuard permission="RoleManagement.Delete">
                          <ActionTooltip title="ปิดใช้งานบทบาท">
                            <IconButton
                              aria-label="ปิดใช้งานบทบาท"
                              disabled={role.isSystemRole || !role.isActive || deactivateMutation.isPending}
                              onClick={() => deactivateMutation.mutate(role.id)}
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
      </Stack>
    </>
  );
}
