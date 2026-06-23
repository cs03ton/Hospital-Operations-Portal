import { Alert, Snackbar, Slide } from "@mui/material";
import type { SlideProps } from "@mui/material";
import type { AppNotification } from "../../contexts/NotificationContext";

type AppSnackbarProps = {
  notification: AppNotification | null;
  open: boolean;
  onClose: () => void;
};

export function AppSnackbar({ notification, open, onClose }: AppSnackbarProps) {
  return (
    <Snackbar
      open={open}
      autoHideDuration={4500}
      onClose={(_, reason) => {
        if (reason !== "clickaway") {
          onClose();
        }
      }}
      anchorOrigin={{ vertical: "top", horizontal: "right" }}
      TransitionComponent={SlideTransition}
    >
      <Alert
        variant="filled"
        severity={notification?.type ?? "info"}
        onClose={onClose}
        sx={{ minWidth: { xs: "auto", sm: 320 }, boxShadow: 3 }}
      >
        {notification?.message}
      </Alert>
    </Snackbar>
  );
}

function SlideTransition(props: SlideProps) {
  return <Slide {...props} direction="left" />;
}
