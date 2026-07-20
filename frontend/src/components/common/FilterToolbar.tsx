import { Card, CardContent, Grid } from "@mui/material";
import type { ReactNode } from "react";

type FilterToolbarProps = {
  children: ReactNode;
};

export function FilterToolbar({ children }: FilterToolbarProps) {
  return (
    <Card sx={{ mb: 2 }}>
      <CardContent sx={{ py: 2 }}>
        <Grid container spacing={1.5} alignItems="center" sx={{ "& > .MuiGrid-item": { minWidth: 0 } }}>
          {children}
        </Grid>
      </CardContent>
    </Card>
  );
}
