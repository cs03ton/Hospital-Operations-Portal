import CheckCircleOutlineIcon from "@mui/icons-material/CheckCircleOutline";
import SettingsOutlinedIcon from "@mui/icons-material/SettingsOutlined";
import WarningAmberOutlinedIcon from "@mui/icons-material/WarningAmberOutlined";
import { Box, Card, CardContent, Chip, Grid, Stack, Typography } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { getSystemSettings } from "../api/adminApi";
import { EmptyState } from "../components/common/EmptyState";
import { LoadingState } from "../components/common/LoadingState";
import { PageHeader } from "../components/PageHeader";

export function SystemSettingsPage() {
  const { data, isLoading, isError } = useQuery({
    queryKey: ["system-settings"],
    queryFn: getSystemSettings,
  });

  if (isLoading) {
    return <LoadingState message="กำลังโหลดการตั้งค่าระบบ..." />;
  }

  if (isError || !data) {
    return <EmptyState message="ไม่สามารถโหลดการตั้งค่าระบบได้ กรุณาตรวจสอบสิทธิ์หรือการเชื่อมต่อ API" />;
  }

  return (
    <>
      <PageHeader title="ตั้งค่าระบบ" subtitle="ตรวจสอบค่าคอนฟิกที่ใช้งานจริงสำหรับ Phase 1 Production" />
      <Stack spacing={2}>
        <Card>
          <CardContent>
            <Stack direction="row" spacing={1.5} alignItems="center" sx={{ mb: 2 }}>
              <SettingsOutlinedIcon color="primary" />
              <Typography variant="h6">ข้อมูลระบบ</Typography>
            </Stack>
            <Grid container spacing={2}>
              <Setting label="ชื่อโรงพยาบาล" value={data.hospitalName} />
              <Setting label="โลโก้โรงพยาบาล" value={data.hospitalLogoPath} />
              <Setting label="ข้อความ Footer" value={data.footerText} />
              <Setting label="ผู้พัฒนา" value={data.footerDeveloper} />
              <Setting label="Application Version" value={data.applicationVersion} />
            </Grid>
          </CardContent>
        </Card>

        <Card>
          <CardContent>
            <Typography variant="h6" sx={{ mb: 2 }}>
              Theme Settings
            </Typography>
            <Grid container spacing={2}>
              <ColorSetting label="Primary Deep Green" value={data.themePrimaryColor} />
              <ColorSetting label="Secondary Earth Brown" value={data.themeSecondaryColor} />
            </Grid>
          </CardContent>
        </Card>

        <Card>
          <CardContent>
            <Typography variant="h6" sx={{ mb: 2 }}>
              ตั้งค่าเอกสาร PDF
            </Typography>
            <Grid container spacing={2}>
              <Setting label="Template Config" value={data.pdfTemplateConfigPath} />
              <Setting label="Font Path" value={data.pdfFontPath} />
              <Setting label="Font Family" value={data.pdfFontFamily} />
              <Setting label="Font Size" value={`${data.pdfFontSize} pt`} />
              <Setting label="Line Height" value={`${data.pdfLineHeight}`} />
            </Grid>
          </CardContent>
        </Card>

        <Card>
          <CardContent>
            <Stack direction="row" spacing={1.5} alignItems="center" sx={{ mb: 2 }}>
              {data.lineEnabled && data.lineChannelAccessTokenConfigured ? (
                <CheckCircleOutlineIcon color="success" />
              ) : (
                <WarningAmberOutlinedIcon color="warning" />
              )}
              <Typography variant="h6">LINE Configuration</Typography>
              <Chip
                size="small"
                color={data.lineEnabled ? "success" : "default"}
                label={data.lineEnabled ? "เปิดใช้งาน" : "ปิดใช้งาน"}
              />
            </Stack>
            <Grid container spacing={2}>
              <Setting label="LINE Endpoint" value={data.lineEndpoint} />
              <Setting
                label="Channel Access Token"
                value={data.lineChannelAccessTokenConfigured ? "ตั้งค่าแล้ว (ไม่แสดงค่า secret)" : "ยังไม่ได้ตั้งค่า"}
              />
            </Grid>
          </CardContent>
        </Card>
      </Stack>
    </>
  );
}

function Setting({ label, value }: { label: string; value: string }) {
  return (
    <Grid item xs={12} md={6}>
      <Box>
        <Typography variant="caption" color="text.secondary">
          {label}
        </Typography>
        <Typography sx={{ overflowWrap: "anywhere" }}>{value || "-"}</Typography>
      </Box>
    </Grid>
  );
}

function ColorSetting({ label, value }: { label: string; value: string }) {
  return (
    <Grid item xs={12} md={6}>
      <Stack direction="row" spacing={1.5} alignItems="center">
        <Box sx={{ width: 28, height: 28, borderRadius: 1, bgcolor: value, border: "1px solid", borderColor: "divider" }} />
        <Box>
          <Typography variant="caption" color="text.secondary">
            {label}
          </Typography>
          <Typography>{value}</Typography>
        </Box>
      </Stack>
    </Grid>
  );
}
