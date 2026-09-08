import { useState } from "react";
import {
  Alert,
  Button,
  Container,
  MenuItem,
  Paper,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useNavigate, useParams } from "react-router-dom";
import {
  createFleetMaintenance,
  getFleetMaintenanceDetail,
  getFleetVehicles,
  getMaintenanceTypes,
  updateFleetMaintenance,
} from "../api/fleetApi";

export function FleetMaintenanceFormPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [error, setError] = useState("");
  const detail = useQuery({
    queryKey: ["fleet-maintenance", id],
    queryFn: () => getFleetMaintenanceDetail(id!),
    enabled: Boolean(id),
  });
  const types = useQuery({
    queryKey: ["fleet-maintenance-types"],
    queryFn: getMaintenanceTypes,
  });
  const vehicles = useQuery({
    queryKey: ["fleet-vehicles"],
    queryFn: getFleetVehicles,
  });
  const initial = detail.data;
  const [draft, setDraft] = useState<Record<string, string>>({});
  const value = (key: string, fallback: unknown = "") =>
    draft[key] ?? String(fallback ?? "");
  const selectedType = types.data?.find(
    (x) => x.id === value("maintenanceTypeId", initial?.maintenanceTypeId),
  );
  const mutation = useMutation({
    mutationFn: async () => {
      const dueDate = value("dueDate", initial?.dueDate);
      const dueMileage = value("dueMileage", initial?.dueMileage);
      if (selectedType?.isDateBased && !dueDate)
        throw new Error("ประเภทนี้ต้องระบุวันครบกำหนด");
      if (selectedType?.isMileageBased && !dueMileage)
        throw new Error("ประเภทนี้ต้องระบุเลขไมล์ครบกำหนด");
      const body = {
        vehicleId: value("vehicleId", initial?.vehicleId),
        maintenanceTypeId: value(
          "maintenanceTypeId",
          initial?.maintenanceTypeId,
        ),
        dueDate: dueDate ? new Date(dueDate).toISOString() : null,
        dueMileage: dueMileage ? Number(dueMileage) : null,
        reminderDays:
          Number(value("reminderDays", initial?.reminderDays) || 0) || null,
        reminderMileage:
          Number(value("reminderMileage", initial?.reminderMileage) || 0) ||
          null,
        recurrenceDays:
          Number(value("recurrenceDays", initial?.recurrenceDays) || 0) || null,
        recurrenceMileage:
          Number(value("recurrenceMileage", initial?.recurrenceMileage) || 0) ||
          null,
        notes: value("notes", initial?.notes),
        concurrencyToken: initial?.concurrencyToken,
      };
      return id
        ? updateFleetMaintenance(id, body)
        : createFleetMaintenance(body);
    },
    onSuccess: (r) => navigate(`/fleet/maintenance/${id ?? r.id}`),
    onError: (e: Error) =>
      setError(
        e.message.includes("409")
          ? "ข้อมูลถูกแก้ไขโดยผู้อื่น กรุณารีเฟรช"
          : "บันทึกไม่สำเร็จ: " + e.message,
      ),
  });
  const set = (key: string) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setDraft((x) => ({ ...x, [key]: e.target.value }));
  return (
    <Container maxWidth="md">
      <Typography variant="h4" mb={2}>
        {id ? "แก้ไข" : "สร้าง"}แผนบำรุงรักษา
      </Typography>
      <Paper sx={{ p: 3 }}>
        <Stack spacing={2}>
          {error && <Alert severity="error">{error}</Alert>}
          <TextField
            select
            required
            label="รถ"
            value={value("vehicleId", initial?.vehicleId)}
            onChange={set("vehicleId")}
            disabled={Boolean(id)}
          >
            {vehicles.data
              ?.filter((x) => x.isActive)
              .map((x) => (
                <MenuItem key={x.id} value={x.id}>
                  {x.vehicleCode} · {x.registrationNumber}
                </MenuItem>
              ))}
          </TextField>
          <TextField
            select
            required
            label="ประเภทบำรุงรักษา"
            value={value("maintenanceTypeId", initial?.maintenanceTypeId)}
            onChange={set("maintenanceTypeId")}
            disabled={Boolean(id)}
          >
            {types.data
              ?.filter((x) => x.isActive)
              .map((x) => (
                <MenuItem key={x.id} value={x.id}>
                  {x.name}
                </MenuItem>
              ))}
          </TextField>
          <TextField
            label="วันครบกำหนด (Asia/Bangkok)"
            type="datetime-local"
            InputLabelProps={{ shrink: true }}
            value={value("dueDate", initial?.dueDate?.slice(0, 16))}
            onChange={set("dueDate")}
          />
          <TextField
            label="เลขไมล์ครบกำหนด"
            type="number"
            value={value("dueMileage", initial?.dueMileage)}
            onChange={set("dueMileage")}
          />
          <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
            <TextField
              fullWidth
              label="เตือนล่วงหน้า (วัน)"
              type="number"
              value={value("reminderDays", initial?.reminderDays)}
              onChange={set("reminderDays")}
            />
            <TextField
              fullWidth
              label="เตือนล่วงหน้า (กม.)"
              type="number"
              value={value("reminderMileage", initial?.reminderMileage)}
              onChange={set("reminderMileage")}
            />
          </Stack>
          <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
            <TextField
              fullWidth
              label="ทำซ้ำทุก (วัน)"
              type="number"
              value={value("recurrenceDays", initial?.recurrenceDays)}
              onChange={set("recurrenceDays")}
            />
            <TextField
              fullWidth
              label="ทำซ้ำทุก (กม.)"
              type="number"
              value={value("recurrenceMileage", initial?.recurrenceMileage)}
              onChange={set("recurrenceMileage")}
            />
          </Stack>
          <TextField
            multiline
            minRows={3}
            label="หมายเหตุ"
            value={value("notes", initial?.notes)}
            onChange={set("notes")}
          />
          <Stack direction="row" spacing={1} justifyContent="flex-end">
            <Button onClick={() => navigate(-1)}>ยกเลิก</Button>
            <Button
              variant="contained"
              disabled={
                mutation.isPending ||
                !value("vehicleId", initial?.vehicleId) ||
                !value("maintenanceTypeId", initial?.maintenanceTypeId)
              }
              onClick={() => mutation.mutate()}
            >
              {mutation.isPending ? "กำลังบันทึก..." : "บันทึก"}
            </Button>
          </Stack>
        </Stack>
      </Paper>
    </Container>
  );
}
