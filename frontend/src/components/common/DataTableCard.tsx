import { Box, Card, CardContent, Stack, Table, TableContainer, Typography } from "@mui/material";
import type { ReactNode } from "react";

type DataTableCardProps = {
  title?: string;
  subtitle?: string;
  actions?: ReactNode;
  children: ReactNode;
  minTableWidth?: number;
};

export function DataTableCard({ title, subtitle, actions, children, minTableWidth = 720 }: DataTableCardProps) {
  return (
    <Card>
      <CardContent sx={{ p: { xs: 1.5, md: 2 }, "&:last-child": { pb: { xs: 1.5, md: 2 } } }}>
        {(title || subtitle || actions) && (
          <Stack
            direction={{ xs: "column", md: "row" }}
            spacing={1.5}
            alignItems={{ xs: "stretch", md: "flex-start" }}
            justifyContent="space-between"
            sx={{ mb: 2 }}
          >
            <Box sx={{ minWidth: 0 }}>
              {title && <Typography variant="h6">{title}</Typography>}
              {subtitle && (
                <Typography variant="body2" color="text.secondary" sx={{ mt: 0.25 }}>
                  {subtitle}
                </Typography>
              )}
            </Box>
            {actions && <Box sx={{ flexShrink: 0 }}>{actions}</Box>}
          </Stack>
        )}
        <TableContainer sx={{ overflowX: "auto" }}>
          <Table size="small" sx={{ minWidth: minTableWidth }}>{children}</Table>
        </TableContainer>
      </CardContent>
    </Card>
  );
}
