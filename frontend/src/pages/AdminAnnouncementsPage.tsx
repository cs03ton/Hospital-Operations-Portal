import AddOutlinedIcon from "@mui/icons-material/AddOutlined";
import ArchiveOutlinedIcon from "@mui/icons-material/ArchiveOutlined";
import CancelOutlinedIcon from "@mui/icons-material/CancelOutlined";
import ContentCopyOutlinedIcon from "@mui/icons-material/ContentCopyOutlined";
import DeleteOutlineOutlinedIcon from "@mui/icons-material/DeleteOutlineOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import PublishOutlinedIcon from "@mui/icons-material/PublishOutlined";
import SearchOutlinedIcon from "@mui/icons-material/SearchOutlined";
import VisibilityOutlinedIcon from "@mui/icons-material/VisibilityOutlined";
import {
  Box,
  Button,
  Card,
  CardContent,
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
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import {
  archiveAnnouncement,
  cancelAnnouncement,
  deleteAnnouncement,
  duplicateAnnouncement,
  getAdminAnnouncements,
  previewAnnouncementNotification,
  publishAnnouncement,
} from "../api/announcementsApi";
import { EmptyState } from "../components/common/EmptyState";
import { StatusBadge } from "../components/common/StatusBadge";
import { PageHeader } from "../components/PageHeader";
import { useNotification } from "../hooks/useNotification";
import { brandColors } from "../theme/theme";
import { formatThaiDateTime } from "../utils/dateFormat";

const statusOptions = [
  { value: "", label: "ทุกสถานะ" },
  { value: "Draft", label: "แบบร่าง" },
  { value: "Scheduled", label: "ตั้งเวลา" },
  { value: "Published", label: "เผยแพร่แล้ว" },
  { value: "Archived", label: "จัดเก็บแล้ว" },
  { value: "Cancelled", label: "ยกเลิก" },
];

export function AdminAnnouncementsPage() {
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(20);
  const [status, setStatus] = useState("");
  const [search, setSearch] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");
  const queryClient = useQueryClient();
  const notify = useNotification();
  const { data, isLoading } = useQuery({
    queryKey: ["admin", "announcements", page, pageSize, status, appliedSearch],
    queryFn: () => getAdminAnnouncements({ page: page + 1, pageSize, status: status || undefined, search: appliedSearch || undefined }),
  });

  const actionMutation = useMutation({
    mutationFn: async ({ id, action }: { id: string; action: "publish" | "archive" | "cancel" | "duplicate" | "delete" }) => {
      if (action === "publish") {
        const preview = await previewAnnouncementNotification(id);
        const warningText = preview.warnings.length ? `\n\nคำเตือน:\n${preview.warnings.map((warning) => `- ${warning}`).join("\n")}` : "";
        const confirmed = window.confirm(
          `ยืนยันเผยแพร่ประกาศ?\n\n` +
          `ผู้รับทั้งหมด: ${preview.totalTargetUsers.toLocaleString("th-TH")} ราย\n` +
          `Notification Bell: ${preview.inAppRecipientCount.toLocaleString("th-TH")} ราย\n` +
          `LINE: ${preview.lineBoundRecipientCount.toLocaleString("th-TH")} ราย\n` +
          `รายการที่คาดว่าจะเข้าคิว: ${preview.estimatedQueueItems.toLocaleString("th-TH")} รายการ${warningText}`
        );
        if (!confirmed) throw new Error("__cancelled__");
        await publishAnnouncement(id);
      }
      else if (action === "archive") await archiveAnnouncement(id);
      else if (action === "cancel") await cancelAnnouncement(id);
      else if (action === "duplicate") await duplicateAnnouncement(id);
      else {
        const confirmed = window.confirm("ยืนยันลบประกาศนี้?\n\nประกาศจะถูกนำออกจากศูนย์ประกาศ และลบรูปภาพ/ไฟล์แนบที่เกี่ยวข้อง");
        if (!confirmed) throw new Error("__cancelled__");
        await deleteAnnouncement(id);
      }
    },
    onSuccess: async (_, variables) => {
      notify.showSuccess(variables.action === "delete" ? "ลบประกาศเรียบร้อยแล้ว" : "อัปเดตประกาศเรียบร้อยแล้ว");
      await queryClient.invalidateQueries({ queryKey: ["admin", "announcements"] });
    },
    onError: (error) => {
      if (error instanceof Error && error.message === "__cancelled__") return;
      notify.showError("ไม่สามารถอัปเดตประกาศได้");
    },
  });

  function applySearch() {
    setPage(0);
    setAppliedSearch(search.trim());
  }

  function resetFilters() {
    setPage(0);
    setStatus("");
    setSearch("");
    setAppliedSearch("");
  }

  return (
    <Box sx={{ maxWidth: 1440, mx: "auto" }}>
      <Stack spacing={3}>
        <PageHeader title="จัดการประกาศ" subtitle="สร้าง เผยแพร่ ตั้งเวลา และติดตามประกาศภายในโรงพยาบาล" />

        <Stack direction={{ xs: "column", md: "row" }} spacing={2} justifyContent="space-between" alignItems={{ xs: "stretch", md: "center" }}>
          <Button component={RouterLink} to="/admin/announcements/create" variant="contained" startIcon={<AddOutlinedIcon />}>
            เพิ่มประกาศ
          </Button>
        </Stack>

        <Card sx={{ border: `1px solid ${brandColors.border}`, borderTop: `5px solid ${brandColors.accent}`, borderRadius: 3 }}>
          <CardContent>
            <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", md: "minmax(0, 1fr) 220px auto" }, gap: 2, alignItems: "center" }}>
              <TextField
                label="ค้นหาประกาศ"
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                onKeyDown={(event) => {
                  if (event.key === "Enter") applySearch();
                }}
                InputProps={{ startAdornment: <InputAdornment position="start"><SearchOutlinedIcon /></InputAdornment> }}
              />
              <TextField select label="สถานะ" value={status} onChange={(event) => { setPage(0); setStatus(event.target.value); }}>
                {statusOptions.map((option) => <MenuItem key={option.value} value={option.value}>{option.label}</MenuItem>)}
              </TextField>
              <Stack direction="row" spacing={1}>
                <Button variant="contained" onClick={applySearch}>ค้นหา</Button>
                <Button variant="outlined" onClick={resetFilters}>ล้าง</Button>
              </Stack>
            </Box>
          </CardContent>
        </Card>

        <Card sx={{ border: `1px solid ${brandColors.border}`, borderRadius: 3 }}>
          <CardContent>
            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>หัวข้อ</TableCell>
                    <TableCell>หมวดหมู่</TableCell>
                    <TableCell>ความสำคัญ</TableCell>
                    <TableCell>สถานะ</TableCell>
                    <TableCell>เผยแพร่</TableCell>
                    <TableCell align="right">จัดการ</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {data?.items.length ? data.items.map((item) => (
                    <TableRow key={item.id} hover>
                      <TableCell>
                        <Typography fontWeight={900}>{item.title}</Typography>
                        <Typography variant="body2" color="text.secondary">{item.summary}</Typography>
                        <Typography variant="caption" color="text.secondary">
                          แจ้งเตือน: {item.notifyInApp ? "Bell" : "-"} {item.notifyViaLine ? "· LINE" : ""} {item.notificationDispatchStatus ? `· ${item.notificationDispatchStatus}` : ""}
                        </Typography>
                      </TableCell>
                      <TableCell>{item.category?.name ?? "-"}</TableCell>
                      <TableCell><StatusBadge domain="announcementPriority" status={item.priority} /></TableCell>
                      <TableCell><StatusBadge domain="announcement" status={item.status} /></TableCell>
                      <TableCell>{formatThaiDateTime(item.publishedAt ?? item.publishAt ?? item.createdAt)}</TableCell>
                      <TableCell align="right">
                        <Stack direction="row" spacing={0.5} justifyContent="flex-end">
                          <Tooltip title="ดูประกาศ">
                            <IconButton component={RouterLink} to={`/admin/announcements/${item.id}`}><VisibilityOutlinedIcon /></IconButton>
                          </Tooltip>
                          <Tooltip title="แก้ไข">
                            <IconButton component={RouterLink} to={`/admin/announcements/${item.id}/edit`}><EditOutlinedIcon /></IconButton>
                          </Tooltip>
                          <Tooltip title="เผยแพร่">
                            <span><IconButton disabled={item.status === "Published" || actionMutation.isPending} onClick={() => actionMutation.mutate({ id: item.id, action: "publish" })}><PublishOutlinedIcon /></IconButton></span>
                          </Tooltip>
                          <Tooltip title="คัดลอก">
                            <span><IconButton disabled={actionMutation.isPending} onClick={() => actionMutation.mutate({ id: item.id, action: "duplicate" })}><ContentCopyOutlinedIcon /></IconButton></span>
                          </Tooltip>
                          <Tooltip title="จัดเก็บ">
                            <span><IconButton disabled={item.status === "Archived" || actionMutation.isPending} onClick={() => actionMutation.mutate({ id: item.id, action: "archive" })}><ArchiveOutlinedIcon /></IconButton></span>
                          </Tooltip>
                          <Tooltip title="ยกเลิก">
                            <span><IconButton disabled={item.status === "Cancelled" || actionMutation.isPending} onClick={() => actionMutation.mutate({ id: item.id, action: "cancel" })}><CancelOutlinedIcon /></IconButton></span>
                          </Tooltip>
                          <Tooltip title="ลบประกาศ">
                            <span><IconButton disabled={actionMutation.isPending} color="error" onClick={() => actionMutation.mutate({ id: item.id, action: "delete" })}><DeleteOutlineOutlinedIcon /></IconButton></span>
                          </Tooltip>
                        </Stack>
                      </TableCell>
                    </TableRow>
                  )) : (
                    <TableRow>
                      <TableCell colSpan={6}>
                        <EmptyState message={isLoading ? "กำลังโหลดประกาศ..." : "ยังไม่มีประกาศ"} />
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
          </CardContent>
        </Card>
      </Stack>
    </Box>
  );
}
