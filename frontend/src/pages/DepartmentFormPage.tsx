import {
  Alert,
  Button,
  Card,
  CardContent,
  Checkbox,
  FormControlLabel,
  Stack,
  TextField,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect } from "react";
import { Controller, useForm } from "react-hook-form";
import { useNavigate, useParams } from "react-router-dom";
import {
  createDepartment,
  getDepartment,
  updateDepartment,
  type SaveDepartmentRequest,
} from "../api/adminApi";
import { PageHeader } from "../components/PageHeader";
import { useNotification } from "../hooks/useNotification";

type DepartmentFormValues = {
  name: string;
  description: string;
  isActive: boolean;
};

export function DepartmentFormPage() {
  const { id } = useParams();
  const isEdit = Boolean(id);
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { showSuccess } = useNotification();

  const {
    control,
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<DepartmentFormValues>({
    defaultValues: {
      name: "",
      description: "",
      isActive: true,
    },
  });

  const { data: editingDepartment } = useQuery({
    queryKey: ["departments", id],
    queryFn: () => getDepartment(id!),
    enabled: isEdit,
  });

  useEffect(() => {
    if (editingDepartment) {
      reset({
        name: editingDepartment.name,
        description: editingDepartment.description ?? "",
        isActive: editingDepartment.isActive,
      });
    }
  }, [editingDepartment, reset]);

  const saveMutation = useMutation({
    mutationFn: (values: SaveDepartmentRequest) =>
      isEdit ? updateDepartment(id!, values) : createDepartment(values),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["departments"] });
      showSuccess(isEdit ? "บันทึกข้อมูลหน่วยงานเรียบร้อยแล้ว" : "เพิ่มหน่วยงานเรียบร้อยแล้ว");
      navigate("/admin/departments");
    },
  });

  function onSubmit(values: DepartmentFormValues) {
    saveMutation.mutate({
      name: values.name,
      description: values.description || null,
      isActive: values.isActive,
    });
  }

  return (
    <>
      <PageHeader
        title={isEdit ? "แก้ไขหน่วยงาน" : "เพิ่มหน่วยงาน"}
        subtitle="กำหนดชื่อ รายละเอียด และสถานะของหน่วยงาน"
      />
      <Card>
        <CardContent>
          <Stack component="form" spacing={2.5} onSubmit={handleSubmit(onSubmit)}>
            {saveMutation.isError && <Alert severity="error">บันทึกข้อมูลไม่สำเร็จ</Alert>}
            <TextField
              fullWidth
              label="ชื่อหน่วยงาน"
              InputLabelProps={{ shrink: true }}
              error={Boolean(errors.name)}
              helperText={errors.name?.message}
              {...register("name", { required: "กรุณากรอกชื่อหน่วยงาน" })}
            />
            <TextField fullWidth label="รายละเอียด" InputLabelProps={{ shrink: true }} multiline minRows={3} {...register("description")} />
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
              <Button variant="outlined" onClick={() => navigate("/admin/departments")}>
                ยกเลิก
              </Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>
    </>
  );
}
