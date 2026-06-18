import { createTheme } from "@mui/material/styles";

export const brandColors = {
  primary: "#056839",
  primaryDark: "#034D2A",
  primaryLight: "#2B8A55",
  secondary: "#126D3F",
  accent: "#F4FFE0",
  background: "#F6FAF4",
  border: "#DCE7DD",
};

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
      light: "#4C805A",
      dark: "#0A4F2D",
      contrastText: "#FFFFFF",
    },
    background: {
      default: brandColors.background,
      paper: "#FFFFFF",
    },
    success: {
      main: "#15803D",
    },
    warning: {
      main: "#B7791F",
    },
    error: {
      main: "#C2410C",
    },
  },
  shape: {
    borderRadius: 8,
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
  components: {
    MuiCard: {
      styleOverrides: {
        root: {
          border: `1px solid ${brandColors.border}`,
          boxShadow: "0 10px 24px rgba(5, 104, 57, 0.08)",
        },
      },
    },
    MuiButton: {
      styleOverrides: {
        root: {
          borderRadius: 8,
        },
      },
    },
    MuiOutlinedInput: {
      styleOverrides: {
        root: {
          borderRadius: 8,
        },
      },
    },
  },
});
