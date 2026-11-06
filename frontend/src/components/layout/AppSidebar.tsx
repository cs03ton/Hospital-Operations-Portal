import ChevronLeftOutlinedIcon from "@mui/icons-material/ChevronLeftOutlined";
import ChevronRightOutlinedIcon from "@mui/icons-material/ChevronRightOutlined";
import { Box, Divider, Drawer, IconButton, List, Stack, Toolbar, Tooltip, Typography, useMediaQuery } from "@mui/material";
import { alpha, useTheme } from "@mui/material/styles";
import { useLocation } from "react-router-dom";
import hospitalLogo from "../../assets/logo/hospital-logo.png";
import { appName, hospitalName } from "../../config/appConfig";
import HomeOutlinedIcon from "@mui/icons-material/HomeOutlined";
import GroupsOutlinedIcon from "@mui/icons-material/GroupsOutlined";
import SettingsOutlinedIcon from "@mui/icons-material/SettingsOutlined";
import { SidebarSection } from "./SidebarSection";
import { ModuleMenuItem } from "./ModuleMenuItem";
import { leaveNavigationBadgeCount, navigationModules } from "../../config/menuConfig";
import { effectiveFleetPermissions, fleetNavigationBadgeCount } from "../../config/fleetNavigation";
import { useAuth } from "../../context/AuthContext";
import { usePermission } from "../../context/PermissionContext";
import { useModuleMenuState } from "../../hooks/useModuleMenuState";
import { isItemActive, ModuleMenuGroup } from "./ModuleMenuGroup";
import { useQuery } from "@tanstack/react-query";
import { FLEET_DASHBOARD_QUERY_KEY, getFleetDashboard, getFleetRolloutAccess } from "../../api/fleetApi";
import { dashboardPollingOptions } from "../../config/queryPolling";
import { getMyPendingApprovalCount } from "../../api/leaveApi";
import { repairSummary, repairWorkPermissions } from "../../api/repairApi";

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
  const { permissions, hasPermission, hasAnyPermission } = usePermission();
  const location = useLocation();
  const canViewPendingApprovals =
    !["Admin", "SuperAdmin"].includes(user?.role ?? "") &&
    hasAnyPermission(["LeaveRequest.ViewPendingApproval", "LeaveApproval.ApproveCurrentStep"]);
  const pendingApprovalCount = useQuery({
    queryKey: ["approvals", "my-pending", "count"],
    queryFn: getMyPendingApprovalCount,
    enabled: canViewPendingApprovals,
    ...dashboardPollingOptions,
    retry: false,
  });
  const fleetRollout = useQuery({ queryKey: ["fleet-rollout-access"], queryFn: getFleetRolloutAccess, staleTime: 60_000, retry: 1 });
  const fleetDashboard = useQuery({
    queryKey: FLEET_DASHBOARD_QUERY_KEY,
    queryFn: getFleetDashboard,
    enabled: fleetRollout.data?.isAllowed === true,
    ...dashboardPollingOptions,
    retry: false,
  });
  const fleetPermissions = effectiveFleetPermissions(permissions, fleetDashboard.data?.capabilities);
  const repairs = useQuery({ queryKey: ["repairs", "summary"], queryFn: repairSummary,
    enabled: hasAnyPermission(repairWorkPermissions), ...dashboardPollingOptions, retry: false });
  const visibleModules = navigationModules
    .filter((module) => module.enabled && (module.moduleId !== "VehicleBooking" || fleetRollout.data?.isAllowed === true))
    .map((module) => ({
      ...module,
      children: module.children.filter((item) => {
        const roleAllowed = item.allowedRoles?.includes(user?.role ?? "") ?? false;
        if (item.hiddenForRoles?.includes(user?.role ?? "")) {
          return false;
        }

        if (module.moduleId === "VehicleBooking") {
          if (item.permissions?.length) return item.permissions.some(permission => fleetPermissions.has(permission));
          return !item.permission || fleetPermissions.has(item.permission);
        }

        if (item.permissions?.length) {
          return roleAllowed || hasAnyPermission(item.permissions);
        }

        return roleAllowed || !item.permission || hasPermission(item.permission);
      }).map(item => {
        if (module.moduleId === "RepairManagement" && item.path === "/repairs")
          return { ...item, badgeCount: repairs.data?.teamPending ?? 0 };
        if (module.moduleId === "VehicleBooking") {
          return { ...item, badgeCount: fleetNavigationBadgeCount(item.path, fleetDashboard.data?.badges) };
        }

        if (module.moduleId === "LeaveManagement") {
          return { ...item, badgeCount: leaveNavigationBadgeCount(item.path, pendingApprovalCount.data?.total) };
        }

        return item;
      }),
    }))
    .filter((module) => (!module.permission || hasPermission(module.permission)) && module.children.length > 0);
  const activeModule = visibleModules.find((module) =>
    module.children.some((item) => isItemActive(location.pathname, item)),
  );
  const moduleMenu = useModuleMenuState(
    visibleModules.map((module) => module.moduleId), activeModule?.moduleId, undefined, location.pathname,
  );
  const collapsed = isCollapsed && isDesktop;
  const home = visibleModules.find(m => m.moduleId === "DashboardPortal")?.children.find(item => item.path === "/dashboard");
  const serviceIds = ["AnnouncementCenter", "LeaveManagement", "VehicleBooking", "MeetingRoomBooking", "RepairManagement", "Correspondence"];
  const services = serviceIds.flatMap(id => visibleModules.filter(m => m.moduleId === id)).map(m => m.moduleId === "VehicleBooking" ? { ...m, moduleLabel: "จองรถราชการ" } : m);
  const adminDashboard = visibleModules.filter(m => m.moduleId === "DashboardPortal").map(m => ({ ...m, moduleId: "AdminDashboard", moduleLabel: "แดชบอร์ดผู้ดูแล", children: m.children.filter(item => item.path !== "/dashboard") })).filter(m => m.children.length > 0);
  const settings = [...adminDashboard, ...visibleModules.filter(m => ["UserManagement", "SystemManagement"].includes(m.moduleId))];
  const serviceActive = services.some(m => m.children.some(item => isItemActive(location.pathname, item)));
  const settingsActive = settings.some(m => m.children.some(item => isItemActive(location.pathname, item)));
  const sections = useModuleMenuState([...(services.length ? ["StaffServices"] : []), ...(settings.length ? ["PortalSettings"] : [])], serviceActive ? "StaffServices" : settingsActive ? "PortalSettings" : undefined, "hop.sidebar.openSections.v1", location.pathname);
  const renderModule = (module: typeof visibleModules[number]) => <ModuleMenuGroup key={module.moduleId} module={module} activePath={location.pathname}
    isActive={module.children.some(item => isItemActive(location.pathname, item))}
    isOpen={module.moduleId === "AdminDashboard" ? moduleMenu.isModuleOpen("DashboardPortal") : moduleMenu.isModuleOpen(module.moduleId)}
    isCollapsed={collapsed} onToggle={() => moduleMenu.toggleModule(module.moduleId === "AdminDashboard" ? "DashboardPortal" : module.moduleId)}
    onExpandSidebar={onExpandSidebar} onNavigate={onClose} />;

  const drawer = (
    <Box sx={{ height: "100%", bgcolor: "primary.main", color: "primary.contrastText", display: "flex", flexDirection: "column", overflowX: "hidden" }}>
      <Toolbar sx={{ px: collapsed ? 1 : 2, py: collapsed ? 2 : 3.5, minHeight: "auto", flexShrink: 0 }}>
        <Tooltip title={collapsed ? appName : ""} placement="right"><Stack spacing={2} alignItems="center" sx={{ width: "100%", minWidth: 0, textAlign: "center" }}>
          {collapsed ? <Box component="img" src={hospitalLogo} alt={hospitalName} sx={{ width: 38, height: 44, flexShrink: 0, objectFit: "contain" }} /> :
            <Box component="svg" role="img" aria-label={hospitalName} viewBox="120 127 460 574" sx={{ width: 154, height: 134, flexShrink: 0 }}>
              {/* Frame the visible emblem without the source PNG's transparent margins. */}
              <image href={hospitalLogo} width="700" height="785" />
            </Box>}
          {!collapsed && <Box><Typography fontSize="1.05rem" fontWeight={900} sx={{ color: "common.white", lineHeight: 1.35 }}>{appName}</Typography><Typography variant="caption" sx={{ display: "block", mt: .25, color: alpha(theme.palette.common.white, .85), lineHeight: 1.5 }}>ระบบบริหารงาน{hospitalName}</Typography></Box>}
        </Stack></Tooltip>
      </Toolbar>
      <Divider sx={{ borderColor: alpha(theme.palette.common.white, 0.14) }} />
      <Box sx={{ flex: 1, overflowY: "auto", px: collapsed ? 1 : 1.5, py: 1.5 }}>
        <List disablePadding>
          {home && <ModuleMenuItem item={{ ...home, label: "หน้าหลัก", icon: HomeOutlinedIcon }} isActive={isItemActive(location.pathname, home)} isCollapsed={collapsed} topLevel onClick={onClose} />}
          {services.length > 0 && <SidebarSection id="staff" label="บริการเจ้าหน้าที่" icon={GroupsOutlinedIcon} open={sections.isModuleOpen("StaffServices")} active={serviceActive} collapsed={collapsed} onToggle={() => sections.toggleModule("StaffServices")} onExpand={onExpandSidebar}>
            {services.map(renderModule)}
          </SidebarSection>}
          {settings.length > 0 && <SidebarSection id="settings" label="ตั้งค่าระบบ" icon={SettingsOutlinedIcon} open={sections.isModuleOpen("PortalSettings")} active={settingsActive} collapsed={collapsed} onToggle={() => sections.toggleModule("PortalSettings")} onExpand={onExpandSidebar}>
            {settings.map(renderModule)}
          </SidebarSection>}
          {visibleModules.filter(m => m.moduleId === "HelpCenter").map(renderModule)}
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
