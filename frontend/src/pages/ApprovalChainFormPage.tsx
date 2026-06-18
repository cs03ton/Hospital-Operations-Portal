import DeleteOutlineOutlinedIcon from "@mui/icons-material/DeleteOutlineOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import { Alert, Button, Card, CardContent, Checkbox, FormControlLabel, Grid, IconButton, MenuItem, Stack, Table, TableBody, TableCell, TableHead, TableRow, TextField, Typography } from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { useNavigate, useParams } from "react-router-dom";
import { getDepartments, getPermissions, getRoles, getUsers } from "../api/adminApi";
import {
  createApprovalChain,
  createApprovalChainStep,
  deleteApprovalChainStep,
  getApprovalChain,
  getApprovalChainSteps,
  getLeaveTypes,
  updateApprovalChain,
  updateApprovalChainStep,
  type ApprovalChainStep,
  type SaveApprovalChainRequest,
  type SaveApprovalChainStepRequest,
} from "../api/leaveApi";
import { PageHeader } from "../components/PageHeader";
import { PermissionGuard } from "../context/PermissionContext";

const emptyChain: SaveApprovalChainRequest = {
  name: "",
  description: "",
  departmentId: "",
  leaveTypeId: "",
  minimumDays: 0,
  isActive: true,
};

const emptyStep: SaveApprovalChainStepRequest = {
  stepOrder: 1,
  name: "",
  approverRoleId: "",
  approverUserId: "",
  requiredPermissionCode: "LeaveManagement.Approve",
  isActive: true,
};

export function ApprovalChainFormPage() {
  const { id } = useParams();
  const isEdit = Boolean(id);
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [editingStep, setEditingStep] = useState<ApprovalChainStep | null>(null);
  const { data: chain } = useQuery({ queryKey: ["approval-chains", id], queryFn: () => getApprovalChain(id!), enabled: isEdit });
  const { data: steps = [] } = useQuery({ queryKey: ["approval-chains", id, "steps"], queryFn: () => getApprovalChainSteps(id!), enabled: isEdit });
  const { data: departments = [] } = useQuery({ queryKey: ["departments"], queryFn: getDepartments });
  const { data: leaveTypes = [] } = useQuery({ queryKey: ["leave-types"], queryFn: getLeaveTypes });
  const { data: roles = [] } = useQuery({ queryKey: ["roles"], queryFn: getRoles });
  const { data: users = [] } = useQuery({ queryKey: ["users"], queryFn: getUsers });
  const { data: permissions = [] } = useQuery({ queryKey: ["permissions"], queryFn: getPermissions });

  const chainForm = useForm<SaveApprovalChainRequest>({ defaultValues: emptyChain });
  const stepForm = useForm<SaveApprovalChainStepRequest>({ defaultValues: emptyStep });

  useEffect(() => {
    if (chain) {
      chainForm.reset({
        name: chain.name,
        description: chain.description ?? "",
        departmentId: chain.departmentId ?? "",
        leaveTypeId: chain.leaveTypeId ?? "",
        minimumDays: chain.minimumDays,
        isActive: chain.isActive,
      });
    }
  }, [chain, chainForm]);

  const saveChainMutation = useMutation({
    mutationFn: (values: SaveApprovalChainRequest) => {
      const payload = normalizeChain(values);
      return isEdit ? updateApprovalChain(id!, payload) : createApprovalChain(payload);
    },
    onSuccess: async (saved) => {
      await queryClient.invalidateQueries({ queryKey: ["approval-chains"] });
      if (!isEdit) {
        navigate(`/admin/approval-chains/${saved.id}/edit`);
      }
    },
  });

  const saveStepMutation = useMutation({
    mutationFn: (values: SaveApprovalChainStepRequest) => {
      const payload = normalizeStep(values);
      return editingStep ? updateApprovalChainStep(editingStep.id, payload) : createApprovalChainStep(id!, payload);
    },
    onSuccess: async () => {
      setEditingStep(null);
      stepForm.reset(emptyStep);
      await queryClient.invalidateQueries({ queryKey: ["approval-chains", id, "steps"] });
    },
  });

  const deleteStepMutation = useMutation({
    mutationFn: deleteApprovalChainStep,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["approval-chains", id, "steps"] }),
  });

  function onEditStep(step: ApprovalChainStep) {
    setEditingStep(step);
    stepForm.reset({
      stepOrder: step.stepOrder,
      name: step.name,
      approverRoleId: step.approverRoleId ?? "",
      approverUserId: step.approverUserId ?? "",
      requiredPermissionCode: step.requiredPermissionCode,
      isActive: step.isActive,
    });
  }

  return (
    <>
      <PageHeader title={isEdit ? "แก้ไขสายอนุมัติ" : "สร้างสายอนุมัติ"} subtitle="กำหนดเงื่อนไขและลำดับผู้อนุมัติสำหรับคำขอลา" />
      <Stack spacing={2}>
        <Card>
          <CardContent>
            <Stack component="form" spacing={2} onSubmit={chainForm.handleSubmit((values) => saveChainMutation.mutate(values))}>
              {saveChainMutation.isError && <Alert severity="error">บันทึกสายอนุมัติไม่สำเร็จ</Alert>}
              <TextField label="ชื่อสายอนุมัติ" error={Boolean(chainForm.formState.errors.name)} helperText={chainForm.formState.errors.name?.message} {...chainForm.register("name", { required: "กรุณากรอกชื่อสายอนุมัติ" })} />
              <TextField label="รายละเอียด" {...chainForm.register("description")} />
              <Grid container spacing={2}>
                <Grid item xs={12} md={4}>
                  <TextField fullWidth select label="หน่วยงาน" defaultValue="" {...chainForm.register("departmentId")}>
                    <MenuItem value="">ทุกหน่วยงาน</MenuItem>
                    {departments.map((item) => <MenuItem key={item.id} value={item.id}>{item.name}</MenuItem>)}
                  </TextField>
                </Grid>
                <Grid item xs={12} md={4}>
                  <TextField fullWidth select label="ประเภทการลา" defaultValue="" {...chainForm.register("leaveTypeId")}>
                    <MenuItem value="">ทุกประเภท</MenuItem>
                    {leaveTypes.map((item) => <MenuItem key={item.id} value={item.id}>{item.name}</MenuItem>)}
                  </TextField>
                </Grid>
                <Grid item xs={12} md={4}>
                  <TextField fullWidth type="number" label="จำนวนวันขั้นต่ำ" inputProps={{ min: 0, step: 0.5 }} {...chainForm.register("minimumDays")} />
                </Grid>
              </Grid>
              <Controller name="isActive" control={chainForm.control} render={({ field }) => <FormControlLabel control={<Checkbox checked={field.value} onChange={(event) => field.onChange(event.target.checked)} />} label="เปิดใช้งาน" />} />
              <Stack direction="row" spacing={1.5}>
                <Button type="submit" variant="contained" disabled={saveChainMutation.isPending}>บันทึกข้อมูล</Button>
                <Button variant="outlined" onClick={() => navigate("/admin/approval-chains")}>กลับ</Button>
              </Stack>
            </Stack>
          </CardContent>
        </Card>

        {isEdit && (
          <PermissionGuard permission="ApprovalChain.Edit">
            <Card>
              <CardContent>
                <Typography variant="h6" sx={{ mb: 2 }}>{editingStep ? "แก้ไขขั้นอนุมัติ" : "เพิ่มขั้นอนุมัติ"}</Typography>
                <Stack component="form" spacing={2} onSubmit={stepForm.handleSubmit((values) => saveStepMutation.mutate(values))}>
                  {saveStepMutation.isError && <Alert severity="error">บันทึกขั้นอนุมัติไม่สำเร็จ</Alert>}
                  <Grid container spacing={2}>
                    <Grid item xs={12} md={2}>
                      <TextField fullWidth type="number" label="ลำดับ" inputProps={{ min: 1 }} {...stepForm.register("stepOrder", { required: true })} />
                    </Grid>
                    <Grid item xs={12} md={4}>
                      <TextField fullWidth label="ชื่อขั้นอนุมัติ" {...stepForm.register("name", { required: true })} />
                    </Grid>
                    <Grid item xs={12} md={3}>
                      <TextField fullWidth select label="บทบาทผู้อนุมัติ" defaultValue="" {...stepForm.register("approverRoleId")}>
                        <MenuItem value="">ไม่ระบุ</MenuItem>
                        {roles.map((item) => <MenuItem key={item.id} value={item.id}>{item.name}</MenuItem>)}
                      </TextField>
                    </Grid>
                    <Grid item xs={12} md={3}>
                      <TextField fullWidth select label="ผู้อนุมัติเฉพาะราย" defaultValue="" {...stepForm.register("approverUserId")}>
                        <MenuItem value="">ไม่ระบุ</MenuItem>
                        {users.map((item) => <MenuItem key={item.id} value={item.id}>{item.fullname}</MenuItem>)}
                      </TextField>
                    </Grid>
                    <Grid item xs={12} md={6}>
                      <TextField fullWidth select label="Permission ที่ต้องมี" defaultValue="LeaveManagement.Approve" {...stepForm.register("requiredPermissionCode")}>
                        {permissions.map((item) => <MenuItem key={item.id} value={item.code}>{item.code}</MenuItem>)}
                      </TextField>
                    </Grid>
                    <Grid item xs={12} md={6}>
                      <Controller name="isActive" control={stepForm.control} render={({ field }) => <FormControlLabel control={<Checkbox checked={field.value} onChange={(event) => field.onChange(event.target.checked)} />} label="เปิดใช้งานขั้นนี้" />} />
                    </Grid>
                  </Grid>
                  <Stack direction="row" spacing={1.5}>
                    <Button type="submit" variant="contained" disabled={saveStepMutation.isPending}>บันทึกขั้นอนุมัติ</Button>
                    {editingStep && <Button variant="outlined" onClick={() => { setEditingStep(null); stepForm.reset(emptyStep); }}>ยกเลิก</Button>}
                  </Stack>
                </Stack>
              </CardContent>
            </Card>
          </PermissionGuard>
        )}

        {isEdit && (
          <Card>
            <CardContent>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>ลำดับ</TableCell>
                    <TableCell>ชื่อขั้น</TableCell>
                    <TableCell>ผู้อนุมัติ</TableCell>
                    <TableCell>Permission</TableCell>
                    <TableCell>สถานะ</TableCell>
                    <TableCell align="right">จัดการ</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {steps.length ? steps.map((step) => (
                    <TableRow key={step.id}>
                      <TableCell>{step.stepOrder}</TableCell>
                      <TableCell>{step.name}</TableCell>
                      <TableCell>{step.approverUserName ?? step.approverRoleName ?? "-"}</TableCell>
                      <TableCell>{step.requiredPermissionCode}</TableCell>
                      <TableCell>{step.isActive ? "ใช้งาน" : "ปิดใช้งาน"}</TableCell>
                      <TableCell align="right">
                        <IconButton aria-label="แก้ไขขั้นอนุมัติ" onClick={() => onEditStep(step)}><EditOutlinedIcon /></IconButton>
                        <IconButton aria-label="ลบขั้นอนุมัติ" disabled={deleteStepMutation.isPending} onClick={() => deleteStepMutation.mutate(step.id)}><DeleteOutlineOutlinedIcon /></IconButton>
                      </TableCell>
                    </TableRow>
                  )) : (
                    <TableRow><TableCell colSpan={6}>ยังไม่มีขั้นอนุมัติ</TableCell></TableRow>
                  )}
                </TableBody>
              </Table>
            </CardContent>
          </Card>
        )}
      </Stack>
    </>
  );
}

function normalizeChain(values: SaveApprovalChainRequest): SaveApprovalChainRequest {
  return {
    ...values,
    departmentId: values.departmentId || null,
    leaveTypeId: values.leaveTypeId || null,
    minimumDays: Number(values.minimumDays),
  };
}

function normalizeStep(values: SaveApprovalChainStepRequest): SaveApprovalChainStepRequest {
  return {
    ...values,
    stepOrder: Number(values.stepOrder),
    approverRoleId: values.approverRoleId || null,
    approverUserId: values.approverUserId || null,
    requiredPermissionCode: values.requiredPermissionCode || "LeaveManagement.Approve",
  };
}
