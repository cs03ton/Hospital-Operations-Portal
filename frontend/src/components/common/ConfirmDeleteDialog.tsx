import { Alert, Stack, Typography } from "@mui/material";
import type { DeleteReferenceSummary } from "../../api/adminApi";
import { ActionDialog } from "./ActionDialog";

type ConfirmDeleteDialogProps = {
  open: boolean;
  title: string;
  itemName?: string | null;
  description?: string;
  references?: DeleteReferenceSummary[];
  confirmLabel?: string;
  isLoading?: boolean;
  onClose: () => void;
  onConfirm: () => void;
};

export function ConfirmDeleteDialog({
  open,
  title,
  itemName,
  description,
  references = [],
  confirmLabel = "ยืนยัน",
  isLoading,
  onClose,
  onConfirm,
}: ConfirmDeleteDialogProps) {
  const visibleReferences = references.filter((item) => item.count > 0);

  return (
    <ActionDialog
      open={open}
      title={title}
      confirmLabel={confirmLabel}
      confirmColor="error"
      isLoading={isLoading}
      onClose={onClose}
      onConfirm={onConfirm}
    >
      <Stack spacing={2}>
        <Typography>
          คุณต้องการดำเนินการกับ “{itemName || "-"}” ใช่หรือไม่
        </Typography>
        {description && (
          <Typography variant="body2" color="text.secondary">
            {description}
          </Typography>
        )}
        {visibleReferences.length > 0 && (
          <Alert severity="warning">
            <Typography variant="subtitle2" fontWeight={700}>
              ระบบตรวจพบข้อมูลอ้างอิง
            </Typography>
            <Stack component="ul" sx={{ m: 0, pl: 2 }}>
              {visibleReferences.map((reference) => (
                <li key={reference.label}>
                  {reference.label}: {reference.count.toLocaleString("th-TH")} รายการ
                </li>
              ))}
            </Stack>
          </Alert>
        )}
      </Stack>
    </ActionDialog>
  );
}
