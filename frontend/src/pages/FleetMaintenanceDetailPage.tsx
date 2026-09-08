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
import { Link, useParams } from "react-router-dom";
import {
  deleteMaintenanceAttachment,
  getFleetMaintenanceDetail,
  maintenanceAction,
  uploadMaintenanceAttachment,
} from "../api/fleetApi";
import { getFleetStatusLabel } from "../utils/fleetLabels";
export function FleetMaintenanceDetailPage() {
  const { id } = useParams();
  const qc = useQueryClient();
  const q = useQuery({
    queryKey: ["fleet-maintenance", id],
    queryFn: () => getFleetMaintenanceDetail(id!),
  });
  const [dialog, setDialog] = useState<"start" | "complete" | "cancel" | null>(null);
  const [form, setForm] = useState({
    completedMileage: "",
    startMileage: "",
    note: "",
    cost: "",
    vendor: "",
    invoiceNumber: "",
    result: "",
    overrideReason: "",
    reason: "",
  });
  const mutate = useMutation({
    mutationFn: (action: "start" | "complete" | "cancel") =>
      maintenanceAction(id!, action, {
        ...form,
        startMileage: form.startMileage ? Number(form.startMileage) : null,
        completedMileage: form.completedMileage
          ? Number(form.completedMileage)
          : null,
        cost: form.cost ? Number(form.cost) : null,
        concurrencyToken: q.data!.concurrencyToken,
      }),
    onSuccess: () => {
      setDialog(null);
      void qc.invalidateQueries({ queryKey: ["fleet-maintenance", id] });
    },
  });
  if (q.isLoading) return <Typography p={4}>กำลังโหลด...</Typography>;
  if (q.error || !q.data)
    return <Alert severity="error">โหลดรายละเอียดไม่สำเร็จ</Alert>;
  const d = q.data;
  const upload = async (file?: File) => {
    if (!file) return;
    const record = d.records[d.records.length - 1];
    if (!record) throw new Error("ต้อง complete maintenance ก่อนแนบไฟล์");
    await uploadMaintenanceAttachment(record.id, file);
    await q.refetch();
  };
  return (
    <Container>
      <Stack direction="row" justifyContent="space-between" mb={2}>
        <Typography variant="h4">Maintenance Detail</Typography>
        <Button component={Link} to={`/fleet/maintenance/${id}/edit`}>
          แก้ไข
        </Button>
      </Stack>
      {mutate.error && (
        <Alert severity="error">
          ดำเนินการไม่สำเร็จ อาจเกิด concurrency conflict กรุณารีเฟรช
        </Alert>
      )}
      <Card>
        <CardContent>
          <Typography>สถานะ: {getFleetStatusLabel(d.status)}</Typography>
          <Typography>
            Due:{" "}
            {d.dueDate
              ? new Date(d.dueDate).toLocaleString("th-TH", {
                  timeZone: "Asia/Bangkok",
                })
              : d.dueMileage}
          </Typography>
          <Stack direction="row" spacing={1} mt={2}>
            {d.status === "ACTIVE" && (
              <Button
                variant="contained"
                disabled={mutate.isPending}
                onClick={() => setDialog("start")}
              >
                เริ่มงาน
              </Button>
            )}
            {d.status === "IN_PROGRESS" && (
              <Button variant="contained" onClick={() => setDialog("complete")}>
                เสร็จสิ้นงานบำรุงรักษา
              </Button>
            )}
            {!["COMPLETED", "CANCELLED"].includes(d.status) && (
              <Button color="error" onClick={() => setDialog("cancel")}>
                ยกเลิก
              </Button>
            )}
          </Stack>
        </CardContent>
      </Card>
      <Typography variant="h6" mt={3}>
        ไฟล์แนบ
      </Typography>
      <input
        type="file"
        accept=".pdf,.jpg,.jpeg,.png"
        onChange={(e) =>
          void upload(e.target.files?.[0]).catch(() => undefined)
        }
      />
      {d.records.flatMap((r) =>
        r.attachments
          .filter((a) => !a.isDeleted)
          .map((a) => (
            <Stack direction="row" key={a.id} alignItems="center">
              <Button href={`/api/fleet/maintenance/attachments/${a.id}`}>
                {a.originalFileName}
              </Button>
              <Button
                color="error"
                onClick={() =>
                  void deleteMaintenanceAttachment(a.id).then(() => q.refetch())
                }
              >
                ลบ
              </Button>
            </Stack>
          )),
      )}
      <Dialog open={dialog !== null} onClose={() => setDialog(null)}>
        <DialogTitle>
          {dialog === "start" ? "เริ่ม maintenance" : dialog === "complete" ? "Complete maintenance" : "ยกเลิก maintenance"}
        </DialogTitle>
        <DialogContent>
          <Stack spacing={2} mt={1}>
            {dialog === "start" ? (
              <>
                <TextField label="เลขไมล์เริ่มต้น" type="number" onChange={(e) => setForm((x) => ({ ...x, startMileage: e.target.value }))} />
                <TextField label="Vendor" onChange={(e) => setForm((x) => ({ ...x, vendor: e.target.value }))} />
                <TextField label="หมายเหตุ" multiline onChange={(e) => setForm((x) => ({ ...x, note: e.target.value }))} />
              </>
            ) : dialog === "complete" ? (
              <>
                <TextField
                  label="เลขไมล์เมื่อเสร็จ"
                  type="number"
                  onChange={(e) =>
                    setForm((x) => ({ ...x, completedMileage: e.target.value }))
                  }
                />
                <TextField
                  label="ค่าใช้จ่าย"
                  type="number"
                  onChange={(e) =>
                    setForm((x) => ({ ...x, cost: e.target.value }))
                  }
                />
                <TextField
                  label="Vendor"
                  onChange={(e) =>
                    setForm((x) => ({ ...x, vendor: e.target.value }))
                  }
                />
                <TextField
                  label="Invoice"
                  onChange={(e) =>
                    setForm((x) => ({ ...x, invoiceNumber: e.target.value }))
                  }
                />
                <TextField
                  label="ผลการดำเนินงาน"
                  onChange={(e) =>
                    setForm((x) => ({ ...x, result: e.target.value }))
                  }
                />
                <TextField
                  label="เหตุผล Override (แสดงเมื่อเลขไมล์ต่ำกว่าค่ารถ)"
                  onChange={(e) =>
                    setForm((x) => ({ ...x, overrideReason: e.target.value }))
                  }
                />
              </>
            ) : (
              <TextField
                required
                label="เหตุผล"
                multiline
                onChange={(e) =>
                  setForm((x) => ({ ...x, reason: e.target.value }))
                }
              />
            )}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialog(null)}>ปิด</Button>
          <Button
            variant="contained"
            color={dialog === "cancel" ? "error" : "primary"}
            disabled={mutate.isPending || (dialog === "cancel" && !form.reason)}
            onClick={() => mutate.mutate(dialog!)}
          >
            ยืนยัน
          </Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
}
