import { Box, Typography } from "@mui/material";

type PageHeaderProps = {
  title: string;
  subtitle: string;
};

export function PageHeader({ title, subtitle }: PageHeaderProps) {
  return (
    <Box sx={{ mb: 3 }}>
      <Typography variant="h4" sx={{ mb: 0.75 }}>
        {title}
      </Typography>
      <Typography color="text.secondary">{subtitle}</Typography>
    </Box>
  );
}
