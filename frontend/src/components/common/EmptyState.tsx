import InboxOutlinedIcon from "@mui/icons-material/InboxOutlined";
import type { SvgIconComponent } from "@mui/icons-material";
import { Stack, Typography } from "@mui/material";
import type { ReactNode } from "react";

type EmptyStateProps = {
  message?: string;
  title?: string;
  description?: string;
  icon?: SvgIconComponent;
  action?: ReactNode;
};

export function EmptyState({ message, title, description, icon: Icon = InboxOutlinedIcon, action }: EmptyStateProps) {
  return (
    <Stack spacing={1} alignItems="center" textAlign="center" sx={{ py: { xs: 3, md: 4 }, px: 2, color: "text.secondary" }}>
      <Icon color="disabled" />
      {title && <Typography fontWeight={900} color="text.primary">{title}</Typography>}
      <Typography variant="body2">{description ?? message}</Typography>
      {action}
    </Stack>
  );
}
