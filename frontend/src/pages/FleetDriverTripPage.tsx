/* eslint-disable react-hooks/exhaustive-deps */
import { useEffect, useMemo, useState } from "react";
import {
  Alert,
  Button,
  Card,
  CardContent,
  Checkbox,
  FormControlLabel,
  LinearProgress,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { useQueryClient } from "@tanstack/react-query";
import { useNavigate, useParams } from "react-router-dom";
import {
  completeFleetTrip,
  deleteTripAttachment,
  FLEET_DASHBOARD_QUERY_KEY,
  getDriverJob,
  getTripAttachments,
  startFleetTrip,
  tripAttachmentDownloadUrl,
  uploadTripAttachment,
  type DriverJobDetail,
  type TripAttachment,
} from "../api/fleetApi";
import { notifyGlobal } from "../contexts/NotificationContext";
import { ActionDialog } from "../components/common/ActionDialog";

const key = () => crypto.randomUUID();
export function FleetDriverTripPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [job, setJob] = useState<DriverJobDetail | null>(null);
  const [files, setFiles] = useState<TripAttachment[]>([]);
  const [mileage, setMileage] = useState("");
  const [note, setNote] = useState("");
  const [safe, setSafe] = useState(false);
  const [busy, setBusy] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState("");
  const [unknown, setUnknown] = useState(false);
  const [confirmAction, setConfirmAction] = useState<"start" | "complete" | null>(null);
  const load = async () => {
    setError("");
    try {
      const j = await getDriverJob(id!);
      setJob(j);
      const latestMileage = j.trip?.endMileage ??
        j.trip?.startMileage ??
        (j.vehicle.currentMileage > 0 ? j.vehicle.currentMileage : null);
      setMileage(latestMileage == null ? "" : String(latestMileage));
      try {
        setFiles(await getTripAttachments(id!));
      } catch {
        setFiles([]);
      }
    } catch {
      setError("โหลดสถานะล่าสุดไม่สำเร็จ");
    }
  };
  useEffect(() => {
    void load();
  }, [id]);
  const distance = useMemo(
    () => (job?.trip ? Number(mileage) - job.trip.startMileage : 0),
    [job, mileage],
  );
  const execute = async (kind: "start" | "complete") => {
    if (!job || busy) return;
    setBusy(true);
    setUnknown(false);
    setError("");
    try {
      if (kind === "start")
        await startFleetTrip(
          id!,
          {
            concurrencyToken: job.concurrencyToken,
            startMileage: Number(mileage),
            tripNotes: note,
          },
          key(),
        );
      else
        await completeFleetTrip(
          id!,
          {
            concurrencyToken: job.concurrencyToken,
            tripConcurrencyToken: job.trip?.concurrencyToken,
            endMileage: Number(mileage),
            completionNotes: note,
          },
          key(),
        );
      await queryClient.invalidateQueries({ queryKey: FLEET_DASHBOARD_QUERY_KEY });
      notifyGlobal(
        "success",
        kind === "start"
          ? "บันทึกเลขไมล์และเริ่มเดินทางเรียบร้อยแล้ว"
          : "บันทึกเลขไมล์และจบทริปเรียบร้อยแล้ว",
      );
      navigate("/fleet/driver/jobs", { replace: true });
    } catch {
      setConfirmAction(null);
      setUnknown(true);
      setError(
        "ไม่ทราบว่าคำสั่งสำเร็จหรือไม่ ห้ามกดซ้ำจนกว่าจะโหลดสถานะล่าสุด",
      );
      notifyGlobal(
        "error",
        "ดำเนินการไม่สำเร็จ กรุณาโหลดสถานะล่าสุดก่อนลองใหม่",
      );
    } finally {
      setBusy(false);
    }
  };
  const upload = async (list: FileList | null) => {
    if (!list) return;
    setUploading(true);
    setError("");
    try {
      for (const f of Array.from(list)) {
        if (
          f.size > 10 * 1024 * 1024 ||
          ![/image\/jpeg/, /image\/png/, /application\/pdf/].some((x) =>
            x.test(f.type),
          )
        )
          throw new Error();
        await uploadTripAttachment(id!, f, key());
      }
      await load();
    } catch {
      setError(
        "อัปโหลดไม่สำเร็จ: รองรับ JPG/JPEG/PNG/PDF ไม่เกิน policy ของระบบ",
      );
    } finally {
      setUploading(false);
    }
  };
  if (!job)
    return (
      <>
        {error ? (
          <Alert
            severity="error"
            action={<Button onClick={() => void load()}>ลองใหม่</Button>}
          >
            {error}
          </Alert>
        ) : (
          <Typography>กำลังโหลด…</Typography>
        )}
      </>
    );
  const start = job.status === "READY";
  const complete = job.status === "IN_PROGRESS";
  return (
    <Stack
      spacing={2}
      maxWidth={620}
      sx={{ pb: "calc(96px + env(safe-area-inset-bottom))" }}
    >
      <Typography variant="h4">
        {start ? "เริ่มเดินทาง" : complete ? "จบทริป" : "สรุปทริป"}
      </Typography>
      <Alert severity={job.priority === "EMERGENCY" ? "error" : "info"}>
        {job.requestNo} · {job.vehicle.vehicleCode}{" "}
        {job.vehicle.registrationNumber} · {job.destination}
      </Alert>
      {error && (
        <Alert
          severity="error"
          action={
            unknown ? (
              <Button onClick={() => void load()}>โหลดสถานะล่าสุด</Button>
            ) : undefined
          }
        >
          {error}
        </Alert>
      )}
      <Card>
        <CardContent>
          <Stack spacing={2}>
            <Typography>
              เลขไมล์รถล่าสุด: {job.vehicle.currentMileage > 0 ? `${job.vehicle.currentMileage.toLocaleString()} กม.` : "ยังไม่มีข้อมูล"}
            </Typography>
            {job.trip && (
              <Typography>
                เลขไมล์เริ่ม: {job.trip.startMileage.toLocaleString()}
              </Typography>
            )}
            <TextField
              type="number"
              required
              label={start ? "เลขไมล์ก่อนเริ่มเดินทาง" : "เลขไมล์เมื่อจบทริป"}
              value={mileage}
              onChange={(e) => setMileage(e.target.value)}
              helperText={start
                ? job.vehicle.currentMileage > 0
                  ? "กรอกค่าเริ่มต้นจากเลขไมล์ล่าสุดของรถให้อัตโนมัติ กรุณาตรวจสอบกับหน้าปัดรถ"
                  : "ยังไม่มีเลขไมล์ล่าสุด กรุณากรอกเลขไมล์จากหน้าปัดรถ"
                : "กรุณากรอกเลขไมล์จากหน้าปัดรถเมื่อจบทริป"}
              inputProps={{
                inputMode: "numeric",
                min: start
                  ? job.vehicle.currentMileage
                  : job.trip?.startMileage,
              }}
            />
            {complete && (
              <Typography>
                ระยะทาง: {Math.max(0, distance).toLocaleString()} กม.
              </Typography>
            )}
            <TextField
              multiline
              label={start ? "หมายเหตุเริ่มทริป" : "Completion note / incident"}
              value={note}
              onChange={(e) => setNote(e.target.value)}
            />
            {start && (
              <FormControlLabel
                control={
                  <Checkbox checked={safe} onChange={(_, v) => setSafe(v)} />
                }
                label="ตรวจสภาพรถและยืนยันว่าปลอดภัยพร้อมเดินทาง"
              />
            )}
          </Stack>
        </CardContent>
      </Card>
      <Card>
        <CardContent>
          <Stack spacing={1}>
            <Typography variant="h6">ไฟล์แนบ ({files.length})</Typography>
            <Button
              component="label"
              variant="outlined"
              disabled={uploading}
              sx={{ minHeight: 48 }}
            >
              ถ่ายภาพ/เลือกไฟล์
              <input
                hidden
                multiple
                type="file"
                accept="image/jpeg,image/png,application/pdf"
                onChange={(e) => void upload(e.target.files)}
              />
            </Button>
            {uploading && <LinearProgress />}
            {files.length === 0 && (
              <Typography color="text.secondary">ยังไม่มีไฟล์แนบ</Typography>
            )}
            {files.map((f) => (
              <Stack
                key={f.id}
                direction="row"
                justifyContent="space-between"
                alignItems="center"
              >
                <Button href={tripAttachmentDownloadUrl(f.id)}>
                  {f.originalFileName}
                </Button>
                <Button
                  color="error"
                  onClick={() => {
                    if (window.confirm("ยืนยันลบไฟล์แนบ?"))
                      void deleteTripAttachment(f.id).then(load);
                  }}
                >
                  ลบ
                </Button>
              </Stack>
            ))}
          </Stack>
        </CardContent>
      </Card>
      {(start || complete) && (
        <Button
          sx={{
            position: "sticky",
            bottom: "calc(8px + env(safe-area-inset-bottom))",
            minHeight: 52,
          }}
          variant="contained"
          disabled={
            busy ||
            unknown ||
            !mileage ||
            (start && !safe) ||
            (complete && distance < 0)
          }
          onClick={() => setConfirmAction(start ? "start" : "complete")}
        >
          {busy ? "กำลังส่ง…" : start ? "เริ่มเดินทาง" : "จบทริป"}
        </Button>
      )}
      <ActionDialog
        open={confirmAction !== null}
        title={
          confirmAction === "start"
            ? "ยืนยันเริ่มเดินทาง"
            : "ยืนยันจบทริป"
        }
        confirmLabel={
          confirmAction === "start" ? "ยืนยันเริ่มเดินทาง" : "ยืนยันจบทริป"
        }
        cancelLabel="ยกเลิก"
        isLoading={busy}
        onClose={() => {
          if (!busy) setConfirmAction(null);
        }}
        onConfirm={() => {
          if (!confirmAction) return;
          void execute(confirmAction);
        }}
      >
        <Stack spacing={1.5}>
          <Alert severity={confirmAction === "start" ? "info" : "warning"}>
            {confirmAction === "start"
              ? "กรุณาตรวจสอบเลขไมล์เริ่มต้นก่อนยืนยัน เมื่อบันทึกแล้วระบบจะเปลี่ยนสถานะเป็นกำลังเดินทาง"
              : "กรุณาตรวจสอบเลขไมล์สิ้นสุดก่อนยืนยัน เมื่อบันทึกแล้วระบบจะปิดงานทริปนี้"}
          </Alert>
          <Typography>
            เลขที่คำขอ: <strong>{job.requestNo}</strong>
          </Typography>
          <Typography>
            {confirmAction === "start" ? "เลขไมล์เริ่มต้น" : "เลขไมล์สิ้นสุด"}: {Number(mileage).toLocaleString()} กม.
          </Typography>
          {confirmAction === "complete" && (
            <Typography>
              ระยะทางรวม: {Math.max(0, distance).toLocaleString()} กม.
            </Typography>
          )}
        </Stack>
      </ActionDialog>
    </Stack>
  );
}
