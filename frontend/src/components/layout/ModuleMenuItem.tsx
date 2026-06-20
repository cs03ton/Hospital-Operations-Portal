import { ListItemButton, ListItemIcon, ListItemText, Tooltip } from "@mui/material";
import { alpha } from "@mui/material/styles";
import { NavLink } from "react-router-dom";
import { brandColors } from "../../theme/theme";
import type { NavigationItem } from "../../types/navigation";

type ModuleMenuItemProps = {
  item: NavigationItem;
  isActive: boolean;
  isCollapsed: boolean;
  onClick: () => void;
};

export function ModuleMenuItem({ item, isActive, isCollapsed, onClick }: ModuleMenuItemProps) {
  const Icon = item.icon;
  const itemButton = (
    <ListItemButton
      component={NavLink}
      to={item.path}
      onClick={onClick}
      selected={isActive}
      sx={(theme) => ({
        my: 0.25,
        ml: isCollapsed ? 0 : 2.25,
        borderRadius: 2,
        color: isActive ? theme.palette.common.white : alpha(theme.palette.common.white, 0.68),
        minHeight: 40,
        px: isCollapsed ? 0 : 1.25,
        justifyContent: isCollapsed ? "center" : "flex-start",
        border: "1px solid transparent",
        "& .MuiListItemIcon-root": {
          color: "inherit",
        },
        "&.Mui-selected": {
          bgcolor: alpha(brandColors.accent, 0.18),
          borderColor: alpha(brandColors.accent, 0.32),
          color: theme.palette.common.white,
          boxShadow: `inset 3px 0 0 ${brandColors.accent}`,
        },
        "&.Mui-selected:hover": {
          bgcolor: alpha(brandColors.accent, 0.24),
        },
        "&:hover": {
          bgcolor: alpha(theme.palette.common.white, 0.1),
          color: theme.palette.common.white,
        },
      })}
    >
      <ListItemIcon sx={{ minWidth: isCollapsed ? 0 : 34, justifyContent: "center" }}>
        <Icon fontSize="small" />
      </ListItemIcon>
      {!isCollapsed && (
        <ListItemText
          primary={item.label}
          primaryTypographyProps={{ variant: "body2", fontWeight: isActive ? 800 : 600 }}
        />
      )}
    </ListItemButton>
  );

  return (
    <Tooltip title={isCollapsed ? item.label : ""} placement="right">
      {itemButton}
    </Tooltip>
  );
}
