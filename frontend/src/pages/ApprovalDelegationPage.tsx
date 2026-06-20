import AddOutlinedIcon from "@mui/icons-material/AddOutlined";
import BlockOutlinedIcon from "@mui/icons-material/BlockOutlined";
import PlayArrowOutlinedIcon from "@mui/icons-material/PlayArrowOutlined";
import { Alert, Button, Card, CardContent, Chip, Grid, MenuItem, Stack, Table, TableBody, TableCell, TableHead, TableRow, TextField, Typography } from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
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
import { ActionTooltip } from "../components/common/ActionTooltip";
import { AppDatePicker } from "../components/common/AppDatePicker";
import { PageHeader } from "../components/PageHeader";
import { PermissionGuard } from "../context/PermissionContext";
import { formatThaiDate } from "../utils/dateFormat";
import { getRoleLabel } from "../utils/roleLabels";

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
              <Grid container spacing={1.5} alignItems="center">
                <Grid item xs={12} md={3}>
                  <TextField fullWidth size="small" select label="ผู้อนุมัติหลัก" value={delegation.approverUserId} onChange={(event) => setDelegation({ ...delegation, approverUserId: event.target.value })}>
                    {users.map((user) => <MenuItem key={user.id} value={user.id}>{user.fullname}</MenuItem>)}
                  </TextField>
                </Grid>
                <Grid item xs={12} md={3}>
                  <TextField fullWidth size="small" select label="ผู้รับมอบหมาย" value={delegation.delegateUserId} onChange={(event) => setDelegation({ ...delegation, delegateUserId: event.target.value })}>
                    {users.map((user) => <MenuItem key={user.id} value={user.id}>{user.fullname}</MenuItem>)}
                  </TextField>
                </Grid>
                <Grid item xs={12} sm={6} md={2}>
                  <AppDatePicker label="วันที่เริ่ม" value={delegation.startDate} onChange={(value) => setDelegation({ ...delegation, startDate: value })} />
                </Grid>
                <Grid item xs={12} sm={6} md={2}>
                  <AppDatePicker label="วันที่สิ้นสุด" value={delegation.endDate} onChange={(value) => setDelegation({ ...delegation, endDate: value })} />
                </Grid>
                <Grid item xs={12} md={2}>
                  <TextField fullWidth size="small" label="เหตุผล" InputLabelProps={{ shrink: true }} value={delegation.reason} onChange={(event) => setDelegation({ ...delegation, reason: event.target.value })} />
                </Grid>
                <Grid item xs={12}>
                  <ActionTooltip title="บันทึกการมอบหมายอนุมัติ">
                    <Button variant="contained" startIcon={<AddOutlinedIcon />} onClick={() => createDelegationMutation.mutate(delegation)} disabled={!delegation.approverUserId || !delegation.delegateUserId || createDelegationMutation.isPending}>
                      บันทึกการมอบหมาย
                    </Button>
                  </ActionTooltip>
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </PermissionGuard>
        <Card>
          <CardContent>
            <Typography variant="h6" sx={{ mb: 2 }}>รายการมอบหมาย</Typography>
            <Table size="small">
              <TableHead><TableRow><TableCell>ผู้อนุมัติ</TableCell><TableCell>ผู้รับมอบหมาย</TableCell><TableCell>ช่วงวันที่</TableCell><TableCell>สถานะ</TableCell><TableCell align="right">จัดการ</TableCell></TableRow></TableHead>
              <TableBody>
                {delegations.length ? delegations.map((item) => (
                  <TableRow key={item.id}>
                    <TableCell>{item.approverName ?? "-"}</TableCell>
                    <TableCell>{item.delegateName ?? "-"}</TableCell>
                    <TableCell>{formatThaiDate(item.startDate)} - {formatThaiDate(item.endDate)}</TableCell>
                    <TableCell><Chip size="small" label={item.isActive ? "เปิดใช้งาน" : "ปิดใช้งาน"} color={item.isActive ? "success" : "default"} /></TableCell>
                    <TableCell align="right">
                      <PermissionGuard permission="ApprovalDelegation.Delete" fallback="-">
                        <ActionTooltip title="ปิดใช้งานการมอบหมายนี้">
                          <Button size="small" color="warning" variant="outlined" startIcon={<BlockOutlinedIcon />} onClick={() => deactivateDelegationMutation.mutate(item.id)} disabled={!item.isActive || deactivateDelegationMutation.isPending}>ปิดใช้งาน</Button>
                        </ActionTooltip>
                      </PermissionGuard>
                    </TableCell>
                  </TableRow>
                )) : (
                  <TableRow><TableCell colSpan={5}>ยังไม่มีรายการมอบหมายอนุมัติ</TableCell></TableRow>
                )}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
        <Card>
          <CardContent>
            <Typography variant="h6" sx={{ mb: 2 }}>กติกา Escalation</Typography>
            <PermissionGuard permission="ApprovalDelegation.Manage">
              <Stack direction={{ xs: "column", md: "row" }} spacing={1.5} alignItems={{ md: "center" }}>
                <TextField size="small" label="ชื่อกติกา" InputLabelProps={{ shrink: true }} value={rule.name} onChange={(event) => setRule({ ...rule, name: event.target.value })} />
                <TextField size="small" type="number" label="ค้างเกินกี่ชั่วโมง" InputLabelProps={{ shrink: true }} value={rule.escalateAfterHours} onChange={(event) => setRule({ ...rule, escalateAfterHours: Number(event.target.value) })} />
                <TextField size="small" select label="ส่งต่อไปยังบทบาท" value={rule.escalateToRoleId} onChange={(event) => setRule({ ...rule, escalateToRoleId: event.target.value })}>
                  {roles.map((role) => <MenuItem key={role.id} value={role.id}>{getRoleLabel(role.name)}</MenuItem>)}
                </TextField>
                <Button variant="contained" startIcon={<AddOutlinedIcon />} onClick={() => createRuleMutation.mutate({ ...rule, escalateToRoleId: rule.escalateToRoleId || null })} disabled={!rule.name || createRuleMutation.isPending}>
                  เพิ่มกติกา
                </Button>
                <ActionTooltip title="ตรวจคำขอค้างอนุมัติและส่งต่อทันที">
                  <Button variant="outlined" startIcon={<PlayArrowOutlinedIcon />} onClick={() => runEscalationMutation.mutate()} disabled={runEscalationMutation.isPending}>รัน Escalation ตอนนี้</Button>
                </ActionTooltip>
              </Stack>
            </PermissionGuard>
            <Table size="small" sx={{ mt: 2 }}>
              <TableHead><TableRow><TableCell>ชื่อ</TableCell><TableCell>ชั่วโมง</TableCell><TableCell>ส่งต่อ</TableCell><TableCell>สถานะ</TableCell></TableRow></TableHead>
              <TableBody>
                {rules.length ? rules.map((item) => (
                  <TableRow key={item.id}>
                    <TableCell>{item.name}</TableCell>
                    <TableCell>{item.escalateAfterHours}</TableCell>
                    <TableCell>{item.escalateToUserName ?? getRoleLabel(item.escalateToRoleName)}</TableCell>
                    <TableCell><Chip size="small" label={item.isActive ? "เปิดใช้งาน" : "ปิดใช้งาน"} color={item.isActive ? "success" : "default"} /></TableCell>
                  </TableRow>
                )) : (
                  <TableRow><TableCell colSpan={4}>ยังไม่มีกติกา Escalation</TableCell></TableRow>
                )}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      </Stack>
    </>
  );
}
