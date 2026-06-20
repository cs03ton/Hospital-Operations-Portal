import { Box, Card, CardContent, Stack, Typography } from "@mui/material";
import type { ReactNode } from "react";

type InfoCardProps = {
  title: string;
  subtitle?: string;
  actions?: ReactNode;
  children: ReactNode;
};

export function InfoCard({ title, subtitle, actions, children }: InfoCardProps) {
  return (
    <Card sx={{ height: "100%" }}>
      <CardContent>
        <Stack
          direction={{ xs: "column", md: "row" }}
          spacing={1.5}
          alignItems={{ xs: "stretch", md: "flex-start" }}
          justifyContent="space-between"
          sx={{ mb: 2 }}
        >
          <Box sx={{ minWidth: 0 }}>
            <Typography variant="h6">{title}</Typography>
            {subtitle && (
              <Typography variant="body2" color="text.secondary" sx={{ mt: 0.25 }}>
                {subtitle}
              </Typography>
            )}
          </Box>
          {actions && <Box sx={{ flexShrink: 0 }}>{actions}</Box>}
        </Stack>
        {children}
      </CardContent>
    </Card>
  );
}
