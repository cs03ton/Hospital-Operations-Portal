import { useEffect, useState, type ReactNode } from "react";
import { isAxiosError } from "axios";
import {
  Alert,
  Box,
  Button,
  Chip,
  IconButton,
  LinearProgress,
  Stack,
  Tooltip,
  Typography,
} from "@mui/material";
import RefreshIcon from "@mui/icons-material/Refresh";
import AddPhotoAlternateOutlinedIcon from "@mui/icons-material/AddPhotoAlternateOutlined";
import CloseIcon from "@mui/icons-material/Close";
import {
  repairPriorityLabels,
  repairStatusColors,
  repairStatusLabels,
  repairTeamLabel,
} from "../../utils/repairPresentation";
import { brandColors } from "../../theme/theme";

const goldBadgeSx = {
  bgcolor: "#FFF3D6",
  borderColor: brandColors.accent,
  color: brandColors.secondaryDark,
  fontWeight: 700,
  "& .MuiChip-label": { px: 1.25 },
};

export function RepairStatus({ value }: { value: string }) {
  return (
    <Chip
      size="small"
      label={repairStatusLabels[value] ?? value}
      color={
        repairStatusColors[value as keyof typeof repairStatusColors] ??
        "default"
      }
    />
  );
}
export function RepairPriority({ value }: { value?: string }) {
  return (
    <Chip
      size="small"
      variant="outlined"
      label={repairPriorityLabels[value ?? ""] ?? "ยังไม่ประเมิน"}
      sx={goldBadgeSx}
    />
  );
}

export function RepairTeam({ value }: { value: string }) {
  return (
    <Chip
      size="small"
      variant="outlined"
      label={repairTeamLabel(value)}
      sx={goldBadgeSx}
    />
  );
}
export function RepairError({
  error,
  action,
}: {
  error: unknown;
  action?: ReactNode;
}) {
  if (!error) return null;
  const status = isAxiosError(error) ? error.response?.status : undefined;
  const message =
    status === 409
      ? "ข้อมูลเปลี่ยนแล้ว กรุณาโหลดข้อมูลล่าสุดและตรวจสอบก่อนยืนยันอีกครั้ง ข้อความที่กรอกยังคงอยู่"
      : status === 403
        ? "ไม่มีสิทธิ์ดำเนินการ หรือสถานะใบงานไม่อนุญาต"
        : status === 400
          ? "ข้อมูลไม่ถูกต้อง กรุณาตรวจช่องที่จำเป็นและค่าที่กรอก"
          : "ไม่สามารถดำเนินการได้ กรุณาลองใหม่อีกครั้ง";
  return (
    <Alert severity={status === 409 ? "warning" : "error"} action={action}>
      {message}
    </Alert>
  );
}
export function RepairRefresh({
  busy,
  onClick,
}: {
  busy: boolean;
  onClick: () => void;
}) {
  return (
    <Tooltip title="โหลดข้อมูลล่าสุด">
      <span>
        <IconButton
          aria-label="โหลดข้อมูลล่าสุด"
          disabled={busy}
          onClick={onClick}
          color="primary"
        >
          <RefreshIcon />
        </IconButton>
      </span>
    </Tooltip>
  );
}
export function RepairField({
  label,
  children,
}: {
  label: string;
  children: ReactNode;
}) {
  return (
    <Box sx={{ minWidth: 0 }}>
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Typography
        component="div"
        sx={{ overflowWrap: "anywhere", whiteSpace: "pre-wrap" }}
      >
        {children || "-"}
      </Typography>
    </Box>
  );
}
function SelectedPhoto({
  file,
  remove,
  disabled,
}: {
  file: File;
  remove: () => void;
  disabled: boolean;
}) {
  const [url, setUrl] = useState("");
  useEffect(() => {
    const next = URL.createObjectURL(file);
    setUrl(next);
    return () => URL.revokeObjectURL(next);
  }, [file]);
  return (
    <Box
      sx={{
        border: 1,
        borderColor: "divider",
        borderRadius: 1,
        p: 1,
        minWidth: 0,
      }}
    >
      <Box
        component="img"
        src={url}
        alt={`ภาพตัวอย่าง ${file.name}`}
        sx={{
          width: "100%",
          height: 140,
          objectFit: "contain",
          display: "block",
        }}
      />
      <Stack direction="row" alignItems="center" gap={0.5}>
        <Typography
          variant="caption"
          sx={{ flex: 1, minWidth: 0, overflowWrap: "anywhere" }}
        >
          {file.name}
        </Typography>
        <Tooltip title="นำรูปออก">
          <span>
            <IconButton
              size="small"
              disabled={disabled}
              onClick={remove}
              aria-label={`นำรูป ${file.name} ออก`}
            >
              <CloseIcon fontSize="small" />
            </IconButton>
          </span>
        </Tooltip>
      </Stack>
    </Box>
  );
}
export function RepairFilePicker({
  files,
  setFiles,
  disabled = false,
}: {
  files: File[];
  setFiles: (files: File[]) => void;
  disabled?: boolean;
}) {
  const [error, setError] = useState("");
  const select = (incoming: File[]) => {
    if (disabled) return;
    const items = [...files, ...incoming];
    if (
      items.length > 5 ||
      items.some(
        (f) =>
          !f.size ||
          f.size > 10 * 1024 * 1024 ||
          !["image/jpeg", "image/png", "image/webp"].includes(f.type),
      )
    ) {
      setError("เลือก JPG/PNG/WebP ไม่เกิน 5 รูป รูปละ 10 MB");
      return;
    }
    setError("");
    setFiles(items);
  };
  return (
    <Stack spacing={1.5}>
      <Box
        onDragOver={(e) => e.preventDefault()}
        onDrop={(e) => {
          e.preventDefault();
          select(Array.from(e.dataTransfer.files));
        }}
        sx={{
          p: 2,
          border: "1px dashed",
          borderColor: "divider",
          borderRadius: 1,
          textAlign: "center",
        }}
      >
        <Button
          component="label"
          disabled={disabled}
          startIcon={<AddPhotoAlternateOutlinedIcon />}
        >
          เลือกรูปภาพ
          <input
            aria-label="เลือกรูปภาพแนบ"
            hidden
            disabled={disabled}
            type="file"
            multiple
            accept="image/jpeg,image/png,image/webp"
            onChange={(e) => {
              select(Array.from(e.target.files ?? []));
              e.target.value = "";
            }}
          />
        </Button>
        <Typography variant="caption" display="block" color="text.secondary">
          JPG / PNG / WebP · สูงสุด 5 รูป รูปละ 10 MB · หลีกเลี่ยงข้อมูลผู้ป่วย
        </Typography>
      </Box>
      {error && <Alert severity="warning">{error}</Alert>}
      {!!files.length && (
        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: "repeat(auto-fit,minmax(140px,1fr))",
            gap: 1,
          }}
        >
          {files.map((file, i) => (
            <SelectedPhoto
              key={`${file.name}-${i}`}
              file={file}
              disabled={disabled}
              remove={() => {
                setFiles(files.filter((_, n) => n !== i));
                setError("");
              }}
            />
          ))}
        </Box>
      )}
    </Stack>
  );
}
export function RepairUpdating({ busy }: { busy: boolean }) {
  return (
    <Box sx={{ height: 3 }} aria-live="polite">
      {busy && (
        <LinearProgress aria-label="กำลังอัปเดตข้อมูล" sx={{ height: 3 }} />
      )}
    </Box>
  );
}
