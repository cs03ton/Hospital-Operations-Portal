import { Alert, Button, Card, CardContent, MenuItem, Stack, Table, TableBody, TableCell, TableHead, TableRow, TextField, Typography } from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import dayjs from "dayjs";
import { useState } from "react";
import { getRoles, getUsers } from "../api/adminApi";
import {
  createApprovalDelegation,
  createApprovalEscalationRule,
  deactivateApprovalDelegation,
  getApprovalDelegations,
  getApprovalEscalationRules,
  runApprovalEscalation,
} from "../api/leaveApi";
import { PageHeader } from "../components/PageHeader";
import { PermissionGuard } from "../context/PermissionContext";

export function ApprovalDelegationPage() {
  const queryClient = useQueryClient();
  const { data: users = [] } = useQuery({ queryKey: ["users"], queryFn: getUsers });
  const { data: roles = [] } = useQuery({ queryKey: ["roles"], queryFn: getRoles });
  const { data: delegations = [] } = useQuery({ queryKey: ["approval-delegations"], queryFn: getApprovalDelegations });
  const { data: rules = [] } = useQuery({ queryKey: ["approval-escalation-rules"], queryFn: getApprovalEscalationRules });
  const [delegation, setDelegation] = useState({ approverUserId: "", delegateUserId: "", startDate: "", endDate: "", reason: "", isActive: true });
  const [rule, setRule] = useState({ name: "", escalateAfterHours: 24, escalateToRoleId: "", isActive: true });

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["approval-delegations"] });
    queryClient.invalidateQueries({ queryKey: ["approval-escalation-rules"] });
  };
  const createDelegationMutation = useMutation({ mutationFn: createApprovalDelegation, onSuccess: invalidate });
  const createRuleMutation = useMutation({ mutationFn: createApprovalEscalationRule, onSuccess: invalidate });
  const deactivateDelegationMutation = useMutation({ mutationFn: deactivateApprovalDelegation, onSuccess: invalidate });
  const runEscalationMutation = useMutation({ mutationFn: runApprovalEscalation, onSuccess: invalidate });

  return (
    <>
      <PageHeader title="มอบหมายและ Escalation งานอนุมัติ" subtitle="ตั้งผู้รับมอบหมายและกติกาการส่งต่อคำขอที่ค้างอนุมัติ" />
      {(createDelegationMutation.isError || createRuleMutation.isError || deactivateDelegationMutation.isError || runEscalationMutation.isError) && <Alert severity="error" sx={{ mb: 2 }}>ดำเนินการไม่สำเร็จ กรุณาตรวจสอบสิทธิ์หรือข้อมูลอีกครั้ง</Alert>}
      <Stack spacing={2}>
        <PermissionGuard permission="ApprovalDelegation.Create">
          <Card>
            <CardContent>
              <Typography variant="h6" sx={{ mb: 2 }}>เพิ่มการมอบหมายอนุมัติ</Typography>
              <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
                <TextField select label="ผู้อนุมัติหลัก" value={delegation.approverUserId} onChange={(event) => setDelegation({ ...delegation, approverUserId: event.target.value })}>
                  {users.map((user) => <MenuItem key={user.id} value={user.id}>{user.fullname}</MenuItem>)}
                </TextField>
                <TextField select label="ผู้รับมอบหมาย" value={delegation.delegateUserId} onChange={(event) => setDelegation({ ...delegation, delegateUserId: event.target.value })}>
                  {users.map((user) => <MenuItem key={user.id} value={user.id}>{user.fullname}</MenuItem>)}
                </TextField>
                <TextField type="date" label="วันที่เริ่ม" InputLabelProps={{ shrink: true }} value={delegation.startDate} onChange={(event) => setDelegation({ ...delegation, startDate: event.target.value })} />
                <TextField type="date" label="วันที่สิ้นสุด" InputLabelProps={{ shrink: true }} value={delegation.endDate} onChange={(event) => setDelegation({ ...delegation, endDate: event.target.value })} />
                <TextField label="เหตุผล" value={delegation.reason} onChange={(event) => setDelegation({ ...delegation, reason: event.target.value })} />
                <Button variant="contained" onClick={() => createDelegationMutation.mutate(delegation)} disabled={!delegation.approverUserId || !delegation.delegateUserId || createDelegationMutation.isPending}>
                  บันทึก
                </Button>
              </Stack>
            </CardContent>
          </Card>
        </PermissionGuard>
        <Card>
          <CardContent>
            <Typography variant="h6" sx={{ mb: 2 }}>รายการมอบหมาย</Typography>
            <Table size="small">
              <TableHead><TableRow><TableCell>ผู้อนุมัติ</TableCell><TableCell>ผู้รับมอบหมาย</TableCell><TableCell>ช่วงวันที่</TableCell><TableCell>สถานะ</TableCell><TableCell align="right">จัดการ</TableCell></TableRow></TableHead>
              <TableBody>
                {delegations.map((item) => (
                  <TableRow key={item.id}>
                    <TableCell>{item.approverName ?? "-"}</TableCell>
                    <TableCell>{item.delegateName ?? "-"}</TableCell>
                    <TableCell>{dayjs(item.startDate).format("DD/MM/YYYY")} - {dayjs(item.endDate).format("DD/MM/YYYY")}</TableCell>
                    <TableCell>{item.isActive ? "เปิดใช้งาน" : "ปิดใช้งาน"}</TableCell>
                    <TableCell align="right">
                      <PermissionGuard permission="ApprovalDelegation.Delete" fallback="-">
                        <Button color="warning" onClick={() => deactivateDelegationMutation.mutate(item.id)} disabled={!item.isActive || deactivateDelegationMutation.isPending}>ปิดใช้งาน</Button>
                      </PermissionGuard>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
        <Card>
          <CardContent>
            <Typography variant="h6" sx={{ mb: 2 }}>กติกา Escalation</Typography>
            <PermissionGuard permission="ApprovalDelegation.Manage">
              <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
                <TextField label="ชื่อกติกา" value={rule.name} onChange={(event) => setRule({ ...rule, name: event.target.value })} />
                <TextField type="number" label="ค้างเกินกี่ชั่วโมง" value={rule.escalateAfterHours} onChange={(event) => setRule({ ...rule, escalateAfterHours: Number(event.target.value) })} />
                <TextField select label="ส่งต่อไปยังบทบาท" value={rule.escalateToRoleId} onChange={(event) => setRule({ ...rule, escalateToRoleId: event.target.value })}>
                  {roles.map((role) => <MenuItem key={role.id} value={role.id}>{role.name}</MenuItem>)}
                </TextField>
                <Button variant="contained" onClick={() => createRuleMutation.mutate({ ...rule, escalateToRoleId: rule.escalateToRoleId || null })} disabled={!rule.name || createRuleMutation.isPending}>
                  เพิ่มกติกา
                </Button>
                <Button variant="outlined" onClick={() => runEscalationMutation.mutate()} disabled={runEscalationMutation.isPending}>รัน Escalation ตอนนี้</Button>
              </Stack>
            </PermissionGuard>
            <Table size="small" sx={{ mt: 2 }}>
              <TableHead><TableRow><TableCell>ชื่อ</TableCell><TableCell>ชั่วโมง</TableCell><TableCell>ส่งต่อ</TableCell><TableCell>สถานะ</TableCell></TableRow></TableHead>
              <TableBody>{rules.map((item) => <TableRow key={item.id}><TableCell>{item.name}</TableCell><TableCell>{item.escalateAfterHours}</TableCell><TableCell>{item.escalateToRoleName ?? item.escalateToUserName ?? "-"}</TableCell><TableCell>{item.isActive ? "เปิดใช้งาน" : "ปิดใช้งาน"}</TableCell></TableRow>)}</TableBody>
            </Table>
          </CardContent>
        </Card>
      </Stack>
    </>
  );
}
