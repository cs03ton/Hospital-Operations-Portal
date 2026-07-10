import ChevronLeftOutlinedIcon from "@mui/icons-material/ChevronLeftOutlined";
import ChevronRightOutlinedIcon from "@mui/icons-material/ChevronRightOutlined";
import { Box, Divider, Drawer, IconButton, List, Stack, Toolbar, Tooltip, Typography, useMediaQuery } from "@mui/material";
import { alpha, useTheme } from "@mui/material/styles";
import { useLocation } from "react-router-dom";
import hospitalLogo from "../../assets/logo/hospital-logo.png";
import { appName, hospitalName } from "../../config/appConfig";
import { navigationModules } from "../../config/menuConfig";
import { useAuth } from "../../context/AuthContext";
import { usePermission } from "../../context/PermissionContext";
import { useModuleMenuState } from "../../hooks/useModuleMenuState";
import { isItemActive, ModuleMenuGroup } from "./ModuleMenuGroup";

type AppSidebarProps = {
  drawerWidth: number;
  expandedDrawerWidth: number;
  isCollapsed: boolean;
  mobileOpen: boolean;
  onClose: () => void;
  onExpandSidebar: () => void;
  onToggleCollapse: () => void;
};

export function AppSidebar({
  drawerWidth,
  expandedDrawerWidth,
  isCollapsed,
  mobileOpen,
  onClose,
  onExpandSidebar,
  onToggleCollapse,
}: AppSidebarProps) {
  const theme = useTheme();
  const isDesktop = useMediaQuery(theme.breakpoints.up("md"));
  const { user } = useAuth();
  const { hasPermission, hasAnyPermission } = usePermission();
  const location = useLocation();
  const visibleModules = navigationModules
    .filter((module) => module.enabled)
    .map((module) => ({
      ...module,
      children: module.children.filter((item) => {
        const roleAllowed = item.allowedRoles?.includes(user?.role ?? "") ?? false;
        if (item.hiddenForRoles?.includes(user?.role ?? "")) {
          return false;
        }

        if (item.permissions?.length) {
          return roleAllowed || hasAnyPermission(item.permissions);
        }

        return roleAllowed || !item.permission || hasPermission(item.permission);
      }),
    }))
    .filter((module) => (!module.permission || hasPermission(module.permission)) && module.children.length > 0);
  const activeModule = visibleModules.find((module) =>
    module.children.some((item) => isItemActive(location.pathname, item)),
  );
  const moduleMenu = useModuleMenuState(
    visibleModules.map((module) => module.moduleId),
    activeModule?.moduleId,
  );

  const drawer = (
    <Box
      sx={(theme) => ({
        height: "100%",
        bgcolor: "primary.main",
        color: "primary.contrastText",
        display: "flex",
        flexDirection: "column",
        overflowX: "hidden",
        transition: theme.transitions.create("width", {
          duration: theme.transitions.duration.shorter,
          easing: theme.transitions.easing.easeInOut,
        }),
      })}
    >
      <Toolbar sx={{ px: isCollapsed && isDesktop ? 1 : 2.5, py: isCollapsed && isDesktop ? 1.5 : 2.25, minHeight: "auto" }}>
        <Tooltip title={isCollapsed && isDesktop ? `${appName} - ${hospitalName}` : ""} placement="right">
          <Stack spacing={1} alignItems="center" sx={{ width: "100%", textAlign: "center" }}>
            <Box
              component="img"
              src={hospitalLogo}
              alt={hospitalName}
              sx={{
                width: isCollapsed && isDesktop ? 44 : 86,
                height: isCollapsed && isDesktop ? 44 : 86,
                objectFit: "contain",
                transition: theme.transitions.create(["width", "height"], {
                  duration: theme.transitions.duration.shorter,
                  easing: theme.transitions.easing.easeInOut,
                }),
              }}
            />
            {(!isCollapsed || !isDesktop) && (
              <Box>
                <Typography variant="subtitle1" fontWeight={800} sx={{ lineHeight: 1.25, color: "common.white" }}>
                  {appName}
                </Typography>
                <Typography variant="caption" sx={{ display: "block", mt: 0.5, color: alpha(theme.palette.common.white, 0.72) }}>
                  ระบบบริหารงาน{hospitalName}
                </Typography>
              </Box>
            )}
          </Stack>
        </Tooltip>
      </Toolbar>
      <Divider sx={{ borderColor: alpha(theme.palette.common.white, 0.14) }} />
      <Box sx={{ flex: 1, overflowY: "auto", px: isCollapsed && isDesktop ? 1 : 1.5, py: 1.5 }}>
        <List disablePadding>
          {visibleModules.map((module) => (
            <ModuleMenuGroup
              key={module.moduleId}
              module={module}
              activePath={location.pathname}
              isActive={module.children.some((item) => isItemActive(location.pathname, item))}
              isOpen={moduleMenu.isModuleOpen(module.moduleId)}
              isCollapsed={isCollapsed && isDesktop}
              onToggle={() => moduleMenu.toggleModule(module.moduleId)}
              onExpandSidebar={onExpandSidebar}
              onNavigate={onClose}
            />
          ))}
        </List>
        {visibleModules.length === 0 && (
          <Box sx={{ px: 1.5, py: 2 }}>
            <Typography variant="body2" sx={{ color: alpha(theme.palette.common.white, 0.72) }}>
              ไม่มีเมนูที่ได้รับสิทธิ์
            </Typography>
          </Box>
        )}
      </Box>
      {isDesktop && (
        <>
          <Divider sx={{ borderColor: alpha(theme.palette.common.white, 0.14) }} />
          <Box sx={{ p: 1 }}>
            <Tooltip title={isCollapsed ? "ขยายเมนู" : "พับเมนู"} placement="right">
              <IconButton
                onClick={onToggleCollapse}
                aria-label={isCollapsed ? "ขยายเมนู" : "พับเมนู"}
                sx={{
                  width: "100%",
                  borderRadius: 2,
                  justifyContent: isCollapsed ? "center" : "flex-end",
                  color: alpha(theme.palette.common.white, 0.78),
                  "&:hover": {
                    bgcolor: alpha(theme.palette.common.white, 0.1),
                    color: "common.white",
                  },
                }}
              >
                {isCollapsed ? <ChevronRightOutlinedIcon /> : <ChevronLeftOutlinedIcon />}
              </IconButton>
            </Tooltip>
          </Box>
        </>
      )}
    </Box>
  );

  return (
    <Box
      component="nav"
      sx={(theme) => ({
        width: { md: drawerWidth },
        flexShrink: { md: 0 },
        transition: theme.transitions.create("width", {
          duration: theme.transitions.duration.shorter,
          easing: theme.transitions.easing.easeInOut,
        }),
      })}
    >
      <Drawer
        variant={isDesktop ? "permanent" : "temporary"}
        open={isDesktop || mobileOpen}
        onClose={onClose}
        ModalProps={{ keepMounted: true }}
        sx={{
          "& .MuiDrawer-paper": {
            width: { xs: expandedDrawerWidth, md: drawerWidth },
            boxSizing: "border-box",
            borderRight: "none",
            boxShadow: isDesktop
              ? `8px 0 26px ${alpha(theme.palette.primary.dark, 0.18)}`
              : `16px 0 38px ${alpha(theme.palette.primary.dark, 0.28)}`,
            transition: theme.transitions.create("width", {
              duration: theme.transitions.duration.shorter,
              easing: theme.transitions.easing.easeInOut,
            }),
          },
        }}
      >
        {drawer}
      </Drawer>
    </Box>
  );
}
