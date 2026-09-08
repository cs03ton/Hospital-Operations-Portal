import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Container,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { Link, useParams, useSearchParams } from "react-router-dom";
import { getFleetMaintenance } from "../api/fleetApi";
export function FleetMaintenancePage() {
  const { vehicleId } = useParams();
  const [params, setParams] = useSearchParams();
  const key = params.toString();
  const q = useQuery({
    queryKey: ["fleet-maintenance", vehicleId, key],
    queryFn: () =>
      getFleetMaintenance({
        vehicleId,
        status: params.get("status") || undefined,
        overdueOnly: params.get("overdueOnly") === "true",
        page: Number(params.get("page") || 1),
      }),
  });
  return (
    <Container>
      <Stack
        direction={{ xs: "column", sm: "row" }}
        justifyContent="space-between"
        gap={2}
        mb={2}
      >
        <Typography variant="h4">Vehicle Maintenance</Typography>
        <Button
          component={Link}
          to="/fleet/maintenance/create"
          variant="contained"
        >
          สร้างแผน
        </Button>
      </Stack>
      <Stack direction={{ xs: "column", sm: "row" }} spacing={2} mb={2}>
        <TextField
          select
          size="small"
          label="สถานะ"
          value={params.get("status") ?? ""}
          onChange={(e) =>
            setParams((p) => {
              e.target.value
                ? p.set("status", e.target.value)
                : p.delete("status");
              return p;
            })
          }
        >
          <MenuItem value="">ทั้งหมด</MenuItem>
          <MenuItem value="ACTIVE">Upcoming</MenuItem>
          <MenuItem value="IN_PROGRESS">In progress</MenuItem>
        </TextField>
        <Button
          variant={
            params.get("overdueOnly") === "true" ? "contained" : "outlined"
          }
          onClick={() =>
            setParams((p) => {
              p.set("overdueOnly", String(p.get("overdueOnly") !== "true"));
              return p;
            })
          }
        >
          Overdue only
        </Button>
      </Stack>
      {q.isLoading && (
        <Box textAlign="center" py={6}>
          กำลังโหลด...
        </Box>
      )}
      {q.isError && (
        <Alert
          severity="error"
          action={<Button onClick={() => q.refetch()}>ลองใหม่</Button>}
        >
          โหลดข้อมูลไม่สำเร็จ
        </Alert>
      )}
      {q.data?.items.length === 0 && (
        <Alert severity="info">ยังไม่มีรายการบำรุงรักษา</Alert>
      )}
      <Stack spacing={1}>
        {q.data?.items.map((x) => (
          <Card
            key={x.id}
            component={Link}
            to={`/fleet/maintenance/${x.id}`}
            sx={{ textDecoration: "none" }}
          >
            <CardContent>
              <Box display="flex" justifyContent="space-between">
                <Typography>
                  {x.vehicle} · {x.type}
                </Typography>
                <Chip
                  label={x.dueState}
                  color={x.dueState === "OVERDUE" ? "error" : "default"}
                />
              </Box>
              <Typography color="text.secondary">
                {x.dueDate
                  ? new Date(x.dueDate).toLocaleString("th-TH", {
                      timeZone: "Asia/Bangkok",
                    })
                  : `เลขไมล์ ${x.dueMileage}`}
              </Typography>
            </CardContent>
          </Card>
        ))}
      </Stack>
    </Container>
  );
}
