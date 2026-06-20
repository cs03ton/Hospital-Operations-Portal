import { Box, Stack, Typography } from "@mui/material";
import { appDeveloper, appName, appVersion, hospitalName } from "../../config/appConfig";

export function AppFooter() {
  const currentYear = new Date().getFullYear();

  return (
    <Box
      component="footer"
      sx={{
        mt: { xs: 3, md: 4 },
        pt: 2,
        borderTop: "1px solid",
        borderColor: "divider",
        color: "text.secondary",
      }}
    >
      <Stack
        direction={{ xs: "column", md: "row" }}
        spacing={{ xs: 0.5, md: 1.25 }}
        alignItems={{ xs: "flex-start", md: "center" }}
        justifyContent="space-between"
      >
        <Typography variant="caption" fontWeight={700} color="text.primary">
          {appName} (HOP)
        </Typography>
        <Stack
          direction={{ xs: "column", sm: "row" }}
          spacing={{ xs: 0.25, sm: 1.25 }}
          alignItems={{ xs: "flex-start", sm: "center" }}
          sx={{ minWidth: 0 }}
        >
          <Typography variant="caption">© {currentYear} {hospitalName}</Typography>
          <Typography variant="caption">พัฒนาโดย {appDeveloper}</Typography>
          <Typography variant="caption">Version {appVersion}</Typography>
        </Stack>
      </Stack>
    </Box>
  );
}
