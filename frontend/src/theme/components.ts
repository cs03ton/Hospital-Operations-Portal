import { alpha } from "@mui/material/styles";
import type { Components, Theme } from "@mui/material/styles";
import { healthcarePalette } from "./palette";

export const healthcareComponents: Components<Omit<Theme, "components">> = {
  MuiCssBaseline: {
    styleOverrides: {
      body: {
        backgroundColor: healthcarePalette.background,
        color: healthcarePalette.textPrimary,
      },
    },
  },
  MuiAppBar: {
    styleOverrides: {
      root: {
        backgroundImage: "none",
      },
    },
  },
  MuiButton: {
    styleOverrides: {
      root: {
        borderRadius: 10,
        boxShadow: "none",
        textTransform: "none",
      },
      containedPrimary: {
        boxShadow: `0 8px 18px ${alpha(healthcarePalette.primary, 0.18)}`,
        "&:hover": {
          backgroundColor: healthcarePalette.primaryDark,
          boxShadow: `0 10px 24px ${alpha(healthcarePalette.primary, 0.22)}`,
        },
      },
      containedSecondary: {
        "&:hover": {
          backgroundColor: healthcarePalette.secondaryDark,
        },
      },
      outlined: {
        borderColor: healthcarePalette.border,
      },
    },
  },
  MuiCard: {
    styleOverrides: {
      root: {
        border: `1px solid ${healthcarePalette.border}`,
        borderRadius: 14,
        boxShadow: `0 14px 36px ${alpha(healthcarePalette.primary, 0.08)}`,
        backgroundImage: "none",
      },
    },
  },
  MuiChip: {
    styleOverrides: {
      root: {
        borderRadius: 999,
        fontWeight: 700,
      },
    },
  },
  MuiDialog: {
    styleOverrides: {
      paper: {
        borderRadius: 16,
        border: `1px solid ${healthcarePalette.border}`,
        boxShadow: `0 24px 64px ${alpha(healthcarePalette.textPrimary, 0.18)}`,
      },
    },
  },
  MuiInputLabel: {
    styleOverrides: {
      root: {
        color: healthcarePalette.textSecondary,
      },
    },
  },
  MuiMenu: {
    styleOverrides: {
      paper: {
        borderRadius: 14,
        border: `1px solid ${healthcarePalette.border}`,
        boxShadow: `0 18px 46px ${alpha(healthcarePalette.textPrimary, 0.14)}`,
      },
    },
  },
  MuiOutlinedInput: {
    styleOverrides: {
      root: {
        borderRadius: 10,
        backgroundColor: healthcarePalette.surface,
        "& .MuiOutlinedInput-notchedOutline": {
          borderColor: healthcarePalette.border,
        },
        "&:hover .MuiOutlinedInput-notchedOutline": {
          borderColor: healthcarePalette.accent,
        },
        "&.Mui-focused .MuiOutlinedInput-notchedOutline": {
          borderColor: healthcarePalette.primary,
          borderWidth: 1,
        },
      },
    },
  },
  MuiPaper: {
    styleOverrides: {
      root: {
        backgroundImage: "none",
      },
    },
  },
  MuiTableCell: {
    styleOverrides: {
      head: {
        backgroundColor: healthcarePalette.background,
        color: healthcarePalette.textPrimary,
        fontWeight: 800,
        borderBottomColor: healthcarePalette.border,
      },
      body: {
        borderBottomColor: healthcarePalette.border,
      },
    },
  },
  MuiTableRow: {
    styleOverrides: {
      root: {
        "&:hover": {
          backgroundColor: alpha(healthcarePalette.accent, 0.08),
        },
      },
    },
  },
};

