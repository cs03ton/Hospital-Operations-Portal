import { useQuery } from "@tanstack/react-query";
import { Link as RouterLink } from "react-router-dom";
import {
  Box,
  Button,
  Card,
  CardActionArea,
  CardContent,
  Chip,
  Divider,
  Skeleton,
  Stack,
  Typography,
  useTheme,
} from "@mui/material";
import { alpha } from "@mui/material/styles";
import AddOutlinedIcon from "@mui/icons-material/AddOutlined";
import ArrowForwardOutlinedIcon from "@mui/icons-material/ArrowForwardOutlined";
import BuildOutlinedIcon from "@mui/icons-material/BuildOutlined";
import InboxOutlinedIcon from "@mui/icons-material/InboxOutlined";
import Inventory2OutlinedIcon from "@mui/icons-material/Inventory2Outlined";
import FactCheckOutlinedIcon from "@mui/icons-material/FactCheckOutlined";
import TaskAltOutlinedIcon from "@mui/icons-material/TaskAltOutlined";
import UndoOutlinedIcon from "@mui/icons-material/UndoOutlined";
import BlockOutlinedIcon from "@mui/icons-material/BlockOutlined";
import SettingsOutlinedIcon from "@mui/icons-material/SettingsOutlined";
import { PageHeader } from "../components/PageHeader";
import { InfoCard } from "../components/common/InfoCard";
import { EmptyState } from "../components/common/EmptyState";
import {
  RepairError,
  RepairPriority,
  RepairTeam,
  RepairRefresh,
  RepairStatus,
  RepairUpdating,
} from "../components/repairs/RepairUi";
import { usePermission } from "../context/PermissionContext";
import {
  repairList,
  repairSummary,
  repairWorkPermissions,
} from "../api/repairApi";
import { dashboardPollingOptions } from "../config/queryPolling";
import { formatThaiBuddhistDateTime } from "../utils/dateFormat";
import {
  repairNumber,
  repairStatusLabels,
  repairTeamLabel,
} from "../utils/repairPresentation";

const metrics = [
  { status: "Submitted", icon: InboxOutlinedIcon, color: "info" },
  { status: "InProgress", icon: BuildOutlinedIcon, color: "primary" },
  { status: "WaitingParts", icon: Inventory2OutlinedIcon, color: "warning" },
  { status: "Resolved", icon: FactCheckOutlinedIcon, color: "success" },
  { status: "Closed", icon: TaskAltOutlinedIcon, color: "success" },
  { status: "Returned", icon: UndoOutlinedIcon, color: "warning" },
  { status: "Cancelled", icon: BlockOutlinedIcon, color: "secondary" },
] as const;

export function RepairDashboardPage() {
  const theme = useTheme();
  const { hasPermission, hasAnyPermission } = usePermission();
  const canTeam = hasAnyPermission(repairWorkPermissions);
  const all = hasPermission("RepairManagement.ViewAll");
  const scopeLabel = all
    ? "ภาพรวมงานทั้งระบบ"
    : canTeam
      ? hasPermission("RepairManagement.ViewOwn")
        ? "งานของฉันและงานของทีมที่มีสิทธิ์ดู"
        : "งานของทีมที่มีสิทธิ์ดู"
      : "งานของฉัน";
  const summary = useQuery({
    queryKey: ["repairs", "summary"],
    queryFn: repairSummary,
    ...dashboardPollingOptions,
  });
  const recent = useQuery({
    queryKey: ["repairs", "recent", "all", 5],
    queryFn: () =>
      repairList({
        scope: "all",
        page: 1,
        pageSize: 5,
        status: "",
        search: "",
      }),
    ...dashboardPollingOptions,
  });
  return (
    <Stack spacing={3} sx={{ maxWidth: 1440, mx: "auto", minWidth: 0 }}>
      <PageHeader
        title="Dashboard แจ้งซ่อม"
        subtitle="ภาพรวมสถานะงาน IT และช่างทั่วไป"
      />
      <Stack
        direction={{ xs: "column", md: "row" }}
        gap={2}
        justifyContent="space-between"
        alignItems={{ xs: "stretch", md: "center" }}
        sx={{ borderBottom: 1, borderColor: "divider", pb: 2 }}
      >
        <Box>
          <Typography variant="h6" color="primary.main">
            {scopeLabel}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {summary.data
              ? `อัปเดตล่าสุด ${formatThaiBuddhistDateTime(summary.data.generatedAtUtc)}`
              : summary.isError
                ? "ไม่สามารถโหลดภาพรวม"
                : "กำลังโหลดภาพรวม"}
          </Typography>
        </Box>
        <Stack direction="row" gap={1} flexWrap="wrap" alignItems="center">
          {hasPermission("RepairManagement.Create") && (
            <Button
              component={RouterLink}
              to="/repairs/new"
              variant="contained"
              startIcon={<AddOutlinedIcon />}
            >
              แจ้งซ่อม
            </Button>
          )}
          <RepairRefresh
            busy={summary.isFetching || recent.isFetching}
            onClick={() => {
              void summary.refetch();
              void recent.refetch();
            }}
          />
        </Stack>
      </Stack>
      <RepairUpdating busy={summary.isFetching || recent.isFetching} />
      <RepairError error={summary.error} />
      <Box
        aria-label="สรุปสถานะงานแจ้งซ่อม"
        sx={{
          display: "grid",
          gridTemplateColumns: {
            xs: "repeat(2,minmax(0,1fr))",
            sm: "repeat(3,minmax(0,1fr))",
            lg: "repeat(4,minmax(0,1fr))",
          },
          gap: 2,
        }}
      >
        {metrics.map(({ status, icon: Icon, color }) => {
          const accent = theme.palette[color].main;
          return (
            <Card
              key={status}
              sx={{
                borderRadius: 2,
                borderTop: `4px solid ${accent}`,
                boxShadow: `0 6px 18px ${alpha(theme.palette.primary.dark, 0.06)}`,
              }}
            >
              <CardActionArea
                component={RouterLink}
                to={`/repairs?scope=all&status=${status}`}
                aria-label={`ดูงาน${repairStatusLabels[status]}`}
                sx={{ height: "100%" }}
              >
                <CardContent sx={{ p: { xs: 1.5, sm: 2 }, minHeight: 145 }}>
                  <Stack
                    direction="row"
                    justifyContent="space-between"
                    alignItems="center"
                    gap={1}
                  >
                    <Box
                      sx={{
                        p: 1,
                        borderRadius: 1,
                        bgcolor: alpha(accent, 0.1),
                        color: accent,
                        display: "flex",
                      }}
                    >
                      <Icon />
                    </Box>
                    <ArrowForwardOutlinedIcon
                      sx={{ color: "text.secondary", fontSize: 18 }}
                    />
                  </Stack>
                  <Typography
                    variant="body2"
                    sx={{ mt: 1.5, overflowWrap: "anywhere" }}
                  >
                    {repairStatusLabels[status]}
                  </Typography>
                  {summary.data ? (
                    <Typography variant="h4" fontWeight={800} color={accent}>
                      {summary.data.counts.find((x) => x.status === status)
                        ?.count ?? 0}
                    </Typography>
                  ) : summary.isLoading ? (
                    <Skeleton width={55} height={40} />
                  ) : (
                    <Typography variant="h4">-</Typography>
                  )}
                </CardContent>
              </CardActionArea>
            </Card>
          );
        })}
      </Box>
      <Box
        sx={{
          display: "grid",
          gridTemplateColumns: {
            xs: "minmax(0,1fr)",
            lg: "minmax(0,2fr) minmax(260px,1fr)",
          },
          gap: 3,
          alignItems: "start",
        }}
      >
        <InfoCard
          title="รายการแจ้งซ่อมล่าสุด"
          subtitle={scopeLabel}
          actions={
            <Button
              component={RouterLink}
              to="/repairs?scope=all"
              endIcon={<ArrowForwardOutlinedIcon />}
            >
              ดูทั้งหมด
            </Button>
          }
        >
          <RepairError error={recent.error} />
          {recent.isLoading && (
            <Stack spacing={2}>
              {[1, 2, 3].map((i) => (
                <Skeleton key={i} variant="rounded" height={90} />
              ))}
            </Stack>
          )}
          {recent.data?.items.length === 0 && (
            <EmptyState title="ยังไม่มีรายการแจ้งซ่อม" />
          )}
          <Stack spacing={2} divider={<Divider />}>
            {recent.data?.items.slice(0, 5).map((r) => (
              <Box key={r.id} sx={{ minWidth: 0, overflowWrap: "anywhere" }}>
                <Stack
                  direction="row"
                  flexWrap="wrap"
                  useFlexGap
                  justifyContent="space-between"
                  gap={1}
                >
                  <Typography fontWeight={700}>
                    {repairNumber(r.number)}
                  </Typography>
                  <RepairStatus value={r.status} />
                </Stack>
                <Typography sx={{ mt: 0.5 }}>{r.title}</Typography>
                <Stack
                  direction="row"
                  flexWrap="wrap"
                  useFlexGap
                  gap={1}
                  alignItems="center"
                  sx={{ my: 1 }}
                >
                  <RepairTeam value={r.teamCode} />
                  <RepairPriority value={r.priority} />
                </Stack>
                <Stack
                  direction={{ xs: "column", sm: "row" }}
                  justifyContent="space-between"
                  gap={1}
                  alignItems={{ xs: "flex-start", sm: "center" }}
                >
                  <Typography variant="caption" color="text.secondary">
                    อัปเดต {formatThaiBuddhistDateTime(r.updatedAt)}
                  </Typography>
                  <Button component={RouterLink} to={`/repairs/${r.id}`}>
                    ดูรายละเอียด
                  </Button>
                </Stack>
              </Box>
            ))}
          </Stack>
        </InfoCard>
        <InfoCard title="ทางลัด">
          <Stack spacing={1.5}>
            <Button
              component={RouterLink}
              to="/repairs?scope=all"
              variant="outlined"
              startIcon={<InboxOutlinedIcon />}
              sx={{ justifyContent: "flex-start" }}
            >
              รายการทั้งหมดที่มีสิทธิ์ดู
            </Button>
            {canTeam && (
              <>
                <Button
                  component={RouterLink}
                  to="/repairs?scope=team"
                  variant="outlined"
                  startIcon={<BuildOutlinedIcon />}
                  sx={{ justifyContent: "flex-start" }}
                >
                  {all ? "คิวงานทั้งระบบ" : "คิวงานของทีม"}
                </Button>
                {summary.data && (
                  <Typography variant="body2" color="text.secondary">
                    งานค้าง{all ? "ทั้งระบบ" : "ของทีม"}:{" "}
                    {summary.data.teamPending}
                  </Typography>
                )}
              </>
            )}
            {hasPermission("RepairManagement.Manage") && (
              <Button
                component={RouterLink}
                to="/repairs/settings"
                variant="outlined"
                startIcon={<SettingsOutlinedIcon />}
                sx={{ justifyContent: "flex-start" }}
              >
                ตั้งค่าระบบแจ้งซ่อม
              </Button>
            )}
          </Stack>
        </InfoCard>
      </Box>
    </Stack>
  );
}
