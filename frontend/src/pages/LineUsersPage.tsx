import SendOutlinedIcon from "@mui/icons-material/SendOutlined";
import { Avatar, Box, Button, Card, CardContent, Chip, MenuItem, Pagination, Stack, Table, TableBody, TableCell, TableHead, TableRow, TextField, Typography } from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { getLineUserStats, getLineUsers, sendLineUserTestMessage } from "../api/adminApi";
import { PageHeader } from "../components/PageHeader";
import { useNotification } from "../hooks/useNotification";
import { brandColors } from "../theme/theme";

const statusLabels: Record<string, string> = {
  Pending: "รอผูกบัญชี",
  Bound: "เชื่อมต่อแล้ว",
  Unbound: "ยกเลิกการเชื่อมต่อ",
};

const statusColors: Record<string, "default" | "success" | "warning"> = {
  Pending: "warning",
  Bound: "success",
  Unbound: "default",
};

export function LineUsersPage() {
  const queryClient = useQueryClient();
  const { showError, showSuccess } = useNotification();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [status, setStatus] = useState("");
  const [search, setSearch] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");

  const query = useQuery({
    queryKey: ["admin", "line-users", page, pageSize, status, appliedSearch],
    queryFn: () => getLineUsers({ page, pageSize, status: status || undefined, search: appliedSearch || undefined }),
  });
  const statsQuery = useQuery({
    queryKey: ["admin", "line-users", "stats"],
    queryFn: getLineUserStats,
  });

  const testMutation = useMutation({
    mutationFn: (id: string) => sendLineUserTestMessage(id),
    onSuccess: () => showSuccess("ส่งข้อความทดสอบ LINE สำเร็จ"),
    onError: () => showError("ส่งข้อความทดสอบ LINE ไม่สำเร็จ"),
  });

  function applyFilters() {
    setPage(1);
    setAppliedSearch(search.trim());
  }

  function clearFilters() {
    setPage(1);
    setStatus("");
    setSearch("");
    setAppliedSearch("");
  }

  return (
    <>
      <PageHeader title="ผู้ใช้ LINE" subtitle="ตรวจสอบการเชื่อมต่อ LINE OA กับบัญชี HOP" />
      <Stack spacing={2}>
        <Box
          sx={{
            display: "grid",
            gap: 2,
            gridTemplateColumns: { xs: "1fr", md: "repeat(3, minmax(0, 1fr))" },
          }}
        >
          <SummaryCard label="รหัสเชื่อมต่อที่รอใช้งาน" value={statsQuery.data?.pendingConnectTokenCount ?? 0} />
          <SummaryCard label="รหัสที่หมดอายุ" value={statsQuery.data?.expiredConnectTokenCount ?? 0} />
          <SummaryCard label="เชื่อมต่อใหม่ใน 7 วัน" value={statsQuery.data?.recentlyBoundUserCount ?? 0} />
        </Box>

        <Card>
          <CardContent>
            <Stack direction={{ xs: "column", md: "row" }} spacing={1.5} alignItems={{ xs: "stretch", md: "center" }}>
              <TextField
                size="small"
                label="ค้นหา"
                placeholder="LINE User ID, Display Name, ชื่อผู้ใช้"
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                sx={{ flex: 1 }}
              />
              <TextField
                select
                size="small"
                label="สถานะ"
                value={status}
                onChange={(event) => {
                  setStatus(event.target.value);
                  setPage(1);
                }}
                sx={{ minWidth: 180 }}
              >
                <MenuItem value="">ทั้งหมด</MenuItem>
                <MenuItem value="Pending">รอผูกบัญชี</MenuItem>
                <MenuItem value="Bound">เชื่อมต่อแล้ว</MenuItem>
                <MenuItem value="Unbound">ยกเลิกการเชื่อมต่อ</MenuItem>
              </TextField>
              <Button variant="contained" onClick={applyFilters}>ค้นหา</Button>
              <Button variant="outlined" onClick={clearFilters}>ล้างตัวกรอง</Button>
            </Stack>
          </CardContent>
        </Card>

        <Card>
          <CardContent>
            <Box sx={{ overflowX: "auto" }}>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>LINE User</TableCell>
                    <TableCell>บัญชี HOP</TableCell>
                    <TableCell>สถานะ</TableCell>
                    <TableCell>เหตุการณ์ล่าสุด</TableCell>
                    <TableCell>วันที่เชื่อมต่อ</TableCell>
                    <TableCell align="right">จัดการ</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {query.data?.items.map((item) => (
                    <TableRow key={item.id} hover>
                      <TableCell>
                        <Stack direction="row" spacing={1.5} alignItems="center">
                          <Avatar src={item.pictureUrl ?? undefined} sx={{ bgcolor: brandColors.accent, color: brandColors.primaryDark }}>
                            {(item.displayName ?? "L").slice(0, 1)}
                          </Avatar>
                          <Stack spacing={0.25}>
                            <Typography fontWeight={700}>{item.displayName ?? "-"}</Typography>
                            <Typography variant="caption" color="text.secondary">{item.lineUserIdMasked}</Typography>
                          </Stack>
                        </Stack>
                      </TableCell>
                      <TableCell>
                        <Typography>{item.fullname ?? "-"}</Typography>
                        {item.username && <Typography variant="caption" color="text.secondary">{item.username}</Typography>}
                      </TableCell>
                      <TableCell>
                        <Chip size="small" color={statusColors[item.status] ?? "default"} label={statusLabels[item.status] ?? item.status} />
                      </TableCell>
                      <TableCell>
                        <Typography>{item.lastEventType ?? "-"}</Typography>
                        <Typography variant="caption" color="text.secondary">{formatDateTime(item.lastEventAt)}</Typography>
                      </TableCell>
                      <TableCell>{formatDateTime(item.boundAt)}</TableCell>
                      <TableCell align="right">
                        <Button
                          size="small"
                          variant="outlined"
                          startIcon={<SendOutlinedIcon />}
                          disabled={testMutation.isPending}
                          onClick={() => testMutation.mutate(item.id)}
                        >
                          ส่งทดสอบ
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                  {!query.isLoading && query.data?.items.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={6}>
                        <Box sx={{ py: 4, textAlign: "center" }}>
                          <Typography color="text.secondary">ยังไม่มีข้อมูลผู้ใช้ LINE</Typography>
                        </Box>
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </Box>

            <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5} justifyContent="space-between" alignItems={{ xs: "stretch", sm: "center" }} sx={{ mt: 2 }}>
              <Typography variant="body2" color="text.secondary">
                ทั้งหมด {query.data?.totalItems ?? 0} รายการ
              </Typography>
              <Stack direction="row" spacing={1} alignItems="center">
                <TextField
                  select
                  size="small"
                  label="ต่อหน้า"
                  value={pageSize}
                  onChange={(event) => {
                    setPageSize(Number(event.target.value));
                    setPage(1);
                  }}
                  sx={{ width: 100 }}
                >
                  {[10, 20, 50].map((value) => <MenuItem key={value} value={value}>{value}</MenuItem>)}
                </TextField>
                <Pagination count={query.data?.totalPages ?? 1} page={page} onChange={(_, value) => setPage(value)} color="primary" />
              </Stack>
            </Stack>
          </CardContent>
        </Card>
      </Stack>
    </>
  );
}

function SummaryCard({ label, value }: { label: string; value: number }) {
  return (
    <Card sx={{ borderTop: `4px solid ${brandColors.accent}` }}>
      <CardContent>
        <Typography variant="body2" color="text.secondary">{label}</Typography>
        <Typography variant="h4" color="primary.main" fontWeight={800}>{value}</Typography>
      </CardContent>
    </Card>
  );
}

function formatDateTime(value?: string | null) {
  if (!value) {
    return "-";
  }

  return new Intl.DateTimeFormat("th-TH", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }).format(new Date(value));
}
