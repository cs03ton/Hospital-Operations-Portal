import { Box, Typography } from "@mui/material";

type PageHeaderProps = {
  title: string;
  subtitle: string;
};

export function PageHeader({ title, subtitle }: PageHeaderProps) {
  return (
    <Box
      className="hop-page-header"
      sx={(theme) => ({
        mb: 3,
        pb: 1.25,
        borderBottom: "2px solid",
        borderColor: theme.palette.warning.main,
      })}
    >
      <Typography className="hop-page-title" variant="h4" color="primary" sx={{ mb: 0.75, fontSize: { xs: "1.55rem", md: "2rem" } }}>
        {title}
      </Typography>
      <Typography className="hop-page-subtitle" color="text.secondary">{subtitle}</Typography>
    </Box>
  );
}
