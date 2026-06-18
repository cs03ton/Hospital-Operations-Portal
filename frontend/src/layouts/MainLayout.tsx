import LogoutOutlinedIcon from "@mui/icons-material/LogoutOutlined";
import MenuIcon from "@mui/icons-material/Menu";
import {
  AppBar,
  Box,
  Button,
  Divider,
  Drawer,
  IconButton,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Toolbar,
  Typography,
  useMediaQuery,
} from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { useState } from "react";
import { NavLink, Outlet, useNavigate } from "react-router-dom";
import hospitalLogo from "../assets/logo/hospital-logo.png";
import { appName, hospitalName } from "../config/appConfig";
import { useAuth } from "../context/AuthContext";
import { usePermission } from "../context/PermissionContext";
import { navigationItems } from "../routes/navigation";

const drawerWidth = 292;

export function MainLayout() {
  const theme = useTheme();
  const isDesktop = useMediaQuery(theme.breakpoints.up("md"));
  const [isOpen, setIsOpen] = useState(false);
  const { logout, user } = useAuth();
  const { hasPermission } = usePermission();
  const navigate = useNavigate();
  const visibleNavigationItems = navigationItems.filter(
    (item) => !item.permission || hasPermission(item.permission),
  );

  async function handleLogout() {
    await logout();
    navigate("/login", { replace: true });
  }

  const drawer = (
    <Box sx={{ height: "100%", bgcolor: "background.paper" }}>
      <Toolbar sx={{ px: 2.5, minHeight: 76 }}>
        <Box sx={{ display: "flex", alignItems: "center", gap: 1.5, minWidth: 0 }}>
          <Box
            component="img"
            src={hospitalLogo}
            alt={hospitalName}
            sx={{ width: 48, height: 48, objectFit: "contain", flexShrink: 0 }}
          />
          <Box sx={{ minWidth: 0 }}>
            <Typography variant="h6" color="primary" sx={{ lineHeight: 1.2 }}>
              {appName}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              ระบบบริหารงาน{hospitalName}
            </Typography>
          </Box>
        </Box>
      </Toolbar>
      <Divider />
      <List sx={{ px: 1.5, py: 2 }}>
        {visibleNavigationItems.map((item) => {
          const Icon = item.icon;

          return (
            <ListItemButton
              key={item.path}
              component={NavLink}
              to={item.path}
              onClick={() => setIsOpen(false)}
              sx={{
                mb: 0.5,
                borderRadius: 2,
                color: "text.secondary",
                minHeight: 44,
                "&.active": {
                  bgcolor: "primary.main",
                  color: "primary.contrastText",
                  "& .MuiListItemIcon-root": {
                    color: "primary.contrastText",
                  },
                },
              }}
            >
              <ListItemIcon sx={{ minWidth: 40, color: "inherit" }}>
                <Icon fontSize="small" />
              </ListItemIcon>
              <ListItemText primary={item.label} />
            </ListItemButton>
          );
        })}
      </List>
    </Box>
  );

  return (
    <Box sx={{ display: "flex", minHeight: "100vh", bgcolor: "background.default" }}>
      <AppBar
        position="fixed"
        elevation={0}
        sx={{
          bgcolor: "background.paper",
          color: "text.primary",
          borderBottom: "1px solid #DCE7DD",
          width: { md: `calc(100% - ${drawerWidth}px)` },
          ml: { md: `${drawerWidth}px` },
        }}
      >
        <Toolbar sx={{ gap: 2, minHeight: 72 }}>
          {!isDesktop && (
            <IconButton color="inherit" edge="start" onClick={() => setIsOpen(true)} sx={{ mr: 0.5 }}>
              <MenuIcon />
            </IconButton>
          )}
          <Box
            component="img"
            src={hospitalLogo}
            alt={hospitalName}
            sx={{ width: 40, height: 40, objectFit: "contain" }}
          />
          <Box sx={{ flexGrow: 1, minWidth: 0 }}>
            <Typography variant="h6">พื้นที่ปฏิบัติงาน</Typography>
            <Typography variant="caption" color="text.secondary">
              {user?.fullname} · {user?.role}
            </Typography>
          </Box>
          <Button
            color="primary"
            variant="outlined"
            startIcon={<LogoutOutlinedIcon />}
            onClick={handleLogout}
            sx={{ flexShrink: 0 }}
          >
            ออกจากระบบ
          </Button>
        </Toolbar>
      </AppBar>

      <Box component="nav" sx={{ width: { md: drawerWidth }, flexShrink: { md: 0 } }}>
        <Drawer
          variant={isDesktop ? "permanent" : "temporary"}
          open={isDesktop || isOpen}
          onClose={() => setIsOpen(false)}
          ModalProps={{ keepMounted: true }}
          sx={{
            "& .MuiDrawer-paper": {
              width: drawerWidth,
              boxSizing: "border-box",
              borderRight: "1px solid #DCE7DD",
            },
          }}
        >
          {drawer}
        </Drawer>
      </Box>

      <Box component="main" sx={{ flexGrow: 1, p: { xs: 2, md: 3 }, mt: 9, minWidth: 0 }}>
        <Outlet />
      </Box>
    </Box>
  );
}
