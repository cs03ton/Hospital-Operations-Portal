import SearchOutlinedIcon from "@mui/icons-material/SearchOutlined";
import {
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Grid,
  IconButton,
  InputAdornment,
  MenuItem,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material";
import MarkEmailReadOutlinedIcon from "@mui/icons-material/MarkEmailReadOutlined";
import OpenInNewOutlinedIcon from "@mui/icons-material/OpenInNewOutlined";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { getNotificationCenterItems, markNotificationRead } from "../services/notificationService";
import { StatusBadge } from "../components/common/StatusBadge";
import { formatThaiDateTime } from "../utils/dateFormat";

const filterOptions = [
  { value: "", label: "ทั้งหมด" },
  { value: "action-required", label: "ต้องดำเนินการ" },
  { value: "unread", label: "ยังไม่ได้อ่าน" },
  { value: "read", label: "อ่านแล้ว" },
];

const categoryOptions = [
  { value: "", label: "ทุกระบบ" },
  { value: "Leave", label: "ระบบลา" },
  { value: "User", label: "ระบบผู้ใช้" },
  { value: "Notification", label: "ระบบแจ้งเตือน" },
  { value: "Backup", label: "ระบบสำรองข้อมูล" },
  { value: "System", label: "ระบบ" },
];

export function NotificationCenterPage() {
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(20);
  const [filter, setFilter] = useState("");
  const [category, setCategory] = useState("");
  const [search, setSearch] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: ["notifications", "center", page, pageSize, filter, category, appliedSearch],
    queryFn: () =>
      getNotificationCenterItems({
        page: page + 1,
        pageSize,
        filter: filter || undefined,
        category: category || undefined,
        search: appliedSearch || undefined,
      }),
  });

  const markReadMutation = useMutation({
    mutationFn: markNotificationRead,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["notifications"] });
    },
  });

  function applySearch() {
    setPage(0);
    setAppliedSearch(search.trim());
  }

  function resetFilters() {
    setPage(0);
    setFilter("");
    setCategory("");
    setSearch("");
    setAppliedSearch("");
  }

  return (
    <Stack spacing={2}>
      <Card>
        <CardContent>
          <Grid container spacing={2} alignItems="center">
            <Grid item xs={12} md={3}>
              <TextField select fullWidth size="small" label="สถานะ" value={filter} onChange={(event) => { setPage(0); setFilter(event.target.value); }}>
                {filterOptions.map((option) => (
                  <MenuItem key={option.value} value={option.value}>{option.label}</MenuItem>
                ))}
              </TextField>
            </Grid>
            <Grid item xs={12} md={3}>
              <TextField select fullWidth size="small" label="หมวดระบบ" value={category} onChange={(event) => { setPage(0); setCategory(event.target.value); }}>
                {categoryOptions.map((option) => (
                  <MenuItem key={option.value} value={option.value}>{option.label}</MenuItem>
                ))}
              </TextField>
            </Grid>
            <Grid item xs={12} md={4}>
              <TextField
                fullWidth
                size="small"
                label="ค้นหา"
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                onKeyDown={(event) => {
                  if (event.key === "Enter") applySearch();
                }}
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <SearchOutlinedIcon fontSize="small" />
                    </InputAdornment>
                  ),
                }}
              />
            </Grid>
            <Grid item xs={12} md={2}>
              <Stack direction="row" spacing={1}>
                <Button variant="contained" onClick={applySearch}>ค้นหา</Button>
                <Button variant="outlined" onClick={resetFilters}>ล้าง</Button>
              </Stack>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      <Card>
        <CardContent>
          <Stack spacing={1.5}>
            <Typography variant="h6" fontWeight={800}>รายการแจ้งเตือน</Typography>
            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>หัวข้อ</TableCell>
                    <TableCell>ประเภท</TableCell>
                    <TableCell>ความสำคัญ</TableCell>
                    <TableCell>หมวดระบบ</TableCell>
                    <TableCell>วันที่</TableCell>
                    <TableCell align="right">จัดการ</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {data?.items.length ? (
                    data.items.map((item) => (
                      <TableRow key={item.id} hover>
                        <TableCell>
                          <Stack spacing={0.5}>
                            <Stack direction="row" spacing={1} alignItems="center">
                              <Typography fontWeight={item.unread ? 800 : 600}>{item.title}</Typography>
                              {item.unread && <Chip size="small" label="ใหม่" color="warning" />}
                            </Stack>
                            <Typography variant="body2" color="text.secondary">{item.message}</Typography>
                          </Stack>
                        </TableCell>
                        <TableCell>
                          <StatusBadge domain="notificationType" status={item.notificationType} />
                        </TableCell>
                        <TableCell>
                          <StatusBadge domain="notificationPriority" status={item.priority} />
                        </TableCell>
                        <TableCell>{categoryOptions.find((option) => option.value === item.category)?.label ?? item.category}</TableCell>
                        <TableCell>{formatThaiDateTime(item.createdAt)}</TableCell>
                        <TableCell align="right">
                          <Stack direction="row" spacing={0.5} justifyContent="flex-end">
                            {isGuid(item.id) && item.unread && (
                              <Tooltip title="ทำเครื่องหมายว่าอ่านแล้ว">
                                <IconButton size="small" onClick={() => markReadMutation.mutate(item.id)}>
                                  <MarkEmailReadOutlinedIcon fontSize="small" />
                                </IconButton>
                              </Tooltip>
                            )}
                            {item.path && (
                              <Tooltip title="เปิดรายการที่เกี่ยวข้อง">
                                <IconButton size="small" onClick={() => navigate(item.path)}>
                                  <OpenInNewOutlinedIcon fontSize="small" />
                                </IconButton>
                              </Tooltip>
                            )}
                          </Stack>
                        </TableCell>
                      </TableRow>
                    ))
                  ) : (
                    <TableRow>
                      <TableCell colSpan={6}>
                        <Box sx={{ py: 4, textAlign: "center" }}>
                          <Typography color="text.secondary">{isLoading ? "กำลังโหลดข้อมูล..." : "ไม่พบรายการแจ้งเตือน"}</Typography>
                        </Box>
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </TableContainer>
            <TablePagination
              component="div"
              count={data?.totalItems ?? 0}
              page={page}
              onPageChange={(_, nextPage) => setPage(nextPage)}
              rowsPerPage={pageSize}
              onRowsPerPageChange={(event) => {
                setPage(0);
                setPageSize(Number(event.target.value));
              }}
              rowsPerPageOptions={[10, 20, 50]}
              labelRowsPerPage="จำนวนต่อหน้า"
            />
          </Stack>
        </CardContent>
      </Card>
    </Stack>
  );
}

function isGuid(value: string) {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value);
}
