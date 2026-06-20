import { Box, Typography } from "@mui/material";

type PageTitleProps = {
  title: string;
  subtitle?: string;
};

export function PageTitle({ title, subtitle }: PageTitleProps) {
  return (
    <Box sx={{ minWidth: 0 }}>
      <Typography variant="h6" color="primary" noWrap>
        {title}
      </Typography>
      {subtitle && (
        <Typography variant="caption" color="text.secondary" noWrap sx={{ display: "block" }}>
          {subtitle}
        </Typography>
      )}
    </Box>
  );
}
