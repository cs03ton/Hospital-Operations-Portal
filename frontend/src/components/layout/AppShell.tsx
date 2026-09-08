import AutoAwesomeOutlinedIcon from "@mui/icons-material/AutoAwesomeOutlined";
import DirectionsCarFilledOutlinedIcon from "@mui/icons-material/DirectionsCarFilledOutlined";
import { Box, Chip, Stack, Typography } from "@mui/material";
import { alpha } from "@mui/material/styles";
import { Outlet, useLocation } from "react-router-dom";
import { AppFooter } from "./AppFooter";
import { AppHeader } from "./AppHeader";
import { AppSidebar } from "./AppSidebar";
import { useSidebarState } from "../../hooks/useSidebarState";
import { brandColors } from "../../theme/theme";

const expandedDrawerWidth = 292;
const collapsedDrawerWidth = 84;

export function AppShell() {
  const sidebar = useSidebarState();
  const location = useLocation();
  const isFleetModule = location.pathname.startsWith("/fleet");
  const drawerWidth = sidebar.isCollapsed ? collapsedDrawerWidth : expandedDrawerWidth;

  return (
    <Box sx={{ display: "flex", minHeight: "100vh", bgcolor: "background.default" }}>
      <AppHeader
        drawerWidth={drawerWidth}
        isSidebarCollapsed={sidebar.isCollapsed}
        onMobileMenuClick={sidebar.openMobileSidebar}
        onToggleSidebar={sidebar.toggleSidebar}
      />
      <AppSidebar
        drawerWidth={drawerWidth}
        expandedDrawerWidth={expandedDrawerWidth}
        isCollapsed={sidebar.isCollapsed}
        mobileOpen={sidebar.isMobileOpen}
        onClose={sidebar.closeMobileSidebar}
        onExpandSidebar={sidebar.expandSidebar}
        onToggleCollapse={sidebar.toggleSidebar}
      />

      <Box
        component="main"
        className={isFleetModule ? "fleet-module-shell" : undefined}
        sx={(theme) => ({
          flexGrow: 1,
          minWidth: 0,
          mt: 9,
          px: { xs: 2, sm: 2.5, md: 3.5 },
          py: { xs: 2, md: 3 },
          transition: theme.transitions.create(["padding", "margin"], {
            duration: theme.transitions.duration.shorter,
            easing: theme.transitions.easing.easeInOut,
          }),
          ...(isFleetModule && fleetModuleStyles),
        })}
      >
        <Box sx={{ width: "100%", maxWidth: 1440, mx: "auto", position: "relative", zIndex: 1 }}>
          {isFleetModule && <FleetModuleChrome />}
          <Box className={isFleetModule ? "fleet-module-content" : undefined}>
            <Outlet />
          </Box>
          <AppFooter />
        </Box>
      </Box>
    </Box>
  );
}

const fleetModuleStyles = {
  position: "relative",
  isolation: "isolate",
  background: `
    radial-gradient(circle at 92% 3%, ${alpha(brandColors.accentSoft, 0.2)} 0, transparent 25%),
    radial-gradient(circle at 8% 22%, ${alpha(brandColors.primaryLight, 0.08)} 0, transparent 24%),
    linear-gradient(180deg, ${brandColors.background} 0%, #F8F6EF 100%)
  `,
  "&::before": {
    content: '""',
    position: "fixed",
    zIndex: -1,
    width: 260,
    height: 260,
    right: -110,
    top: 120,
    borderRadius: "50%",
    border: `42px solid ${alpha(brandColors.accent, 0.06)}`,
    pointerEvents: "none",
  },
  "& .fleet-module-content": {
    animation: "fleetPageEnter 360ms ease-out both",
  },
  "& .hop-page-header": {
    position: "relative",
    overflow: "hidden",
    p: { xs: 2, md: 2.5 },
    mb: 2.5,
    border: `1px solid ${alpha(brandColors.accent, 0.42)}`,
    borderBottom: `3px solid ${brandColors.accent}`,
    borderRadius: "18px",
    background: `linear-gradient(120deg, #FFFFFF 0%, ${alpha(brandColors.accentSoft, 0.18)} 68%, ${alpha(brandColors.primaryLight, 0.06)} 100%)`,
    boxShadow: `0 16px 38px ${alpha(brandColors.primaryDark, 0.08)}`,
  },
  "& .hop-page-header::after": {
    content: '""',
    position: "absolute",
    width: 120,
    height: 120,
    right: -35,
    top: -55,
    borderRadius: "50%",
    background: alpha(brandColors.accent, 0.12),
    pointerEvents: "none",
  },
  "& .hop-page-title": {
    letterSpacing: "-0.02em",
    textShadow: `0 2px 12px ${alpha(brandColors.primary, 0.08)}`,
  },
  "& .MuiCard-root": {
    background: `linear-gradient(145deg, #FFFFFF 0%, ${alpha(brandColors.background, 0.72)} 100%)`,
    borderColor: alpha(brandColors.accent, 0.36),
    boxShadow: `0 12px 30px ${alpha(brandColors.primaryDark, 0.07)}`,
    transition: "transform 180ms ease, box-shadow 180ms ease, border-color 180ms ease",
  },
  "& .MuiCard-root:hover": {
    borderColor: alpha(brandColors.accent, 0.7),
    boxShadow: `0 16px 36px ${alpha(brandColors.primaryDark, 0.1)}`,
  },
  "& .MuiButton-root": {
    minHeight: 42,
    transition: "transform 160ms ease, box-shadow 160ms ease, background-color 160ms ease",
  },
  "& .MuiButton-root:hover": {
    transform: "translateY(-1px)",
  },
  "& .MuiButton-containedPrimary": {
    background: `linear-gradient(135deg, ${brandColors.primary} 0%, ${brandColors.primaryDark} 100%)`,
    boxShadow: `0 10px 22px ${alpha(brandColors.primary, 0.2)}`,
  },
  "& .MuiButton-outlined": {
    bgcolor: alpha("#FFFFFF", 0.72),
    backdropFilter: "blur(8px)",
  },
  "& .MuiOutlinedInput-root": {
    transition: "box-shadow 160ms ease, background-color 160ms ease",
  },
  "& .MuiOutlinedInput-root.Mui-focused": {
    boxShadow: `0 0 0 4px ${alpha(brandColors.primary, 0.08)}`,
  },
  "& .MuiTableContainer-root": {
    border: `1px solid ${brandColors.border}`,
    borderRadius: "14px",
    overflow: "hidden",
  },
  "& .MuiTableCell-head": {
    background: `linear-gradient(180deg, ${alpha(brandColors.accentSoft, 0.2)} 0%, ${brandColors.background} 100%)`,
  },
  "& .MuiAlert-root": {
    borderRadius: "14px",
    border: "1px solid",
    borderColor: alpha(brandColors.accent, 0.3),
    alignItems: "center",
  },
  "& .MuiChip-root": {
    transition: "transform 150ms ease, box-shadow 150ms ease",
  },
  "& .MuiChip-root:hover": {
    transform: "translateY(-1px)",
  },
  "@keyframes fleetPageEnter": {
    from: { opacity: 0, transform: "translateY(7px)" },
    to: { opacity: 1, transform: "translateY(0)" },
  },
} as const;

function FleetModuleChrome() {
  return (
    <Stack direction={{ xs: "column", sm: "row" }} justifyContent="space-between" alignItems={{ xs: "flex-start", sm: "center" }} spacing={1} sx={{ mb: 1.5 }}>
      <Stack direction="row" spacing={1} alignItems="center">
        <Box sx={{ width: 38, height: 38, borderRadius: 2, display: "grid", placeItems: "center", color: "primary.contrastText", background: `linear-gradient(135deg, ${brandColors.primaryLight}, ${brandColors.primaryDark})`, boxShadow: `0 8px 18px ${alpha(brandColors.primary, 0.2)}` }}>
          <DirectionsCarFilledOutlinedIcon fontSize="small" />
        </Box>
        <Box>
          <Typography variant="overline" color="primary" fontWeight={900} lineHeight={1.2}>Fleet Operations</Typography>
          <Typography variant="caption" color="text.secondary" display="block">ระบบจองและบริหารยานพาหนะ</Typography>
        </Box>
      </Stack>
      <Chip icon={<AutoAwesomeOutlinedIcon />} label="พร้อมให้บริการ" size="small" sx={{ bgcolor: alpha(brandColors.accent, 0.14), color: "primary.dark", border: `1px solid ${alpha(brandColors.accent, 0.45)}`, "& .MuiChip-icon": { color: brandColors.accent } }} />
    </Stack>
  );
}
