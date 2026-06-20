import { LinearProgress, Stack, Typography } from "@mui/material";

type LoadingStateProps = {
  message: string;
};

export function LoadingState({ message }: LoadingStateProps) {
  return (
    <Stack spacing={1.25} sx={{ py: 2 }}>
      <LinearProgress />
      <Typography variant="body2" color="text.secondary">{message}</Typography>
    </Stack>
  );
}
