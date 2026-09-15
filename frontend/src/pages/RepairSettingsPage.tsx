import { useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import AddOutlinedIcon from "@mui/icons-material/AddOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import SettingsOutlinedIcon from "@mui/icons-material/SettingsOutlined";
import NotificationsActiveOutlinedIcon from "@mui/icons-material/NotificationsActiveOutlined";
import CategoryOutlinedIcon from "@mui/icons-material/CategoryOutlined";
import GroupsOutlinedIcon from "@mui/icons-material/GroupsOutlined";
import HistoryOutlinedIcon from "@mui/icons-material/HistoryOutlined";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Alert,
  Box,
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  MenuItem,
  Stack,
  Skeleton,
  TextField,
  Typography,
  Tabs,
  Tab,
  Card,
  CardContent,
  Chip,
  TableHead,
  TableBody,
  TableCell,
  TableRow,
  Tooltip,
  IconButton,
  useMediaQuery,
  useTheme,
} from "@mui/material";
import { PageHeader } from "../components/PageHeader";
import * as api from "../api/repairApi";
import { dashboardPollingOptions } from "../config/queryPolling";
import { repairSelectProps } from "../components/repairs/repairSelectProps";
import { DataTableCard } from "../components/common/DataTableCard";
import { EmptyState } from "../components/common/EmptyState";
import { PageToolbar } from "../components/common/PageToolbar";
import { ListPagination } from "../components/common/ListPagination";
import { RepairRefresh, RepairUpdating } from "../components/repairs/RepairUi";
import {
  repairTeamLabel,
  repairDeliveryLabels,
} from "../utils/repairPresentation";
import { extractApiErrorMessage } from "../utils/apiError";
import { brandColors } from "../theme/theme";

const settingTabs = [
  { label: "ประเภทงาน", icon: <CategoryOutlinedIcon /> },
  { label: "กลุ่มแจ้งเตือน", icon: <GroupsOutlinedIcon /> },
  { label: "ผลส่งแจ้งเตือน", icon: <HistoryOutlinedIcon /> },
];

export function RepairSettingsPage() {
  const desktop = useMediaQuery(useTheme().breakpoints.up("md"));
  const [tab, setTab] = useState(0);
  const [search, setSearch] = useState("");
  const [team, setTeam] = useState("");
  const [active, setActive] = useState("");
  const [categoryPage, setCategoryPage] = useState(1);
  const [categoryPageSize, setCategoryPageSize] = useState(10);
  const query = useQuery({
    queryKey: ["repairs", "settings"],
    queryFn: api.repairSettings,
    ...dashboardPollingOptions,
  });
  const qc = useQueryClient();
  const categories =
    query.data?.categories.filter(
      (c) =>
        c.name
          .toLocaleLowerCase()
          .includes(search.trim().toLocaleLowerCase()) &&
        (!team || c.teamCode === team) &&
        (!active || c.isActive === (active === "active")),
    ) ?? [];
  const categoryPageCount = Math.max(1, Math.ceil(categories.length / categoryPageSize));
  const safeCategoryPage = Math.min(categoryPage, categoryPageCount);
  const pagedCategories = categories.slice(
    (safeCategoryPage - 1) * categoryPageSize,
    safeCategoryPage * categoryPageSize,
  );
  const [category, setCategory] = useState<Partial<api.RepairCategory> | null>(
    null,
  );
  const [group, setGroup] = useState<api.RepairGroup | null>(null);
  const [secret, setSecret] = useState("");
  const saveCategory = useMutation({
    mutationFn: () => api.saveRepairCategory(category!),
    onSuccess: () => {
      setCategory(null);
      void qc.invalidateQueries({ queryKey: ["repairs"] });
    },
  });
  const saveGroup = useMutation({
    mutationFn: () =>
      api.saveRepairGroup(group!.module.replace("REPAIR_", ""), {
        displayName: group!.displayName,
        endpointUrl: group!.endpointUrl ?? "",
        clientId: group!.clientId ?? "",
        clientSecret: secret || undefined,
        enabled: group!.status === "Active",
        concurrencyToken: group!.concurrencyToken,
      }),
    onSuccess: () => {
      setGroup(null);
      setSecret("");
      void qc.invalidateQueries({ queryKey: ["repairs"] });
    },
  });
  const activeBadge = (enabled: boolean) => (
    <Chip
      size="small"
      label={enabled ? "เปิดใช้งาน" : "ปิดใช้งาน"}
      color={enabled ? "success" : "default"}
    />
  );
  const deliveryBadge = (status: string) => (
    <Chip
      size="small"
      label={repairDeliveryLabels[status] ?? status}
      color={
        status === "Sent"
          ? "success"
          : status === "Attention"
            ? "error"
            : status === "Superseded"
              ? "default"
              : "info"
      }
    />
  );
  const editCategory = (c: api.RepairCategory) => (
    <Tooltip title={`แก้ไข ${c.name}`}>
      <IconButton
        aria-label={`แก้ไข ${c.name}`}
        color="primary"
        onClick={() => {
          setCategory({ ...c });
          saveCategory.reset();
        }}
      >
        <EditOutlinedIcon />
      </IconButton>
    </Tooltip>
  );
  let validEndpoint = false;
  try {
    const url = new URL(group?.endpointUrl ?? "");
    validEndpoint = url.protocol === "https:" && !url.username && !url.password;
  } catch {
    /* Incomplete URL while editing. */
  }
  const groupInvalid =
    !group?.displayName.trim() ||
    !group.clientId?.trim() ||
    !validEndpoint ||
    (!group.hasSecret && !secret.trim());
  return (
    <Stack spacing={3} sx={{ minWidth: 0, maxWidth: 1440, mx: "auto" }}>
      <PageHeader
        title="ตั้งค่าระบบแจ้งซ่อม"
        subtitle="หมวดงานและปลายทางแจ้งเตือน IT / ช่างทั่วไป"
      />
      <RepairUpdating busy={query.isFetching} />
      {query.isLoading && (
        <Stack aria-label="กำลังโหลดการตั้งค่า" spacing={1}>
          {[0, 1, 2].map((i) => (
            <Skeleton key={i} variant="rounded" height={64} />
          ))}
        </Stack>
      )}
      {query.isError && <Alert severity="error">โหลดการตั้งค่าไม่สำเร็จ</Alert>}
      <Stack
        direction="row"
        alignItems="center"
        gap={1}
        sx={{
          minWidth: 0,
          p: 0.75,
          border: "1px solid",
          borderColor: brandColors.accentSoft,
          borderRadius: 1,
          bgcolor: "#FFF9EB",
          boxShadow: "0 4px 14px rgba(111, 85, 57, 0.07)",
        }}
      >
        <Tabs
          value={tab}
          onChange={(_, value: number) => setTab(value)}
          variant="scrollable"
          scrollButtons="auto"
          allowScrollButtonsMobile
          aria-label="ตั้งค่าระบบแจ้งซ่อม"
          sx={{
            minWidth: 0,
            flex: 1,
            minHeight: 48,
            "& .MuiTabs-indicator": {
              height: 3,
              borderRadius: "3px 3px 0 0",
              bgcolor: brandColors.accent,
            },
            "& .MuiTab-root": {
              minHeight: 48,
              minWidth: { xs: 150, sm: 170 },
              borderRadius: 1,
              color: "text.secondary",
              fontWeight: 700,
              transition: "background-color 160ms ease, color 160ms ease",
            },
            "& .MuiTab-root:hover": {
              bgcolor: "rgba(200, 169, 107, 0.16)",
              color: "primary.main",
            },
            "& .MuiTab-root.Mui-selected": {
              bgcolor: "primary.main",
              color: "primary.contrastText",
            },
          }}
        >
          {settingTabs.map(
            (item, index) => (
              <Tab
                key={item.label}
                label={item.label}
                icon={item.icon}
                iconPosition="start"
                id={`repair-tab-${index}`}
                aria-controls={`repair-panel-${index}`}
              />
            ),
          )}
        </Tabs>
        <RepairRefresh
          busy={query.isFetching}
          onClick={() => {
            void query.refetch();
          }}
        />
      </Stack>
      <Box
        role="tabpanel"
        id={`repair-panel-${tab}`}
        aria-labelledby={`repair-tab-${tab}`}
        sx={{ minWidth: 0 }}
      >
        {tab === 0 && (
          <Stack spacing={2}>
            <PageToolbar>
              <Stack
                direction={{ xs: "column", md: "row" }}
                gap={1.5}
                useFlexGap
                flexWrap="wrap"
                sx={{ width: "100%" }}
              >
                <TextField
                  size="small"
                  label="ค้นหาประเภทงาน"
                  value={search}
                  onChange={(e) => {
                    setSearch(e.target.value);
                    setCategoryPage(1);
                  }}
                  sx={{ flex: 1, minWidth: 160 }}
                />
                <TextField
                  size="small"
                  select
                  SelectProps={repairSelectProps}
                  label="ทีม"
                  value={team}
                  onChange={(e) => {
                    setTeam(e.target.value);
                    setCategoryPage(1);
                  }}
                  sx={{ minWidth: 150 }}
                >
                  <MenuItem value="">ทุกทีม</MenuItem>
                  <MenuItem value="IT">IT</MenuItem>
                  <MenuItem value="GENERAL">ช่างทั่วไป</MenuItem>
                </TextField>
                <TextField
                  size="small"
                  select
                  SelectProps={repairSelectProps}
                  label="สถานะการใช้งาน"
                  value={active}
                  onChange={(e) => {
                    setActive(e.target.value);
                    setCategoryPage(1);
                  }}
                  sx={{ minWidth: 160 }}
                >
                  <MenuItem value="">ทั้งหมด</MenuItem>
                  <MenuItem value="active">เปิดใช้งาน</MenuItem>
                  <MenuItem value="inactive">ปิดใช้งาน</MenuItem>
                </TextField>
                <Button
                  onClick={() => {
                    setSearch("");
                    setTeam("");
                    setActive("");
                    setCategoryPage(1);
                  }}
                >
                  ล้างตัวกรอง
                </Button>
                <Button
                  variant="contained"
                  startIcon={<AddOutlinedIcon />}
                  onClick={() => {
                    setCategory({ name: "", teamCode: "IT", isActive: true });
                    saveCategory.reset();
                  }}
                >
                  เพิ่มประเภท
                </Button>
              </Stack>
            </PageToolbar>
            {query.data && categories.length === 0 && (
              <EmptyState title="ไม่พบประเภทงาน" />
            )}
            {desktop && categories.length > 0 ? (
              <DataTableCard minTableWidth={600}>
                <TableHead>
                  <TableRow>
                    {["ชื่อประเภท", "ทีมรับผิดชอบ", "สถานะ", "จัดการ"].map(
                      (label) => (
                        <TableCell key={label}>{label}</TableCell>
                      ),
                    )}
                  </TableRow>
                </TableHead>
                <TableBody>
                  {pagedCategories.map((c) => (
                    <TableRow key={c.id} hover>
                      <TableCell
                        sx={{ overflowWrap: "anywhere", maxWidth: 500 }}
                      >
                        {c.name}
                      </TableCell>
                      <TableCell>{repairTeamLabel(c.teamCode)}</TableCell>
                      <TableCell>{activeBadge(c.isActive)}</TableCell>
                      <TableCell>{editCategory(c)}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </DataTableCard>
            ) : (
              !desktop &&
              pagedCategories.map((c) => (
                <Card key={c.id} sx={{ borderRadius: 2 }}>
                  <CardContent>
                    <Stack
                      direction="row"
                      justifyContent="space-between"
                      alignItems="flex-start"
                      gap={1}
                    >
                      <Box sx={{ minWidth: 0 }}>
                        <Typography
                          fontWeight={700}
                          sx={{ overflowWrap: "anywhere" }}
                        >
                          {c.name}
                        </Typography>
                        <Typography
                          color="text.secondary"
                          variant="body2"
                          sx={{ my: 1 }}
                        >
                          {repairTeamLabel(c.teamCode)}
                        </Typography>
                        {activeBadge(c.isActive)}
                      </Box>
                      {editCategory(c)}
                    </Stack>
                  </CardContent>
                </Card>
              ))
            )}
            {categories.length > 0 && (
              <ListPagination
                page={safeCategoryPage}
                pageSize={categoryPageSize}
                totalItems={categories.length}
                onPageChange={setCategoryPage}
                onPageSizeChange={(nextPageSize) => {
                  setCategoryPageSize(nextPageSize);
                  setCategoryPage(1);
                }}
                pageSizeOptions={[10, 20, 50]}
                disabled={query.isFetching}
              />
            )}
          </Stack>
        )}
        {tab === 1 && (
          <Box
            sx={{
              display: "grid",
              gridTemplateColumns: {
                xs: "minmax(0,1fr)",
                md: "repeat(2,minmax(0,1fr))",
              },
              gap: 2,
            }}
          >
            {query.data?.groups.length === 0 && (
              <EmptyState title="ยังไม่มีปลายทางแจ้งเตือน" />
            )}
            {query.data?.groups.map((g) => (
              <Card
                key={g.id}
                sx={{
                  borderRadius: 2,
                  borderTop: 4,
                  borderTopColor: "primary.main",
                }}
              >
                <CardContent>
                  <Stack spacing={2}>
                    <Stack direction="row" alignItems="center" gap={1}>
                      <NotificationsActiveOutlinedIcon color="primary" />
                      <Typography variant="h6">
                        {repairTeamLabel(g.module.replace("REPAIR_", ""))}
                      </Typography>
                    </Stack>
                    <Typography
                      fontWeight={700}
                      sx={{ overflowWrap: "anywhere" }}
                    >
                      {g.displayName}
                    </Typography>
                    <Stack direction="row" flexWrap="wrap" useFlexGap gap={1}>
                      {activeBadge(g.status === "Active")}
                      <Chip
                        size="small"
                        variant="outlined"
                        color={g.hasSecret ? "success" : "warning"}
                        label={
                          g.hasSecret
                            ? "ตั้งค่า credentials แล้ว"
                            : "ยังไม่มี credentials"
                        }
                      />
                    </Stack>
                    <Button
                      variant="outlined"
                      startIcon={<SettingsOutlinedIcon />}
                      onClick={() => {
                        setGroup({ ...g });
                        setSecret("");
                        saveGroup.reset();
                      }}
                    >
                      ตั้งค่าปลายทาง
                    </Button>
                  </Stack>
                </CardContent>
              </Card>
            ))}
          </Box>
        )}
        {tab === 2 && (
          <Stack spacing={2}>
            <Typography variant="h6">ผลส่งแจ้งเตือนล่าสุด</Typography>
            {query.data?.deliveries.length === 0 && (
              <EmptyState title="ยังไม่มีรายการส่ง" />
            )}
            {desktop && !!query.data?.deliveries.length ? (
              <DataTableCard minTableWidth={650}>
                <TableHead>
                  <TableRow>
                    {[
                      "ทีม",
                      "สถานะ",
                      "จำนวนครั้ง",
                      "รหัสข้อผิดพลาด",
                      "ใบงาน",
                    ].map((label) => (
                      <TableCell key={label}>{label}</TableCell>
                    ))}
                  </TableRow>
                </TableHead>
                <TableBody>
                  {query.data.deliveries.map((d) => (
                    <TableRow key={d.id}>
                      <TableCell>{repairTeamLabel(d.teamCode)}</TableCell>
                      <TableCell>{deliveryBadge(d.status)}</TableCell>
                      <TableCell>{d.attempts}</TableCell>
                      <TableCell
                        sx={{ maxWidth: 300, overflowWrap: "anywhere" }}
                      >
                        {d.errorCode || "-"}
                      </TableCell>
                      <TableCell>
                        <Button
                          component={RouterLink}
                          to={`/repairs/${d.requestId}`}
                        >
                          ดูใบงาน
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </DataTableCard>
            ) : (
              !desktop &&
              query.data?.deliveries.map((d) => (
                <Card key={d.id} sx={{ borderRadius: 2 }}>
                  <CardContent>
                    <Stack spacing={1}>
                      <Stack
                        direction="row"
                        gap={1}
                        flexWrap="wrap"
                        useFlexGap
                        justifyContent="space-between"
                      >
                        <Typography fontWeight={700}>
                          {repairTeamLabel(d.teamCode)}
                        </Typography>
                        {deliveryBadge(d.status)}
                      </Stack>
                      <Typography variant="body2">
                        จำนวนครั้ง: {d.attempts}
                      </Typography>
                      <Typography
                        variant="body2"
                        sx={{ overflowWrap: "anywhere" }}
                      >
                        รหัสข้อผิดพลาด: {d.errorCode || "-"}
                      </Typography>
                      <Button
                        component={RouterLink}
                        to={`/repairs/${d.requestId}`}
                      >
                        ดูใบงาน
                      </Button>
                    </Stack>
                  </CardContent>
                </Card>
              ))
            )}
          </Stack>
        )}
      </Box>
      <Dialog
        open={!!category}
        onClose={() => {
          if (!saveCategory.isPending) setCategory(null);
        }}
        fullWidth
        maxWidth="sm"
      >
        <DialogTitle>ประเภทงาน</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {saveCategory.isError && (
              <Alert severity="error">
                บันทึกไม่สำเร็จ ชื่ออาจซ้ำหรือข้อมูลเปลี่ยนแล้ว
              </Alert>
            )}
            {saveCategory.isError && category?.id && (
              <Button
                onClick={async () => {
                  const latest = await query.refetch();
                  const row = latest.data?.categories.find(
                    (c) => c.id === category.id,
                  );
                  if (row && !latest.isError) {
                    setCategory({
                      ...category,
                      concurrencyToken: row.concurrencyToken,
                    });
                    saveCategory.reset();
                  }
                }}
              >
                โหลดข้อมูลล่าสุดโดยคงข้อความ
              </Button>
            )}
            <TextField
              required
              label="ชื่อประเภท"
              value={category?.name ?? ""}
              onChange={(e) =>
                setCategory({ ...category, name: e.target.value })
              }
              inputProps={{ maxLength: 100 }}
            />
            <TextField
              select
              SelectProps={repairSelectProps}
              label="ทีมรับผิดชอบ"
              value={category?.teamCode ?? "IT"}
              onChange={(e) =>
                setCategory({ ...category, teamCode: e.target.value })
              }
            >
              <MenuItem value="IT">IT</MenuItem>
              <MenuItem value="GENERAL">ช่างทั่วไป</MenuItem>
            </TextField>
            <FormControlLabel
              label="เปิดใช้งาน"
              control={
                <Checkbox
                  checked={category?.isActive ?? false}
                  onChange={(_, v) => setCategory({ ...category, isActive: v })}
                />
              }
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button
            onClick={() => setCategory(null)}
            disabled={saveCategory.isPending}
          >
            กลับ
          </Button>
          <Button
            disabled={saveCategory.isPending || !category?.name?.trim()}
            variant="contained"
            onClick={() => saveCategory.mutate()}
          >
            บันทึก
          </Button>
        </DialogActions>
      </Dialog>
      <Dialog
        open={!!group}
        onClose={() => {
          if (!saveGroup.isPending) setGroup(null);
        }}
        fullWidth
        maxWidth="sm"
      >
        <DialogTitle>ปลายทางหมอพร้อม</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {saveGroup.isError && (
              <Alert severity="error">
                {extractApiErrorMessage(
                  saveGroup.error,
                  "บันทึกไม่สำเร็จ กรุณาตรวจสอบข้อมูลแล้วลองใหม่",
                )}
              </Alert>
            )}
            {saveGroup.isError && group && (
              <Button
                onClick={async () => {
                  const latest = await query.refetch();
                  const row = latest.data?.groups.find(
                    (g) => g.id === group.id,
                  );
                  if (row && !latest.isError) {
                    setGroup({
                      ...group,
                      concurrencyToken: row.concurrencyToken,
                    });
                    saveGroup.reset();
                  }
                }}
              >
                โหลดข้อมูลล่าสุดโดยคงข้อความ
              </Button>
            )}
            <TextField
              label="ชื่อกลุ่ม"
              required
              inputProps={{ maxLength: 100 }}
              value={group?.displayName ?? ""}
              onChange={(e) =>
                group && setGroup({ ...group, displayName: e.target.value })
              }
            />
            <TextField
              label="Endpoint URL"
              required
              error={!!group?.endpointUrl && !validEndpoint}
              helperText="ใช้ HTTPS และ host ที่ผู้ดูแลอนุญาต"
              inputProps={{ maxLength: 1000 }}
              value={group?.endpointUrl ?? ""}
              onChange={(e) =>
                group && setGroup({ ...group, endpointUrl: e.target.value })
              }
            />
            <TextField
              label="Client ID"
              required
              inputProps={{ maxLength: 300 }}
              value={group?.clientId ?? ""}
              onChange={(e) =>
                group && setGroup({ ...group, clientId: e.target.value })
              }
            />
            <TextField
              label="Client Secret"
              helperText={
                group?.hasSecret
                  ? "เว้นว่างเพื่อคงค่าเดิม"
                  : "กรอกข้อมูลเชื่อมต่อก่อนเปิดใช้งาน"
              }
              type="password"
              required={!group?.hasSecret}
              inputProps={{ maxLength: 2000 }}
              autoComplete="new-password"
              value={secret}
              onChange={(e) => setSecret(e.target.value)}
            />
            <FormControlLabel
              label="เปิดส่งแจ้งเตือน"
              control={
                <Checkbox
                  checked={group?.status === "Active"}
                  onChange={(_, v) =>
                    group &&
                    setGroup({ ...group, status: v ? "Active" : "Disabled" })
                  }
                />
              }
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button
            disabled={saveGroup.isPending}
            onClick={() => {
              setGroup(null);
              setSecret("");
            }}
          >
            กลับ
          </Button>
          <Button
            disabled={saveGroup.isPending || groupInvalid}
            variant="contained"
            onClick={() => saveGroup.mutate()}
          >
            บันทึก
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
}
