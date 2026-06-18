import AddOutlinedIcon from "@mui/icons-material/AddOutlined";
import VisibilityOutlinedIcon from "@mui/icons-material/VisibilityOutlined";
import { Button, Card, CardContent, Chip, IconButton, Stack, Table, TableBody, TableCell, TableHead, TableRow } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import dayjs from "dayjs";
import { Link as RouterLink } from "react-router-dom";
import { getLeaveRequests } from "../api/leaveApi";
import { PageHeader } from "../components/PageHeader";
import { PermissionGuard } from "../context/PermissionContext";

const statusLabels: Record<string, { label: string; color: "default" | "info" | "warning" | "success" | "error" }> = {
  Draft: { label: "แบบร่าง", color: "default" },
  Pending: { label: "รออนุมัติ", color: "warning" },
  Approved: { label: "อนุมัติแล้ว", color: "success" },
  Rejected: { label: "ไม่อนุมัติ", color: "error" },
  Cancelled: { label: "ยกเลิก", color: "default" },
};

export function LeaveManagementPage() {
  const { data = [], isLoading } = useQuery({ queryKey: ["leave-requests"], queryFn: getLeaveRequests });

  return (
    <>
      <Stack direction={{ xs: "column", sm: "row" }} justifyContent="space-between" spacing={2}>
        <PageHeader title="รายการคำขอลา" subtitle="สร้างคำขอลา ติดตามสถานะ และดำเนินการอนุมัติ" />
        <PermissionGuard permission="LeaveManagement.Create">
          <Button component={RouterLink} to="/leave/create" variant="contained" startIcon={<AddOutlinedIcon />}>
            สร้างคำขอลา
          </Button>
        </PermissionGuard>
      </Stack>
      <Card>
        <CardContent>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>ผู้ขอ</TableCell>
                <TableCell>ประเภทลา</TableCell>
                <TableCell>วันที่ลา</TableCell>
                <TableCell>จำนวนวัน</TableCell>
                <TableCell>สถานะ</TableCell>
                <TableCell align="right">จัดการ</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {isLoading ? (
                <TableRow><TableCell colSpan={6}>กำลังโหลดคำขอลา...</TableCell></TableRow>
              ) : data.length ? (
                data.map((item) => {
                  const status = statusLabels[item.status] ?? { label: item.status, color: "default" as const };
                  return (
                    <TableRow key={item.id}>
                      <TableCell>{item.fullname ?? "-"}</TableCell>
                      <TableCell>{item.leaveTypeName ?? "-"}</TableCell>
                      <TableCell>{dayjs(item.startDate).format("DD/MM/YYYY")} - {dayjs(item.endDate).format("DD/MM/YYYY")}</TableCell>
                      <TableCell>{item.totalDays}</TableCell>
                      <TableCell><Chip size="small" label={status.label} color={status.color} /></TableCell>
                      <TableCell align="right">
                        <IconButton component={RouterLink} to={`/leave/${item.id}`} aria-label="ดูรายละเอียดคำขอลา">
                          <VisibilityOutlinedIcon />
                        </IconButton>
                      </TableCell>
                    </TableRow>
                  );
                })
              ) : (
                <TableRow><TableCell colSpan={6}>ไม่พบคำขอลา</TableCell></TableRow>
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </>
  );
}
