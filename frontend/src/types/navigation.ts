import type { SvgIconComponent } from "@mui/icons-material";

export type NavigationItem = {
  label: string;
  path: string;
  icon: SvgIconComponent;
  permission?: string;
  permissions?: string[];
  activePatterns?: string[];
  hiddenForRoles?: string[];
};
