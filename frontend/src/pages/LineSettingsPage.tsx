import CheckCircleOutlineIcon from "@mui/icons-material/CheckCircleOutline";
import ClearOutlinedIcon from "@mui/icons-material/ClearOutlined";
import ContentCopyOutlinedIcon from "@mui/icons-material/ContentCopyOutlined";
import DataObjectOutlinedIcon from "@mui/icons-material/DataObjectOutlined";
import HelpOutlineOutlinedIcon from "@mui/icons-material/HelpOutlineOutlined";
import PlayCircleOutlineOutlinedIcon from "@mui/icons-material/PlayCircleOutlineOutlined";
import RefreshOutlinedIcon from "@mui/icons-material/RefreshOutlined";
import SendOutlinedIcon from "@mui/icons-material/SendOutlined";
import VerifiedOutlinedIcon from "@mui/icons-material/VerifiedOutlined";
import WarningAmberOutlinedIcon from "@mui/icons-material/WarningAmberOutlined";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Divider,
  FormControl,
  Grid,
  InputLabel,
  LinearProgress,
  MenuItem,
  Pagination,
  Select,
  Skeleton,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from "@mui/material";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useState } from "react";
import {
  getLineDeliveryLogs,
  getLineFlexPreview,
  getLineOperationsStatus,
  getLineTestHistory,
  getUsers,
  sendLineTestFlex,
  sendLineTestMessage,
  simulateLineNotification,
  validateLineFlexPayload,
  validateLineConnection,
} from "../api/adminApi";
import { EmptyState } from "../components/common/EmptyState";
import { LoadingState } from "../components/common/LoadingState";
import { PageHeader } from "../components/PageHeader";
import { useNotification } from "../hooks/useNotification";
import { formatThaiDateTime } from "../utils/dateFormat";

export function LineSettingsPage() {
  const { showError, showSuccess } = useNotification();
  const [toUserId, setToUserId] = useState("");
  const [message, setMessage] = useState("ทดสอบการแจ้งเตือนจาก HOP");
  const [testSearch, setTestSearch] = useState("");
  const [testPage, setTestPage] = useState(1);
  const [deliveryStatus, setDeliveryStatus] = useState("");
  const [deliveryPage, setDeliveryPage] = useState(1);
  const [simUserId, setSimUserId] = useState("");
  const [simEvent, setSimEvent] = useState("PendingApproval");
  const [simMessage, setSimMessage] = useState("");
  const [flexToUserId, setFlexToUserId] = useState("");
  const [flexJson, setFlexJson] = useState("");
  const [flexVariant, setFlexVariant] = useState("pending");
  const [flexAvatarMode, setFlexAvatarMode] = useState("auto");

  const statusQuery = useQuery({
    queryKey: ["line-operations-status"],
    queryFn: getLineOperationsStatus,
  });
  const usersQuery = useQuery({
    queryKey: ["line-simulator-users"],
    queryFn: getUsers,
  });
  const testHistoryQuery = useQuery({
    queryKey: ["line-test-history", testPage, testSearch],
    queryFn: () => getLineTestHistory({ page: testPage, pageSize: 10, search: testSearch || undefined }),
  });
  const deliveryLogsQuery = useQuery({
    queryKey: ["line-delivery-logs", deliveryPage, deliveryStatus],
    queryFn: () => getLineDeliveryLogs({ page: deliveryPage, pageSize: 20, status: deliveryStatus || undefined }),
  });
  const flexPreviewQuery = useQuery({
    queryKey: ["line-flex-preview", flexVariant, flexAvatarMode],
    queryFn: () => getLineFlexPreview({ variant: flexVariant, avatarMode: flexAvatarMode }),
    enabled: false,
  });

  const testSendMutation = useMutation({
    mutationFn: sendLineTestMessage,
    onSuccess: (result) => {
      if (result.success) {
        showSuccess("ส่งข้อความทดสอบ LINE สำเร็จ");
        testHistoryQuery.refetch();
        deliveryLogsQuery.refetch();
        statusQuery.refetch();
      } else {
        showError("ส่งข้อความทดสอบ LINE ไม่สำเร็จ กรุณาตรวจสอบ Token หรือ LINE User ID");
      }
    },
    onError: () => showError("ส่งข้อความทดสอบ LINE ไม่สำเร็จ กรุณาตรวจสอบ Token หรือ LINE User ID"),
  });

  const validateMutation = useMutation({
    mutationFn: validateLineConnection,
    onSuccess: (result) => {
      if (result.success) {
        showSuccess(result.message || "ตรวจสอบการเชื่อมต่อสำเร็จ");
      } else {
        showError(result.message || "ตรวจสอบการเชื่อมต่อไม่สำเร็จ");
      }
      statusQuery.refetch();
    },
    onError: () => showError("ตรวจสอบการเชื่อมต่อไม่สำเร็จ"),
  });

  const simulatorMutation = useMutation({
    mutationFn: simulateLineNotification,
    onSuccess: (result) => {
      if (result.success) {
        showSuccess("จำลองการแจ้งเตือนสำเร็จ");
        testHistoryQuery.refetch();
        deliveryLogsQuery.refetch();
        statusQuery.refetch();
      } else {
        showError(result.message || "จำลองการแจ้งเตือนไม่สำเร็จ");
      }
    },
    onError: () => showError("จำลองการแจ้งเตือนไม่สำเร็จ"),
  });

  const flexValidateMutation = useMutation({
    mutationFn: validateLineFlexPayload,
    onSuccess: (result) => {
      if (result.isValid) {
        showSuccess("Flex JSON ผ่านการตรวจสอบเบื้องต้น");
      } else {
        showError(result.message || "Flex JSON ยังไม่ถูกต้อง");
      }
    },
    onError: () => showError("ตรวจสอบ Flex JSON ไม่สำเร็จ"),
  });

  const flexSendMutation = useMutation({
    mutationFn: sendLineTestFlex,
    onSuccess: (result) => {
      if (result.success) {
        showSuccess("ส่ง Flex Message ทดสอบสำเร็จ");
        testHistoryQuery.refetch();
        deliveryLogsQuery.refetch();
        statusQuery.refetch();
      } else {
        showError(result.message || "ส่ง Flex Message ทดสอบไม่สำเร็จ");
      }
    },
    onError: () => showError("ส่ง Flex Message ทดสอบไม่สำเร็จ กรุณาตรวจสอบ Flex JSON, Public URL หรือ LINE User ID"),
  });

  if (statusQuery.isLoading) {
    return <LoadingState message="กำลังโหลด LINE Operations Center..." />;
  }

  if (statusQuery.isError || !statusQuery.data) {
    return <EmptyState message="ไม่สามารถโหลดการตั้งค่า LINE ได้" />;
  }

  const status = statusQuery.data;
  const canSend = status.enabled && status.hasAccessToken && (Boolean(toUserId.trim()) || status.hasTestUserId);
  const canSendFlex = status.enabled && status.hasAccessToken && (Boolean(flexToUserId.trim()) || status.hasTestUserId);
  const selectableUsers = (usersQuery.data ?? []).filter((user) => user.lineUserId);

  return (
    <>
      <PageHeader title="LINE Operations Center" subtitle="Monitor, Diagnose, Test และ Audit LINE Messaging API" />
      <Stack spacing={2}>
        <Card sx={{ borderTop: (theme) => `4px solid ${theme.palette.secondary.main}` }}>
          <CardContent>
            <Stack direction={{ xs: "column", md: "row" }} justifyContent="space-between" spacing={2}>
              <Box>
                <Stack direction="row" spacing={1.5} alignItems="center">
                  {status.connectionStatus === "Connected" ? <CheckCircleOutlineIcon color="success" /> : <WarningAmberOutlinedIcon color="error" />}
                  <Typography variant="h6">LINE Messaging API Status</Typography>
                  <Chip
                    color={status.connectionStatus === "Connected" ? "success" : "error"}
                    label={status.connectionStatus === "Connected" ? "Connected" : "Disconnected"}
                    size="small"
                  />
                </Stack>
                <Typography variant="body2" color="text.secondary" sx={{ mt: 0.75 }}>
                  หน้านี้ใช้สำหรับ Monitor, Diagnose, Test และ Audit เท่านั้น ไม่สามารถแก้ไข Token ผ่านหน้าเว็บได้
                </Typography>
              </Box>
              <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                <Button
                  variant="contained"
                  startIcon={<RefreshOutlinedIcon />}
                  disabled={validateMutation.isPending}
                  onClick={() => validateMutation.mutate()}
                >
                  ตรวจสอบการเชื่อมต่อ
                </Button>
                <Button variant="outlined" startIcon={<HelpOutlineOutlinedIcon />} href="/docs/LINE-MESSAGING.md" target="_blank">
                  ดูคู่มือ
                </Button>
              </Stack>
            </Stack>
            {validateMutation.isPending && <LinearProgress sx={{ my: 2 }} />}
            {validateMutation.data && (
              <Alert severity={validateMutation.data.success ? "success" : "error"} sx={{ mt: 2 }}>
                {validateMutation.data.message} • HTTP {validateMutation.data.httpStatusCode ?? "-"} • {validateMutation.data.responseTimeMs} ms
                {validateMutation.data.botName ? ` • Bot: ${validateMutation.data.botName}` : ""}
              </Alert>
            )}
            <Grid container spacing={2} sx={{ mt: 1 }}>
              <Setting label="สถานะ LINE" value={status.enabled ? "เปิดใช้งาน" : "ปิดใช้งาน"} state={status.enabled ? "success" : "warning"} />
              <Setting label="Channel ID" value={status.channelIdMasked || "-"} />
              <Setting label="Channel Secret" value={status.hasChannelSecret ? "**************" : "ยังไม่ได้ตั้งค่า"} state={status.hasChannelSecret ? "success" : "error"} />
              <Setting label="Channel Access Token" value={status.hasAccessToken ? "**************" : "ยังไม่ได้ตั้งค่า"} state={status.hasAccessToken ? "success" : "error"} />
              <Setting label="Test User ID" value={status.testUserIdMasked || "ยังไม่ได้ตั้งค่า"} state={status.hasTestUserId ? "success" : "warning"} />
              <Setting label="Webhook" value={status.webhookActive ? "Active" : "Inactive"} state={status.webhookActive ? "success" : "warning"} />
              <Setting label="LINE Bot Name" value={validateMutation.data?.botName || status.botName || "-"} />
              <Setting label="Environment" value={status.environment} />
              <Setting label="Endpoint" value={status.endpoint} />
            </Grid>
          </CardContent>
        </Card>

        <Grid container spacing={2}>
          <Grid item xs={12} lg={7}>
            <Card>
              <CardContent>
                <Stack spacing={2}>
                  <SectionTitle title="ส่งข้อความทดสอบ" />
                  <Grid container spacing={2}>
                    <Grid item xs={12} md={5}>
                      <TextField
                        fullWidth
                        label="LINE User ID"
                        value={toUserId}
                        onChange={(event) => setToUserId(event.target.value)}
                        helperText={status.hasTestUserId ? "ถ้าเว้นว่าง ระบบจะใช้ LINE_TEST_USER_ID จาก config" : "ตัวอย่าง: Uxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"}
                      />
                    </Grid>
                    <Grid item xs={12} md={7}>
                      <TextField
                        fullWidth
                        label="ข้อความทดสอบ"
                        value={message}
                        onChange={(event) => setMessage(event.target.value)}
                      />
                    </Grid>
                  </Grid>
                  <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                    <Button
                      variant="contained"
                      startIcon={<SendOutlinedIcon />}
                      disabled={!canSend || testSendMutation.isPending}
                      onClick={() => testSendMutation.mutate({ toUserId: toUserId || null, message })}
                    >
                      {testSendMutation.isPending ? "กำลังส่ง..." : "Send Plain Text Test"}
                    </Button>
                    <Button variant="outlined" startIcon={<ClearOutlinedIcon />} onClick={() => { setToUserId(""); setMessage("ทดสอบการแจ้งเตือนจาก HOP"); }}>
                      ล้างข้อมูล
                    </Button>
                  </Stack>
                  {!canSend && (
                    <Alert severity="warning">
                      ต้องเปิด LINE, ตั้งค่า Channel Access Token และระบุ LINE User ID หรือ LINE_TEST_USER_ID ก่อนส่งข้อความทดสอบ
                    </Alert>
                  )}
                  {testSendMutation.data && (
                    <Alert severity={testSendMutation.data.success ? "success" : "error"}>
                      Result: {testSendMutation.data.success ? "Success" : "Failed"} • HTTP {testSendMutation.data.httpStatusCode ?? "-"} • Response Time {testSendMutation.data.responseTimeMs ?? "-"} ms
                    </Alert>
                  )}
                </Stack>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} lg={5}>
            <Card>
              <CardContent>
                <Stack spacing={2}>
                  <SectionTitle title="Health Check" />
                  <Grid container spacing={1.5}>
                    <Metric label="Last Successful Delivery" value={status.lastSuccessfulDelivery ? formatThaiDateTime(status.lastSuccessfulDelivery) : "-"} />
                    <Metric label="Last Failed Delivery" value={status.lastFailedDelivery ? formatThaiDateTime(status.lastFailedDelivery) : "-"} />
                    <Metric label="Queue Length" value={`${status.queueLength}`} />
                    <Metric label="Pending Retry" value={`${status.pendingRetry}`} />
                    <Metric label="Average Response Time" value={status.averageResponseTimeMs ? `${Math.round(status.averageResponseTimeMs)} ms` : "-"} />
                  </Grid>
                </Stack>
              </CardContent>
            </Card>
          </Grid>
        </Grid>

        <Card sx={{ borderTop: (theme) => `4px solid ${theme.palette.secondary.main}` }}>
          <CardContent>
            <Stack spacing={2}>
              <Stack direction={{ xs: "column", md: "row" }} justifyContent="space-between" spacing={2}>
                <Box>
                  <SectionTitle title="Flex Message Debug Mode" />
                  <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                    ใช้ตรวจ payload, action URL และผลตอบกลับจาก LINE Push API สำหรับ Flex Message
                  </Typography>
                </Box>
                <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                  <Button
                    variant="outlined"
                    startIcon={<DataObjectOutlinedIcon />}
                    disabled={flexPreviewQuery.isFetching}
                    onClick={async () => {
                      const result = await flexPreviewQuery.refetch();
                      if (result.data?.payload) {
                        setFlexJson(JSON.stringify(JSON.parse(result.data.payload), null, 2));
                        showSuccess("สร้าง Preview Flex JSON แล้ว");
                      }
                    }}
                  >
                    Preview Flex JSON
                  </Button>
                  <Button
                    variant="outlined"
                    startIcon={<ContentCopyOutlinedIcon />}
                    disabled={!flexJson.trim()}
                    onClick={async () => {
                      await navigator.clipboard.writeText(flexJson);
                      showSuccess("คัดลอก Flex JSON แล้ว");
                    }}
                  >
                    Copy JSON
                  </Button>
                  <Button
                    variant="outlined"
                    startIcon={<VerifiedOutlinedIcon />}
                    disabled={!flexJson.trim() || flexValidateMutation.isPending}
                    onClick={() => flexValidateMutation.mutate({ payload: flexJson })}
                  >
                    Validate
                  </Button>
                  <Button
                    variant="contained"
                    startIcon={<SendOutlinedIcon />}
                    disabled={!canSendFlex || flexSendMutation.isPending}
                    onClick={() => flexSendMutation.mutate({ toUserId: flexToUserId || null, payload: flexJson || null, variant: flexVariant, avatarMode: flexAvatarMode })}
                  >
                    {flexSendMutation.isPending ? "กำลังส่ง..." : "Send Full Leave Flex Test"}
                  </Button>
                  <Button
                    variant="outlined"
                    startIcon={<SendOutlinedIcon />}
                    disabled={!canSendFlex || flexSendMutation.isPending}
                    onClick={() => flexSendMutation.mutate({ toUserId: flexToUserId || null, payload: buildMinimalFlexPayload(), variant: "minimal", avatarMode: "without-image" })}
                  >
                    Send Minimal Flex Test
                  </Button>
                </Stack>
              </Stack>
              <Grid container spacing={2}>
                <Grid item xs={12} md={4}>
                  <FormControl fullWidth>
                    <InputLabel>ตัวอย่าง Flex</InputLabel>
                    <Select
                      label="ตัวอย่าง Flex"
                      value={flexVariant}
                      onChange={(event) => {
                        setFlexVariant(event.target.value);
                        setFlexJson("");
                      }}
                    >
                      <MenuItem value="pending">Pending Approval</MenuItem>
                      <MenuItem value="approved">Approved</MenuItem>
                      <MenuItem value="rejected">Rejected</MenuItem>
                      <MenuItem value="cancelled">Cancelled</MenuItem>
                    </Select>
                  </FormControl>
                </Grid>
                <Grid item xs={12} md={4}>
                  <FormControl fullWidth>
                    <InputLabel>Avatar Test Case</InputLabel>
                    <Select
                      label="Avatar Test Case"
                      value={flexAvatarMode}
                      onChange={(event) => {
                        setFlexAvatarMode(event.target.value);
                        setFlexJson("");
                      }}
                    >
                      <MenuItem value="auto">Auto จากข้อมูลจริง</MenuItem>
                      <MenuItem value="with-image">บังคับตัวอย่างมีรูป</MenuItem>
                      <MenuItem value="without-image">บังคับไม่มีรูป</MenuItem>
                    </Select>
                  </FormControl>
                </Grid>
                <Grid item xs={12} md={4}>
                  <TextField
                    fullWidth
                    label="LINE User ID สำหรับทดสอบ Flex"
                    value={flexToUserId}
                    onChange={(event) => setFlexToUserId(event.target.value)}
                    helperText={status.hasTestUserId ? "ถ้าเว้นว่าง ระบบจะใช้ LINE_TEST_USER_ID จาก config" : "ต้องระบุ LINE User ID ก่อนส่ง Flex"}
                  />
                </Grid>
                <Grid item xs={12}>
                  <Alert severity="info">
                    ถ้า Action URL เป็น localhost หรือไม่ใช่ HTTPS สำหรับ environment จริง LINE อาจปฏิเสธ Flex payload หรือผู้รับเปิดปุ่มไม่ได้
                  </Alert>
                </Grid>
                <Grid item xs={12}>
                  <TextField
                    fullWidth
                    multiline
                    minRows={10}
                    maxRows={18}
                    label="Flex JSON Payload"
                    value={flexJson}
                    onChange={(event) => setFlexJson(event.target.value)}
                    placeholder="กด Preview Flex JSON เพื่อสร้าง payload ตัวอย่าง"
                    inputProps={{ sx: { fontFamily: "Consolas, monospace", fontSize: 13 } }}
                  />
                </Grid>
              </Grid>
              {flexValidateMutation.data && (
                <Alert severity={flexValidateMutation.data.isValid ? "success" : "warning"}>
                  {flexValidateMutation.data.message}
                </Alert>
              )}
              {(flexValidateMutation.data?.checks ?? flexPreviewQuery.data?.validation ?? []).length > 0 && (
                <Grid container spacing={1.25}>
                  {(flexValidateMutation.data?.checks ?? flexPreviewQuery.data?.validation ?? []).map((item) => (
                    <Grid item xs={12} md={6} lg={4} key={item.label}>
                      <Stack direction="row" spacing={1} alignItems="flex-start">
                        {item.passed ? <CheckCircleOutlineIcon color="success" fontSize="small" /> : <WarningAmberOutlinedIcon color="warning" fontSize="small" />}
                        <Box>
                          <Typography fontWeight={700}>{item.label}</Typography>
                          <Typography variant="body2" color="text.secondary">{item.passed ? "ผ่าน" : item.recommendation}</Typography>
                        </Box>
                      </Stack>
                    </Grid>
                  ))}
                </Grid>
              )}
              {flexSendMutation.data && (
                <Alert severity={flexSendMutation.data.success ? "success" : "error"}>
                  Result: {flexSendMutation.data.success ? "Success" : "Failed"} • HTTP {flexSendMutation.data.httpStatusCode ?? "-"} • Latency {flexSendMutation.data.responseTimeMs ?? "-"} ms
                  {flexSendMutation.data.error ? ` • LINE Error: ${flexSendMutation.data.error}` : ""}
                </Alert>
              )}
              {!canSendFlex && (
                <Alert severity="warning">
                  ต้องเปิด LINE, ตั้งค่า Channel Access Token และระบุ LINE User ID หรือ LINE_TEST_USER_ID ก่อนส่ง Flex Message
                </Alert>
              )}
            </Stack>
          </CardContent>
        </Card>

        <Grid container spacing={2}>
          <Grid item xs={12} lg={6}>
            <Card>
              <CardContent>
                <Stack spacing={2}>
                  <SectionTitle title="จำลอง Event แจ้งเตือน" />
                  <Grid container spacing={2}>
                    <Grid item xs={12} md={6}>
                      <FormControl fullWidth>
                        <InputLabel>Event</InputLabel>
                        <Select label="Event" value={simEvent} onChange={(event) => setSimEvent(event.target.value)}>
                          <MenuItem value="LeaveSubmitted">Leave Submitted</MenuItem>
                          <MenuItem value="LeaveApproved">Leave Approved</MenuItem>
                          <MenuItem value="LeaveRejected">Leave Rejected</MenuItem>
                          <MenuItem value="PendingApproval">Pending Approval</MenuItem>
                          <MenuItem value="Cancelled">Cancelled</MenuItem>
                          <MenuItem value="Reminder">Reminder</MenuItem>
                        </Select>
                      </FormControl>
                    </Grid>
                    <Grid item xs={12} md={6}>
                      <FormControl fullWidth>
                        <InputLabel>ผู้รับ</InputLabel>
                        <Select label="ผู้รับ" value={simUserId} onChange={(event) => setSimUserId(event.target.value)}>
                          {usersQuery.isLoading && <MenuItem value="">กำลังโหลด...</MenuItem>}
                          {selectableUsers.map((user) => (
                            <MenuItem key={user.id} value={user.id}>{user.fullname}</MenuItem>
                          ))}
                        </Select>
                      </FormControl>
                    </Grid>
                    <Grid item xs={12}>
                      <TextField fullWidth label="ข้อความเพิ่มเติม (ไม่บังคับ)" value={simMessage} onChange={(event) => setSimMessage(event.target.value)} />
                    </Grid>
                  </Grid>
                  <Button
                    variant="contained"
                    startIcon={<PlayCircleOutlineOutlinedIcon />}
                    disabled={!simUserId || simulatorMutation.isPending}
                    onClick={() => simulatorMutation.mutate({ userId: simUserId, eventType: simEvent, message: simMessage || null })}
                  >
                    จำลองการแจ้งเตือน
                  </Button>
                </Stack>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} lg={6}>
            <Card>
              <CardContent>
                <Stack spacing={1.5}>
                  <SectionTitle title="Troubleshooting Checklist" />
                  {status.checklist.map((item) => (
                    <Stack key={item.label} direction="row" spacing={1.25} alignItems="flex-start">
                      {item.passed ? <CheckCircleOutlineIcon color="success" fontSize="small" /> : <WarningAmberOutlinedIcon color="warning" fontSize="small" />}
                      <Box>
                        <Typography fontWeight={700}>{item.label}</Typography>
                        <Typography variant="body2" color="text.secondary">{item.passed ? "ผ่าน" : item.recommendation}</Typography>
                      </Box>
                    </Stack>
                  ))}
                </Stack>
              </CardContent>
            </Card>
          </Grid>
        </Grid>

        <Card>
          <CardContent>
            <Stack spacing={2}>
              <Stack direction={{ xs: "column", md: "row" }} justifyContent="space-between" spacing={2}>
                <SectionTitle title="Test History" />
                <TextField
                  size="small"
                  label="Search"
                  value={testSearch}
                  onChange={(event) => { setTestSearch(event.target.value); setTestPage(1); }}
                />
              </Stack>
              <LogTable loading={testHistoryQuery.isLoading} rows={testHistoryQuery.data?.items ?? []} />
              <Pagination count={testHistoryQuery.data?.totalPages ?? 1} page={testPage} onChange={(_, page) => setTestPage(page)} />
            </Stack>
          </CardContent>
        </Card>

        <Card>
          <CardContent>
            <Stack spacing={2}>
              <Stack direction={{ xs: "column", md: "row" }} justifyContent="space-between" spacing={2}>
                <SectionTitle title="Delivery Log - Last Messages" />
                <FormControl size="small" sx={{ minWidth: 180 }}>
                  <InputLabel>สถานะ</InputLabel>
                  <Select label="สถานะ" value={deliveryStatus} onChange={(event) => { setDeliveryStatus(event.target.value); setDeliveryPage(1); }}>
                    <MenuItem value="">ทั้งหมด</MenuItem>
                    <MenuItem value="Sent">Success</MenuItem>
                    <MenuItem value="Failed">Failed</MenuItem>
                    <MenuItem value="Queued">Pending</MenuItem>
                  </Select>
                </FormControl>
              </Stack>
              <LogTable loading={deliveryLogsQuery.isLoading} rows={(deliveryLogsQuery.data?.items ?? []).slice(0, 10)} showAction />
              <Pagination count={deliveryLogsQuery.data?.totalPages ?? 1} page={deliveryPage} onChange={(_, page) => setDeliveryPage(page)} />
            </Stack>
          </CardContent>
        </Card>
      </Stack>
    </>
  );
}

function SectionTitle({ title }: { title: string }) {
  return (
    <Box>
      <Typography variant="h6">{title}</Typography>
      <Divider sx={{ mt: 1 }} />
    </Box>
  );
}

function Metric({ label, value }: { label: string; value: string }) {
  return (
    <Grid item xs={12} sm={6}>
      <Box sx={{ p: 1.5, border: (theme) => `1px solid ${theme.palette.divider}`, borderRadius: 2 }}>
        <Typography variant="caption" color="text.secondary">{label}</Typography>
        <Typography fontWeight={800}>{value}</Typography>
      </Box>
    </Grid>
  );
}

type LineLogRow = {
  id: string;
  date: string;
  recipient: string;
  module: string;
  event: string;
  status: string;
  retry: number;
  durationMs?: number | null;
  error?: string | null;
  requestType?: string | null;
  sanitizedRecipient?: string | null;
  payloadPreview?: string | null;
  httpStatusCode?: number | null;
  responseBody?: string | null;
};

function LogTable({ loading, rows, showAction = false }: { loading: boolean; rows: LineLogRow[]; showAction?: boolean }) {
  if (loading) {
    return <Skeleton variant="rounded" height={180} />;
  }

  if (rows.length === 0) {
    return <EmptyState message="ไม่พบข้อมูล" />;
  }

  return (
    <Box sx={{ overflowX: "auto" }}>
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>วันที่</TableCell>
            <TableCell>ผู้รับ</TableCell>
            <TableCell>Module</TableCell>
            <TableCell>Event</TableCell>
            <TableCell>Type</TableCell>
            <TableCell>LINE Status</TableCell>
            <TableCell>Status</TableCell>
            <TableCell>Retry</TableCell>
            <TableCell>Duration</TableCell>
            <TableCell>Masked Payload Preview</TableCell>
            <TableCell>Response Body</TableCell>
            {showAction && <TableCell>Action</TableCell>}
          </TableRow>
        </TableHead>
        <TableBody>
          {rows.map((row) => (
            <TableRow key={row.id} hover>
              <TableCell>{formatThaiDateTime(row.date)}</TableCell>
              <TableCell>{row.recipient}</TableCell>
              <TableCell>{row.module}</TableCell>
              <TableCell>{row.event}</TableCell>
              <TableCell>{row.requestType ?? "-"}</TableCell>
              <TableCell>{row.httpStatusCode ?? "-"}</TableCell>
              <TableCell><StatusChip status={row.status} /></TableCell>
              <TableCell>{row.retry}</TableCell>
              <TableCell>{row.durationMs ? `${row.durationMs} ms` : "-"}</TableCell>
              <TableCell sx={{ minWidth: 240 }}>
                <Typography variant="caption" sx={{ display: "block", fontFamily: "Consolas, monospace", whiteSpace: "pre-wrap", wordBreak: "break-word" }}>
                  ผู้รับจริงถูก mask: {row.sanitizedRecipient ?? "-"}
                  {"\n"}
                  {row.payloadPreview ?? "-"}
                </Typography>
              </TableCell>
              <TableCell sx={{ minWidth: 220 }}>
                <Typography variant="caption" color={row.status === "Failed" ? "error" : "text.secondary"} sx={{ display: "block", whiteSpace: "pre-wrap", wordBreak: "break-word" }}>
                  {row.responseBody ?? row.error ?? "-"}
                </Typography>
              </TableCell>
              {showAction && <TableCell>{row.error ? <Typography variant="caption" color="error">{row.error.slice(0, 80)}</Typography> : "-"}</TableCell>}
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </Box>
  );
}

function StatusChip({ status }: { status: string }) {
  const color = status === "Sent" ? "success" : status === "Failed" ? "error" : status === "Queued" ? "warning" : "default";
  const label = status === "Sent" ? "Success" : status === "Failed" ? "Failed" : status === "Queued" ? "Pending" : status;
  return <Chip size="small" color={color} label={label} />;
}

function Setting({ label, value, state }: { label: string; value: string; state?: "success" | "warning" | "error" }) {
  return (
    <Grid item xs={12} md={6} lg={4}>
      <Box sx={{ p: 1.5, border: (theme) => `1px solid ${theme.palette.divider}`, borderRadius: 2, minHeight: 72 }}>
        <Stack direction="row" justifyContent="space-between" spacing={1} alignItems="center">
          <Typography variant="caption" color="text.secondary">
            {label}
          </Typography>
          {state && <Chip size="small" color={state} label={state === "success" ? "ผ่าน" : state === "warning" ? "ตรวจสอบ" : "ไม่ผ่าน"} />}
        </Stack>
        <Typography sx={{ overflowWrap: "anywhere", fontWeight: 700, mt: 0.5 }}>{value || "-"}</Typography>
      </Box>
    </Grid>
  );
}

function buildMinimalFlexPayload() {
  return JSON.stringify({
    to: "",
    messages: [
      {
        type: "flex",
        altText: "ทดสอบ Minimal Flex จาก HOP",
        contents: {
          type: "bubble",
          size: "mega",
          body: {
            type: "box",
            layout: "vertical",
            spacing: "md",
            contents: [
              {
                type: "text",
                text: "ทดสอบ Minimal Flex",
                weight: "bold",
                size: "lg",
                color: "#1F2937",
              },
              {
                type: "text",
                text: "ถ้าข้อความนี้ส่งได้ แปลว่า token และ LINE User ID ใช้งานกับ OA เดียวกัน",
                wrap: true,
                size: "sm",
                color: "#6B7280",
              },
            ],
          },
          footer: {
            type: "box",
            layout: "vertical",
            contents: [
              {
                type: "button",
                style: "primary",
                color: "#2563EB",
                action: {
                  type: "uri",
                  label: "เปิด HOP",
                  uri: "https://example.com",
                },
              },
            ],
          },
        },
      },
    ],
  });
}
