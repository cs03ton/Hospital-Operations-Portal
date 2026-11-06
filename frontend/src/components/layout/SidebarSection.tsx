import { type ReactNode } from "react";
import type { SvgIconComponent } from "@mui/icons-material";
import { Box, Collapse, List, ListItemButton, ListItemIcon, ListItemText, Tooltip } from "@mui/material";
import ExpandLessOutlinedIcon from "@mui/icons-material/ExpandLessOutlined";
import ExpandMoreOutlinedIcon from "@mui/icons-material/ExpandMoreOutlined";

export function SidebarSection({ id, label, icon: Icon, open, active, collapsed, onToggle, onExpand, children }: {
  id: string; label: string; icon: SvgIconComponent; open: boolean; active: boolean; collapsed: boolean;
  onToggle: () => void; onExpand: () => void; children: ReactNode;
}) {
  return <Box sx={{ my: .5 }}><Tooltip title={collapsed ? label : ""} placement="right"><ListItemButton
    aria-label={label} aria-expanded={!collapsed && open} aria-controls={`section-${id}`} selected={active}
    onClick={() => { if (collapsed) { if (!open) onToggle(); onExpand(); } else onToggle(); }}
    sx={{ minHeight: 46, px: collapsed ? 0 : 1.5, justifyContent: collapsed ? "center" : "flex-start", borderRadius: 2, color: "common.white",
      "&.Mui-selected": { bgcolor: "rgba(200,168,102,.18)" }, "&:hover": { bgcolor: "rgba(255,255,255,.09)" } }}>
    <ListItemIcon sx={{ minWidth: collapsed ? 0 : 36, color: "inherit", justifyContent: "center" }}><Icon fontSize="small" /></ListItemIcon>
    {!collapsed && <><ListItemText primary={label} primaryTypographyProps={{ fontWeight: 800, fontSize: ".94rem" }} />{open ? <ExpandLessOutlinedIcon fontSize="small" /> : <ExpandMoreOutlinedIcon fontSize="small" />}</>}
  </ListItemButton></Tooltip>
  {!collapsed && <Collapse id={`section-${id}`} in={open} timeout="auto" unmountOnExit><List disablePadding sx={{ pl: .5, ml: 1, borderLeft: "1px solid rgba(255,255,255,.13)" }}>{children}</List></Collapse>}
  </Box>;
}
