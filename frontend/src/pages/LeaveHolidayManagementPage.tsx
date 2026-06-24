import BlockOutlinedIcon from "@mui/icons-material/BlockOutlined";
import DownloadOutlinedIcon from "@mui/icons-material/DownloadOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import ClearOutlinedIcon from "@mui/icons-material/ClearOutlined";
import SearchOutlinedIcon from "@mui/icons-material/SearchOutlined";
import UploadFileOutlinedIcon from "@mui/icons-material/UploadFileOutlined";
import { Alert, Button, Card, CardContent, Checkbox, Chip, Dialog, DialogContent, DialogTitle, FormControlLabel, Grid, IconButton, MenuItem, Stack, TableBody, TableCell, TableHead, TablePagination, TableRow, TextField, Typography } from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { confirmLeaveHolidayImport, createLeaveHoliday, deactivateLeaveHoliday, downloadLeaveHolidayTemplate, getLeaveHolidaysPaged, previewLeaveHolidayImport, updateLeaveHoliday, type LeaveHoliday, type LeaveHolidayImportPreview, type SaveLeaveHolidayRequest } from "../api/leaveApi";
import { ActionTooltip } from "../components/common/ActionTooltip";
import { AppDatePicker } from "../components/common/AppDatePicker";
import { DataTableCard } from "../components/common/DataTableCard";
import { FilterToolbar } from "../components/common/FilterToolbar";
import { PageToolbar } from "../components/common/PageToolbar";
import { PageHeader } from "../components/PageHeader";
import { useNotification } from "../hooks/useNotification";
import { formatDateForApi, formatThaiDate, isValidApiDate } from "../utils/dateFormat";

const emptyHoliday: SaveLeaveHolidayRequest = {
  holidayDate: formatDateForApi(new Date()),
  name: "",
  isActive: true,
};

export function LeaveHolidayManagementPage() {
  const queryClient = useQueryClient();
  const { showSuccess } = useNotification();
  const currentYear = new Date().getFullYear();
  const [year, setYear] = useState(currentYear);
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(20);
  const [editing, setEditing] = useState<LeaveHoliday | null>(null);
  const [importOpen, setImportOpen] = useState(false);
  const [importFile, setImportFile] = useState<File | null>(null);
  const [preview, setPreview] = useState<LeaveHolidayImportPreview | null>(null);
  const yearOptions = useMemo(() => [currentYear - 1, currentYear, currentYear + 1, currentYear + 2], [currentYear]);
  const queryParams = useMemo(() => ({
    year,
    page: page + 1,
    pageSize,
    search: search || undefined,
  }), [page, pageSize, search, year]);
  const { data, isLoading } = useQuery({
    queryKey: ["leave-holidays", "paged", queryParams],
    queryFn: () => getLeaveHolidaysPaged(queryParams),
  });
  const { control, register, handleSubmit, reset, formState: { errors } } = useForm<SaveLeaveHolidayRequest>({ defaultValues: emptyHoliday });

  const saveMutation = useMutation({
    mutationFn: (values: SaveLeaveHolidayRequest) => editing ? updateLeaveHoliday(editing.id, values) : createLeaveHoliday(values),
    onSuccess: async () => {
      showSuccess(editing ? "บันทึกวันหยุดราชการเรียบร้อยแล้ว" : "เพิ่มวันหยุดราชการเรียบร้อยแล้ว");
      setEditing(null);
      reset(emptyHoliday);
      await queryClient.invalidateQueries({ queryKey: ["leave-holidays"] });
    },
  });
  const deleteMutation = useMutation({
    mutationFn: deactivateLeaveHoliday,
    onSuccess: () => {
      showSuccess("ปิดใช้งานวันหยุดราชการเรียบร้อยแล้ว");
      queryClient.invalidateQueries({ queryKey: ["leave-holidays"] });
    },
  });
  const previewMutation = useMutation({ mutationFn: previewLeaveHolidayImport, onSuccess: setPreview });
  const confirmMutation = useMutation({
    mutationFn: confirmLeaveHolidayImport,
    onSuccess: async () => {
      showSuccess("นำเข้าวันหยุดราชการเรียบร้อยแล้ว");
      setImportOpen(false);
      setImportFile(null);
      setPreview(null);
      await queryClient.invalidateQueries({ queryKey: ["leave-holidays"] });
    },
  });

  const validRows = useMemo(() => preview?.rows.filter((row) => row.isValid && row.holidayDate).map((row) => ({
    holidayDate: row.holidayDate!,
    name: row.name,
    holidayType: row.holidayType,
  })) ?? [], [preview]);

  function onEdit(item: LeaveHoliday) {
    setEditing(item);
    reset({ holidayDate: formatDateForApi(item.holidayDate), name: item.name, isActive: item.isActive });
  }

  function applyFilters() {
    setSearch(searchInput.trim());
    setPage(0);
  }

  function clearFilters() {
    setYear(currentYear);
    setSearchInput("");
    setSearch("");
    setPage(0);
  }

  async function handleDownloadTemplate() {
    const blob = await downloadLeaveHolidayTemplate();
    downloadBlob(blob, "leave-holiday-import-template.csv");
    showSuccess("ดาวน์โหลดตัวอย่างไฟล์เรียบร้อยแล้ว");
  }

  function handleCloseImport() {
    setImportOpen(false);
    setImportFile(null);
    setPreview(null);
  }

  return (
    <>
      <PageHeader title="วันหยุดราชการ" subtitle="กำหนดวันหยุดที่ต้องตัดออกจากการคำนวณจำนวนวันลา" />
      <PageToolbar>
        <Typography variant="body2" color="text.secondary">
          จัดการวันหยุดรายปีและเตรียมนำเข้าข้อมูลวันหยุดสำหรับปีถัดไป
        </Typography>
        <Stack direction={{ xs: "column", sm: "row" }} spacing={1} justifyContent="flex-end">
          <Button variant="outlined" startIcon={<DownloadOutlinedIcon />} onClick={handleDownloadTemplate}>
            ดาวน์โหลดตัวอย่างไฟล์
          </Button>
          <Button variant="contained" startIcon={<UploadFileOutlinedIcon />} onClick={() => setImportOpen(true)}>
            Import วันหยุดราชการ
          </Button>
        </Stack>
      </PageToolbar>
      <Stack spacing={2}>
        <FilterToolbar>
          <Grid item xs={12} md={2}>
            <TextField
              select
              fullWidth
              size="small"
              label="ปี"
              value={year}
              onChange={(event) => {
                setYear(Number(event.target.value));
                setPage(0);
              }}
            >
              {yearOptions.map((item) => (
                <MenuItem key={item} value={item}>
                  {item + 543}
                </MenuItem>
              ))}
            </TextField>
          </Grid>
          <Grid item xs={12} md={6}>
            <TextField
              fullWidth
              size="small"
              label="ค้นหาวันหยุด"
              value={searchInput}
              onChange={(event) => setSearchInput(event.target.value)}
              onKeyDown={(event) => {
                if (event.key === "Enter") {
                  event.preventDefault();
                  applyFilters();
                }
              }}
              InputProps={{ startAdornment: <SearchOutlinedIcon color="action" sx={{ mr: 1 }} /> }}
            />
          </Grid>
          <Grid item xs={12} md={4}>
            <Stack direction="row" spacing={1} justifyContent="flex-end" flexWrap="wrap" useFlexGap>
              <Button variant="contained" startIcon={<SearchOutlinedIcon />} onClick={applyFilters}>
                ค้นหา
              </Button>
              <Button variant="outlined" startIcon={<ClearOutlinedIcon />} onClick={clearFilters}>
                ล้างตัวกรอง
              </Button>
            </Stack>
          </Grid>
        </FilterToolbar>

        <Card>
          <CardContent>
            <Typography variant="h6" sx={{ mb: 2 }}>{editing ? "แก้ไขวันหยุด" : "เพิ่มวันหยุด"}</Typography>
            <Stack component="form" spacing={2} onSubmit={handleSubmit((values) => saveMutation.mutate(values))}>
              {saveMutation.isError && <Alert severity="error">บันทึกวันหยุดไม่สำเร็จ</Alert>}
              <Grid container spacing={1.5}>
                <Grid item xs={12} md={4}>
                  <Controller
                    name="holidayDate"
                    control={control}
                    rules={{
                      required: "กรุณาเลือกวันที่",
                      validate: (value) => isValidApiDate(value) || "กรุณาเลือกวันที่ให้ถูกต้อง",
                    }}
                    render={({ field }) => (
                      <AppDatePicker
                        label="วันที่"
                        value={field.value}
                        onChange={field.onChange}
                        error={Boolean(errors.holidayDate)}
                        helperText={errors.holidayDate?.message ?? "เลือกวันที่จากปฏิทิน"}
                      />
                    )}
                  />
                </Grid>
                <Grid item xs={12} md={5}>
                  <TextField fullWidth size="small" label="ชื่อวันหยุด" InputLabelProps={{ shrink: true }} error={Boolean(errors.name)} helperText={errors.name?.message} {...register("name", { required: "กรุณากรอกชื่อวันหยุด" })} />
                </Grid>
                <Grid item xs={12} md={3}>
                  <Controller name="isActive" control={control} render={({ field }) => <FormControlLabel control={<Checkbox checked={field.value} onChange={(event) => field.onChange(event.target.checked)} />} label="เปิดใช้งาน" />} />
                </Grid>
              </Grid>
              <Stack direction="row" spacing={1.5}>
                <Button type="submit" variant="contained" disabled={saveMutation.isPending}>บันทึกข้อมูล</Button>
                {editing && <Button variant="outlined" onClick={() => { setEditing(null); reset(emptyHoliday); }}>ยกเลิก</Button>}
              </Stack>
            </Stack>
          </CardContent>
        </Card>

        <DataTableCard
          title="รายการวันหยุดราชการ"
          subtitle={`แสดงวันหยุดปี ${year + 543}${search ? ` ที่ค้นหา "${search}"` : ""}`}
        >
              <TableHead>
                <TableRow>
                  <TableCell>วันที่</TableCell>
                  <TableCell>ชื่อวันหยุด</TableCell>
                  <TableCell>สถานะ</TableCell>
                  <TableCell align="right">จัดการ</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {isLoading ? (
                  <TableRow><TableCell colSpan={4}>กำลังโหลดวันหยุด...</TableCell></TableRow>
                ) : data?.items.length ? data.items.map((item) => (
                  <TableRow key={item.id}>
                    <TableCell>{formatThaiDate(item.holidayDate)}</TableCell>
                    <TableCell>{item.name}</TableCell>
                    <TableCell><Chip size="small" label={item.isActive ? "ใช้งาน" : "ปิดใช้งาน"} color={item.isActive ? "success" : "default"} /></TableCell>
                    <TableCell align="right">
                      <ActionTooltip title="แก้ไขวันหยุดราชการ">
                        <IconButton aria-label="แก้ไขวันหยุดราชการ" onClick={() => onEdit(item)}><EditOutlinedIcon /></IconButton>
                      </ActionTooltip>
                      <ActionTooltip title="ปิดใช้งานวันหยุดราชการ">
                        <IconButton aria-label="ปิดใช้งานวันหยุดราชการ" disabled={!item.isActive || deleteMutation.isPending} onClick={() => deleteMutation.mutate(item.id)}><BlockOutlinedIcon /></IconButton>
                      </ActionTooltip>
                    </TableCell>
                  </TableRow>
                )) : (
                  <TableRow><TableCell colSpan={4}>ไม่พบวันหยุดราชการในปีนี้</TableCell></TableRow>
                )}
              </TableBody>
        </DataTableCard>
        <Card>
          <TablePagination
            component="div"
            count={data?.totalItems ?? 0}
            page={page}
            onPageChange={(_, nextPage) => setPage(nextPage)}
            rowsPerPage={pageSize}
            onRowsPerPageChange={(event) => {
              setPageSize(Number(event.target.value));
              setPage(0);
            }}
            rowsPerPageOptions={[10, 20, 50]}
            labelRowsPerPage="จำนวนรายการต่อหน้า"
            labelDisplayedRows={({ from, to, count }) =>
              `${from}-${to} จาก ${count !== -1 ? count : `มากกว่า ${to}`}`
            }
            getItemAriaLabel={(type) => {
              if (type === "first") return "ไปหน้าแรก";
              if (type === "last") return "ไปหน้าสุดท้าย";
              if (type === "next") return "ไปหน้าถัดไป";
              return "ไปหน้าก่อนหน้า";
            }}
          />
        </Card>
      </Stack>

      <Dialog open={importOpen} onClose={handleCloseImport} fullWidth maxWidth="md">
        <DialogTitle>Import วันหยุดราชการ</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            <Alert severity="info">
              รองรับไฟล์ .csv และ .xlsx โดยใช้คอลัมน์ วันที่, ชื่อวันหยุด, ประเภทวันหยุด วันที่ในไฟล์ใช้รูปแบบ YYYY-MM-DD เช่น 2027-01-01 และระบบจะแสดงเป็น 01/01/2027
            </Alert>
            {(previewMutation.isError || confirmMutation.isError) && (
              <Alert severity="error">Import ไม่สำเร็จ กรุณาตรวจสอบไฟล์และข้อมูลอีกครั้ง</Alert>
            )}
            <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
              <Button component="label" variant="outlined" startIcon={<UploadFileOutlinedIcon />}>
                Upload File
                <input hidden type="file" accept=".csv,.xlsx" onChange={(event) => { setImportFile(event.target.files?.[0] ?? null); setPreview(null); }} />
              </Button>
              <Button variant="outlined" startIcon={<DownloadOutlinedIcon />} onClick={handleDownloadTemplate}>
                ดาวน์โหลด Template
              </Button>
              <Button variant="contained" disabled={!importFile || previewMutation.isPending} onClick={() => importFile && previewMutation.mutate(importFile)}>
                Preview ก่อน Import
              </Button>
              <Button variant="contained" color="success" disabled={!preview || preview.invalidRows > 0 || validRows.length === 0 || confirmMutation.isPending} onClick={() => confirmMutation.mutate({ rows: validRows })}>
                Confirm Import
              </Button>
            </Stack>
            <Typography variant="body2" color="text.secondary">
              ไฟล์ที่เลือก: {importFile?.name ?? "ยังไม่ได้เลือกไฟล์"}
            </Typography>
            {preview && (
              <>
                <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                  <Chip label={`ทั้งหมด ${preview.totalRows} รายการ`} />
                  <Chip color="success" label={`ถูกต้อง ${preview.validRows} รายการ`} />
                  <Chip color={preview.invalidRows ? "error" : "default"} label={`ผิดพลาด ${preview.invalidRows} รายการ`} />
                </Stack>
                <DataTableCard>
                  <TableHead>
                    <TableRow>
                      <TableCell>แถว</TableCell>
                      <TableCell>วันที่</TableCell>
                      <TableCell>ชื่อวันหยุด</TableCell>
                      <TableCell>ประเภท</TableCell>
                      <TableCell>ผลตรวจสอบ</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {preview.rows.map((row) => (
                      <TableRow key={row.rowNumber}>
                        <TableCell>{row.rowNumber}</TableCell>
                        <TableCell>{formatThaiDate(row.holidayDate)}</TableCell>
                        <TableCell>{row.name || "-"}</TableCell>
                        <TableCell>{row.holidayType || "-"}</TableCell>
                        <TableCell>
                          {row.isValid ? (
                            <Chip size="small" color="success" label="พร้อม Import" />
                          ) : (
                            <Typography variant="body2" color="error">{row.errors.join(", ")}</Typography>
                          )}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </DataTableCard>
              </>
            )}
          </Stack>
        </DialogContent>
      </Dialog>
    </>
  );
}

function downloadBlob(blob: Blob, fileName: string) {
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.URL.revokeObjectURL(url);
}
