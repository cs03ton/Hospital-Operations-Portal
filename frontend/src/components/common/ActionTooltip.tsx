import { Tooltip } from "@mui/material";
import type { ReactElement } from "react";

type ActionTooltipProps = {
  title: string;
  children: ReactElement;
};

export function ActionTooltip({ title, children }: ActionTooltipProps) {
  return (
    <Tooltip title={title} arrow>
      <span>{children}</span>
    </Tooltip>
  );
}

