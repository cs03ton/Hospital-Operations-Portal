import { Button, Dialog, DialogActions, DialogContent, DialogTitle } from "@mui/material";
import type { ButtonProps, DialogProps } from "@mui/material";
import type { ReactNode } from "react";

type ActionDialogProps = {
  open: boolean;
  title: string;
  children: ReactNode;
  confirmLabel?: string;
  cancelLabel?: string;
  confirmColor?: ButtonProps["color"];
  confirmVariant?: ButtonProps["variant"];
  maxWidth?: DialogProps["maxWidth"];
  isLoading?: boolean;
  isConfirmDisabled?: boolean;
  onClose: () => void;
  onConfirm: () => void;
};

export function ActionDialog({
  open,
  title,
  children,
  confirmLabel = "ยืนยัน",
  cancelLabel = "ยกเลิก",
  confirmColor = "primary",
  confirmVariant = "contained",
  maxWidth = "sm",
  isLoading,
  isConfirmDisabled,
  onClose,
  onConfirm,
}: ActionDialogProps) {
  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth={maxWidth}>
      <DialogTitle>{title}</DialogTitle>
      <DialogContent dividers>{children}</DialogContent>
      <DialogActions sx={{ px: 3, py: 2 }}>
        <Button onClick={onClose} disabled={isLoading}>
          {cancelLabel}
        </Button>
        <Button
          color={confirmColor}
          variant={confirmVariant}
          disabled={isLoading || isConfirmDisabled}
          onClick={onConfirm}
        >
          {isLoading ? "กำลังดำเนินการ..." : confirmLabel}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
