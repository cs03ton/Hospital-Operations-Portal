import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Divider,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useParams } from "react-router-dom";
import {
  assignFleetRequest,
  fleetWorkflowAction,
  FLEET_DASHBOARD_QUERY_KEY,
  getFleetAvailability,
  getFleetFeedbackEligibleTrips,
  getFleetRequest,
  replaceFleetAssignment,
  transitionFleetRequest,
} from "../api/fleetApi";
import { PageHeader } from "../components/PageHeader";
import { useAuth } from "../context/AuthContext";
import { usePermission } from "../context/PermissionContext";
import { formatThaiDateTime } from "../utils/dateFormat";
import { getFleetStatusLabel } from "../utils/fleetLabels";
import { useState } from "react";
import { useNotification } from "../hooks/useNotification";

export function FleetRequestDetailPage() {
  const { id } = useParams();
  const client = useQueryClient();
  const { hasPermission } = usePermission();
  const { user } = useAuth();
  const notify = useNotification();
  const { data, isLoading } = useQuery({
    queryKey: ["fleet", "request", id],
    queryFn: () => getFleetRequest(id!),
  });
  const { data: feedbackTrips = [] } = useQuery({
    queryKey: ["fleet", "feedback-eligible-trips", user?.id],
    queryFn: getFleetFeedbackEligibleTrips,
    enabled: data?.status === "COMPLETED" && hasPermission("FleetFeedback.ViewOwn"),
  });
  const isDispatcher = hasPermission("FleetDispatch.View");
  const canReplace =
    hasPermission("FleetDispatch.ReplaceAssignment") ||
    hasPermission("FleetDispatch.ReplaceApprovedAssignment");
  const { data: availability } = useQuery({
    queryKey: ["fleet", "availability", id],
    queryFn: () => getFleetAvailability(id!),
    enabled: Boolean(
      data &&
        isDispatcher &&
        (data.status === "PENDING_DISPATCH" ||
          (canReplace && data.activeAssignment)),
    ),
  });
  const [reason, setReason] = useState("");
  const [vehicleId, setVehicleId] = useState("");
  const [driverId, setDriverId] = useState("");
  const [reviewAction, setReviewAction] = useState<"approve" | "return" | "reject" | null>(null);
  const [reviewReason, setReviewReason] = useState("");
  const [reviewReturnTarget, setReviewReturnTarget] = useState("");
  const refresh = async () => {
    await Promise.all([
      client.invalidateQueries({ queryKey: ["fleet"] }),
      client.invalidateQueries({ queryKey: FLEET_DASHBOARD_QUERY_KEY }),
    ]);
  };
  const transition = useMutation({
    mutationFn: ({
      action,
      actionReason = reason,
    }: {
      action: "submit" | "cancel" | "return" | "reject";
      actionReason?: string;
    }) =>
      transitionFleetRequest(id!, action, data!.concurrencyToken, actionReason),
    onSuccess: refresh,
  });
  const assign = useMutation({
    mutationFn: () =>
      assignFleetRequest(
        id!,
        vehicleId,
        driverId,
        data!.concurrencyToken,
        reason,
      ),
    onSuccess: refresh,
  });
  const replace = useMutation({
    mutationFn: () =>
      replaceFleetAssignment(data!.activeAssignment!.id, {
        vehicleId: vehicleId || undefined,
        driverUserId: driverId || undefined,
        requestConcurrencyToken: data!.concurrencyToken,
        assignmentConcurrencyToken: data!.activeAssignment!.concurrencyToken,
        reason,
      }),
    onSuccess: () => {
      setReason("");
      setVehicleId("");
      setDriverId("");
      void refresh();
    },
  });
  const isAdminReviewStage = data?.status === "PENDING_ADMIN_REVIEW";
  const isDirectorReviewStage = data?.status === "PENDING_DIRECTOR_APPROVAL" || data?.status === "PENDING_DIRECTOR";
  const canAdminReview = isAdminReviewStage && hasPermission("FleetAdminReview.Approve");
  const canDirectorReview = isDirectorReviewStage && hasPermission("FleetDirector.Approve");
  const reviewKind = canAdminReview ? "admin-review" : canDirectorReview ? "director-approval" : null;
  const feedbackTrip = feedbackTrips.find((trip) => trip.requestNo === data?.requestNo && trip.feedbackStatus === "AVAILABLE");
  const review = useMutation({
    mutationFn: () => fleetWorkflowAction(reviewKind!, id!, reviewAction!, data!.concurrencyToken, reviewAction === "approve" ? undefined : reviewReason.trim(), reviewAction === "return" ? reviewReturnTarget : undefined),
    onSuccess: async () => {
      notify.showSuccess(reviewAction === "approve" ? "อนุมัติและส่งต่อเรียบร้อยแล้ว" : reviewAction === "return" ? "ส่งคำขอกลับเรียบร้อยแล้ว" : "ไม่อนุมัติคำขอเรียบร้อยแล้ว");
      setReviewAction(null); setReviewReason(""); setReviewReturnTarget(""); await refresh();
    },
    onError: () => notify.showError("ดำเนินการ Review ไม่สำเร็จ ข้อมูลอาจมีการเปลี่ยนแปลง กรุณารีเฟรชแล้วลองใหม่"),
  });
  if (isLoading || !data) return <Typography>กำลังโหลด...</Typography>;
  const isRequester = data.requesterUserId === user?.id;
  const isPassenger =
    !isRequester &&
    data.passengers.some((passenger) => passenger.userId === user?.id);
  const editable =
    isRequester &&
    (data.status === "DRAFT" ||
      (data.status === "RETURNED" && data.returnTarget === "REQUESTER"));
  const requesterCanCancel =
    isRequester &&
    ["DRAFT", "PENDING_DISPATCH", "PENDING_ADMIN_REVIEW", "PENDING_DIRECTOR", "RETURNED"].includes(data.status) &&
    !isDispatcher;
  const readyVehicles =
    availability?.vehicles.filter((x) => x.isAvailable) ?? [];
  const readyDrivers = availability?.drivers.filter((x) => x.isAvailable) ?? [];
  return (
    <>
      <PageHeader
        title={`คำขอ ${data.requestNo}`}
        subtitle={`${data.requesterName} · ${data.requesterDepartmentName ?? "-"}`}
      />
      {isPassenger && (
        <Alert severity="info" sx={{ mb: 2 }}>
          คุณเป็นผู้ร่วมเดินทางในคำขอนี้ สามารถดูรายละเอียดและติดตามสถานะได้
        </Alert>
      )}
      {(replace.isError || assign.isError || transition.isError) && (
        <Alert severity="error" sx={{ mb: 2 }}>
          ดำเนินการไม่สำเร็จ ข้อมูลอาจเปลี่ยนแปลงหรือไม่พร้อมใช้งาน
          กรุณาโหลดใหม่
        </Alert>
      )}
      <Stack spacing={2}>
        <Card>
          <CardContent>
            <Stack direction="row" justifyContent="space-between">
              <Chip label={getFleetStatusLabel(data.status)} />
              <Stack direction="row" spacing={1}>
                {editable && (
                  <Button
                    component={Link}
                    to={`/fleet/requests/${data.id}/edit`}
                  >
                    แก้ไข
                  </Button>
                )}
                {editable && (
                  <Button
                    variant="contained"
                    disabled={transition.isPending}
                    onClick={() => transition.mutate({ action: "submit" })}
                  >
                    ส่งคำขอ
                  </Button>
                )}
                {requesterCanCancel && (
                    <Button
                      color="error"
                      onClick={() => {
                        const value = window.prompt("ระบุเหตุผลการยกเลิก");
                        if (
                          value?.trim() &&
                          window.confirm("ยืนยันยกเลิกคำขอ?")
                        )
                          transition.mutate({
                            action: "cancel",
                            actionReason: value.trim(),
                          });
                      }}
                    >
                      ยกเลิก
                    </Button>
                  )}
                {reviewKind && (
                  <Button variant="contained" onClick={() => setReviewAction("approve")}>
                    {canAdminReview ? "Review และส่งต่อ ผอ." : "อนุมัติคำขอ"}
                  </Button>
                )}
                {reviewKind && <Button color="warning" variant="outlined" onClick={() => { setReviewReason(""); setReviewReturnTarget(canAdminReview ? "REQUESTER" : "ADMIN_REVIEW"); setReviewAction("return"); }}>ส่งกลับแก้ไข</Button>}
                {reviewKind && <Button color="error" variant="outlined" onClick={() => { setReviewReason(""); setReviewAction("reject"); }}>ไม่อนุมัติ</Button>}
                {feedbackTrip && (
                  <Button component={Link} to={`/fleet/trips/${feedbackTrip.tripId}/feedback`} variant="contained" color="success">
                    ให้ Feedback การเดินทาง
                  </Button>
                )}
              </Stack>
            </Stack>
            <Divider sx={{ my: 2 }} />
            <Grid container spacing={2}>
              <Info label="ภารกิจ" value={data.purpose} />
              <Info label="ประเภทภารกิจ" value={data.missionType} />
              <Info label="ปลายทาง" value={data.destination} />
              <Info
                label="ผู้ประสานงาน"
                value={`${data.contactPersonName} · ${data.contactPhone}`}
              />
              <Info
                label="ออกเดินทาง"
                value={formatThaiDateTime(data.departureAt)}
              />
              <Info
                label="คาดว่าจะกลับ"
                value={formatThaiDateTime(data.expectedReturnAt)}
              />
              <Info
                label="ผู้โดยสาร"
                value={`${data.passengerCount} คน (ไม่รวมคนขับ)`}
              />
              <Info
                label="ประเภทรถที่ต้องการ"
                value={data.requestedVehicleTypeName ?? "ไม่ระบุ"}
              />
            </Grid>
            {data.returnTarget && (
              <Alert severity="warning" sx={{ mt: 2 }}>
                ส่งกลับไปยัง {data.returnTarget}
              </Alert>
            )}
          </CardContent>
        </Card>
        {isDispatcher && data.status === "PENDING_DISPATCH" && (
          <Card>
            <CardContent>
              <Typography variant="h6" fontWeight={800}>
                จัดรถและคนขับ
              </Typography>
              <Grid container spacing={2} sx={{ mt: 0 }}>
                <Grid item xs={12} md={6}>
                  <TextField
                    select
                    fullWidth
                    label="รถที่พร้อม"
                    value={vehicleId}
                    onChange={(e) => setVehicleId(e.target.value)}
                  >
                    {readyVehicles.map((x) => (
                      <MenuItem key={x.id} value={x.id}>
                        {x.code} · {x.name}
                      </MenuItem>
                    ))}
                  </TextField>
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    select
                    fullWidth
                    label="คนขับที่พร้อม"
                    value={driverId}
                    onChange={(e) => setDriverId(e.target.value)}
                  >
                    {readyDrivers.map((x) => (
                      <MenuItem key={x.id} value={x.id}>
                        {x.name} · {x.monthTripCount} เที่ยวเดือนนี้
                      </MenuItem>
                    ))}
                  </TextField>
                </Grid>
                <Grid item xs={12}>
                  <TextField
                    fullWidth
                    label="เหตุผล/หมายเหตุ"
                    value={reason}
                    onChange={(e) => setReason(e.target.value)}
                  />
                </Grid>
              </Grid>
              <Stack
                direction={{ xs: "column", sm: "row" }}
                spacing={1}
                justifyContent="flex-end"
                sx={{ mt: 2 }}
              >
                <Button
                  color="error"
                  disabled={!reason.trim()}
                  onClick={() => transition.mutate({ action: "reject" })}
                >
                  ไม่รับคำขอ
                </Button>
                <Button
                  disabled={!reason.trim()}
                  onClick={() => transition.mutate({ action: "return" })}
                >
                  ส่งกลับผู้ขอ
                </Button>
                <Button
                  variant="contained"
                  disabled={!vehicleId || !driverId || assign.isPending}
                  onClick={() => assign.mutate()}
                >
                  ยืนยันการจัดรถ
                </Button>
              </Stack>
            </CardContent>
          </Card>
        )}
        {data.activeAssignment && (
          <Alert severity="success">
            จัดรถ {data.activeAssignment.vehicleCode} (
            {data.activeAssignment.registrationNumber}) · คนขับ{" "}
            {data.activeAssignment.driverName}
          </Alert>
        )}
        {data.activeAssignment && canReplace && (
          <Card>
            <CardContent>
              <Typography variant="h6">เปลี่ยนรถหรือคนขับ</Typography>
              <Stack
                direction={{ xs: "column", md: "row" }}
                spacing={1}
                sx={{ mt: 2 }}
              >
                <TextField
                  select
                  fullWidth
                  label="รถใหม่ (เว้นว่างเพื่อใช้เดิม)"
                  value={vehicleId}
                  onChange={(e) => setVehicleId(e.target.value)}
                >
                  <MenuItem value="">ใช้รถเดิม</MenuItem>
                  {readyVehicles.map((x) => (
                    <MenuItem key={x.id} value={x.id}>
                      {x.code} · {x.name}
                    </MenuItem>
                  ))}
                </TextField>
                <TextField
                  select
                  fullWidth
                  label="คนขับใหม่ (เว้นว่างเพื่อใช้เดิม)"
                  value={driverId}
                  onChange={(e) => setDriverId(e.target.value)}
                >
                  <MenuItem value="">ใช้คนขับเดิม</MenuItem>
                  {readyDrivers.map((x) => (
                    <MenuItem key={x.id} value={x.id}>
                      {x.name}
                    </MenuItem>
                  ))}
                </TextField>
                <TextField
                  fullWidth
                  required
                  label="เหตุผล"
                  value={reason}
                  onChange={(e) => setReason(e.target.value)}
                />
                <Button
                  variant="contained"
                  disabled={
                    replace.isPending ||
                    !reason.trim() ||
                    (!vehicleId && !driverId)
                  }
                  onClick={() => {
                    if (window.confirm("ยืนยันเปลี่ยน Assignment?"))
                      replace.mutate();
                  }}
                >
                  ยืนยัน
                </Button>
              </Stack>
            </CardContent>
          </Card>
        )}
        <Card>
          <CardContent>
            <Typography variant="h6" fontWeight={800}>
              ประวัติสถานะ
            </Typography>
            {data.statusHistories.length === 0 ? (
              <Typography color="text.secondary">
                ยังไม่มีประวัติสถานะ
              </Typography>
            ) : (
              data.statusHistories.map((x) => (
                <Box
                  key={x.id}
                  sx={{ py: 1, borderBottom: 1, borderColor: "divider" }}
                >
                  <Typography>
                    {getFleetStatusLabel(x.toStatus)} · {x.actorName ?? "-"}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {formatThaiDateTime(x.createdAt)}{" "}
                    {x.reason ? `· ${x.reason}` : ""}
                  </Typography>
                </Box>
              ))
            )}
          </CardContent>
        </Card>
      </Stack>
      <Dialog open={reviewAction !== null} onClose={() => !review.isPending && setReviewAction(null)} fullWidth maxWidth="sm">
        <DialogTitle>{reviewAction === "approve" ? "ยืนยันการอนุมัติ" : reviewAction === "return" ? "ส่งคำขอกลับแก้ไข" : "ยืนยันการไม่อนุมัติ"}</DialogTitle>
        <DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
          <Alert severity={reviewAction === "approve" ? "success" : reviewAction === "return" ? "warning" : "error"}>คำขอ {data.requestNo} · สถานะ {getFleetStatusLabel(data.status)}</Alert>
          {reviewAction === "return" && <TextField select label="ส่งกลับไปยัง" value={reviewReturnTarget} onChange={e => setReviewReturnTarget(e.target.value)}>{(canAdminReview ? [{value:"REQUESTER",label:"ผู้ขอ"},{value:"DISPATCHER",label:"งานยานพาหนะ"}] : [{value:"ADMIN_REVIEW",label:"หัวหน้าฝ่ายบริหาร"},{value:"DISPATCHER",label:"งานยานพาหนะ"}]).map(x => <MenuItem key={x.value} value={x.value}>{x.label}</MenuItem>)}</TextField>}
          {reviewAction !== "approve" && <TextField required multiline minRows={3} label="เหตุผล" value={reviewReason} onChange={e => setReviewReason(e.target.value)} helperText="กรุณาระบุเหตุผลให้ผู้รับดำเนินการต่อได้ถูกต้อง" />}
          {reviewAction === "approve" && <Typography color="text.secondary">ระบบจะตรวจสอบ permission, สถานะ และ concurrency ก่อนส่งไปขั้นตอนถัดไป</Typography>}
        </Stack></DialogContent>
        <DialogActions><Button disabled={review.isPending} onClick={() => setReviewAction(null)}>ยกเลิก</Button><Button variant="contained" color={reviewAction === "reject" ? "error" : reviewAction === "return" ? "warning" : "primary"} disabled={review.isPending || (reviewAction !== "approve" && !reviewReason.trim()) || (reviewAction === "return" && !reviewReturnTarget)} onClick={() => review.mutate()}>{review.isPending ? "กำลังดำเนินการ..." : "ยืนยัน"}</Button></DialogActions>
      </Dialog>
    </>
  );
}
function Info({ label, value }: { label: string; value: string }) {
  return (
    <Grid item xs={12} md={6}>
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Typography>{value}</Typography>
    </Grid>
  );
}
