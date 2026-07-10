import {
  Box,
  Button,
  Card,
  CardContent,
  Checkbox,
  Divider,
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
import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  getPermissions,
  getRole,
  getRolePermissions,
  updateRolePermissions,
} from "../api/adminApi";
import { PageHeader } from "../components/PageHeader";
import { useSaveFeedback } from "../hooks/useSaveFeedback";
import { getRoleLabel } from "../utils/roleLabels";

const groupLabels: Record<string, string> = {
  LeaveRequest: "คำขอลา",
  LeaveApproval: "การอนุมัติวันลา",
  LeaveAdmin: "จัดการระบบลา",
  AdminDashboard: "Admin Dashboard",
  Dashboard: "แดชบอร์ด",
  UserManagement: "จัดการผู้ใช้",
  DepartmentManagement: "จัดการหน่วยงาน",
  RoleManagement: "บทบาทและสิทธิ์",
  SystemSettings: "ตั้งค่าระบบ",
  System: "ระบบ",
  ReportManagement: "รายงาน",
  LeaveAnalytics: "วิเคราะห์ข้อมูลการลา",
};

const permissionDescriptions: Record<string, string> = {
  "LeaveRequest.ViewOwn": "เห็นเฉพาะคำขอลาของตนเอง",
  "LeaveRequest.ViewPendingApproval": "เห็นเฉพาะคำขอที่ถึงคิวตนเองต้องอนุมัติ",
  "LeaveRequest.ViewDepartment": "เห็นคำขอลาภายในหน่วยงานของตนเอง",
  "LeaveRequest.ViewAll": "เห็นคำขอลาทั้งหมด ใช้สำหรับผู้ดูแลระบบ",
  "LeaveRequest.Create": "สร้างและส่งคำขอลาของตนเอง",
  "LeaveRequest.EditOwn": "แก้ไขคำขอลาของตนเองที่ยังแก้ไขได้",
  "LeaveRequest.CancelOwn": "ยกเลิกคำขอลาของตนเอง",
  "LeaveApproval.ApproveCurrentStep": "อนุมัติหรือไม่อนุมัติเฉพาะขั้นตอนปัจจุบันของตนเอง",
  "LeaveApproval.Delegate": "จัดการการมอบหมายผู้อนุมัติ",
  "LeaveApproval.Override": "สิทธิ์ override สำหรับกรณีพิเศษ ยังไม่เปิด flow ใช้งาน",
  "AdminDashboard.View": "ดู Admin Dashboard แบบ Control Center สำหรับผู้ดูแลระบบ",
  "LeaveAdmin.ManageTypes": "จัดการประเภทการลา",
  "LeaveAdmin.ManageBalances": "จัดการยอดวันลาและปรับยอดวันลา",
  "LeaveAdmin.ManageHolidays": "จัดการวันหยุดราชการ",
  "LeaveAdmin.ManageApprovalChains": "จัดการกฎการอนุมัติวันลา",
  "LeaveAnalytics.View": "ดูหน้า Leave Analytics และส่งออกข้อมูลวิเคราะห์การลา",
  "System.Health.View": "ดู Health Center และสถานะระบบสำคัญโดยไม่เปิดเผยข้อมูลลับ",
  "System.Line.TestSend": "ทดสอบส่งข้อความ LINE และตรวจสอบสถานะ LINE Operations Center",
};

export function RolePermissionsPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { showSaveError, showSuccessAndRedirect } = useSaveFeedback();
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [search, setSearch] = useState("");

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
    const keyword = search.trim().toLowerCase();
    const filtered = keyword
      ? permissions.filter((permission) =>
        [permission.code, permission.name, permission.group, permission.action, permissionDescriptions[permission.code] ?? ""]
          .some((value) => value.toLowerCase().includes(keyword)),
      )
      : permissions;

    return filtered.reduce<Record<string, typeof permissions>>((groups, permission) => {
      groups[permission.group] ??= [];
      groups[permission.group].push(permission);
      return groups;
    }, {});
  }, [permissions, search]);

  const saveMutation = useMutation({
    mutationFn: () => updateRolePermissions(id!, selectedIds),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["roles", id, "permissions"] });
      showSuccessAndRedirect({
        successMessage: "บันทึกสิทธิ์การใช้งานเรียบร้อยแล้ว",
        redirectTo: "/admin/roles",
      });
    },
    onError: (error: unknown) => showSaveError(error, "บันทึกสิทธิ์ไม่สำเร็จ"),
  });

  function togglePermission(permissionId: string) {
    setSelectedIds((current) =>
      current.includes(permissionId)
        ? current.filter((idValue) => idValue !== permissionId)
        : [...current, permissionId],
    );
  }

  function selectPermissions(permissionIds: string[]) {
    setSelectedIds((current) => Array.from(new Set([...current, ...permissionIds])));
  }

  function clearPermissions(permissionIds: string[]) {
    setSelectedIds((current) => current.filter((idValue) => !permissionIds.includes(idValue)));
  }

  return (
    <>
      <PageHeader
        title="กำหนดสิทธิ์บทบาท"
        subtitle={`บทบาท: ${getRoleLabel(role?.name)}`}
      />
      <Card>
        <CardContent>
          <Stack spacing={2}>
            <Stack direction={{ xs: "column", md: "row" }} spacing={1.5} alignItems={{ xs: "stretch", md: "center" }}>
              <TextField
                fullWidth
                size="small"
                label="ค้นหาสิทธิ์"
                placeholder="ค้นหาจากชื่อ กลุ่ม หรือ code"
                value={search}
                onChange={(event) => setSearch(event.target.value)}
              />
              <Button variant="outlined" onClick={() => selectPermissions(Object.values(groupedPermissions).flat().map((permission) => permission.id))}>
                เลือกทั้งหมดที่ค้นหา
              </Button>
              <Button variant="outlined" color="secondary" onClick={() => clearPermissions(Object.values(groupedPermissions).flat().map((permission) => permission.id))}>
                ล้างที่ค้นหา
              </Button>
            </Stack>
            {Object.entries(groupedPermissions).map(([group, groupPermissions]) => (
              <Box key={group}>
                <Stack direction={{ xs: "column", sm: "row" }} justifyContent="space-between" spacing={1} sx={{ mb: 1 }}>
                  <Box>
                    <Typography variant="subtitle1" fontWeight={700}>{groupLabels[group] ?? group}</Typography>
                    <Typography variant="body2" color="text.secondary">{group}</Typography>
                  </Box>
                  <Stack direction="row" spacing={1}>
                    <Button size="small" onClick={() => selectPermissions(groupPermissions.map((permission) => permission.id))}>เลือกทั้งกลุ่ม</Button>
                    <Button size="small" color="secondary" onClick={() => clearPermissions(groupPermissions.map((permission) => permission.id))}>ล้างทั้งกลุ่ม</Button>
                  </Stack>
                </Stack>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell width={56}>เลือก</TableCell>
                      <TableCell>สิทธิ์</TableCell>
                      <TableCell>คำอธิบาย</TableCell>
                      <TableCell>Code</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {groupPermissions.map((permission) => (
                      <TableRow key={permission.id}>
                        <TableCell>
                          <Checkbox
                            checked={selectedIds.includes(permission.id)}
                            onChange={() => togglePermission(permission.id)}
                          />
                        </TableCell>
                        <TableCell>{permission.name}</TableCell>
                        <TableCell>{permissionDescriptions[permission.code] ?? `${permission.group} ${permission.action}`}</TableCell>
                        <TableCell><Box component="code">{permission.code}</Box></TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
                <Divider sx={{ my: 2 }} />
              </Box>
            ))}
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
