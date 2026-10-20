import ArrowForwardOutlinedIcon from "@mui/icons-material/ArrowForwardOutlined";
import AssignmentTurnedInOutlinedIcon from "@mui/icons-material/AssignmentTurnedInOutlined";
import LockOutlinedIcon from "@mui/icons-material/LockOutlined";
import { Alert, Box, Button, Card, CardContent, Chip, Grid, Skeleton, Stack, Typography } from "@mui/material";
import { alpha, useTheme } from "@mui/material/styles";
import { useQuery } from "@tanstack/react-query";
import { Link as RouterLink } from "react-router-dom";
import { getDashboardSummary } from "../api/adminApi";
import { getDashboardActionItems, type DashboardActionItems } from "../api/dashboardActionApi";
import { getFeaturedAnnouncements } from "../api/announcementsApi";
import { PageHeader } from "../components/PageHeader";
import { dashboardModules, getDashboardHubMetrics, getVisibleDashboardModules } from "../config/dashboardModules";
import { hospitalName } from "../config/appConfig";
import { useAuth } from "../context/AuthContext";
import { brandColors } from "../theme/theme";
import { dashboardPollingOptions } from "../config/queryPolling";

export function DashboardHubPage() {
  const { user } = useAuth();
  const theme = useTheme();
  const visibleModules = getVisibleDashboardModules(user);
  const { data, isError, isLoading } = useQuery({
    queryKey: ["dashboard-summary", "hub"],
    queryFn: getDashboardSummary,
    ...dashboardPollingOptions,
  });
  const { data: featuredAnnouncements = [] } = useQuery({
    queryKey: ["announcements", "featured", "hub"],
    queryFn: getFeaturedAnnouncements,
    enabled: Boolean(user?.permissions?.includes("Announcement.View")),
    ...dashboardPollingOptions,
  });
  const { data: actionData } = useQuery({
    queryKey: ["dashboard", "action-items"],
    queryFn: getDashboardActionItems,
    ...dashboardPollingOptions,
  });
  const actionCards = [
    { key: "leave", title: "ระบบลา", count: actionData?.leaveCount ?? 0, detail: "คำขอรออนุมัติ", to: "/leave/pending-approvals" },
    { key: "vehicle", title: "ระบบจองรถ", count: actionData?.fleetCount ?? 0, detail: "คำขอใช้รถรออนุมัติ", to: "/fleet/approvals" },
    { key: "repair", title: "ระบบแจ้งซ่อม", count: actionData?.repairCount ?? 0, detail: "งานเปิดอยู่ของทีม", to: "/repairs?scope=team" },
    { key: "meeting", title: "จองห้องประชุม", count: actionData?.meetingTodayCount ?? 0, detail: "การจองวันนี้", to: "/meeting-rooms/calendar" },
  ].filter(card => card.count > 0 && visibleModules.some(module => module.key === card.key));

  return (
    <Box>
      <PageHeader
        title="Dashboard Hub"
        subtitle={`ศูนย์กลางแดชบอร์ดสำหรับระบบงานของ${hospitalName}`}
      />

      {isError && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          ไม่สามารถโหลดตัวเลขสรุปได้ในขณะนี้ ระบบจะแสดงข้อมูลเริ่มต้นแทน
        </Alert>
      )}

      {featuredAnnouncements.length > 0 && (
        <Card
          sx={{
            mb: 2.5,
            border: `1px solid ${brandColors.border}`,
            borderTop: `4px solid ${brandColors.accent}`,
            boxShadow: `0 14px 34px ${alpha(theme.palette.primary.dark, 0.06)}`,
          }}
        >
          <CardContent>
            <Stack direction={{ xs: "column", md: "row" }} spacing={2} alignItems={{ xs: "stretch", md: "center" }} justifyContent="space-between">
              <Box>
                <Typography variant="h6" fontWeight={900} color="primary.dark">
                  ประกาศสำคัญล่าสุด
                </Typography>
                <Typography color="text.secondary">
                  {featuredAnnouncements[0].title}
                </Typography>
              </Box>
              <Button component={RouterLink} to={`/announcements/${featuredAnnouncements[0].id}`} variant="outlined" endIcon={<ArrowForwardOutlinedIcon />}>
                อ่านประกาศ
              </Button>
            </Stack>
          </CardContent>
        </Card>
      )}

      {actionCards.length > 0 && (
        <Card sx={{ mb: 2.5, border: `1px solid ${alpha(theme.palette.warning.main, 0.45)}`, bgcolor: alpha(theme.palette.warning.light, 0.07) }}>
          <CardContent>
            <Stack direction={{ xs: "column", sm: "row" }} justifyContent="space-between" alignItems={{ xs: "flex-start", sm: "center" }} spacing={1} sx={{ mb: 2 }}>
              <Stack direction="row" spacing={1} alignItems="center">
                <AssignmentTurnedInOutlinedIcon color="primary" />
                <Box>
                  <Typography variant="h6" fontWeight={800}>งานที่ต้องดำเนินการและรายการวันนี้</Typography>
                  <Typography variant="body2" color="text.secondary">คิวงานของคุณและการจองห้องประชุมในวันนี้</Typography>
                </Box>
              </Stack>
              <Typography variant="body2" fontWeight={700} color="primary.main">
                คิวงาน {(actionData!.leaveCount + actionData!.fleetCount + actionData!.repairCount).toLocaleString("th-TH")} รายการ
              </Typography>
            </Stack>
            <Grid container spacing={1.5}>
              {actionCards.map(card => (
                <Grid item xs={12} sm={6} lg={3} key={card.key}>
                  <Box component={RouterLink} to={card.to} aria-label={`${card.title} ${card.count} รายการ`} sx={{ display: "block", height: "100%", p: 1.5, borderRadius: 2, bgcolor: "background.paper", border: `1px solid ${theme.palette.divider}`, color: "inherit", textDecoration: "none", "&:hover, &:focus-visible": { borderColor: "primary.main", boxShadow: `0 6px 18px ${alpha(theme.palette.primary.main, 0.13)}` } }}>
                    <Typography fontWeight={800}>{card.title}</Typography>
                    <Typography variant="h6" color="primary.main" fontWeight={900}>{card.count.toLocaleString("th-TH")} <Typography component="span" variant="body2">{card.key === "meeting" ? "ครั้ง" : "รายการ"}</Typography></Typography>
                    <Typography variant="caption" color="text.secondary">{card.detail}</Typography>
                  </Box>
                </Grid>
              ))}
            </Grid>
            {actionData?.meetingBookings.length ? (
              <Box sx={{ mt: 1.5 }}>
                <Typography variant="caption" color="text.secondary">การจองห้องประชุมวันนี้</Typography>
                <Stack direction="row" gap={1} flexWrap="wrap" sx={{ mt: 0.5 }}>
                  {actionData.meetingBookings.map(booking => <Chip key={booking.id} component={RouterLink} to={booking.path} clickable label={booking.detail} variant="outlined" sx={{ maxWidth: "100%" }} />)}
                </Stack>
              </Box>
            ) : null}
          </CardContent>
        </Card>
      )}

      <Grid container spacing={2.5}>
        {visibleModules.map((module) => {
          const Icon = module.icon;
          const disabled = module.status !== "active";
          const metrics = getDashboardHubMetrics(module, user, data);
          const badge = actionCountForModule(module.key, actionData);

          return (
            <Grid item xs={12} sm={6} lg={4} key={module.key}>
              <Card
                sx={{
                  height: "100%",
                  border: `1px solid ${theme.palette.divider}`,
                  borderTop: `4px solid ${module.status === "active" ? brandColors.accent : alpha(theme.palette.text.secondary, 0.28)}`,
                  boxShadow: `0 18px 38px ${alpha(theme.palette.primary.dark, 0.08)}`,
                }}
              >
                <CardContent>
                  <Stack spacing={2.25} sx={{ height: "100%" }}>
                    <Stack direction="row" spacing={1.5} alignItems="flex-start">
                      <Box
                        sx={{
                          width: 48,
                          height: 48,
                          display: "grid",
                          placeItems: "center",
                          borderRadius: 2.25,
                          bgcolor: alpha(theme.palette.primary.main, 0.08),
                          color: "primary.main",
                          flexShrink: 0,
                        }}
                      >
                        <Icon />
                      </Box>
                      <Box sx={{ flex: 1, minWidth: 0 }}>
                        <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
                          <Typography variant="h6" fontWeight={800}>
                            {module.title}
                          </Typography>
                          {badge > 0 && <Chip size="small" color={module.key === "meeting" ? "info" : "error"} label={module.key === "meeting" ? `วันนี้ ${badge} ครั้ง` : `${badge} ต้องดำเนินการ`} />}
                          {module.status !== "active" && (
                            <Chip
                              size="small"
                              label={module.status === "planned" ? "Planned" : "Coming soon"}
                              color={module.status === "planned" ? "info" : "warning"}
                              variant="outlined"
                            />
                          )}
                        </Stack>
                        <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
                          {module.description}
                        </Typography>
                      </Box>
                    </Stack>

                    <Box
                      sx={{
                        display: "grid",
                        gridTemplateColumns: metrics.length > 1 ? { xs: "1fr", sm: "repeat(2, minmax(0, 1fr))" } : "1fr",
                        gap: 1.25,
                      }}
                    >
                      {metrics.map((metric) => <ModuleMetricBox key={metric.label} {...metric} isLoading={isLoading} />)}
                    </Box>

                    <Box sx={{ mt: "auto" }}>
                      {disabled ? (
                        <Button disabled fullWidth variant="outlined" startIcon={<LockOutlinedIcon />}>
                          ยังไม่เปิดใช้งาน
                        </Button>
                      ) : (
                        <Button
                          component={RouterLink}
                          to={module.route}
                          fullWidth
                          variant="contained"
                          endIcon={<ArrowForwardOutlinedIcon />}
                        >
                          เปิด Dashboard
                        </Button>
                      )}
                    </Box>
                  </Stack>
                </CardContent>
              </Card>
            </Grid>
          );
        })}
      </Grid>

      {visibleModules.length === 0 && (
        <Alert severity="info" sx={{ mt: 2 }}>
          ยังไม่มีแดชบอร์ดที่เปิดให้ใช้งานสำหรับบัญชีของคุณ
        </Alert>
      )}

      <Typography variant="caption" color="text.secondary" sx={{ display: "block", mt: 2.5 }}>
        ระบบที่แสดงทั้งหมด {visibleModules.length.toLocaleString("th-TH")} จาก {dashboardModules.length.toLocaleString("th-TH")} โมดูล
      </Typography>
    </Box>
  );
}

function actionCountForModule(key: string, data?: DashboardActionItems) {
  if (!data) return 0;
  switch (key) {
    case "leave": return data.leaveCount;
    case "vehicle": return data.fleetCount;
    case "repair": return data.repairCount;
    case "meeting": return data.meetingTodayCount;
    default: return 0;
  }
}

function ModuleMetricBox({ label, value, to, isLoading }: { label: string; value: number | string; to?: string; isLoading: boolean }) {
  const theme = useTheme();
  const box = (
    <Box
      sx={{
        p: 1.5,
        minHeight: 104,
        borderRadius: 2,
        bgcolor: alpha(theme.palette.primary.main, 0.045),
        border: `1px solid ${alpha(theme.palette.primary.main, 0.12)}`,
        display: "flex",
        flexDirection: "column",
        justifyContent: "center",
        height: "100%",
      }}
    >
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      {isLoading ? (
        <Skeleton width={96} height={36} />
      ) : (
        <Typography variant="h5" fontWeight={900} color="primary.main">
          {typeof value === "number" ? value.toLocaleString("th-TH") : value}
        </Typography>
      )}
    </Box>
  );
  return to ? (
    <Box component={RouterLink} to={to} aria-label={`${label} ${value}`} sx={{ textDecoration: "none", color: "inherit", borderRadius: 2, "&:focus-visible": { outline: `3px solid ${theme.palette.primary.main}`, outlineOffset: 2 }, "&:hover > *": { borderColor: theme.palette.primary.main, bgcolor: alpha(theme.palette.primary.main, 0.085) } }}>
      {box}
    </Box>
  ) : box;
}
