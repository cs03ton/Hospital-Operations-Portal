import LogoutOutlinedIcon from "@mui/icons-material/LogoutOutlined";
import LockResetOutlinedIcon from "@mui/icons-material/LockResetOutlined";
import MenuIcon from "@mui/icons-material/Menu";
import MenuOpenOutlinedIcon from "@mui/icons-material/MenuOpenOutlined";
import PersonOutlineOutlinedIcon from "@mui/icons-material/PersonOutlineOutlined";
import { AppBar, Avatar, Box, Button, IconButton, Menu, MenuItem, Stack, Toolbar, Tooltip, Typography, useMediaQuery } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { useMemo, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { getPageTitle } from "../../config/pageTitleConfig";
import { useAuth } from "../../context/AuthContext";
import { brandColors } from "../../theme/theme";
import { toAbsoluteMediaUrl } from "../../utils/mediaUrl";
import { NotificationBell } from "../notifications/NotificationBell";
import { PageBreadcrumbs } from "./PageBreadcrumbs";
import { PageTitle } from "./PageTitle";

type AppHeaderProps = {
  drawerWidth: number;
  isSidebarCollapsed: boolean;
  onMobileMenuClick: () => void;
  onToggleSidebar: () => void;
};

export function AppHeader({ drawerWidth, isSidebarCollapsed, onMobileMenuClick, onToggleSidebar }: AppHeaderProps) {
  const theme = useTheme();
  const isDesktop = useMediaQuery(theme.breakpoints.up("md"));
  const location = useLocation();
  const navigate = useNavigate();
  const { logout, user } = useAuth();
  const [userMenuAnchor, setUserMenuAnchor] = useState<HTMLElement | null>(null);
  const pageTitle = useMemo(() => getPageTitle(location.pathname), [location.pathname]);

  async function handleLogout() {
    await logout();
    navigate("/login", { replace: true });
  }

  return (
    <AppBar
      position="fixed"
      elevation={0}
      sx={(theme) => ({
        bgcolor: "background.paper",
        color: "text.primary",
        borderBottom: "1px solid",
        borderColor: "divider",
        backdropFilter: "blur(14px)",
        width: { md: `calc(100% - ${drawerWidth}px)` },
        ml: { md: `${drawerWidth}px` },
        transition: theme.transitions.create(["width", "margin-left"], {
          duration: theme.transitions.duration.shorter,
          easing: theme.transitions.easing.easeInOut,
        }),
      })}
    >
      <Toolbar sx={{ gap: { xs: 1, sm: 2 }, minHeight: 72 }}>
        {isDesktop ? (
          <Tooltip title={isSidebarCollapsed ? "ขยายเมนู" : "พับเมนู"}>
            <IconButton color="inherit" edge="start" onClick={onToggleSidebar} aria-label={isSidebarCollapsed ? "ขยายเมนู" : "พับเมนู"}>
              {isSidebarCollapsed ? <MenuIcon /> : <MenuOpenOutlinedIcon />}
            </IconButton>
          </Tooltip>
        ) : (
          <IconButton color="inherit" edge="start" onClick={onMobileMenuClick} aria-label="เปิดเมนู">
            <MenuIcon />
          </IconButton>
        )}
        <Box sx={{ flexGrow: 1, minWidth: 0 }}>
          <PageBreadcrumbs />
          <PageTitle title={pageTitle.title} subtitle={pageTitle.subtitle} />
        </Box>
        <NotificationBell />
        <Tooltip title="เมนูผู้ใช้งาน">
          <IconButton
            color="inherit"
            onClick={(event) => setUserMenuAnchor(event.currentTarget)}
            aria-label="เปิดเมนูผู้ใช้งาน"
            sx={{ display: { xs: "inline-flex", md: "none" }, flexShrink: 0, p: 0.5 }}
          >
            <Avatar
              src={toAbsoluteMediaUrl(user?.profileImageUrl)}
              sx={{ width: 34, height: 34, bgcolor: brandColors.accent, color: brandColors.primaryDark, fontSize: 14, fontWeight: 700 }}
            >
              {(user?.fullname ?? "U").slice(0, 1)}
            </Avatar>
          </IconButton>
        </Tooltip>
        <Stack
          direction="row"
          spacing={1.25}
          alignItems="center"
          onClick={(event) => setUserMenuAnchor(event.currentTarget)}
          sx={{ minWidth: 0, display: { xs: "none", md: "flex" }, cursor: "pointer" }}
        >
          <Avatar
            src={toAbsoluteMediaUrl(user?.profileImageUrl)}
            sx={{ width: 36, height: 36, bgcolor: brandColors.accent, color: brandColors.primaryDark, fontSize: 14 }}
          >
            {(user?.fullname ?? "U").slice(0, 1)}
          </Avatar>
          <Box sx={{ minWidth: 0, maxWidth: 180 }}>
            <Typography variant="body2" fontWeight={700} noWrap>
              {user?.fullname ?? "ผู้ใช้งาน"}
            </Typography>
            <Typography variant="caption" color="text.secondary" noWrap sx={{ display: "block" }}>
              {user?.role ?? "ผู้ใช้ระบบ"}
            </Typography>
          </Box>
        </Stack>
        <Menu anchorEl={userMenuAnchor} open={Boolean(userMenuAnchor)} onClose={() => setUserMenuAnchor(null)}>
          <MenuItem
            onClick={() => {
              setUserMenuAnchor(null);
              navigate("/profile");
            }}
          >
            <Stack direction="row" spacing={1} alignItems="center">
              <PersonOutlineOutlinedIcon fontSize="small" />
              <Typography variant="body2">ข้อมูลส่วนตัวของฉัน</Typography>
            </Stack>
          </MenuItem>
          <MenuItem
            onClick={() => {
              setUserMenuAnchor(null);
              navigate("/profile/change-password");
            }}
          >
            <Stack direction="row" spacing={1} alignItems="center">
              <LockResetOutlinedIcon fontSize="small" />
              <Typography variant="body2">เปลี่ยนรหัสผ่าน</Typography>
            </Stack>
          </MenuItem>
          <MenuItem
            onClick={async () => {
              setUserMenuAnchor(null);
              await handleLogout();
            }}
          >
            <Stack direction="row" spacing={1} alignItems="center">
              <LogoutOutlinedIcon fontSize="small" />
              <Typography variant="body2">ออกจากระบบ</Typography>
            </Stack>
          </MenuItem>
        </Menu>
        <Button
          color="primary"
          variant="outlined"
          startIcon={<LogoutOutlinedIcon />}
          onClick={handleLogout}
          sx={{ display: { xs: "none", sm: "inline-flex" }, flexShrink: 0 }}
        >
          ออกจากระบบ
        </Button>
      </Toolbar>
    </AppBar>
  );
}
