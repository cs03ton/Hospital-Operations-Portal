import { Box } from "@mui/material";
import { Outlet } from "react-router-dom";
import { AppFooter } from "./AppFooter";
import { AppHeader } from "./AppHeader";
import { AppSidebar } from "./AppSidebar";
import { useSidebarState } from "../../hooks/useSidebarState";

const expandedDrawerWidth = 292;
const collapsedDrawerWidth = 84;

export function AppShell() {
  const sidebar = useSidebarState();
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
        })}
      >
        <Box sx={{ width: "100%", maxWidth: 1440, mx: "auto" }}>
          <Outlet />
          <AppFooter />
        </Box>
      </Box>
    </Box>
  );
}
