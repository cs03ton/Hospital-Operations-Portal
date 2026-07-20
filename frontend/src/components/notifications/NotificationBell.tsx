import NotificationsNoneOutlinedIcon from "@mui/icons-material/NotificationsNoneOutlined";
import {
  Badge,
  Box,
  Divider,
  IconButton,
  ListItemButton,
  ListItemText,
  Menu,
  Button,
  Stack,
  Typography,
} from "@mui/material";
import { alpha } from "@mui/material/styles";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { getNotificationBadge, getNotificationItems } from "../../services/notificationService";
import { brandColors } from "../../theme/theme";
import { StatusBadge } from "../common/StatusBadge";

export function NotificationBell() {
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const navigate = useNavigate();
  const { data: notifications = [] } = useQuery({
    queryKey: ["notifications", "me"],
    queryFn: () => getNotificationItems(),
    refetchInterval: 60000,
  });
  const { data: badgeCount = 0 } = useQuery({
    queryKey: ["notifications", "badge"],
    queryFn: () => getNotificationBadge(),
    refetchInterval: 60000,
  });
  const isOpen = Boolean(anchorEl);

  function handleSelect(path?: string) {
    setAnchorEl(null);
    if (path) {
      navigate(path);
    }
  }

  return (
    <>
      <IconButton color="inherit" aria-label="เปิดรายการแจ้งเตือน" onClick={(event) => setAnchorEl(event.currentTarget)}>
        <Badge color="warning" badgeContent={badgeCount} max={99}>
          <NotificationsNoneOutlinedIcon />
        </Badge>
      </IconButton>
      <Menu
        anchorEl={anchorEl}
        open={isOpen}
        onClose={() => setAnchorEl(null)}
        PaperProps={{
          sx: (theme) => ({
            width: { xs: 320, sm: 390 },
            maxWidth: "calc(100vw - 32px)",
            mt: 1,
            borderRadius: 3,
            border: "1px solid",
            borderColor: "divider",
            boxShadow: `0 18px 42px ${alpha(theme.palette.text.primary, 0.14)}`,
          }),
        }}
      >
        <Box sx={(theme) => ({ px: 2, py: 1.5, bgcolor: alpha(theme.palette.primary.main, 0.06) })}>
          <Stack direction="row" justifyContent="space-between" alignItems="flex-start" spacing={1.5}>
            <Box>
              <Typography variant="subtitle1" fontWeight={700}>
                การแจ้งเตือน
              </Typography>
              <Typography variant="caption" color="text.secondary">
                รายการแจ้งเตือนตามบทบาทและหน้าที่ของคุณ
              </Typography>
            </Box>
            <Button size="small" variant="text" onClick={() => handleSelect("/notifications")}>
              ดูทั้งหมด
            </Button>
          </Stack>
        </Box>
        <Divider />
        {notifications.length ? (
          notifications.map((item) => (
            <ListItemButton key={item.id} onClick={() => handleSelect(item.path)} sx={{ alignItems: "flex-start", py: 1.25 }}>
              <ListItemText
                primary={
                  <Stack direction="row" spacing={1} alignItems="center">
                    <Typography variant="body2" fontWeight={700}>
                      {item.title}
                    </Typography>
                    {item.unread && (
                      <Box sx={{ width: 8, height: 8, borderRadius: "50%", bgcolor: brandColors.accent, flexShrink: 0 }} />
                    )}
                    <StatusBadge domain="notificationPriority" status={item.priority} />
                  </Stack>
                }
                secondary={item.message}
                secondaryTypographyProps={{ variant: "caption", color: "text.secondary" }}
              />
            </ListItemButton>
          ))
        ) : (
          <Box sx={{ px: 2, py: 3 }}>
            <Typography variant="body2" color="text.secondary">
              ไม่มีรายการแจ้งเตือน
            </Typography>
          </Box>
        )}
      </Menu>
    </>
  );
}
