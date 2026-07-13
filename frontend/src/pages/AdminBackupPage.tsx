import BackupOutlinedIcon from "@mui/icons-material/BackupOutlined";
import CheckCircleOutlineOutlinedIcon from "@mui/icons-material/CheckCircleOutlineOutlined";
import DeleteSweepOutlinedIcon from "@mui/icons-material/DeleteSweepOutlined";
import HistoryOutlinedIcon from "@mui/icons-material/HistoryOutlined";
import RefreshOutlinedIcon from "@mui/icons-material/RefreshOutlined";
import RestoreOutlinedIcon from "@mui/icons-material/RestoreOutlined";
import SearchOutlinedIcon from "@mui/icons-material/SearchOutlined";
import SettingsOutlinedIcon from "@mui/icons-material/SettingsOutlined";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  MenuItem,
  Pagination,
  Skeleton,
  Stack,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Tabs,
  TextField,
  Typography,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import {
  applyRetention,
  confirmRestore,
  getBackup,
  getBackupOverview,
  getBackups,
  getRestoreRuns,
  previewRestore,
  previewRetention,
  verifyBackup,
  type BackupQuery,
  type BackupRun,
  type RestorePreview,
} from "../api/adminApi";
import { PageHeader } from "../components/PageHeader";
import { useNotification } from "../hooks/useNotification";
import { brandColors } from "../theme/theme";
import { formatThaiDateTime } from "../utils/dateFormat";

const pageSize = 10;

export function AdminBackupPage() {
  const [tab, setTab] = useState(0);
  const [page, setPage] = useState(1);
  const [type, setType] = useState("");
  const [status, setStatus] = useState("");
  const [search, setSearch] = useState("");
  const [selectedBackupId, setSelectedBackupId] = useState<string | null>(null);
  const [restorePreviewData, setRestorePreviewData] = useState<RestorePreview | null>(null);
  const [restoreDialogOpen, setRestoreDialogOpen] = useState(false);
  const [restoreReason, setRestoreReason] = useState("");
  const [restoreConfirmation, setRestoreConfirmation] = useState("");
  const [retentionReason, setRetentionReason] = useState("");
  const [retentionConfirmation, setRetentionConfirmation] = useState("");
  const queryClient = useQueryClient();
  const notification = useNotification();

  const backupQuery: BackupQuery = useMemo(() => ({
    page,
    pageSize,
    type: type || undefined,
    status: status || undefined,
    search: search || undefined,
    sort: "startedAt",
    direction: "desc",
  }), [page, search, status, type]);

  const overviewQuery = useQuery({
    queryKey: ["backup-overview"],
    queryFn: getBackupOverview,
    refetchOnWindowFocus: false,
  });

  const backupsQuery = useQuery({
    queryKey: ["backups", backupQuery],
    queryFn: () => getBackups(backupQuery),
    refetchOnWindowFocus: false,
  });

  const selectedBackupQuery = useQuery({
    queryKey: ["backup-detail", selectedBackupId],
    queryFn: () => getBackup(selectedBackupId!),
    enabled: Boolean(selectedBackupId),
    refetchOnWindowFocus: false,
  });

  const restoreRunsQuery = useQuery({
    queryKey: ["restore-runs"],
    queryFn: () => getRestoreRuns({ page: 1, pageSize: 10 }),
    refetchOnWindowFocus: false,
  });

  const retentionPreviewQuery = useQuery({
    queryKey: ["backup-retention-preview"],
    queryFn: previewRetention,
    enabled: tab === 4,
    refetchOnWindowFocus: false,
  });

  const verifyMutation = useMutation({
    mutationFn: verifyBackup,
    onSuccess: () => {
      notification.showSuccess("ตรวจสอบไฟล์ Backup เรียบร้อยแล้ว");
      void invalidateBackupQueries(queryClient);
    },
  });

  const restorePreviewMutation = useMutation({
    mutationFn: previewRestore,
    onSuccess: (data) => {
      setRestorePreviewData(data ?? null);
      setRestoreDialogOpen(true);
      notification.showInfo(data?.canRestore ? "ตรวจสอบความพร้อม Restore แล้ว" : "Backup นี้ยังไม่พร้อม Restore");
    },
  });

  const restoreMutation = useMutation({
    mutationFn: ({ id, reason, confirmation }: { id: string; reason: string; confirmation: string }) => confirmRestore(id, {
      confirmationText: confirmation,
      reason,
      restoreDatabase: true,
      restoreStorage: false,
      restoreMode: "TestDatabase",
    }),
    onSuccess: () => {
      notification.showSuccess("บันทึกคำขอ Restore เรียบร้อยแล้ว");
      setRestoreDialogOpen(false);
      setRestoreReason("");
      setRestoreConfirmation("");
      void invalidateBackupQueries(queryClient);
    },
  });

  const applyRetentionMutation = useMutation({
    mutationFn: () => applyRetention({
      reason: retentionReason,
      confirmationText: retentionConfirmation,
    }),
    onSuccess: () => {
      notification.showSuccess("ดำเนินการ Retention Policy เรียบร้อยแล้ว");
      setRetentionReason("");
      setRetentionConfirmation("");
      void invalidateBackupQueries(queryClient);
    },
  });

  const rows = backupsQuery.data?.items ?? [];
  const selectedBackup = selectedBackupQuery.data?.backup;
  const retention = retentionPreviewQuery.data;

  return (
    <Stack spacing={3}>
      <PageHeader title="Backup Center" subtitle="ศูนย์ตรวจสอบ Backup, Restore, History และ Retention สำหรับผู้ดูแลระบบ" />

      <Tabs value={tab} onChange={(_, value) => setTab(value)} variant="scrollable" scrollButtons="auto">
        <Tab icon={<BackupOutlinedIcon />} iconPosition="start" label="Overview" />
        <Tab icon={<HistoryOutlinedIcon />} iconPosition="start" label="Backup History" />
        <Tab icon={<RestoreOutlinedIcon />} iconPosition="start" label="Restore" />
        <Tab icon={<HistoryOutlinedIcon />} iconPosition="start" label="Restore History" />
        <Tab icon={<DeleteSweepOutlinedIcon />} iconPosition="start" label="Retention" />
        <Tab icon={<SettingsOutlinedIcon />} iconPosition="start" label="Settings" />
      </Tabs>

      {tab === 0 && (
        <OverviewTab isLoading={overviewQuery.isLoading} overview={overviewQuery.data ?? null} onRefresh={() => void overviewQuery.refetch()} />
      )}

      {tab === 1 && (
        <Stack spacing={2}>
          <BackupFilters
            type={type}
            status={status}
            search={search}
            onTypeChange={(value) => { setType(value); setPage(1); }}
            onStatusChange={(value) => { setStatus(value); setPage(1); }}
            onSearchChange={(value) => { setSearch(value); setPage(1); }}
            onRefresh={() => void backupsQuery.refetch()}
          />
          <BackupTable
            rows={rows}
            isLoading={backupsQuery.isLoading}
            selectedId={selectedBackupId}
            onSelect={setSelectedBackupId}
            onVerify={(id) => verifyMutation.mutate(id)}
            onPreviewRestore={(id) => restorePreviewMutation.mutate(id)}
          />
          <Stack direction="row" justifyContent="space-between" alignItems="center">
            <Typography color="text.secondary">ทั้งหมด {backupsQuery.data?.totalItems ?? 0} รายการ</Typography>
            <Pagination count={backupsQuery.data?.totalPages ?? 1} page={page} onChange={(_, value) => setPage(value)} />
          </Stack>
        </Stack>
      )}

      {tab === 2 && (
        <RestoreTab
          backups={rows}
          selectedBackup={selectedBackup ?? null}
          detailLog={selectedBackupQuery.data?.logSummary}
          restorePreviewData={restorePreviewData}
          onSelect={setSelectedBackupId}
          onPreview={(id) => restorePreviewMutation.mutate(id)}
          onOpenConfirm={() => setRestoreDialogOpen(true)}
        />
      )}

      {tab === 3 && (
        <RestoreHistoryTab rows={restoreRunsQuery.data?.items ?? []} isLoading={restoreRunsQuery.isLoading} onRefresh={() => void restoreRunsQuery.refetch()} />
      )}

      {tab === 4 && (
        <RetentionTab
          isLoading={retentionPreviewQuery.isLoading}
          preview={retention ?? null}
          policy={overviewQuery.data?.retentionPolicy ?? null}
          reason={retentionReason}
          confirmation={retentionConfirmation}
          onReasonChange={setRetentionReason}
          onConfirmationChange={setRetentionConfirmation}
          onPreview={() => void retentionPreviewQuery.refetch()}
          onApply={() => applyRetentionMutation.mutate()}
          isApplying={applyRetentionMutation.isPending}
        />
      )}

      {tab === 5 && <SettingsTab backupRoot={overviewQuery.data?.backupRoot} policy={overviewQuery.data?.retentionPolicy ?? null} />}

      <Dialog open={restoreDialogOpen} onClose={() => setRestoreDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>ยืนยัน Restore</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            <Alert severity="warning">
              ระบบจะบันทึกคำขอ Restore และตรวจสอบ backup เท่านั้น การ restore production จริงต้องทำใน maintenance window ตาม runbook
            </Alert>
            {restorePreviewData && (
              <Alert severity={restorePreviewData.canRestore ? "info" : "error"}>
                {restorePreviewData.canRestore ? "Backup นี้พร้อมสำหรับ restore workflow" : restorePreviewData.errors.join(", ")}
              </Alert>
            )}
            <TextField
              label="เหตุผล"
              value={restoreReason}
              onChange={(event) => setRestoreReason(event.target.value)}
              multiline
              minRows={3}
              fullWidth
              required
            />
            <TextField
              label='พิมพ์ "RESTORE HOP" เพื่อยืนยัน'
              value={restoreConfirmation}
              onChange={(event) => setRestoreConfirmation(event.target.value)}
              fullWidth
              required
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setRestoreDialogOpen(false)}>ยกเลิก</Button>
          <Button
            variant="contained"
            color="warning"
            disabled={!selectedBackupId || !restoreReason.trim() || restoreConfirmation !== "RESTORE HOP" || restoreMutation.isPending}
            onClick={() => selectedBackupId && restoreMutation.mutate({ id: selectedBackupId, reason: restoreReason, confirmation: restoreConfirmation })}
          >
            ยืนยัน Restore
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
}

function OverviewTab({ isLoading, overview, onRefresh }: { isLoading: boolean; overview: Awaited<ReturnType<typeof getBackupOverview>> | null | undefined; onRefresh: () => void }) {
  if (isLoading) {
    return <Skeleton variant="rounded" height={260} />;
  }

  return (
    <Stack spacing={2}>
      <Stack direction="row" justifyContent="flex-end">
        <Button startIcon={<RefreshOutlinedIcon />} onClick={onRefresh}>รีเฟรช</Button>
      </Stack>
      <Grid container spacing={2}>
        <MetricCard title="Backup สำเร็จล่าสุด" value={overview?.lastSuccessfulBackup ? formatThaiDateTime(overview.lastSuccessfulBackup.completedAt ?? overview.lastSuccessfulBackup.startedAt) : "-"} helper={overview?.lastSuccessfulBackup?.fileName ?? "ยังไม่มีข้อมูล"} />
        <MetricCard title="Backup ล้มเหลวล่าสุด" value={overview?.lastFailedBackup ? formatThaiDateTime(overview.lastFailedBackup.completedAt ?? overview.lastFailedBackup.startedAt) : "-"} helper={overview?.lastFailedBackup?.fileName ?? "ไม่พบรายการล้มเหลว"} color="error" />
        <MetricCard title="Verified ล่าสุด" value={overview?.lastVerifiedBackup ? formatThaiDateTime(overview.lastVerifiedBackup.verifiedAt) : "-"} helper={overview?.lastVerifiedBackup?.fileName ?? "ยังไม่มีการ verify"} color="success" />
        <MetricCard title="ขนาด Backup รวม" value={formatBytes(overview?.totalBackupSizeBytes ?? 0)} helper={overview?.backupRoot ?? "/opt/hop/backups"} color="gold" />
      </Grid>
      <Alert severity="info">Backup Center ไม่แสดง DB password, connection string หรือ secret ใด ๆ บนหน้าเว็บ</Alert>
    </Stack>
  );
}

function BackupFilters(props: {
  type: string;
  status: string;
  search: string;
  onTypeChange: (value: string) => void;
  onStatusChange: (value: string) => void;
  onSearchChange: (value: string) => void;
  onRefresh: () => void;
}) {
  return (
    <Card>
      <CardContent>
        <Grid container spacing={2} alignItems="center">
          <Grid item xs={12} md={3}>
            <TextField select label="ประเภท" value={props.type} onChange={(event) => props.onTypeChange(event.target.value)} fullWidth>
              <MenuItem value="">ทั้งหมด</MenuItem>
              <MenuItem value="Database">Database</MenuItem>
              <MenuItem value="Storage">Storage</MenuItem>
              <MenuItem value="Full">Full</MenuItem>
            </TextField>
          </Grid>
          <Grid item xs={12} md={3}>
            <TextField select label="สถานะ" value={props.status} onChange={(event) => props.onStatusChange(event.target.value)} fullWidth>
              <MenuItem value="">ทั้งหมด</MenuItem>
              {["Running", "Success", "Failed", "Verified", "VerificationFailed", "Deleted"].map((item) => (
                <MenuItem key={item} value={item}>{item}</MenuItem>
              ))}
            </TextField>
          </Grid>
          <Grid item xs={12} md={4}>
            <TextField
              label="ค้นหาชื่อไฟล์"
              value={props.search}
              onChange={(event) => props.onSearchChange(event.target.value)}
              fullWidth
              InputProps={{ startAdornment: <SearchOutlinedIcon fontSize="small" sx={{ mr: 1, color: "text.secondary" }} /> }}
            />
          </Grid>
          <Grid item xs={12} md={2}>
            <Button fullWidth variant="outlined" startIcon={<RefreshOutlinedIcon />} onClick={props.onRefresh}>รีเฟรช</Button>
          </Grid>
        </Grid>
      </CardContent>
    </Card>
  );
}

function BackupTable(props: {
  rows: BackupRun[];
  isLoading: boolean;
  selectedId: string | null;
  onSelect: (id: string) => void;
  onVerify: (id: string) => void;
  onPreviewRestore: (id: string) => void;
}) {
  return (
    <Card sx={{ overflow: "hidden" }}>
      <Box sx={{ overflowX: "auto" }}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>วันที่เวลา</TableCell>
              <TableCell>ประเภท</TableCell>
              <TableCell>ชื่อไฟล์</TableCell>
              <TableCell>ขนาด</TableCell>
              <TableCell>สถานะ</TableCell>
              <TableCell>Checksum</TableCell>
              <TableCell align="right">Action</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {props.isLoading ? (
              <TableRow><TableCell colSpan={7}><Skeleton height={80} /></TableCell></TableRow>
            ) : props.rows.length === 0 ? (
              <TableRow><TableCell colSpan={7}><Typography color="text.secondary">ยังไม่มีประวัติ Backup</Typography></TableCell></TableRow>
            ) : props.rows.map((row) => (
              <TableRow key={row.id} hover selected={row.id === props.selectedId} onClick={() => props.onSelect(row.id)} sx={{ cursor: "pointer" }}>
                <TableCell>{formatThaiDateTime(row.completedAt ?? row.startedAt)}</TableCell>
                <TableCell>{row.backupType}</TableCell>
                <TableCell>
                  <Typography fontWeight={800}>{row.fileName}</Typography>
                  <Typography variant="caption" color="text.secondary">{row.relativePath}</Typography>
                </TableCell>
                <TableCell>{formatBytes(row.fileSizeBytes)}</TableCell>
                <TableCell><StatusChip status={row.status} /></TableCell>
                <TableCell>
                  <Typography variant="caption" sx={{ display: "block", maxWidth: 180, overflow: "hidden", textOverflow: "ellipsis" }}>
                    {row.checksum ?? "-"}
                  </Typography>
                </TableCell>
                <TableCell align="right">
                  <Stack direction="row" spacing={1} justifyContent="flex-end">
                    <Button size="small" onClick={(event) => { event.stopPropagation(); props.onVerify(row.id); }}>ตรวจสอบ</Button>
                    <Button size="small" color="warning" onClick={(event) => { event.stopPropagation(); props.onSelect(row.id); props.onPreviewRestore(row.id); }}>Restore</Button>
                  </Stack>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Box>
    </Card>
  );
}

function RestoreTab(props: {
  backups: BackupRun[];
  selectedBackup: BackupRun | null;
  detailLog?: string;
  restorePreviewData: RestorePreview | null;
  onSelect: (id: string) => void;
  onPreview: (id: string) => void;
  onOpenConfirm: () => void;
}) {
  return (
    <Grid container spacing={2}>
      <Grid item xs={12} md={5}>
        <Card>
          <CardContent>
            <Stack spacing={2}>
              <Typography variant="h6" fontWeight={900}>เลือก Backup</Typography>
              <TextField select label="Backup" value={props.selectedBackup?.id ?? ""} onChange={(event) => props.onSelect(event.target.value)} fullWidth>
                {props.backups.map((item) => (
                  <MenuItem key={item.id} value={item.id}>{item.fileName}</MenuItem>
                ))}
              </TextField>
              <Button disabled={!props.selectedBackup} variant="contained" onClick={() => props.selectedBackup && props.onPreview(props.selectedBackup.id)}>
                ตรวจสอบก่อน Restore
              </Button>
            </Stack>
          </CardContent>
        </Card>
      </Grid>
      <Grid item xs={12} md={7}>
        <Card>
          <CardContent>
            <Stack spacing={2}>
              <Typography variant="h6" fontWeight={900}>Restore Preview</Typography>
              {!props.restorePreviewData ? (
                <Alert severity="info">เลือก backup แล้วกดตรวจสอบก่อน Restore</Alert>
              ) : (
                <>
                  <Alert severity={props.restorePreviewData.canRestore ? "success" : "error"}>
                    {props.restorePreviewData.canRestore ? "สามารถเข้าสู่ขั้นตอนยืนยัน Restore ได้" : "ยังไม่สามารถ Restore ได้"}
                  </Alert>
                  {props.restorePreviewData.warnings.map((item) => <Alert key={item} severity="warning">{item}</Alert>)}
                  {props.restorePreviewData.errors.map((item) => <Alert key={item} severity="error">{item}</Alert>)}
                  <InfoRow label="Environment" value={props.restorePreviewData.currentEnvironment} />
                  <InfoRow label="Recommended Mode" value={props.restorePreviewData.recommendedMode} />
                  <Button color="warning" variant="contained" disabled={!props.restorePreviewData.canRestore} onClick={props.onOpenConfirm}>
                    ยืนยัน Restore
                  </Button>
                </>
              )}
              {props.detailLog && (
                <Box sx={{ p: 2, borderRadius: 2, bgcolor: "#f8fafc", maxHeight: 180, overflow: "auto" }}>
                  <Typography variant="caption" whiteSpace="pre-wrap">{props.detailLog}</Typography>
                </Box>
              )}
            </Stack>
          </CardContent>
        </Card>
      </Grid>
    </Grid>
  );
}

function RestoreHistoryTab({ rows, isLoading, onRefresh }: { rows: Awaited<ReturnType<typeof getRestoreRuns>>["items"]; isLoading: boolean; onRefresh: () => void }) {
  return (
    <Card>
      <CardContent>
        <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
          <Typography variant="h6" fontWeight={900}>Restore History</Typography>
          <Button startIcon={<RefreshOutlinedIcon />} onClick={onRefresh}>รีเฟรช</Button>
        </Stack>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>วันที่</TableCell>
              <TableCell>Backup</TableCell>
              <TableCell>Target</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Reason</TableCell>
              <TableCell>ผู้ดำเนินการ</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {isLoading ? (
              <TableRow><TableCell colSpan={6}><Skeleton height={80} /></TableCell></TableRow>
            ) : rows.length === 0 ? (
              <TableRow><TableCell colSpan={6}>ยังไม่มีประวัติ Restore</TableCell></TableRow>
            ) : rows.map((row) => (
              <TableRow key={row.id}>
                <TableCell>{formatThaiDateTime(row.startedAt)}</TableCell>
                <TableCell>{row.backupFileName}</TableCell>
                <TableCell>{row.targetDatabase || row.targetEnvironment}</TableCell>
                <TableCell><StatusChip status={row.status} /></TableCell>
                <TableCell>{row.reason}</TableCell>
                <TableCell>{row.createdBy ?? "-"}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </CardContent>
    </Card>
  );
}

function RetentionTab(props: {
  isLoading: boolean;
  preview: Awaited<ReturnType<typeof previewRetention>> | null;
  policy: Awaited<ReturnType<typeof getBackupOverview>>["retentionPolicy"] | null;
  reason: string;
  confirmation: string;
  onReasonChange: (value: string) => void;
  onConfirmationChange: (value: string) => void;
  onPreview: () => void;
  onApply: () => void;
  isApplying: boolean;
}) {
  return (
    <Stack spacing={2}>
      <Grid container spacing={2}>
        <MetricCard title="Daily" value={`${props.policy?.dailyDays ?? 14} วัน`} helper="เก็บ backup รายวัน" />
        <MetricCard title="Weekly" value={`${props.policy?.weeklyWeeks ?? 8} สัปดาห์`} helper="แผน archival ภายนอก" />
        <MetricCard title="Monthly" value={`${props.policy?.monthlyMonths ?? 12} เดือน`} helper="แผน archival ภายนอก" />
        <MetricCard title="ลบได้โดยประมาณ" value={formatBytes(props.preview?.freedBytes ?? 0)} helper={`${props.preview?.delete ?? 0} รายการ`} color="gold" />
      </Grid>
      <Card>
        <CardContent>
          <Stack spacing={2}>
            <Stack direction="row" justifyContent="space-between">
              <Typography variant="h6" fontWeight={900}>Retention Preview</Typography>
              <Button startIcon={<RefreshOutlinedIcon />} onClick={props.onPreview}>Preview</Button>
            </Stack>
            {props.isLoading ? <Skeleton height={100} /> : (
              <Box sx={{ overflowX: "auto" }}>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>ไฟล์</TableCell>
                      <TableCell>วันที่</TableCell>
                      <TableCell>Action</TableCell>
                      <TableCell>เหตุผล</TableCell>
                      <TableCell>ขนาด</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {(props.preview?.items ?? []).slice(0, 20).map((item) => (
                      <TableRow key={item.backupId}>
                        <TableCell>{item.fileName}</TableCell>
                        <TableCell>{formatThaiDateTime(item.createdAt)}</TableCell>
                        <TableCell><StatusChip status={item.action} /></TableCell>
                        <TableCell>{item.reason}</TableCell>
                        <TableCell>{formatBytes(item.fileSizeBytes)}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </Box>
            )}
            <Alert severity="warning">Apply Retention จะลบเฉพาะรายการที่ policy อนุญาต และจะไม่ลบ backup ล่าสุดหรือ backup ที่ protected</Alert>
            <TextField label="เหตุผล" value={props.reason} onChange={(event) => props.onReasonChange(event.target.value)} fullWidth />
            <TextField label='พิมพ์ "APPLY RETENTION" เพื่อยืนยัน' value={props.confirmation} onChange={(event) => props.onConfirmationChange(event.target.value)} fullWidth />
            <Button
              color="warning"
              variant="contained"
              disabled={props.confirmation !== "APPLY RETENTION" || !props.reason.trim() || props.isApplying}
              onClick={props.onApply}
            >
              Apply Retention
            </Button>
          </Stack>
        </CardContent>
      </Card>
    </Stack>
  );
}

function SettingsTab({ backupRoot, policy }: { backupRoot?: string | null; policy: Awaited<ReturnType<typeof getBackupOverview>>["retentionPolicy"] | null }) {
  return (
    <Card>
      <CardContent>
        <Stack spacing={2}>
          <Typography variant="h6" fontWeight={900}>Settings</Typography>
          <InfoRow label="Backup Root" value={backupRoot ?? "/opt/hop/backups"} />
          <InfoRow label="Database folder" value={`${backupRoot ?? "/opt/hop/backups"}/postgres`} />
          <InfoRow label="Storage folder" value={`${backupRoot ?? "/opt/hop/backups"}/storage`} />
          <InfoRow label="Daily retention" value={`${policy?.dailyDays ?? 14} วัน`} />
          <InfoRow label="Keep verified" value={policy?.keepVerified === false ? "ไม่ใช่" : "ใช่"} />
          <Alert severity="info">ค่า retention มาจาก environment/config เช่น BackupRetention__DailyDays, BackupRetention__KeepVerified</Alert>
        </Stack>
      </CardContent>
    </Card>
  );
}

function MetricCard({ title, value, helper, color = "primary" }: { title: string; value: string; helper: string; color?: "primary" | "success" | "error" | "gold" }) {
  const valueColor = color === "success" ? "success.main" : color === "error" ? "error.main" : color === "gold" ? brandColors.accent : "primary.main";
  return (
    <Grid item xs={12} md={3}>
      <Card sx={{ height: "100%", borderTop: `4px solid ${brandColors.accent}` }}>
        <CardContent>
          <Typography color="text.secondary">{title}</Typography>
          <Typography variant="h4" fontWeight={900} color={valueColor}>{value}</Typography>
          <Typography color="text.secondary" sx={{ wordBreak: "break-word" }}>{helper}</Typography>
        </CardContent>
      </Card>
    </Grid>
  );
}

function InfoRow({ label, value }: { label: string; value: string }) {
  return (
    <Stack direction="row" justifyContent="space-between" spacing={2}>
      <Typography color="text.secondary">{label}</Typography>
      <Typography fontWeight={900} textAlign="right" sx={{ wordBreak: "break-word" }}>{value || "-"}</Typography>
    </Stack>
  );
}

function StatusChip({ status }: { status: string }) {
  const color = status === "Success" || status === "Verified" || status === "Keep"
    ? "success"
    : status === "Failed" || status === "VerificationFailed" || status === "Delete"
      ? "error"
      : status === "Protected" || status === "Previewed"
        ? "info"
        : "warning";
  return <Chip size="small" label={status} color={color} variant="outlined" />;
}

function formatBytes(value: number) {
  if (!Number.isFinite(value) || value <= 0) {
    return "0 B";
  }

  const units = ["B", "KB", "MB", "GB", "TB"];
  let size = value;
  let unitIndex = 0;
  while (size >= 1024 && unitIndex < units.length - 1) {
    size /= 1024;
    unitIndex += 1;
  }
  return `${size.toFixed(size >= 10 || unitIndex === 0 ? 0 : 1)} ${units[unitIndex]}`;
}

async function invalidateBackupQueries(queryClient: ReturnType<typeof useQueryClient>) {
  await Promise.all([
    queryClient.invalidateQueries({ queryKey: ["backup-overview"] }),
    queryClient.invalidateQueries({ queryKey: ["backups"] }),
    queryClient.invalidateQueries({ queryKey: ["backup-detail"] }),
    queryClient.invalidateQueries({ queryKey: ["restore-runs"] }),
    queryClient.invalidateQueries({ queryKey: ["backup-retention-preview"] }),
  ]);
}
