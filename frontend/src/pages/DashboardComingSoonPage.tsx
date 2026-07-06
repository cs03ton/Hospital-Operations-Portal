import ArrowBackOutlinedIcon from "@mui/icons-material/ArrowBackOutlined";
import ConstructionOutlinedIcon from "@mui/icons-material/ConstructionOutlined";
import { Box, Button, Card, CardContent, Chip, Stack, Typography } from "@mui/material";
import { alpha, useTheme } from "@mui/material/styles";
import { Link as RouterLink } from "react-router-dom";
import { PageHeader } from "../components/PageHeader";
import type { DashboardModuleDefinition } from "../config/dashboardModules";
import { brandColors } from "../theme/theme";

type DashboardComingSoonPageProps = {
  module: DashboardModuleDefinition;
};

export function DashboardComingSoonPage({ module }: DashboardComingSoonPageProps) {
  const theme = useTheme();
  const Icon = module.icon;

  return (
    <Box>
      <PageHeader title={module.title} subtitle="แดชบอร์ดนี้อยู่ระหว่างเตรียมเปิดใช้งาน" />
      <Button
        component={RouterLink}
        to="/dashboard"
        variant="outlined"
        startIcon={<ArrowBackOutlinedIcon />}
        sx={{ mb: 2 }}
      >
        กลับไป Dashboard Hub
      </Button>

      <Card
        sx={{
          border: `1px solid ${theme.palette.divider}`,
          borderTop: `4px solid ${brandColors.accent}`,
          boxShadow: `0 18px 38px ${alpha(theme.palette.primary.dark, 0.08)}`,
        }}
      >
        <CardContent>
          <Stack spacing={2.5} alignItems="flex-start">
            <Box
              sx={{
                width: 64,
                height: 64,
                display: "grid",
                placeItems: "center",
                borderRadius: 3,
                color: "primary.main",
                bgcolor: alpha(theme.palette.primary.main, 0.08),
              }}
            >
              <Icon fontSize="large" />
            </Box>
            <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
              <Typography variant="h5" fontWeight={900}>
                {module.title}
              </Typography>
              <Chip
                icon={<ConstructionOutlinedIcon />}
                label={module.status === "planned" ? "Planned" : "Coming soon"}
                color={module.status === "planned" ? "info" : "warning"}
                variant="outlined"
              />
            </Stack>
            <Typography color="text.secondary" sx={{ maxWidth: 720 }}>
              {module.description}
            </Typography>
            <Typography color="text.secondary">
              ระบบจะแสดงข้อมูลจริงเมื่อโมดูลนี้เปิดใช้งานใน Phase ถัดไป โดยยังคงใช้ Dashboard Hub และ permission model เดียวกัน
            </Typography>
          </Stack>
        </CardContent>
      </Card>
    </Box>
  );
}
