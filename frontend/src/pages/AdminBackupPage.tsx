import BackupOutlinedIcon from "@mui/icons-material/BackupOutlined";
import HealthAndSafetyOutlinedIcon from "@mui/icons-material/HealthAndSafetyOutlined";
import HistoryOutlinedIcon from "@mui/icons-material/HistoryOutlined";
import { Alert, Box, Button, Card, CardContent, Grid, Skeleton, Stack, Typography } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { Link as RouterLink } from "react-router-dom";
import { getAdminHealth } from "../api/adminApi";
import { PageHeader } from "../components/PageHeader";
import { formatThaiDateTime } from "../utils/dateFormat";
import { brandColors } from "../theme/theme";

export function AdminBackupPage() {
  const { data, isLoading, isError } = useQuery({
    queryKey: ["admin-health", "backup-center"],
    queryFn: getAdminHealth,
    refetchOnWindowFocus: false,
  });

  return (
    <Stack spacing={3}>
      <PageHeader title="Backup Center" subtitle="ตรวจสอบสถานะ backup และแนวทาง restore test สำหรับผู้ดูแลระบบ" />

      {isError && <Alert severity="error">ไม่สามารถโหลดสถานะ backup ได้ กรุณาลองใหม่อีกครั้ง</Alert>}

      <Grid container spacing={2}>
        <Grid item xs={12} md={6}>
          <Card sx={{ height: "100%", borderTop: `4px solid ${brandColors.accent}` }}>
            <CardContent>
              <Stack spacing={2}>
                <Stack direction="row" spacing={1.5} alignItems="center">
                  <BackupOutlinedIcon color="primary" />
                  <Box>
                    <Typography variant="h6" fontWeight={900}>Backup Status</Typography>
                    <Typography color="text.secondary">อ่าน metadata จาก Health Center เท่านั้น</Typography>
                  </Box>
                </Stack>
                {isLoading ? (
                  <Skeleton height={140} />
                ) : (
                  <Stack spacing={1}>
                    <InfoRow label="สถานะ" value={data?.backup.status ?? "-"} />
                    <InfoRow label="Backup ล่าสุด" value={data?.backup.lastBackupAt ? formatThaiDateTime(data.backup.lastBackupAt) : "-"} />
                    <InfoRow label="ไฟล์ล่าสุด" value={data?.backup.latestBackupFile ?? "-"} />
                    <InfoRow label="โฟลเดอร์" value={data?.backup.backupDirectory ?? "-"} />
                    <InfoRow label="Restore test ล่าสุด" value={data?.backup.lastRestoreTestAt ? formatThaiDateTime(data.backup.lastRestoreTestAt) : "-"} />
                  </Stack>
                )}
              </Stack>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={6}>
          <Card sx={{ height: "100%", borderTop: `4px solid ${brandColors.accent}` }}>
            <CardContent>
              <Stack spacing={2}>
                <Stack direction="row" spacing={1.5} alignItems="center">
                  <HistoryOutlinedIcon color="primary" />
                  <Box>
                    <Typography variant="h6" fontWeight={900}>Runbook</Typography>
                    <Typography color="text.secondary">การ backup/restore ต้องทำผ่าน script และ maintenance window</Typography>
                  </Box>
                </Stack>
                <Alert severity="info">
                  หน้านี้ไม่รันคำสั่ง backup หรือ restore โดยตรง เพื่อป้องกันการทับข้อมูล production โดยไม่ตั้งใจ
                </Alert>
                <Stack spacing={1}>
                  <Typography>1. รัน backup ผ่าน `scripts/backup/backup-hop.sh`</Typography>
                  <Typography>2. ตรวจไฟล์ `.dump` และ storage `.tar.gz`</Typography>
                  <Typography>3. ทดสอบ restore ใน environment แยก</Typography>
                  <Typography>4. บันทึกหลักฐาน restore test</Typography>
                </Stack>
                <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                  <Button component={RouterLink} to="/admin/health" variant="outlined" startIcon={<HealthAndSafetyOutlinedIcon />}>
                    เปิด Health Center
                  </Button>
                </Stack>
              </Stack>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Stack>
  );
}

function InfoRow({ label, value }: { label: string; value: string }) {
  return (
    <Stack direction="row" justifyContent="space-between" spacing={2}>
      <Typography color="text.secondary">{label}</Typography>
      <Typography fontWeight={900} sx={{ textAlign: "right", wordBreak: "break-word" }}>{value}</Typography>
    </Stack>
  );
}
