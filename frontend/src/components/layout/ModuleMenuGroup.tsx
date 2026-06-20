import ExpandLessOutlinedIcon from "@mui/icons-material/ExpandLessOutlined";
import ExpandMoreOutlinedIcon from "@mui/icons-material/ExpandMoreOutlined";
import { Collapse, List, ListItemButton, ListItemIcon, ListItemText, Tooltip } from "@mui/material";
import { alpha } from "@mui/material/styles";
import type { NavigationModule } from "../../config/menuConfig";
import { brandColors } from "../../theme/theme";
import { ModuleMenuItem } from "./ModuleMenuItem";

type ModuleMenuGroupProps = {
  module: NavigationModule;
  activePath: string;
  isActive: boolean;
  isOpen: boolean;
  isCollapsed: boolean;
  onToggle: () => void;
  onExpandSidebar: () => void;
  onNavigate: () => void;
};

export function ModuleMenuGroup({
  module,
  activePath,
  isActive,
  isOpen,
  isCollapsed,
  onToggle,
  onExpandSidebar,
  onNavigate,
}: ModuleMenuGroupProps) {
  const ModuleIcon = module.moduleIcon;

  function handleModuleClick() {
    if (isCollapsed) {
      onExpandSidebar();
      return;
    }

    onToggle();
  }

  const groupButton = (
    <ListItemButton
      selected={isActive}
      onClick={handleModuleClick}
      sx={(theme) => ({
        my: 0.35,
        borderRadius: 2,
        minHeight: 44,
        px: isCollapsed ? 0 : 1.5,
        justifyContent: isCollapsed ? "center" : "flex-start",
        color: isActive ? theme.palette.common.white : alpha(theme.palette.common.white, 0.82),
        border: "1px solid transparent",
        "& .MuiListItemIcon-root": {
          color: "inherit",
        },
        "&.Mui-selected": {
          bgcolor: alpha(brandColors.accent, 0.2),
          borderColor: alpha(brandColors.accent, 0.38),
        },
        "&.Mui-selected:hover": {
          bgcolor: alpha(brandColors.accent, 0.26),
        },
        "&:hover": {
          bgcolor: alpha(theme.palette.common.white, 0.1),
          color: theme.palette.common.white,
        },
      })}
    >
      <ListItemIcon sx={{ minWidth: isCollapsed ? 0 : 36, justifyContent: "center" }}>
        <ModuleIcon fontSize="small" />
      </ListItemIcon>
      {!isCollapsed && (
        <>
          <ListItemText
            primary={module.moduleLabel}
            primaryTypographyProps={{ variant: "body2", fontWeight: isActive ? 800 : 700 }}
          />
          {isOpen ? <ExpandLessOutlinedIcon fontSize="small" /> : <ExpandMoreOutlinedIcon fontSize="small" />}
        </>
      )}
    </ListItemButton>
  );

  return (
    <>
      <Tooltip title={isCollapsed ? module.moduleLabel : ""} placement="right">
        {groupButton}
      </Tooltip>
      {!isCollapsed && (
        <Collapse in={isOpen} timeout="auto" unmountOnExit>
          <List disablePadding>
            {module.children.map((item) => (
              <ModuleMenuItem
                key={item.path}
                item={item}
                isActive={isItemActive(activePath, item.path)}
                isCollapsed={false}
                onClick={onNavigate}
              />
            ))}
          </List>
        </Collapse>
      )}
    </>
  );
}

export function isItemActive(pathname: string, itemPath: string) {
  if (itemPath === "/dashboard") {
    return pathname === "/" || pathname === "/dashboard";
  }

  if (itemPath === "/leave") {
    const leaveChildMenuPaths = ["/leave/create", "/leave/calendar", "/leave/types", "/leave/balances"];
    return pathname === "/leave" || (pathname.startsWith("/leave/") && !leaveChildMenuPaths.some((path) => pathname.startsWith(path)));
  }

  return pathname === itemPath || pathname.startsWith(`${itemPath}/`);
}
