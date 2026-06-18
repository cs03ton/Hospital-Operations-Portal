import BlockOutlinedIcon from "@mui/icons-material/BlockOutlined";
import { Card, CardContent, Chip, IconButton, Table, TableBody, TableCell, TableHead, TableRow } from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import dayjs from "dayjs";
import { getSessions, revokeSession } from "../api/securityApi";
import { PageHeader } from "../components/PageHeader";

export function SessionManagementPage() {
  const queryClient = useQueryClient();
  const { data = [], isLoading } = useQuery({ queryKey: ["sessions"], queryFn: getSessions });
  const revokeMutation = useMutation({
    mutationFn: revokeSession,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["sessions"] }),
  });

  return (
    <>
      <PageHeader title="จัดการเซสชัน" subtitle="ตรวจสอบและยกเลิก refresh token session ของผู้ใช้งาน" />
      <Card>
        <CardContent>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>ผู้ใช้งาน</TableCell>
                <TableCell>สร้างเมื่อ</TableCell>
                <TableCell>หมดอายุ</TableCell>
                <TableCell>IP</TableCell>
                <TableCell>สถานะ</TableCell>
                <TableCell>เหตุผล</TableCell>
                <TableCell align="right">จัดการ</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {isLoading ? (
                <TableRow><TableCell colSpan={7}>กำลังโหลดเซสชัน...</TableCell></TableRow>
              ) : data.map((item) => (
                <TableRow key={item.id}>
                  <TableCell>{item.fullname ?? item.username ?? "-"}</TableCell>
                  <TableCell>{dayjs(item.createdAt).format("DD/MM/YYYY HH:mm")}</TableCell>
                  <TableCell>{dayjs(item.expiresAt).format("DD/MM/YYYY HH:mm")}</TableCell>
                  <TableCell>{item.createdByIp ?? "-"}</TableCell>
                  <TableCell><Chip size="small" color={item.isActive ? "success" : "default"} label={item.isActive ? "ใช้งาน" : "ถูกยกเลิก"} /></TableCell>
                  <TableCell>{item.revokedReason ?? "-"}</TableCell>
                  <TableCell align="right">
                    <IconButton aria-label="ยกเลิกเซสชัน" disabled={!item.isActive || revokeMutation.isPending} onClick={() => revokeMutation.mutate(item.id)}>
                      <BlockOutlinedIcon />
                    </IconButton>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </>
  );
}
