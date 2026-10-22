import { Alert, Box, Button, Card, CardContent, Chip, Stack, Typography } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { getAnnouncementLineGroupDeliveries } from "../../api/announcementsApi";
import { usePermission } from "../../context/PermissionContext";
import { formatThaiDateTime } from "../../utils/dateFormat";

export function AnnouncementGroupDeliveryPanel({ announcementId }: { announcementId: string }) {
  const { hasAnyPermission } = usePermission();
  const canView = hasAnyPermission(["Announcement.Manage", "Announcement.Notification.ViewDelivery"]);
  const query = useQuery({ queryKey: ["admin", "announcement-group-deliveries", announcementId],
    queryFn: () => getAnnouncementLineGroupDeliveries(announcementId), enabled: canView,
    refetchInterval: query => query.state.data?.some(item => item.status === "Pending" || item.status === "Retry") ? 15000 : false });
  if (!canView) return null;
  return <Card variant="outlined"><CardContent><Stack spacing={1.5}>
    <Stack direction="row" justifyContent="space-between" alignItems="center"><Typography variant="h6">ประวัติส่ง LINE กลุ่ม</Typography>
      <Button onClick={() => void query.refetch()} disabled={query.isFetching}>รีเฟรช</Button></Stack>
    {query.isError ? <Alert severity="error">โหลดประวัติการส่งไม่สำเร็จ</Alert> : !query.data?.length ?
      <Typography color="text.secondary">{query.isLoading ? "กำลังโหลด..." : "ยังไม่มีการส่งประกาศไปยังกลุ่ม LINE"}</Typography> : query.data.map(item =>
        <Box key={item.id} sx={{ border: 1, borderColor: "divider", borderRadius: 2, p: 1.5 }}>
          <Stack direction={{ xs: "column", sm: "row" }} spacing={1} justifyContent="space-between">
            <Typography fontWeight={700}>{item.displayName}</Typography>
            <Chip size="small" color={item.status === "Sent" ? "success" : item.status === "Failed" ? "error" : "default"}
              label={({ Pending: "รอส่ง", Retry: "รอลองส่งใหม่", Sent: "ส่งสำเร็จ", Failed: "ส่งไม่สำเร็จ", Skipped: "ข้ามส่ง" } as Record<string, string>)[item.status] ?? item.status} />
          </Stack>
          <Typography variant="body2" color="text.secondary">เข้าคิว {formatThaiDateTime(item.createdAt)} · ส่ง {item.attemptCount} ครั้ง</Typography>
          {item.sentAt && <Typography variant="body2">ส่งสำเร็จ {formatThaiDateTime(item.sentAt)}</Typography>}
          {item.errorCode && <Typography variant="body2" color="error.main">{item.errorCode}{item.errorMessage ? ` · ${item.errorMessage}` : ""}</Typography>}
        </Box>)}
  </Stack></CardContent></Card>;
}
