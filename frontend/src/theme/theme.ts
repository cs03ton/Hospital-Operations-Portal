import { createTheme } from "@mui/material/styles";
import { healthcareComponents } from "./components";
import { healthcarePalette } from "./palette";

export const brandColors = healthcarePalette;

export const theme = createTheme({
  palette: {
    mode: "light",
    primary: {
      main: brandColors.primary,
      light: brandColors.primaryLight,
      dark: brandColors.primaryDark,
      contrastText: "#FFFFFF",
    },
    secondary: {
      main: brandColors.secondary,
      light: brandColors.secondaryLight,
      dark: brandColors.secondaryDark,
      contrastText: "#FFFFFF",
    },
    background: {
      default: brandColors.background,
      paper: brandColors.surface,
    },
    divider: brandColors.border,
    text: {
      primary: brandColors.textPrimary,
      secondary: brandColors.textSecondary,
    },
    success: {
      main: brandColors.success,
    },
    warning: {
      main: brandColors.warning,
    },
    error: {
      main: brandColors.error,
    },
    info: {
      main: brandColors.info,
    },
  },
  shape: {
    borderRadius: 10,
  },
  typography: {
    fontFamily: ["Prompt", "Sarabun", "Roboto", "Arial", "sans-serif"].join(","),
    h1: { fontWeight: 700 },
    h2: { fontWeight: 700 },
    h3: { fontWeight: 700 },
    h4: { fontWeight: 700 },
    h5: { fontWeight: 700 },
    h6: { fontWeight: 700 },
    button: {
      fontWeight: 700,
      textTransform: "none",
    },
  },
  components: healthcareComponents,
});
