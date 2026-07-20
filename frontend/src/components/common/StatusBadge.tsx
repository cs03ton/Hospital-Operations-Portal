import { Chip, type ChipProps } from "@mui/material";
import { getStatusMeta, type AppStatusDomain } from "../../utils/statusLabels";

type StatusBadgeProps = {
  domain: AppStatusDomain;
  status?: string | null;
  label?: string;
  size?: ChipProps["size"];
  variant?: ChipProps["variant"];
};

export function StatusBadge({ domain, status, label, size = "small", variant }: StatusBadgeProps) {
  const meta = getStatusMeta(domain, status);
  return (
    <Chip
      size={size}
      variant={variant}
      color={meta.tone}
      label={label ?? meta.label}
      sx={{ fontWeight: 800 }}
    />
  );
}
