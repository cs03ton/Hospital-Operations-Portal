import ArrowForwardOutlinedIcon from "@mui/icons-material/ArrowForwardOutlined";
import LockOutlinedIcon from "@mui/icons-material/LockOutlined";
import { Alert, Box, Button, Card, CardContent, Chip, Grid, Skeleton, Stack, Typography } from "@mui/material";
import { alpha, useTheme } from "@mui/material/styles";
import { useQuery } from "@tanstack/react-query";
import { Link as RouterLink } from "react-router-dom";
import { getDashboardSummary } from "../api/adminApi";
import { PageHeader } from "../components/PageHeader";
import { dashboardModules, getDashboardModuleMetricLabel, getVisibleDashboardModules } from "../config/dashboardModules";
import { hospitalName } from "../config/appConfig";
import { useAuth } from "../context/AuthContext";
import { brandColors } from "../theme/theme";

export function DashboardHubPage() {
  const { user } = useAuth();
  const theme = useTheme();
  const visibleModules = getVisibleDashboardModules(user);
  const { data, isError, isLoading } = useQuery({
    queryKey: ["dashboard-summary", "hub"],
    queryFn: getDashboardSummary,
  });

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

      <Grid container spacing={2.5}>
        {visibleModules.map((module) => {
          const Icon = module.icon;
          const disabled = module.status !== "active";
          const metric = module.metricSelector(data);
          const metricLabel = getDashboardModuleMetricLabel(module, user);

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
                        gridTemplateColumns: module.key === "leave" ? { xs: "1fr", sm: "repeat(2, minmax(0, 1fr))" } : "1fr",
                        gap: 1.25,
                      }}
                    >
                      <ModuleMetricBox label={metricLabel} value={metric} isLoading={isLoading} />
                      {module.key === "leave" && (
                        <ModuleMetricBox
                          label="คำขอยกเลิกใบลา"
                          value={data?.myLeaveCancellationRequestsPending ?? 0}
                          isLoading={isLoading}
                        />
                      )}
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

function ModuleMetricBox({ label, value, isLoading }: { label: string; value: number | string; isLoading: boolean }) {
  const theme = useTheme();
  return (
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
}
