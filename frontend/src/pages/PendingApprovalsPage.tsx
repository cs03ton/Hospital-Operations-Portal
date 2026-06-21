import SearchOutlinedIcon from "@mui/icons-material/SearchOutlined";
import VisibilityOutlinedIcon from "@mui/icons-material/VisibilityOutlined";
import {
  Alert,
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
  TableHead,
  TablePagination,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import { getMyPendingApprovals, type PendingApprovalNotification } from "../api/leaveApi";
import { EmptyState } from "../components/common/EmptyState";
import { LoadingState } from "../components/common/LoadingState";
import { PageHeader } from "../components/PageHeader";
import { formatThaiDate, formatThaiDateTime } from "../utils/dateFormat";
import { getLeaveTypeLabel } from "../utils/leaveLabels";

type PendingApprovalFilters = {
  keyword: string;
  leaveType: string;
  priority: string;
};

const priorityLabels: Record<string, string> = {
  High: "ด่วน",
  Medium: "ใกล้ถึงวันลา",
  Normal: "ปกติ",
};

export function PendingApprovalsPage() {
  const [filters, setFilters] = useState<PendingApprovalFilters>({
    keyword: "",
    leaveType: "",
    priority: "",
  });
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);

  const { data = [], isError, isLoading } = useQuery({
    queryKey: ["approvals", "my-pending"],
    queryFn: getMyPendingApprovals,
  });

  const leaveTypeOptions = useMemo(() => {
    const types = new Set(data.map((item) => item.leaveType).filter(Boolean) as string[]);
    return Array.from(types).sort((a, b) => getLeaveTypeLabel(a).localeCompare(getLeaveTypeLabel(b), "th"));
  }, [data]);

  const filteredData = useMemo(() => {
    const keyword = filters.keyword.trim().toLowerCase();
    return data.filter((item) => {
      const haystack = [
        item.employeeName,
        item.leaveType,
        getLeaveTypeLabel(item.leaveType),
        item.currentStep.toString(),
        priorityLabels[item.priority] ?? item.priority,
      ]
        .filter(Boolean)
        .join(" ")
        .toLowerCase();

      return (
        (!keyword || haystack.includes(keyword)) &&
        (!filters.leaveType || item.leaveType === filters.leaveType) &&
        (!filters.priority || item.priority === filters.priority)
      );
    });
  }, [data, filters]);

  const visibleRows = filteredData.slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage);

  function updateFilters(nextFilters: PendingApprovalFilters) {
    setFilters(nextFilters);
    setPage(0);
  }

  function clearFilters() {
    updateFilters({ keyword: "", leaveType: "", priority: "" });
  }

  return (
    <Box>
      <PageHeader title="งานรออนุมัติของฉัน" subtitle="แสดงเฉพาะคำขอลาที่ถึงคิวอนุมัติของผู้ใช้งานปัจจุบัน" />

      {isError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          ไม่สามารถโหลดรายการรออนุมัติได้ กรุณาลองใหม่อีกครั้ง
        </Alert>
      )}

      <Card sx={{ mb: 2 }}>
        <CardContent sx={{ py: 2 }}>
          <Grid container spacing={1.5} alignItems="center">
            <Grid item xs={12} md={5}>
              <TextField
                fullWidth
                size="small"
                label="ค้นหา"
                placeholder="ค้นหาชื่อผู้ขอ ประเภทลา หรือขั้นตอน"
                value={filters.keyword}
                onChange={(event) => updateFilters({ ...filters, keyword: event.target.value })}
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <SearchOutlinedIcon fontSize="small" />
                    </InputAdornment>
                  ),
                }}
              />
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <TextField
                select
                fullWidth
                size="small"
                label="ประเภทลา"
                value={filters.leaveType}
                onChange={(event) => updateFilters({ ...filters, leaveType: event.target.value })}
              >
                <MenuItem value="">ทุกประเภทลา</MenuItem>
                {leaveTypeOptions.map((leaveType) => (
                  <MenuItem key={leaveType} value={leaveType}>
                    {getLeaveTypeLabel(leaveType)}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={6} md={2}>
              <TextField
                select
                fullWidth
                size="small"
                label="ความสำคัญ"
                value={filters.priority}
                onChange={(event) => updateFilters({ ...filters, priority: event.target.value })}
              >
                <MenuItem value="">ทุกระดับ</MenuItem>
                {Object.entries(priorityLabels).map(([value, label]) => (
                  <MenuItem key={value} value={value}>
                    {label}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>
            <Grid item xs={12} md={2}>
              <Button fullWidth variant="outlined" onClick={clearFilters}>
                ล้างตัวกรอง
              </Button>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      <Card>
        <CardContent>
          <Stack direction={{ xs: "column", sm: "row" }} justifyContent="space-between" spacing={1.5} sx={{ mb: 2 }}>
            <Box>
              <Typography variant="h6" color="primary" fontWeight={700}>
                รายการคำขอที่รอฉันอนุมัติ
              </Typography>
              <Typography variant="body2" color="text.secondary">
                ทั้งหมด {filteredData.length.toLocaleString("th-TH")} รายการ
              </Typography>
            </Box>
          </Stack>

          {isLoading ? (
            <LoadingState message="กำลังโหลดงานรออนุมัติ..." />
          ) : filteredData.length ? (
            <>
              <Box sx={{ overflowX: "auto" }}>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>ผู้ขอลา</TableCell>
                      <TableCell>ประเภทลา</TableCell>
                      <TableCell>วันที่ลา</TableCell>
                      <TableCell>วันที่ส่งคำขอ</TableCell>
                      <TableCell>ขั้นตอน</TableCell>
                      <TableCell>ความสำคัญ</TableCell>
                      <TableCell align="right">จัดการ</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {visibleRows.map((item) => (
                      <PendingApprovalRow key={item.requestId} item={item} />
                    ))}
                  </TableBody>
                </Table>
              </Box>
              <TablePagination
                component="div"
                count={filteredData.length}
                page={page}
                onPageChange={(_, nextPage) => setPage(nextPage)}
                rowsPerPage={rowsPerPage}
                onRowsPerPageChange={(event) => {
                  setRowsPerPage(Number(event.target.value));
                  setPage(0);
                }}
                rowsPerPageOptions={[5, 10, 25]}
                labelRowsPerPage="จำนวนต่อหน้า"
                labelDisplayedRows={({ from, to, count }) => `${from}-${to} จาก ${count}`}
              />
            </>
          ) : (
            <EmptyState message="ไม่มีรายการรออนุมัติ" />
          )}
        </CardContent>
      </Card>
    </Box>
  );
}

function PendingApprovalRow({ item }: { item: PendingApprovalNotification }) {
  return (
    <TableRow hover>
      <TableCell>{item.employeeName ?? "-"}</TableCell>
      <TableCell>{getLeaveTypeLabel(item.leaveType)}</TableCell>
      <TableCell>
        {formatThaiDate(item.startDate)} - {formatThaiDate(item.endDate)}
      </TableCell>
      <TableCell>{formatThaiDateTime(item.submittedAt)}</TableCell>
      <TableCell>ขั้นที่ {item.currentStep.toLocaleString("th-TH")}</TableCell>
      <TableCell>
        <Chip size="small" color={getPriorityColor(item.priority)} label={priorityLabels[item.priority] ?? item.priority} />
      </TableCell>
      <TableCell align="right">
        <Tooltip title="ดูรายละเอียดคำขอลา">
          <IconButton component={RouterLink} to={`/leave/${item.requestId}`} aria-label="ดูรายละเอียดคำขอลา">
            <VisibilityOutlinedIcon />
          </IconButton>
        </Tooltip>
      </TableCell>
    </TableRow>
  );
}

function getPriorityColor(priority: string): "default" | "warning" | "error" | "info" {
  switch (priority) {
    case "High":
      return "error";
    case "Medium":
      return "warning";
    case "Normal":
      return "info";
    default:
      return "default";
  }
}
