import CloseOutlinedIcon from "@mui/icons-material/CloseOutlined";
import DownloadOutlinedIcon from "@mui/icons-material/DownloadOutlined";
import { Alert, Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, Stack, Typography } from "@mui/material";

type AttachmentPreviewDialogProps = {
  open: boolean;
  fileName?: string;
  fileSizeBytes?: number;
  contentType?: string | null;
  previewUrl?: string | null;
  isLoading?: boolean;
  errorMessage?: string | null;
  onClose: () => void;
  onDownload?: () => void;
};

export function AttachmentPreviewDialog({
  open,
  fileName,
  fileSizeBytes,
  contentType,
  previewUrl,
  isLoading,
  errorMessage,
  onClose,
  onDownload,
}: AttachmentPreviewDialogProps) {
  const previewKind = getPreviewKind(fileName, contentType);
  const sizeLabel = typeof fileSizeBytes === "number" ? `${Math.ceil(fileSizeBytes / 1024).toLocaleString("th-TH")} KB` : "-";

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="lg">
      <DialogTitle>
        <Stack direction="row" spacing={1.5} justifyContent="space-between" alignItems="flex-start">
          <Box sx={{ minWidth: 0 }}>
            <Typography variant="h6" fontWeight={800} noWrap>
              ดูตัวอย่างไฟล์แนบ
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ overflowWrap: "anywhere" }}>
              {fileName ?? "-"} · {sizeLabel}
            </Typography>
          </Box>
          <Button size="small" startIcon={<CloseOutlinedIcon />} onClick={onClose}>
            ปิด
          </Button>
        </Stack>
      </DialogTitle>
      <DialogContent dividers>
        {isLoading && <Alert severity="info">กำลังโหลดตัวอย่างไฟล์...</Alert>}
        {!isLoading && errorMessage && <Alert severity="error">{errorMessage}</Alert>}
        {!isLoading && !errorMessage && previewKind === "unsupported" && (
          <Alert severity="warning">ไม่รองรับการแสดงตัวอย่างไฟล์ประเภทนี้</Alert>
        )}
        {!isLoading && !errorMessage && previewUrl && previewKind === "pdf" && (
          <Box component="iframe" title={fileName ?? "attachment-preview"} src={previewUrl} sx={{ width: "100%", height: { xs: "70vh", md: "76vh" }, border: 0 }} />
        )}
        {!isLoading && !errorMessage && previewUrl && previewKind === "image" && (
          <Box sx={{ display: "grid", placeItems: "center", minHeight: { xs: 320, md: 520 }, bgcolor: "background.default", borderRadius: 2 }}>
            <Box component="img" src={previewUrl} alt={fileName ?? "attachment preview"} sx={{ maxWidth: "100%", maxHeight: "76vh", objectFit: "contain" }} />
          </Box>
        )}
      </DialogContent>
      <DialogActions>
        {onDownload && (
          <Button variant="outlined" startIcon={<DownloadOutlinedIcon />} onClick={onDownload}>
            ดาวน์โหลด
          </Button>
        )}
        <Button variant="contained" onClick={onClose}>
          ปิด
        </Button>
      </DialogActions>
    </Dialog>
  );
}

function getPreviewKind(fileName?: string, contentType?: string | null): "pdf" | "image" | "unsupported" {
  const normalizedContentType = contentType?.trim().toLowerCase();
  if (normalizedContentType === "application/pdf") {
    return "pdf";
  }

  if (normalizedContentType && ["image/jpeg", "image/jpg", "image/png", "image/webp"].includes(normalizedContentType)) {
    return "image";
  }

  const extension = fileName?.split(".").pop()?.toLowerCase();
  if (extension === "pdf") {
    return "pdf";
  }

  if (extension && ["jpg", "jpeg", "png", "webp"].includes(extension)) {
    return "image";
  }

  return "unsupported";
}
