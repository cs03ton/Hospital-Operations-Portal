import SwipeOutlinedIcon from "@mui/icons-material/SwipeOutlined";
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
            {actions && <Box sx={{ flexShrink: 0, width: { xs: "100%", md: "auto" }, "& .MuiButton-root": { width: { xs: "100%", sm: "auto" } } }}>{actions}</Box>}
          </Stack>
        )}
        <Stack direction="row" spacing={0.75} alignItems="center" sx={{ display: { xs: "flex", md: "none" }, mb: 1, color: "text.secondary" }}>
          <SwipeOutlinedIcon fontSize="small" />
          <Typography variant="caption">เลื่อนตารางด้านข้างเพื่อดูข้อมูลเพิ่มเติม</Typography>
        </Stack>
        <TableContainer sx={{ overflowX: "auto", WebkitOverflowScrolling: "touch" }}>
          <Table size="small" sx={{ minWidth: minTableWidth }}>{children}</Table>
        </TableContainer>
      </CardContent>
    </Card>
  );
}
