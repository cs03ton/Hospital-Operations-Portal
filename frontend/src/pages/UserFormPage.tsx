import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Checkbox,
  FormControl,
  FormControlLabel,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect } from "react";
import { Controller, useForm } from "react-hook-form";
import { useNavigate, useParams } from "react-router-dom";
import {
  createUser,
  getDepartments,
  getRoles,
  getUser,
  updateUser,
  type SaveUserRequest,
} from "../api/adminApi";
import { PageHeader } from "../components/PageHeader";

type UserFormValues = {
  employeeCode: string;
  fullname: string;
  username: string;
  password: string;
  roleIds: string[];
  departmentId: string;
  lineUserId: string;
  isActive: boolean;
};

export function UserFormPage() {
  const { id } = useParams();
  const isEdit = Boolean(id);
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const { data: departments = [] } = useQuery({
    queryKey: ["departments"],
    queryFn: getDepartments,
  });
  const { data: roles = [] } = useQuery({ queryKey: ["roles"], queryFn: getRoles });

  const {
    control,
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<UserFormValues>({
    defaultValues: {
      employeeCode: "",
      fullname: "",
      username: "",
      password: "",
      roleIds: [],
      departmentId: "",
      lineUserId: "",
      isActive: true,
    },
  });

  const { data: editingUser } = useQuery({
    queryKey: ["users", id],
    queryFn: () => getUser(id!),
    enabled: isEdit,
  });

  useEffect(() => {
    if (editingUser) {
      reset({
        employeeCode: editingUser.employeeCode ?? "",
        fullname: editingUser.fullname,
        username: editingUser.username,
        password: "",
        roleIds: editingUser.roleIds,
        departmentId: editingUser.departmentId ?? "",
        lineUserId: editingUser.lineUserId ?? "",
        isActive: editingUser.isActive,
      });
    }
  }, [editingUser, reset]);

  const saveMutation = useMutation({
    mutationFn: (values: SaveUserRequest) => (isEdit ? updateUser(id!, values) : createUser(values)),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["users"] });
      navigate("/admin/users");
    },
  });

  function onSubmit(values: UserFormValues) {
    saveMutation.mutate({
      employeeCode: values.employeeCode || null,
      fullname: values.fullname,
      username: values.username,
      password: values.password || undefined,
      roleIds: values.roleIds,
      departmentId: values.departmentId || null,
      lineUserId: values.lineUserId || null,
      isActive: values.isActive,
    });
  }

  return (
    <>
      <PageHeader
        title={isEdit ? "แก้ไขผู้ใช้" : "เพิ่มผู้ใช้"}
        subtitle="กำหนดข้อมูลบัญชี หน่วยงาน และบทบาทของผู้ใช้"
      />
      <Card>
        <CardContent>
          <Stack component="form" spacing={2.5} onSubmit={handleSubmit(onSubmit)}>
            {saveMutation.isError && <Alert severity="error">บันทึกข้อมูลไม่สำเร็จ</Alert>}
            <TextField label="รหัสพนักงาน" {...register("employeeCode")} />
            <TextField
              label="ชื่อ-สกุล"
              error={Boolean(errors.fullname)}
              helperText={errors.fullname?.message}
              {...register("fullname", { required: "กรุณากรอกชื่อ-สกุล" })}
            />
            <TextField
              label="ชื่อผู้ใช้"
              disabled={isEdit}
              error={Boolean(errors.username)}
              helperText={errors.username?.message}
              {...register("username", { required: "กรุณากรอกชื่อผู้ใช้" })}
            />
            <TextField
              label={isEdit ? "รหัสผ่านใหม่ (ไม่กรอกหากไม่เปลี่ยน)" : "รหัสผ่าน"}
              type="password"
              error={Boolean(errors.password)}
              helperText={errors.password?.message}
              {...register("password", {
                required: isEdit ? false : "กรุณากรอกรหัสผ่าน",
              })}
            />
            <Controller
              name="departmentId"
              control={control}
              render={({ field }) => (
                <FormControl>
                  <InputLabel>หน่วยงาน</InputLabel>
                  <Select label="หน่วยงาน" {...field}>
                    <MenuItem value="">ไม่ระบุ</MenuItem>
                    {departments.map((department) => (
                      <MenuItem key={department.id} value={department.id}>
                        {department.name}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              )}
            />
            <Controller
              name="roleIds"
              control={control}
              rules={{ validate: (value) => value.length > 0 || "กรุณาเลือกบทบาทอย่างน้อย 1 รายการ" }}
              render={({ field }) => (
                <FormControl error={Boolean(errors.roleIds)}>
                  <InputLabel>บทบาท</InputLabel>
                  <Select label="บทบาท" multiple {...field}>
                    {roles
                      .filter((role) => role.isActive)
                      .map((role) => (
                        <MenuItem key={role.id} value={role.id}>
                          {role.name}
                        </MenuItem>
                      ))}
                  </Select>
                  {errors.roleIds && (
                    <Box sx={{ mt: 0.5, color: "error.main", fontSize: 12 }}>
                      {errors.roleIds.message}
                    </Box>
                  )}
                </FormControl>
              )}
            />
            <TextField label="LINE User ID" {...register("lineUserId")} />
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
              <Button variant="outlined" onClick={() => navigate("/admin/users")}>
                ยกเลิก
              </Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>
    </>
  );
}
