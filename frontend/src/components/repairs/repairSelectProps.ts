import { alpha } from "@mui/material/styles";
import type { SelectProps } from "@mui/material";

export const repairSelectProps: Partial<SelectProps> = {
  MenuProps: {
    PaperProps: {
      sx: (theme) => ({
        backgroundColor: theme.palette.background.paper,
        backgroundImage: "none",
        border: `1px solid ${alpha(theme.palette.primary.main, 0.5)}`,
        borderRadius: 1,
        boxShadow: `0 8px 28px ${alpha(theme.palette.common.black, 0.24)}`,
        maxHeight: "min(360px, calc(100dvh - 64px))",
        "& .MuiMenuItem-root": {
          whiteSpace: "normal",
          overflowWrap: "anywhere",
          "&:hover, &.Mui-focusVisible": {
            backgroundColor: alpha(theme.palette.primary.main, 0.12),
          },
          "&.Mui-selected": {
            backgroundColor: alpha(theme.palette.primary.main, 0.2),
            color: theme.palette.primary.dark,
            fontWeight: 700,
            "&:hover, &.Mui-focusVisible": {
              backgroundColor: alpha(theme.palette.primary.main, 0.28),
            },
          },
        },
      }),
    },
  },
};
