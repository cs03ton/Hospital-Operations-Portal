import { Fragment, useEffect, useRef, useState, type ReactNode } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Link as RouterLink,
  useNavigate,
  useParams,
  useSearchParams,
} from "react-router-dom";
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
  Divider,
  MenuItem,
  Pagination,
  Paper,
  Skeleton,
  Stack,
  TextField,
  Typography,
  TableHead,
  TableBody,
  TableRow,
  TableCell,
  useMediaQuery,
  useTheme,
  InputAdornment,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import ArrowBackOutlinedIcon from "@mui/icons-material/ArrowBackOutlined";
import ArrowForwardOutlinedIcon from "@mui/icons-material/ArrowForwardOutlined";
import AssignmentOutlinedIcon from "@mui/icons-material/AssignmentOutlined";
import BuildOutlinedIcon from "@mui/icons-material/BuildOutlined";
import CancelOutlinedIcon from "@mui/icons-material/CancelOutlined";
import ClearOutlinedIcon from "@mui/icons-material/ClearOutlined";
import FactCheckOutlinedIcon from "@mui/icons-material/FactCheckOutlined";
import FlagOutlinedIcon from "@mui/icons-material/FlagOutlined";
import HistoryOutlinedIcon from "@mui/icons-material/HistoryOutlined";
import ImageOutlinedIcon from "@mui/icons-material/ImageOutlined";
import Inventory2OutlinedIcon from "@mui/icons-material/Inventory2Outlined";
import KeyboardReturnOutlinedIcon from "@mui/icons-material/KeyboardReturnOutlined";
import LocationOnOutlinedIcon from "@mui/icons-material/LocationOnOutlined";
import NoteAddOutlinedIcon from "@mui/icons-material/NoteAddOutlined";
import PlayArrowOutlinedIcon from "@mui/icons-material/PlayArrowOutlined";
import ReplayOutlinedIcon from "@mui/icons-material/ReplayOutlined";
import SearchOutlinedIcon from "@mui/icons-material/SearchOutlined";
import SendOutlinedIcon from "@mui/icons-material/SendOutlined";
import ScheduleOutlinedIcon from "@mui/icons-material/ScheduleOutlined";
import TaskAltOutlinedIcon from "@mui/icons-material/TaskAltOutlined";
import TuneOutlinedIcon from "@mui/icons-material/TuneOutlined";
import UndoOutlinedIcon from "@mui/icons-material/UndoOutlined";
import { PageToolbar } from "../components/common/PageToolbar";
import { InfoCard } from "../components/common/InfoCard";
import { DataTableCard } from "../components/common/DataTableCard";
import { EmptyState } from "../components/common/EmptyState";
import {
  RepairStatus as Status,
  RepairError as Failure,
  RepairPriority,
  RepairTeam,
  RepairFilePicker as FilePicker,
  RepairRefresh,
  RepairUpdating,
  RepairField,
} from "../components/repairs/RepairUi";
import { formatThaiBuddhistDateTime as date } from "../utils/dateFormat";
import {
  repairTeamLabel,
  repairNumber,
  repairStatusLabels,
  repairActionLabels as actionLabels,
  repairPriorityLabels as priorityLabels,
} from "../utils/repairPresentation";
import { PageHeader } from "../components/PageHeader";
import { usePermission } from "../context/PermissionContext";
import { useAuth } from "../context/AuthContext";
import { getMyProfile, type UserProfile } from "../api/profileApi";
import { repairSelectProps } from "../components/repairs/repairSelectProps";
import { dashboardPollingOptions } from "../config/queryPolling";
import {
  orderRepairActions,
  primaryRepairAction,
} from "../utils/repairActions";
import * as api from "../api/repairApi";

const emptyInput: api.RepairInput = {
  categoryId: "",
  title: "",
  description: "",
  location: "",
  contact: "",
};

function requesterContact(profile?: UserProfile) {
  return profile?.phoneNumber?.trim()
    ? `${profile.fullname.trim()} · ${profile.phoneNumber.trim()}`
    : "";
}

type RepairActionGroup = "primary" | "support" | "risk";
const repairActionUi: Record<
  string,
  { group: RepairActionGroup; description: string; icon: ReactNode }
> = {
  start: {
    group: "primary",
    description: "เริ่มตรวจสอบและดำเนินการแก้ไข",
    icon: <PlayArrowOutlinedIcon />,
  },
  resume: {
    group: "primary",
    description: "นำงานกลับจากการรออะไหล่",
    icon: <PlayArrowOutlinedIcon />,
  },
  solve: {
    group: "primary",
    description: "บันทึกผลซ่อมและส่งให้ผู้แจ้งตรวจรับ",
    icon: <BuildOutlinedIcon />,
  },
  accept: {
    group: "primary",
    description: "ยืนยันผลซ่อมและปิดใบงาน",
    icon: <TaskAltOutlinedIcon />,
  },
  resubmit: {
    group: "primary",
    description: "แก้ข้อมูลและส่งกลับไปยังทีมที่รับผิดชอบ",
    icon: <SendOutlinedIcon />,
  },
  reopen: {
    group: "primary",
    description: "เปิดรอบซ่อมใหม่เมื่อพบปัญหาเดิมอีกครั้ง",
    icon: <ReplayOutlinedIcon />,
  },
  wait: {
    group: "support",
    description: "พักเวลาทำงานระหว่างรออะไหล่",
    icon: <Inventory2OutlinedIcon />,
  },
  priority: {
    group: "support",
    description: "กำหนดระดับความเร่งด่วนของงาน",
    icon: <FlagOutlinedIcon />,
  },
  note: {
    group: "support",
    description: "เพิ่มความคืบหน้าหรือข้อมูลประกอบ",
    icon: <NoteAddOutlinedIcon />,
  },
  return: {
    group: "risk",
    description: "ส่งคืนให้ผู้แจ้งแก้ประเภทหรือข้อมูล",
    icon: <KeyboardReturnOutlinedIcon />,
  },
  "reject-solution": {
    group: "risk",
    description: "แจ้งว่ายังแก้ไม่สำเร็จและส่งกลับให้ทีมดำเนินการ",
    icon: <UndoOutlinedIcon />,
  },
  cancel: {
    group: "risk",
    description: "ยกเลิกใบงานพร้อมระบุเหตุผล",
    icon: <CancelOutlinedIcon />,
  },
};

function RepairDetailSection({
  title,
  subtitle,
  icon,
  children,
}: {
  title: string;
  subtitle: string;
  icon: ReactNode;
  children: ReactNode;
}) {
  return (
    <Card sx={{ overflow: "hidden" }}>
      <Box
        sx={{
          px: { xs: 2, md: 2.5 },
          py: 1.75,
          borderLeft: 4,
          borderColor: "primary.main",
          borderBottom: 1,
          bgcolor: "rgba(31, 105, 86, 0.055)",
        }}
      >
        <Stack direction="row" gap={1.5} alignItems="center">
          <Box
            sx={{
              width: 38,
              height: 38,
              borderRadius: 1,
              bgcolor: "primary.main",
              color: "primary.contrastText",
              display: "grid",
              placeItems: "center",
              flexShrink: 0,
            }}
          >
            {icon}
          </Box>
          <Box sx={{ minWidth: 0 }}>
            <Typography variant="h6" fontWeight={800} color="primary.dark">
              {title}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {subtitle}
            </Typography>
          </Box>
        </Stack>
      </Box>
      <CardContent sx={{ p: { xs: 2, md: 2.5 } }}>{children}</CardContent>
    </Card>
  );
}

function useInvalidateRepairs() {
  const qc = useQueryClient();
  return () => qc.invalidateQueries({ queryKey: ["repairs"] });
}

export function RepairListPage() {
  const { hasAnyPermission, hasPermission } = usePermission();
  const canTeam = hasAnyPermission(api.repairWorkPermissions);
  const theme = useTheme();
  const desktop = useMediaQuery(theme.breakpoints.up("md"));
  const teamLabel = hasPermission("RepairManagement.ViewAll")
    ? "งานทั้งระบบ"
    : "งานทั้งหมดของทีม";
  const [params, setParams] = useSearchParams();
  const scopeParam = params.get("scope");
  const defaultScope = canTeam ? "team" : "mine";
  const scope =
    scopeParam === "all" ||
    (scopeParam === "team" && canTeam) ||
    (scopeParam === "mine" && hasPermission("RepairManagement.ViewOwn"))
      ? scopeParam
      : defaultScope;
  const statusParam = params.get("status") ?? "";
  const status = Object.keys(repairStatusLabels).includes(statusParam)
    ? statusParam
    : "";
  const updateFilter = (key: string, value: string) => {
    const next = new URLSearchParams(params);
    if (value) next.set(key, value);
    else next.delete(key);
    setParams(next);
    setPage(1);
  };
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  useEffect(() => {
    setPage(1);
  }, [scope, status]);
  const list = useQuery({
    queryKey: ["repairs", "list", scope, status, search, page],
    queryFn: () => api.repairList({ scope, page, status, search }),
    ...dashboardPollingOptions,
  });
  const summary = useQuery({
    queryKey: ["repairs", "summary"],
    queryFn: api.repairSummary,
    ...dashboardPollingOptions,
  });
  const totalPages = list.data
    ? Math.max(1, Math.ceil(list.data.total / list.data.pageSize))
    : 1;
  const scopeLabel =
    scope === "mine"
      ? "งานของฉัน"
      : scope === "team"
        ? teamLabel
        : "ทั้งหมดที่มีสิทธิ์ดู";
  const statusAccent = (value: string) => {
    if (value === "Submitted") return theme.palette.info.main;
    if (value === "InProgress") return theme.palette.primary.main;
    if (value === "WaitingParts" || value === "Returned")
      return theme.palette.warning.main;
    if (value === "Resolved" || value === "Closed")
      return theme.palette.success.main;
    return theme.palette.grey[500];
  };
  return (
    <Stack spacing={3} sx={{ minWidth: 0, maxWidth: 1440, mx: "auto" }}>
      <PageHeader
        title="งานแจ้งซ่อม"
        subtitle="ค้นหา ติดตามสถานะ และเปิดดูรายละเอียดงาน IT / ช่างทั่วไป"
      />
      <RepairUpdating busy={list.isFetching || summary.isFetching} />
      <PageToolbar>
        <Stack spacing={2} sx={{ width: "100%", minWidth: 0 }}>
          <Stack
            direction={{ xs: "column", sm: "row" }}
            justifyContent="space-between"
            alignItems={{ xs: "stretch", sm: "center" }}
            gap={1.5}
          >
            <Stack direction="row" gap={1} alignItems="center">
              <TuneOutlinedIcon color="primary" />
              <Box>
                <Typography fontWeight={700}>ตัวกรองรายการ</Typography>
                <Typography variant="caption" color="text.secondary">
                  {scopeLabel}
                  {summary.data &&
                    ` · อัปเดต ${date(summary.data.generatedAtUtc)}`}
                </Typography>
              </Box>
            </Stack>
            <Stack direction="row" gap={1} alignItems="center" flexWrap="wrap">
              {hasPermission("RepairManagement.Create") && (
                <Button
                  component={RouterLink}
                  to="/repairs/new"
                  variant="contained"
                  startIcon={<AddIcon />}
                >
                  แจ้งซ่อม
                </Button>
              )}
              <RepairRefresh
                busy={list.isFetching || summary.isFetching}
                onClick={() => {
                  void list.refetch();
                  void summary.refetch();
                }}
              />
            </Stack>
          </Stack>
          <Divider />
          <Box
            sx={{
              display: "grid",
              gridTemplateColumns: {
                xs: "minmax(0,1fr)",
                sm: "repeat(2,minmax(0,1fr))",
                lg: "220px 220px minmax(260px,1fr) auto",
              },
              gap: 1.5,
              alignItems: "center",
            }}
          >
            <TextField
              size="small"
              select
              SelectProps={repairSelectProps}
              label="ขอบเขต"
              value={scope}
              onChange={(e) => {
                updateFilter("scope", e.target.value);
              }}
            >
              <MenuItem value="all">ทั้งหมดที่มีสิทธิ์ดู</MenuItem>
              {hasPermission("RepairManagement.ViewOwn") && (
                <MenuItem value="mine">งานของฉัน</MenuItem>
              )}
              {canTeam && <MenuItem value="team">{teamLabel}</MenuItem>}
            </TextField>
            <TextField
              size="small"
              select
              SelectProps={repairSelectProps}
              label="สถานะ"
              value={status}
              onChange={(e) => {
                updateFilter("status", e.target.value);
              }}
            >
              <MenuItem value="">ทั้งหมด</MenuItem>
              {Object.entries(repairStatusLabels).map(([k, v]) => (
                <MenuItem key={k} value={k}>
                  {v}
                </MenuItem>
              ))}
            </TextField>
            <TextField
              size="small"
              label="ค้นหาหัวข้อหรือสถานที่"
              value={search}
              onChange={(e) => {
                setSearch(e.target.value);
                setPage(1);
              }}
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <SearchOutlinedIcon fontSize="small" />
                  </InputAdornment>
                ),
              }}
              sx={{ gridColumn: { sm: "1 / -1", lg: "auto" } }}
            />
            <Button
              startIcon={<ClearOutlinedIcon />}
              disabled={!status && !search && scope === defaultScope}
              onClick={() => {
                setParams({});
                setSearch("");
                setPage(1);
              }}
            >
              ล้างตัวกรอง
            </Button>
          </Box>
        </Stack>
      </PageToolbar>
      <Failure error={list.error || summary.error} />
      {list.isLoading && (
        <Stack aria-label="กำลังโหลดรายการ" spacing={1}>
          {[0, 1, 2, 3].map((item) => (
            <Skeleton key={item} variant="rounded" height={72} />
          ))}
        </Stack>
      )}
      {list.data?.items.length === 0 && (
        <EmptyState
          title="ไม่พบรายการแจ้งซ่อม"
          description="ลองเปลี่ยนขอบเขต สถานะ หรือคำค้นหา"
        />
      )}
      {desktop && !!list.data?.items.length && (
        <DataTableCard
          title="รายการแจ้งซ่อม"
          subtitle={`พบ ${list.data.total.toLocaleString("th-TH")} รายการ · ${scopeLabel}`}
          minTableWidth={920}
        >
          <TableHead>
            <TableRow>
              {[
                "เลขงาน / หัวข้อ",
                "ทีม",
                "สถานที่",
                "สถานะ",
                "ความเร่งด่วน",
                "รอบ",
                "อัปเดตล่าสุด",
                "จัดการ",
              ].map((label) => (
                <TableCell key={label}>{label}</TableCell>
              ))}
            </TableRow>
          </TableHead>
          <TableBody>
            {list.data?.items.map((r) => (
              <TableRow key={r.id} hover>
                <TableCell
                  sx={{
                    minWidth: 220,
                    maxWidth: 320,
                    overflowWrap: "anywhere",
                    borderLeft: `3px solid ${statusAccent(r.status)}`,
                    pl: 2,
                  }}
                >
                  <Typography fontWeight={800}>
                    {repairNumber(r.number)}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {r.title}
                  </Typography>
                </TableCell>
                <TableCell>
                  <RepairTeam value={r.teamCode} />
                </TableCell>
                <TableCell sx={{ maxWidth: 220, overflowWrap: "anywhere" }}>
                  {r.location}
                </TableCell>
                <TableCell>
                  <Status value={r.status} />
                </TableCell>
                <TableCell>
                  <RepairPriority value={r.priority} />
                </TableCell>
                <TableCell align="center">{r.currentRound}</TableCell>
                <TableCell sx={{ whiteSpace: "nowrap" }}>
                  {date(r.updatedAt)}
                </TableCell>
                <TableCell>
                  <Button
                    component={RouterLink}
                    to={`/repairs/${r.id}`}
                    endIcon={<ArrowForwardOutlinedIcon />}
                    sx={{ whiteSpace: "nowrap" }}
                  >
                    ดูรายละเอียด
                  </Button>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </DataTableCard>
      )}
      {!desktop &&
        list.data?.items.map((r) => (
          <Card
            key={r.id}
            sx={{
              borderRadius: 2,
              borderTop: `4px solid ${statusAccent(r.status)}`,
              overflowWrap: "anywhere",
              boxShadow: "0 6px 18px rgba(22, 65, 54, 0.06)",
            }}
          >
            <CardContent sx={{ p: 2, "&:last-child": { pb: 2 } }}>
              <Stack spacing={1.5}>
                <Stack
                  direction="row"
                  justifyContent="space-between"
                  useFlexGap
                  flexWrap="wrap"
                  gap={1}
                >
                  <Typography fontWeight={800}>
                    {repairNumber(r.number)}
                  </Typography>
                  <Status value={r.status} />
                </Stack>
                <Typography variant="h6" sx={{ lineHeight: 1.35 }}>
                  {r.title}
                </Typography>
                <Stack direction="row" gap={1} flexWrap="wrap" useFlexGap>
                  <RepairTeam value={r.teamCode} />
                  <RepairPriority value={r.priority} />
                  <Chip
                    size="small"
                    variant="outlined"
                    label={`รอบ ${r.currentRound}`}
                  />
                </Stack>
                <Stack direction="row" gap={1} alignItems="flex-start">
                  <LocationOnOutlinedIcon color="action" fontSize="small" />
                  <Typography variant="body2">{r.location}</Typography>
                </Stack>
                <Stack direction="row" gap={1} alignItems="center">
                  <ScheduleOutlinedIcon color="action" fontSize="small" />
                  <Typography variant="caption" color="text.secondary">
                    อัปเดต {date(r.updatedAt)}
                  </Typography>
                </Stack>
                <Divider />
                <Button
                  component={RouterLink}
                  to={`/repairs/${r.id}`}
                  endIcon={<ArrowForwardOutlinedIcon />}
                  sx={{ alignSelf: "flex-end" }}
                >
                  ดูรายละเอียด
                </Button>
              </Stack>
            </CardContent>
          </Card>
        ))}
      {list.data && (
        <Stack
          direction={{ xs: "column", sm: "row" }}
          justifyContent="space-between"
          alignItems="center"
          gap={1}
          sx={{ py: 1 }}
        >
          <Typography variant="body2" color="text.secondary">
            หน้า {page} จาก {totalPages} · ทั้งหมด{" "}
            {list.data.total.toLocaleString("th-TH")} รายการ
          </Typography>
          <Pagination
            page={page}
            onChange={(_, nextPage) => setPage(nextPage)}
            count={totalPages}
            size="small"
            color="primary"
            showFirstButton
            showLastButton
          />
        </Stack>
      )}
    </Stack>
  );
}

function InputFields({
  value,
  onChange,
  department,
  requesterName,
  requesterPhone,
}: {
  value: api.RepairInput;
  onChange: (v: api.RepairInput) => void;
  department?: string;
  requesterName?: string;
  requesterPhone?: string;
}) {
  const options = useQuery({
    queryKey: ["repairs", "options"],
    queryFn: api.repairOptions,
  });
  return (
    <Box
      sx={{
        display: "grid",
        gridTemplateColumns: { xs: "1fr", md: "repeat(2,minmax(0,1fr))" },
        gap: 2,
      }}
    >
      {options.error && (
        <Box sx={{ gridColumn: "1 / -1" }}>
          <Failure error={options.error} />
        </Box>
      )}
      <Typography fontWeight={700} sx={{ gridColumn: "1 / -1" }}>
        ประเภทและหน่วยงาน
      </Typography>
      <TextField
        required
        select
        SelectProps={repairSelectProps}
        label="ประเภทงาน"
        value={value.categoryId}
        onChange={(e) => onChange({ ...value, categoryId: e.target.value })}
      >
        {options.data?.categories.map((c) => (
          <MenuItem key={c.id} value={c.id}>
            {c.name} · {c.teamCode === "IT" ? "IT" : "ช่างทั่วไป"}
          </MenuItem>
        ))}
      </TextField>
      {department && (
        <TextField
          label="หน่วยงานผู้แจ้ง"
          value={department}
          disabled
          sx={(theme) => ({
            "& .MuiInputBase-root.Mui-disabled": { bgcolor: "action.hover" },
            "& .MuiInputBase-input.Mui-disabled": {
              WebkitTextFillColor: theme.palette.text.secondary,
            },
          })}
        />
      )}
      {(["title", "description", "location"] as const).map((key) => (
        <Fragment key={key}>
          {(key === "title" || key === "location") && (
            <Typography fontWeight={700} sx={{ gridColumn: "1 / -1", pt: 1 }}>
              {key === "title" ? "อาการและผลกระทบ" : "สถานที่และผู้ติดต่อ"}
            </Typography>
          )}
          <TextField
            key={key}
            required
            label={
              {
                title: "หัวข้อ",
                description: "อาการและผลกระทบ",
                location: "อาคาร / ชั้น / ห้อง / จุดติดตั้ง",
              }[key]
            }
            sx={{
              gridColumn:
                key === "description" || key === "title" ? "1 / -1" : undefined,
            }}
            value={value[key]}
            onChange={(e) => onChange({ ...value, [key]: e.target.value })}
            multiline={key === "description"}
            minRows={key === "description" ? 3 : undefined}
            inputProps={{
              maxLength: {
                title: 200,
                description: 8000,
                location: 500,
              }[key],
            }}
          />
        </Fragment>
      ))}
      <TextField label="ชื่อ–นามสกุลผู้แจ้ง" value={requesterName ?? ""} disabled />
      <TextField label="เบอร์โทรผู้แจ้ง" value={requesterPhone ?? ""} disabled />
    </Box>
  );
}

export function RepairCreatePage() {
  const { user } = useAuth();
  const profile = useQuery({
    queryKey: ["repair-requester-profile", user?.id],
    queryFn: getMyProfile,
    enabled: !!user?.id,
    staleTime: 0,
    refetchOnMount: "always",
    refetchOnWindowFocus: "always",
    refetchOnReconnect: "always",
    retry: false,
  });
  const [value, setValue] = useState<api.RepairInput>(emptyInput);
  const [files, setFiles] = useState<File[]>([]);
  const navigate = useNavigate();
  const invalidate = useInvalidateRepairs();
  const [created, setCreated] = useState<api.Repair | null>(null);
  const create = useMutation({
    mutationFn: async () => {
      const r = created ?? (await api.repairCreate({ ...value, contact: requesterContact(profile.data) }));
      setCreated(r);
      if (files.length) await api.repairUpload(r.id, r.concurrencyToken, files);
      return r;
    },
    onSuccess: (r) => {
      void invalidate();
      navigate(`/repairs/${r.id}`);
    },
  });
  return (
    <Stack
      spacing={2}
      component="form"
      onSubmit={(e) => {
        e.preventDefault();
        if (created || profile.data?.phoneNumber?.trim()) create.mutate();
      }}
    >
      <PageHeader
        title="แจ้งซ่อมและปัญหาการใช้งาน"
        subtitle="เลือกประเภทเพื่อส่งเข้าคิวทีมที่รับผิดชอบ"
      />
      <Failure error={create.error} />
      {profile.isError && (
        <Alert
          severity="error"
          action={
            <Button
              onClick={() => {
                void profile.refetch();
              }}
            >
              ลองใหม่
            </Button>
          }
        >
          โหลดข้อมูลส่วนตัวผู้แจ้งไม่สำเร็จ
        </Alert>
      )}
      {profile.data && !profile.data.phoneNumber?.trim() && !created && (
        <Alert severity="warning">
          ยังไม่มีเบอร์โทรในข้อมูลส่วนตัว กรุณา <RouterLink to="/profile">เพิ่มเบอร์โทร</RouterLink> ก่อนแจ้งซ่อม
        </Alert>
      )}
      <InfoCard title="รายละเอียดการแจ้งซ่อมและปัญหาการใช้งาน">
        <Stack spacing={2}>
          {created ? (
            <Alert severity="info">
              สร้างใบงานแล้ว หากรูปแนบไม่สำเร็จ สามารถลองแนบอีกครั้งหรือ{" "}
              <RouterLink to={`/repairs/${created.id}`}>เปิดใบงาน</RouterLink>{" "}
              ได้
            </Alert>
          ) : (
            <InputFields
              value={value}
              onChange={setValue}
              department={
                profile.isError
                  ? "โหลดข้อมูลไม่สำเร็จ"
                  : !profile.data
                    ? "กำลังโหลดหน่วยงาน…"
                  : profile.data.departmentName || "ยังไม่ระบุหน่วยงานในบัญชี"
              }
              requesterName={profile.data?.fullname ?? ""}
              requesterPhone={profile.data?.phoneNumber ?? ""}
            />
          )}
        </Stack>
      </InfoCard>
      <InfoCard title="รูปภาพประกอบ">
        <FilePicker
          files={files}
          setFiles={setFiles}
          disabled={create.isPending}
        />
      </InfoCard>
      <PageToolbar>
        <Stack direction={{ xs: "column", sm: "row" }} gap={1}>
          <Button
            type="submit"
            variant="contained"
            disabled={create.isPending || !value.categoryId || (!created && !profile.data?.phoneNumber?.trim())}
          >
            {create.isPending
              ? "กำลังบันทึก…"
              : created
                ? "แนบรูปอีกครั้ง"
                : "ส่งแจ้งซ่อม"}
          </Button>
          <Button
            component={RouterLink}
            to="/repairs"
            disabled={create.isPending}
          >
            กลับรายการ
          </Button>
        </Stack>
      </PageToolbar>
    </Stack>
  );
}

function ImagePreview({ id }: { id: string }) {
  const [url, setUrl] = useState("");
  const [open, setOpen] = useState(false);
  const image = useQuery({
    queryKey: ["repairs", "image", id],
    queryFn: () => api.repairImage(id),
    staleTime: 60000,
  });
  useEffect(() => {
    if (!image.data) return;
    const u = URL.createObjectURL(image.data);
    setUrl(u);
    return () => URL.revokeObjectURL(u);
  }, [image.data]);
  return (
    <>
      {image.isError ? (
        <Alert severity="warning">โหลดรูปไม่สำเร็จ</Alert>
      ) : (
        <Button
          onClick={() => setOpen(true)}
          aria-label="ดูรูปภาพประกอบ"
          sx={{ p: 0 }}
        >
          {url && (
            <Box
              component="img"
              src={url}
              loading="lazy"
              alt="รูปประกอบใบงาน"
              sx={{
                width: 160,
                height: 110,
                objectFit: "cover",
                maxWidth: "100%",
              }}
            />
          )}
        </Button>
      )}
      <Dialog
        open={open}
        onClose={() => setOpen(false)}
        maxWidth="lg"
        fullWidth
      >
        <DialogTitle>รูปภาพประกอบ</DialogTitle>
        <DialogContent>
          <Box
            component="img"
            src={url}
            alt="รูปประกอบขนาดเต็ม"
            sx={{ maxWidth: "100%", maxHeight: "75vh", objectFit: "contain" }}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpen(false)}>ปิด</Button>
        </DialogActions>
      </Dialog>
    </>
  );
}

export function RepairDetailPage() {
  const { id = "" } = useParams();
  const actionsRef = useRef<HTMLDivElement>(null);
  const [actionPanelVisible, setActionPanelVisible] = useState(true);
  const invalidate = useInvalidateRepairs();
  const detail = useQuery({
    queryKey: ["repairs", "detail", id],
    queryFn: () => api.repairDetail(id),
    ...dashboardPollingOptions,
  });
  const [action, setAction] = useState("");
  const [token, setToken] = useState("");
  const [note, setNote] = useState("");
  const [priority, setPriority] = useState("Normal");
  const [solver, setSolver] = useState("");
  const [contributors, setContributors] = useState<string[]>([]);
  const [contributorsOpen, setContributorsOpen] = useState(false);
  const [request, setRequest] = useState<api.RepairInput>(emptyInput);
  const requesterProfile = useQuery({
    queryKey: ["repair-requester-profile", "resubmit"],
    queryFn: getMyProfile,
    enabled: action === "resubmit",
    staleTime: 0,
  });
  const [files, setFiles] = useState<File[]>([]);
  const solvers = useQuery({
    queryKey: ["repairs", "solvers", id],
    queryFn: () => api.repairSolvers(id),
    enabled: action === "solve",
  });
  const change = useMutation({
    mutationFn: () =>
      api.repairChange(id, action, {
        concurrencyToken: token,
        note: action === "start" ? "" : note,
        priority,
        solverId: solver || undefined,
        contributorIds: contributors,
        request: action === "resubmit" ? { ...request, contact: requesterContact(requesterProfile.data) } : undefined,
      }),
    onSuccess: () => {
      setAction("");
      setNote("");
      void invalidate();
    },
    onError: () => {
      void invalidate();
    },
  });
  const upload = useMutation({
    mutationFn: () =>
      api.repairUpload(id, detail.data!.request.concurrencyToken, files),
    onSuccess: () => {
      setFiles([]);
      void invalidate();
    },
    onError: () => {
      void invalidate();
    },
  });
  const loadedRequestId = detail.data?.request.id;
  useEffect(() => {
    const node = actionsRef.current;
    if (!node) return;
    const observer = new IntersectionObserver(
      ([entry]) => setActionPanelVisible(entry.isIntersecting),
      { threshold: 0.1 },
    );
    observer.observe(node);
    return () => observer.disconnect();
  }, [loadedRequestId]);
  const d = detail.data;
  if (!d)
    return (
      <Stack spacing={2} sx={{ maxWidth: 1440, mx: "auto" }}>
        <Failure error={detail.error} />
        {detail.isLoading ? (
          <>
            <Skeleton variant="text" width="35%" height={48} />
            <Skeleton variant="rounded" height={120} />
            <Skeleton variant="rounded" height={260} />
          </>
        ) : (
          <EmptyState title="ไม่พบใบงานหรือไม่มีสิทธิ์" />
        )}
      </Stack>
    );
  const person = (userId?: string) =>
    d.people.find((x) => x.id === userId)?.fullName ?? "-";
  const orderedActions = orderRepairActions(d.actions);
  const primaryAction = primaryRepairAction(d.actions);
  const openAction = (nextAction: string) => {
    setAction(nextAction);
    setToken(d.request.concurrencyToken);
    setNote("");
    setSolver("");
    setContributors([]);
    setRequest(d.request);
    change.reset();
  };
  const actionButton = (item: string) => {
    const meta = repairActionUi[item] ?? {
      group: "support" as const,
      description: "ดำเนินการกับใบงาน",
      icon: <AssignmentOutlinedIcon />,
    };
    return (
      <Button
        key={item}
        variant={meta.group === "primary" ? "contained" : "outlined"}
        color={
          item === "cancel"
            ? "error"
            : meta.group === "risk"
              ? "warning"
              : "primary"
        }
        startIcon={meta.icon}
        onClick={() => openAction(item)}
        sx={{ justifyContent: "flex-start" }}
      >
        {actionLabels[item] ?? item}
      </Button>
    );
  };
  return (
    <Stack
      spacing={3}
      sx={{
        minWidth: 0,
        maxWidth: 1440,
        mx: "auto",
        overflowWrap: "anywhere",
        pb: { xs: primaryAction ? 10 : 0, md: 0 },
      }}
    >
      <PageHeader
        title={repairNumber(d.request.number)}
        subtitle={d.request.title}
      />
      <PageToolbar>
        <Stack
          direction={{ xs: "column", sm: "row" }}
          justifyContent="space-between"
          alignItems={{ xs: "stretch", sm: "center" }}
          gap={1.5}
          sx={{ width: "100%" }}
        >
          <Stack
            direction="row"
            flexWrap="wrap"
            useFlexGap
            gap={1}
            alignItems="center"
          >
            <Button
              component={RouterLink}
              to="/repairs"
              startIcon={<ArrowBackOutlinedIcon />}
            >
              กลับรายการ
            </Button>
            <Status value={d.request.status} />
            <RepairPriority value={d.request.priority} />
            <RepairTeam value={d.request.teamCode} />
          </Stack>
          <Stack direction="row" gap={1} alignItems="center" flexWrap="wrap">
            <Typography variant="caption" color="text.secondary">
              อัปเดตใบงาน {date(d.request.updatedAt)}
            </Typography>
            <RepairRefresh
              busy={detail.isFetching}
              onClick={() => {
                void detail.refetch();
              }}
            />
          </Stack>
        </Stack>
      </PageToolbar>
      <RepairUpdating busy={detail.isFetching} />
      <Failure error={detail.error} />
      <Card ref={actionsRef} sx={{ borderTop: 4, borderColor: "primary.main" }}>
        <CardContent sx={{ p: { xs: 2, md: 2.5 } }}>
          <Stack spacing={2}>
            <Box>
              <Typography
                variant="overline"
                color="primary.main"
                fontWeight={800}
              >
                สถานะปัจจุบัน: {repairStatusLabels[d.request.status]}
              </Typography>
              <Typography variant="h5" fontWeight={800}>
                ขั้นตอนถัดไป
              </Typography>
              <Typography
                variant="body2"
                color="text.secondary"
                sx={{ mt: 0.5 }}
              >
                {primaryAction
                  ? repairActionUi[primaryAction].description
                  : "ใบงานนี้ไม่มีขั้นตอนหลักที่ต้องดำเนินการในขณะนี้"}
              </Typography>
            </Box>
            {(["primary", "support", "risk"] as RepairActionGroup[]).map(
              (group) => {
                const items = orderedActions.filter(
                  (item) =>
                    (repairActionUi[item]?.group ?? "support") === group,
                );
                if (!items.length) return null;
                return (
                  <Box key={group}>
                    <Typography
                      variant="caption"
                      color="text.secondary"
                      fontWeight={700}
                    >
                      {group === "primary"
                        ? "ดำเนินงานต่อ"
                        : group === "support"
                          ? "จัดการข้อมูล"
                          : "ส่งกลับหรือยกเลิก"}
                    </Typography>
                    <Stack
                      direction="row"
                      useFlexGap
                      flexWrap="wrap"
                      gap={1}
                      sx={{ mt: 0.75 }}
                    >
                      {items.map(actionButton)}
                    </Stack>
                  </Box>
                );
              },
            )}
          </Stack>
        </CardContent>
      </Card>
      <RepairDetailSection
        title="ข้อมูลใบงาน"
        subtitle="ผู้แจ้ง สถานที่ และขอบเขตงานที่ทีมรับผิดชอบ"
        icon={<AssignmentOutlinedIcon />}
      >
        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: { xs: "1fr", md: "repeat(2,minmax(0,1fr))" },
            columnGap: 4,
            rowGap: 2.25,
          }}
        >
          <RepairField label="ผู้แจ้ง">
            {person(d.request.requesterId)}
          </RepairField>
          <RepairField label="หน่วยงาน">
            {d.departmentName ?? "ไม่ระบุ"}
          </RepairField>
          <RepairField label="ประเภทงาน">{d.categoryName}</RepairField>
          <RepairField label="ทีมรับผิดชอบ">
            {repairTeamLabel(d.request.teamCode)}
          </RepairField>
          <RepairField label="สถานที่">{d.request.location}</RepairField>
          <RepairField label="ผู้ติดต่อ">{d.request.contact}</RepairField>
          <RepairField label="ความเร่งด่วน">
            <RepairPriority value={d.request.priority} />
          </RepairField>
          <RepairField label="รอบซ่อมปัจจุบัน">
            {d.request.currentRound}
          </RepairField>
          <Box sx={{ gridColumn: "1 / -1" }}>
            <RepairField label="อาการและผลกระทบ">
              {d.request.description}
            </RepairField>
          </Box>
        </Box>
      </RepairDetailSection>
      <RepairDetailSection
        title="ประวัติการดำเนินการ"
        subtitle="ลำดับเหตุการณ์ ผู้ดำเนินการ และบันทึกของแต่ละรอบ"
        icon={<HistoryOutlinedIcon />}
      >
        <Stack spacing={2}>
          {!d.events.length && (
            <EmptyState title="ยังไม่มีประวัติการดำเนินการ" />
          )}
          {d.events.map((e) => (
            <Box
              key={e.id}
              sx={{
                borderLeft: 3,
                borderColor: "primary.main",
                borderRadius: 1,
                bgcolor: "action.hover",
                px: 2,
                py: 1.5,
              }}
            >
              <Stack
                direction={{ xs: "column", sm: "row" }}
                justifyContent="space-between"
                alignItems={{ xs: "flex-start", sm: "center" }}
                gap={1}
              >
                <Box>
                  <Typography fontWeight={700}>
                    {actionLabels[e.action] ??
                      (e.action === "submit" ? "ส่งแจ้งซ่อม" : "แนบรูป")}{" "}
                    · รอบ {e.round}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {date(e.createdAt)} · ดำเนินการโดย {person(e.actorId)}
                  </Typography>
                </Box>
                <Status value={e.toStatus} />
              </Stack>
              {e.note && (
                <Typography sx={{ whiteSpace: "pre-wrap", mt: 1 }}>
                  {e.note}
                </Typography>
              )}
              {e.action === "priority" && (
                <Typography variant="body2">
                  {priorityLabels[e.priority ?? ""] ?? "ยังไม่ประเมิน"}
                </Typography>
              )}
              {e.solverId && (
                <Typography variant="body2">
                  ผู้แก้ไขหลัก: {person(e.solverId)} · ผู้ร่วม:{" "}
                  {d.contributors
                    .filter((c) => c.eventId === e.id)
                    .map((c) => person(c.userId))
                    .join(", ") || "-"}
                </Typography>
              )}
              <Stack direction="row" useFlexGap flexWrap="wrap" gap={1}>
                {d.images
                  .filter((i) => i.eventId === e.id)
                  .map((i) => (
                    <ImagePreview key={i.id} id={i.id} />
                  ))}
              </Stack>
            </Box>
          ))}
        </Stack>
      </RepairDetailSection>
      <RepairDetailSection
        title="รอบการซ่อมและผลตรวจรับ"
        subtitle="ผลการแก้ไข การตรวจรับ และช่วงเวลารออะไหล่"
        icon={<FactCheckOutlinedIcon />}
      >
        <Stack spacing={2}>
          {d.rounds.map((r) => (
            <Box
              key={r.id}
              sx={{
                p: 2,
                border: 1,
                borderColor: "divider",
                borderRadius: 1,
              }}
            >
              <Stack
                direction={{ xs: "column", sm: "row" }}
                justifyContent="space-between"
                gap={1}
              >
                <Typography fontWeight={800}>รอบที่ {r.number}</Typography>
                <Typography variant="caption" color="text.secondary">
                  เริ่ม {date(r.startedAt)} · ปิด {date(r.closedAt)}
                </Typography>
              </Stack>
              <Divider sx={{ my: 1.25 }} />
              <Typography variant="body2">
                ผู้ตรวจรับ: {person(r.acceptedById)}
              </Typography>
              <Typography sx={{ whiteSpace: "pre-wrap", mt: 0.5 }}>
                {r.acceptanceNote || "ยังไม่มีผลตรวจรับ"}
              </Typography>
            </Box>
          ))}
          {!d.rounds.length && <EmptyState title="ยังไม่มีข้อมูลรอบซ่อม" />}
          {d.waiting.length > 0 && (
            <>
              <Divider />
              <Typography fontWeight={600}>ช่วงรออะไหล่</Typography>
              {d.waiting.map((w) => (
                <Typography key={w.id} variant="body2">
                  รอบ {w.round}: {date(w.startedAt)} ถึง{" "}
                  {w.endedAt ? date(w.endedAt) : "กำลังรอ"}
                </Typography>
              ))}
            </>
          )}
        </Stack>
      </RepairDetailSection>
      {d.canUpload && (
        <RepairDetailSection
          title="รูปภาพประกอบ"
          subtitle="เพิ่มภาพความคืบหน้าหรือผลการแก้ไข สูงสุดตามข้อกำหนดของระบบ"
          icon={<ImageOutlinedIcon />}
        >
          <Stack spacing={2}>
            <FilePicker
              files={files}
              setFiles={setFiles}
              disabled={upload.isPending}
            />
            <Failure error={upload.error} />
            <Button
              disabled={!files.length || upload.isPending}
              variant="contained"
              onClick={() => upload.mutate()}
              sx={{ alignSelf: { sm: "flex-end" } }}
            >
              บันทึกรูปภาพ
            </Button>
          </Stack>
        </RepairDetailSection>
      )}
      {primaryAction && !actionPanelVisible && (
        <Paper
          elevation={8}
          aria-label="การดำเนินการหลักบนมือถือ"
          sx={{
            display: { xs: "block", md: "none" },
            position: "fixed",
            left: 12,
            right: 12,
            bottom: 8,
            maxWidth: 560,
            mx: "auto",
            zIndex: (currentTheme) => currentTheme.zIndex.appBar,
            p: 1,
            pb: "max(8px, env(safe-area-inset-bottom))",
            border: 1,
            borderColor: "divider",
            borderRadius: 2,
          }}
        >
          <Stack direction="row" gap={1}>
            <Button
              fullWidth
              variant="contained"
              startIcon={repairActionUi[primaryAction].icon}
              aria-label={`ดำเนินการหลัก ${actionLabels[primaryAction]}`}
              onClick={() => openAction(primaryAction)}
            >
              {actionLabels[primaryAction]}
            </Button>
            <Button
              variant="outlined"
              sx={{ flexShrink: 0 }}
              onClick={() =>
                actionsRef.current?.scrollIntoView({
                  behavior: "smooth",
                  block: "start",
                })
              }
            >
              ตัวเลือกทั้งหมด
            </Button>
          </Stack>
        </Paper>
      )}
      <Dialog
        open={!!action}
        onClose={() => {
          if (!change.isPending) setAction("");
        }}
        fullWidth
        maxWidth="sm"
      >
        <DialogTitle>{actionLabels[action]}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            <Failure error={change.error || solvers.error || (action === "resubmit" ? requesterProfile.error : null)} />
            {token !== d.request.concurrencyToken && (
              <Alert severity="warning">
                สถานะล่าสุด: {repairStatusLabels[d.request.status]}{" "}
                กรุณาตรวจสอบข้อมูลล่าสุดก่อนยืนยัน
              </Alert>
            )}
            {(change.isError || token !== d.request.concurrencyToken) && (
              <Button
                disabled={detail.isFetching}
                onClick={async () => {
                  const latest = await detail.refetch();
                  if (latest.data && !latest.isError) {
                    setToken(latest.data.request.concurrencyToken);
                    change.reset();
                  }
                }}
              >
                โหลดข้อมูลล่าสุดโดยคงข้อความ
              </Button>
            )}
            {!d.actions.includes(action) && (
              <Alert severity="warning">
                สถานะปัจจุบันไม่รองรับการดำเนินการนี้
              </Alert>
            )}
            {action === "resubmit" && (
              <>
                {requesterProfile.data && !requesterProfile.data.phoneNumber?.trim() && (
                  <Alert severity="warning">กรุณา <RouterLink to="/profile">เพิ่มเบอร์โทรในข้อมูลส่วนตัว</RouterLink> ก่อนส่งแจ้งซ่อมอีกครั้ง</Alert>
                )}
                <InputFields
                  value={request}
                  onChange={setRequest}
                  department={requesterProfile.data?.departmentName ?? ""}
                  requesterName={requesterProfile.data?.fullname ?? ""}
                  requesterPhone={requesterProfile.data?.phoneNumber ?? ""}
                />
              </>
            )}
            {action === "priority" && (
              <TextField
                select
                SelectProps={repairSelectProps}
                label="ความเร่งด่วน"
                value={priority}
                onChange={(e) => setPriority(e.target.value)}
              >
                {Object.entries(priorityLabels).map(([k, v]) => (
                  <MenuItem key={k} value={k}>
                    {v}
                  </MenuItem>
                ))}
              </TextField>
            )}
            {action === "solve" && (
              <>
                <TextField
                  select
                  SelectProps={repairSelectProps}
                  required
                  label="ผู้แก้ไขหลัก"
                  value={solver}
                  onChange={(e) => {
                    setSolver(e.target.value);
                    setContributors((current) =>
                      current.filter((id) => id !== e.target.value),
                    );
                  }}
                >
                  {solvers.data?.map((p) => (
                    <MenuItem key={p.id} value={p.id}>
                      {p.fullName}
                    </MenuItem>
                  ))}
                </TextField>
                <TextField
                  select
                  label="ผู้ร่วมแก้ไข"
                  SelectProps={{
                    ...repairSelectProps,
                    multiple: true,
                    open: contributorsOpen,
                    onOpen: () => setContributorsOpen(true),
                    onClose: () => setContributorsOpen(false),
                  }}
                  value={contributors}
                  onChange={(e) => {
                    setContributors(
                      typeof e.target.value === "string"
                        ? e.target.value.split(",")
                        : e.target.value,
                    );
                    setContributorsOpen(false);
                  }}
                >
                  {solvers.data
                    ?.filter((p) => p.id !== solver)
                    .map((p) => (
                      <MenuItem key={p.id} value={p.id}>
                        {p.fullName}
                      </MenuItem>
                    ))}
                </TextField>
              </>
            )}
            {action === "start" ? (
              <Alert severity="info">
                ยืนยันเริ่มดำเนินการงานแจ้งซ่อมนี้
              </Alert>
            ) : (
              <TextField
                required={action !== "resume"}
                label={
                  action === "accept" ? "ผลการตรวจรับ" : "รายละเอียด / เหตุผล"
                }
                multiline
                minRows={3}
                value={note}
                onChange={(e) => setNote(e.target.value)}
                inputProps={{ maxLength: 8000 }}
              />
            )}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button disabled={change.isPending} onClick={() => setAction("")}>
            กลับ
          </Button>
          <Button
            variant="contained"
            disabled={
              change.isPending ||
              token !== d.request.concurrencyToken ||
              !d.actions.includes(action) ||
              (action === "solve" && !solver) ||
              (action === "resubmit" &&
                [
                  request.categoryId,
                  request.title,
                  request.description,
                  request.location,
                ].some((value) => !value.trim())) ||
              (action === "resubmit" && !requesterProfile.data?.phoneNumber?.trim()) ||
              (!["start", "resume"].includes(action) && !note.trim())
            }
            onClick={() => change.mutate()}
          >
            ยืนยัน
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
}
