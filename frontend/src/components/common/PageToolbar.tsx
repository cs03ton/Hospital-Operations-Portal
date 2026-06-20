import { Paper, Stack } from "@mui/material";
import type { ReactNode } from "react";

type PageToolbarProps = {
  children: ReactNode;
};

export function PageToolbar({ children }: PageToolbarProps) {
  return (
    <Paper
      variant="outlined"
      sx={{
        mb: 2,
        p: 2,
        borderColor: "divider",
        borderRadius: 3,
        bgcolor: "background.paper",
      }}
    >
      <Stack direction={{ xs: "column", md: "row" }} spacing={1.5} alignItems={{ md: "center" }} justifyContent="space-between">
        {children}
      </Stack>
    </Paper>
  );
}

