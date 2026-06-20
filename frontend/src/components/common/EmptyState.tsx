import InboxOutlinedIcon from "@mui/icons-material/InboxOutlined";
import { Stack, Typography } from "@mui/material";

type EmptyStateProps = {
  message: string;
};

export function EmptyState({ message }: EmptyStateProps) {
  return (
    <Stack spacing={1} alignItems="center" sx={{ py: 4, color: "text.secondary" }}>
      <InboxOutlinedIcon color="disabled" />
      <Typography variant="body2">{message}</Typography>
    </Stack>
  );
}

