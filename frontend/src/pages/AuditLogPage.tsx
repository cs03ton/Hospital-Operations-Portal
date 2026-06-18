import SearchOutlinedIcon from "@mui/icons-material/SearchOutlined";
import {
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Dialog,
  DialogContent,
  DialogTitle,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TablePagination,
  TableRow,
  TextField,
  Typography,
} from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import dayjs from "dayjs";
import { useMemo, useState } from "react";
import { getAuditLogs, type AuditLogSummary } from "../api/adminApi";
import { PageHeader } from "../components/PageHeader";

const resultLabels: Record<string, { label: string; color: "success" | "error" | "warning" | "default" }> = {
  Success: { label: "สำเร็จ", color: "success" },
  Failed: { label: "ไม่สำเร็จ", color: "error" },
  Denied: { label: "ถูกปฏิเสธ", color: "warning" },
};

export function AuditLogPage() {
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(20);
  const [search, setSearch] = useState("");
  const [action, setAction] = useState("");
  const [selectedLog, setSelectedLog] = useState<AuditLogSummary | null>(null);

  const queryParams = useMemo(
    () => ({
      page: page + 1,
      pageSize,
      search: search || undefined,
      action: action || undefined,
    }),
    [action, page, pageSize, search],
  );

  const { data, isLoading } = useQuery({
    queryKey: ["audit-logs", queryParams],
    queryFn: () => getAuditLogs(queryParams),
  });

  return (
    <>
      <PageHeader title="บันทึกการใช้งาน" subtitle="ตรวจสอบประวัติการเข้าสู่ระบบ การเปลี่ยนแปลงข้อมูล และการถูกปฏิเสธสิทธิ์" />
      <Stack spacing={2}>
        <Card>
          <CardContent>
            <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
              <TextField
                label="ค้นหา"
                value={search}
                onChange={(event) => {
                  setSearch(event.target.value);
                  setPage(0);
                }}
                InputProps={{ startAdornment: <SearchOutlinedIcon color="action" sx={{ mr: 1 }} /> }}
                fullWidth
              />
              <TextField
                label="การกระทำ"
                value={action}
                onChange={(event) => {
                  setAction(event.target.value);
                  setPage(0);
                }}
                sx={{ minWidth: { md: 260 } }}
              />
            </Stack>
          </CardContent>
        </Card>

        <Card>
          <CardContent>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>วันที่</TableCell>
                  <TableCell>ผู้ใช้งาน</TableCell>
                  <TableCell>การกระทำ</TableCell>
                  <TableCell>ทรัพยากร</TableCell>
                  <TableCell>IP Address</TableCell>
                  <TableCell>ผลลัพธ์</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {isLoading ? (
                  <TableRow>
                    <TableCell colSpan={6}>กำลังโหลดบันทึกการใช้งาน...</TableCell>
                  </TableRow>
                ) : data?.items.length ? (
                  data.items.map((log) => {
                    const result = resultLabels[log.result] ?? { label: log.result, color: "default" as const };

                    return (
                      <TableRow
                        key={log.id}
                        hover
                        onClick={() => setSelectedLog(log)}
                        sx={{ cursor: "pointer" }}
                      >
                        <TableCell>{dayjs(log.timestamp).format("DD/MM/YYYY HH:mm")}</TableCell>
                        <TableCell>{log.fullname ?? log.username ?? "-"}</TableCell>
                        <TableCell>{log.action}</TableCell>
                        <TableCell>{log.resource}</TableCell>
                        <TableCell>{log.ipAddress ?? "-"}</TableCell>
                        <TableCell>
                          <Chip size="small" color={result.color} label={result.label} />
                        </TableCell>
                      </TableRow>
                    );
                  })
                ) : (
                  <TableRow>
                    <TableCell colSpan={6}>ไม่พบบันทึกการใช้งาน</TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
            <TablePagination
              component="div"
              count={data?.totalItems ?? 0}
              page={page}
              onPageChange={(_, nextPage) => setPage(nextPage)}
              rowsPerPage={pageSize}
              onRowsPerPageChange={(event) => {
                setPageSize(Number(event.target.value));
                setPage(0);
              }}
              rowsPerPageOptions={[10, 20, 50, 100]}
              labelRowsPerPage="จำนวนแถวต่อหน้า"
              labelDisplayedRows={({ from, to, count }) =>
                `${from}-${to} จาก ${count !== -1 ? count : `มากกว่า ${to}`}`
              }
              getItemAriaLabel={(type) => {
                if (type === "first") return "ไปหน้าแรก";
                if (type === "last") return "ไปหน้าสุดท้าย";
                if (type === "next") return "ไปหน้าถัดไป";
                return "ไปหน้าก่อนหน้า";
              }}
            />
          </CardContent>
        </Card>
      </Stack>

      <Dialog open={Boolean(selectedLog)} onClose={() => setSelectedLog(null)} fullWidth maxWidth="sm">
        <DialogTitle>รายละเอียดบันทึกการใช้งาน</DialogTitle>
        <DialogContent>
          {selectedLog && (
            <Stack spacing={1.5} sx={{ pt: 1 }}>
              <Detail label="วันที่" value={dayjs(selectedLog.timestamp).format("DD/MM/YYYY HH:mm:ss")} />
              <Detail label="ผู้ใช้งาน" value={selectedLog.fullname ?? selectedLog.username ?? "-"} />
              <Detail label="การกระทำ" value={selectedLog.action} />
              <Detail label="ทรัพยากร" value={selectedLog.resource} />
              <Detail label="Resource ID" value={selectedLog.resourceId ?? "-"} />
              <Detail label="IP Address" value={selectedLog.ipAddress ?? "-"} />
              <Detail label="ผลลัพธ์" value={resultLabels[selectedLog.result]?.label ?? selectedLog.result} />
              <Detail label="รายละเอียด" value={selectedLog.detail ?? "-"} />
              <Box sx={{ display: "flex", justifyContent: "flex-end", pt: 1 }}>
                <Button variant="contained" onClick={() => setSelectedLog(null)}>
                  ปิด
                </Button>
              </Box>
            </Stack>
          )}
        </DialogContent>
      </Dialog>
    </>
  );
}

function Detail({ label, value }: { label: string; value: string }) {
  return (
    <Box>
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Typography>{value}</Typography>
    </Box>
  );
}
