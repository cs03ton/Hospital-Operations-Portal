import { Card, CardContent, Table, TableBody, TableCell, TableHead, TableRow } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { getMyLeaveBalances } from "../api/leaveApi";
import { PageHeader } from "../components/PageHeader";
import { getLeaveTypeLabel } from "../utils/leaveLabels";

export function LeaveBalancePage() {
  const { data = [], isLoading } = useQuery({ queryKey: ["leave-balances", "me"], queryFn: getMyLeaveBalances });

  return (
    <>
      <PageHeader title="วันลาคงเหลือของฉัน" subtitle="ตรวจสอบสิทธิ์วันลา ใช้ไป รออนุมัติ และคงเหลือ" />
      <Card>
        <CardContent>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>ประเภทลา</TableCell>
                <TableCell>ปี</TableCell>
                <TableCell>สิทธิ์ทั้งหมด</TableCell>
                <TableCell>ใช้ไป</TableCell>
                <TableCell>รออนุมัติ</TableCell>
                <TableCell>คงเหลือ</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {isLoading ? (
                <TableRow><TableCell colSpan={6}>กำลังโหลดข้อมูลวันลา...</TableCell></TableRow>
              ) : data.map((item) => (
                <TableRow key={item.leaveTypeId}>
                  <TableCell>{getLeaveTypeLabel(item.leaveTypeName)}</TableCell>
                  <TableCell>{item.year}</TableCell>
                  <TableCell>{item.entitledDays}</TableCell>
                  <TableCell>{item.usedDays}</TableCell>
                  <TableCell>{item.pendingDays}</TableCell>
                  <TableCell>{item.remainingDays}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </>
  );
}
