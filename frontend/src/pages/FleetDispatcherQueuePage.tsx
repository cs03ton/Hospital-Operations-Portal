import RefreshOutlinedIcon from "@mui/icons-material/RefreshOutlined";
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
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { getDispatcherQueue, type FleetRequest } from "../api/fleetApi";
import { EmptyState } from "../components/common/EmptyState";
import { ListPagination } from "../components/common/ListPagination";
import { LoadingState } from "../components/common/LoadingState";
import { PageHeader } from "../components/PageHeader";
import { formatThaiDateTime } from "../utils/dateFormat";
import { getFleetStatusLabel } from "../utils/fleetLabels";

const emptyFleetRequests: FleetRequest[] = [];

export function FleetDispatcherQueuePage() {
  const [keyword, setKeyword] = useState("");
  const [status, setStatus] = useState("");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const query = useQuery({
    queryKey: ["fleet", "dispatch"],
    queryFn: getDispatcherQueue,
    retry: 1,
  });
  const rows = query.data ?? emptyFleetRequests;

  const statusOptions = useMemo(() => Array.from(new Set(rows.map((item) => item.status))).sort(), [rows]);
  const filteredRows = useMemo(() => {
    const normalizedKeyword = keyword.trim().toLowerCase();
    return rows.filter((item) => {
      const searchable = [
        item.requestNo,
        item.requesterName,
        item.requesterDepartmentName,
        item.purpose,
        item.destination,
        item.missionType,
        getFleetStatusLabel(item.status),
      ]
        .filter(Boolean)
        .join(" ")
        .toLowerCase();
      return (!normalizedKeyword || searchable.includes(normalizedKeyword)) && (!status || item.status === status);
    });
  }, [keyword, rows, status]);
  const lastPage = Math.max(1, Math.ceil(filteredRows.length / pageSize));
  const safePage = Math.min(page, lastPage);
  const visibleRows = filteredRows.slice((safePage - 1) * pageSize, safePage * pageSize);

  const updateKeyword = (value: string) => {
    setKeyword(value);
    setPage(1);
  };
  const updateStatus = (value: string) => {
    setStatus(value);
    setPage(1);
  };

  return (
    <Box>
      <PageHeader title="คิวรอจัดรถ" subtitle="ค้นหาคำขอ ตรวจสอบความพร้อม และเลือกรถกับคนขับสำหรับแต่ละภารกิจ" />

      {query.isError && (
        <Alert severity="error" sx={{ mb: 2 }} action={<Button disabled={query.isFetching} onClick={() => void query.refetch()}>ลองใหม่</Button>}>
          ไม่สามารถโหลดคิวรอจัดรถได้ กรุณาลองใหม่อีกครั้ง
        </Alert>
      )}

      <Card sx={{ mb: 2 }}>
        <CardContent sx={{ py: 2 }}>
          <Grid container spacing={1.5} alignItems="center">
            <Grid item xs={12} md={7}>
              <TextField
                fullWidth
                size="small"
                label="ค้นหาคำขอ"
                placeholder="เลขคำขอ ผู้ขอ หน่วยงาน ภารกิจ หรือปลายทาง"
                value={keyword}
                onChange={(event) => updateKeyword(event.target.value)}
                InputProps={{ startAdornment: <InputAdornment position="start"><SearchOutlinedIcon fontSize="small" /></InputAdornment> }}
              />
            </Grid>
            <Grid item xs={12} sm={7} md={3}>
              <TextField select fullWidth size="small" label="สถานะคำขอ" value={status} onChange={(event) => updateStatus(event.target.value)}>
                <MenuItem value="">ทุกสถานะ</MenuItem>
                {statusOptions.map((value) => <MenuItem key={value} value={value}>{getFleetStatusLabel(value)}</MenuItem>)}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={5} md={2}>
              <Button fullWidth variant="outlined" startIcon={<RefreshOutlinedIcon />} disabled={query.isFetching} onClick={() => void query.refetch()}>
                รีเฟรช
              </Button>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      <Card>
        <CardContent>
          <Stack direction={{ xs: "column", sm: "row" }} justifyContent="space-between" spacing={1} sx={{ mb: 2 }}>
            <Box>
              <Typography variant="h6" color="primary" fontWeight={800}>รายการรอดำเนินการ</Typography>
              <Typography variant="body2" color="text.secondary">พบ {filteredRows.length.toLocaleString("th-TH")} รายการ</Typography>
            </Box>
            {(keyword || status) && <Button onClick={() => { updateKeyword(""); updateStatus(""); }}>ล้างตัวกรอง</Button>}
          </Stack>

          {query.isLoading ? (
            <LoadingState message="กำลังโหลดคิวรอจัดรถ..." />
          ) : visibleRows.length ? (
            <>
              <Box sx={{ overflowX: "auto" }}>
                <Table size="small" sx={{ minWidth: 900 }}>
                  <TableHead>
                    <TableRow>
                      <TableCell>เลขคำขอ</TableCell>
                      <TableCell>ผู้ขอ/หน่วยงาน</TableCell>
                      <TableCell>ภารกิจและปลายทาง</TableCell>
                      <TableCell>วันเวลาเดินทาง</TableCell>
                      <TableCell>ผู้ร่วมเดินทาง</TableCell>
                      <TableCell>สถานะ</TableCell>
                      <TableCell align="right">จัดการ</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {visibleRows.map((item) => <DispatchQueueRow key={item.id} item={item} />)}
                  </TableBody>
                </Table>
              </Box>
              <ListPagination
                page={safePage}
                pageSize={pageSize}
                totalItems={filteredRows.length}
                pageSizeOptions={[5, 10, 20, 50]}
                disabled={query.isFetching}
                onPageChange={setPage}
                onPageSizeChange={(value) => { setPageSize(value); setPage(1); }}
              />
            </>
          ) : (
            <EmptyState
              title={rows.length ? "ไม่พบคำขอตามเงื่อนไข" : "ไม่มีคำขอรอจัดรถ"}
              description={rows.length ? "ลองเปลี่ยนคำค้นหาหรือเลือกสถานะอื่น" : "คำขอที่ส่งถึงงานยานพาหนะจะแสดงที่หน้านี้"}
            />
          )}
        </CardContent>
      </Card>
    </Box>
  );
}

function DispatchQueueRow({ item }: { item: FleetRequest }) {
  const statusColor = item.status === "RETURNED" ? "warning" : item.status === "REJECTED" ? "error" : "info";
  return (
    <TableRow hover>
      <TableCell>
        <Stack direction="row" spacing={0.75} alignItems="center" flexWrap="wrap" useFlexGap>
          <Typography fontWeight={900} color="primary">{item.requestNo}</Typography>
          {item.isUrgent && <Chip size="small" color="error" label="เร่งด่วน" />}
        </Stack>
      </TableCell>
      <TableCell>
        <Typography variant="body2" fontWeight={700}>{item.requesterName || "-"}</Typography>
        <Typography variant="caption" color="text.secondary">{item.requesterDepartmentName || "ไม่ระบุหน่วยงาน"}</Typography>
      </TableCell>
      <TableCell>
        <Typography variant="body2" fontWeight={700}>{item.purpose || "-"}</Typography>
        <Typography variant="caption" color="text.secondary">ปลายทาง: {item.destination || "-"}</Typography>
      </TableCell>
      <TableCell>
        <Typography variant="body2">{formatThaiDateTime(item.departureAt)}</Typography>
        <Typography variant="caption" color="text.secondary">ถึง {formatThaiDateTime(item.expectedReturnAt)}</Typography>
      </TableCell>
      <TableCell>{item.passengerCount.toLocaleString("th-TH")} คน</TableCell>
      <TableCell><Chip size="small" color={statusColor} variant="outlined" label={getFleetStatusLabel(item.status)} /></TableCell>
      <TableCell align="right">
        <Tooltip title="เปิดรายละเอียดและจัดรถ">
          <IconButton component={Link} to={`/fleet/dispatch/${item.id}`} color="primary" aria-label={`จัดรถคำขอ ${item.requestNo}`}>
            <VisibilityOutlinedIcon />
          </IconButton>
        </Tooltip>
      </TableCell>
    </TableRow>
  );
}
