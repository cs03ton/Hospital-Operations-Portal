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
  Tab,
  Tabs,
  Stack,
  Typography,
} from "@mui/material";
import { alpha } from "@mui/material/styles";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { getNotificationBadge, getNotificationCenterItems } from "../../services/notificationService";
import { getDashboardActionItems } from "../../api/dashboardActionApi";
import { brandColors } from "../../theme/theme";
import { StatusBadge } from "../common/StatusBadge";
import { notificationPollingOptions } from "../../config/queryPolling";

export function NotificationBell() {
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [tab, setTab] = useState(0);
  const navigate = useNavigate();
  const { data: updates } = useQuery({
    queryKey: ["notifications", "hub-updates"],
    queryFn: () => getNotificationCenterItems({ filter: "hub-updates", pageSize: 12 }),
    ...notificationPollingOptions,
  });
  const { data: actionData } = useQuery({
    queryKey: ["dashboard", "action-items"],
    queryFn: getDashboardActionItems,
    ...notificationPollingOptions,
  });
  const { data: badgeCount = 0 } = useQuery({
    queryKey: ["notifications", "badge"],
    queryFn: () => getNotificationBadge(),
    ...notificationPollingOptions,
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
            width: { xs: "calc(100vw - 24px)", sm: 410 },
            maxWidth: "calc(100vw - 32px)",
            maxHeight: "min(75vh, 640px)",
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
                การแจ้งเตือนของฉัน
              </Typography>
              <Typography variant="caption" color="text.secondary">
                คิวงานปัจจุบันและข่าวสารที่เกี่ยวข้องกับคุณ
              </Typography>
            </Box>
            <Button size="small" variant="text" onClick={() => handleSelect(tab === 0 ? "/dashboard" : "/notifications")}>
              {tab === 0 ? "ดูคิวงาน" : "ดูทั้งหมด"}
            </Button>
          </Stack>
        </Box>
        <Divider />
        <Tabs value={tab} onChange={(_, next: number) => setTab(next)} variant="fullWidth" aria-label="ประเภทการแจ้งเตือน">
          <Tab label={`ต้องดำเนินการ (${(actionData?.leaveCount ?? 0) + (actionData?.fleetCount ?? 0) + (actionData?.repairCount ?? 0)})`} />
          <Tab label={`อัปเดตทั่วไป (${updates?.totalItems ?? 0})`} />
        </Tabs>
        <Divider />
        {tab === 0 && (actionData?.items.length ? (
          actionData.items.map((item) => (
            <ListItemButton key={item.id} onClick={() => handleSelect(item.path)} sx={{ alignItems: "flex-start", py: 1.25 }}>
              <ListItemText
                primary={<Typography variant="body2" fontWeight={700}>{item.title}</Typography>}
                secondary={item.detail}
                secondaryTypographyProps={{ variant: "caption", color: "text.secondary" }}
              />
            </ListItemButton>
          ))
        ) : (
          <Box sx={{ px: 2, py: 3 }}><Typography variant="body2" color="text.secondary">ไม่มีงานที่ต้องดำเนินการ</Typography></Box>
        ))}
        {tab === 1 && (updates?.items.length ? (
          updates.items.map((item) => (
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
              ไม่มีอัปเดตทั่วไป
            </Typography>
          </Box>
        ))}
      </Menu>
    </>
  );
}
