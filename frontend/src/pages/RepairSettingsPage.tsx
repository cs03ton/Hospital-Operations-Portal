import { useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import AddOutlinedIcon from "@mui/icons-material/AddOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import CategoryOutlinedIcon from "@mui/icons-material/CategoryOutlined";
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
import { repairTeamLabel } from "../utils/repairPresentation";
import { brandColors } from "../theme/theme";
import { usePermission } from "../context/PermissionContext";

export function RepairSettingsPage() {
  const { hasPermission } = usePermission();
  const desktop = useMediaQuery(useTheme().breakpoints.up("md"));
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
  const saveCategory = useMutation({
    mutationFn: () => api.saveRepairCategory(category!),
    onSuccess: () => {
      setCategory(null);
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
  return (
    <Stack spacing={3} sx={{ minWidth: 0, maxWidth: 1440, mx: "auto" }}>
      <PageHeader
        title="ตั้งค่าระบบแจ้งซ่อม"
        subtitle="จัดการประเภทงานแจ้งซ่อม"
      />
      {(hasPermission("LineGroup.View") || hasPermission("LineGroup.Manage")) && <Button component={RouterLink} to="/admin/line-groups" variant="outlined" sx={{ alignSelf: "flex-start" }}>จัดการกลุ่มแจ้งเตือนส่วนกลาง</Button>}
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
        <Stack direction="row" alignItems="center" gap={1} sx={{ flex: 1, px: 1, fontWeight: 700, color: "primary.main" }}>
          <CategoryOutlinedIcon /> ประเภทงาน
        </Stack>
        <RepairRefresh
          busy={query.isFetching}
          onClick={() => {
            void query.refetch();
          }}
        />
      </Stack>
      <Box sx={{ minWidth: 0 }}>
        {(
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
    </Stack>
  );
}
