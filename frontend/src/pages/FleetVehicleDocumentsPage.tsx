import { useState } from "react";
import {
  Alert,
  Button,
  Card,
  CardContent,
  Container,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useParams } from "react-router-dom";
import {
  createVehicleDocument,
  disableVehicleDocument,
  getVehicleDocuments,
} from "../api/fleetApi";
export function FleetVehicleDocumentsPage() {
  const { vehicleId } = useParams();
  const qc = useQueryClient();
  const q = useQuery({
    queryKey: ["fleet-documents", vehicleId],
    queryFn: () => getVehicleDocuments(vehicleId!),
  });
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState({
    documentType: "",
    documentNumber: "",
    issuedAt: "",
    expiresAt: "",
    provider: "",
    notes: "",
  });
  const create = useMutation({
    mutationFn: () =>
      createVehicleDocument(vehicleId!, {
        ...form,
        issuedAt: form.issuedAt ? new Date(form.issuedAt).toISOString() : null,
        expiresAt: form.expiresAt
          ? new Date(form.expiresAt).toISOString()
          : null,
        isRequired: true,
      }),
    onSuccess: () => {
      setOpen(false);
      void qc.invalidateQueries({ queryKey: ["fleet-documents", vehicleId] });
    },
  });
  return (
    <Container>
      <Stack direction="row" justifyContent="space-between" mb={2}>
        <Typography variant="h4">เอกสารรถ</Typography>
        <Button variant="contained" onClick={() => setOpen(true)}>
          เพิ่มเอกสาร
        </Button>
      </Stack>
      {q.isError && <Alert severity="error">โหลดเอกสารไม่สำเร็จ</Alert>}
      {q.data?.length === 0 && <Alert severity="info">ยังไม่มีเอกสาร</Alert>}
      <Stack spacing={1}>
        {q.data?.map((d) => (
          <Card key={d.id}>
            <CardContent>
              <Typography>
                {d.documentType} · {d.documentNumber}
              </Typography>
              <Typography
                color={
                  d.expiresAt && new Date(d.expiresAt) < new Date()
                    ? "error"
                    : "text.secondary"
                }
              >
                {d.expiresAt
                  ? `หมดอายุ ${new Date(d.expiresAt).toLocaleDateString("th-TH", { timeZone: "Asia/Bangkok" })}`
                  : "ไม่ระบุวันหมดอายุ"}
              </Typography>
              <Button
                color="error"
                onClick={() =>
                  void disableVehicleDocument(
                    d.id,
                    d.concurrencyToken,
                    "ปิดใช้งานโดยผู้ดูแล",
                  ).then(() => q.refetch())
                }
              >
                ปิดใช้งาน
              </Button>
            </CardContent>
          </Card>
        ))}
      </Stack>
      <Dialog open={open} onClose={() => setOpen(false)}>
        <DialogTitle>เพิ่มเอกสารรถ</DialogTitle>
        <DialogContent>
          <Stack spacing={2} mt={1}>
            {Object.keys(form).map((k) => (
              <TextField
                key={k}
                label={k}
                type={k.endsWith("At") ? "date" : "text"}
                InputLabelProps={{ shrink: true }}
                onChange={(e) =>
                  setForm((x) => ({ ...x, [k]: e.target.value }))
                }
              />
            ))}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpen(false)}>ยกเลิก</Button>
          <Button
            variant="contained"
            disabled={!form.documentType || create.isPending}
            onClick={() => create.mutate()}
          >
            บันทึก
          </Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
}
