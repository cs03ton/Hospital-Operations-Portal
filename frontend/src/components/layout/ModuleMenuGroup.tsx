import ExpandLessOutlinedIcon from "@mui/icons-material/ExpandLessOutlined";
import ExpandMoreOutlinedIcon from "@mui/icons-material/ExpandMoreOutlined";
import { Collapse, List, ListItemButton, ListItemIcon, ListItemText, Tooltip } from "@mui/material";
import { alpha } from "@mui/material/styles";
import type { NavigationModule } from "../../config/menuConfig";
import { brandColors } from "../../theme/theme";
import type { NavigationItem } from "../../types/navigation";
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
          bgcolor: alpha(brandColors.accent, 0.14),
          borderColor: alpha(brandColors.accent, 0.34),
          boxShadow: `inset 4px 0 0 ${brandColors.accent}`,
        },
        "&.Mui-selected:hover": {
          bgcolor: alpha(brandColors.accent, 0.2),
        },
        "&:hover": {
          bgcolor: alpha(theme.palette.common.white, 0.1),
          color: theme.palette.common.white,
        },
      })}
    >
      <ListItemIcon sx={{ minWidth: isCollapsed ? 0 : 36, justifyContent: "center", color: isActive ? brandColors.accent : "inherit" }}>
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
                isActive={isItemActive(activePath, item)}
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

export function isItemActive(pathname: string, item: NavigationItem) {
  if (item.activePatterns?.length) {
    return item.activePatterns.some((pattern) => matchRoutePattern(pathname, pattern));
  }

  if (item.path === "/dashboard") {
    return pathname === "/" || pathname === "/dashboard";
  }

  return pathname === item.path || pathname.startsWith(`${item.path}/`);
}

function matchRoutePattern(pathname: string, pattern: string) {
  if (pattern === "/") {
    return pathname === "/";
  }

  const pathSegments = trimSlashes(pathname).split("/");
  const patternSegments = trimSlashes(pattern).split("/");

  if (pathSegments.length !== patternSegments.length) {
    return false;
  }

  return patternSegments.every((segment, index) => {
    if (segment === ":id") {
      return isGuid(pathSegments[index]);
    }

    return segment.startsWith(":") || segment === pathSegments[index];
  });
}

function trimSlashes(value: string) {
  return value.replace(/^\/+|\/+$/g, "");
}

function isGuid(value: string) {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value);
}
