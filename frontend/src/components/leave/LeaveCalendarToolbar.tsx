import { Card, CardContent, Grid, MenuItem, TextField } from "@mui/material";
import type { DepartmentSummary } from "../../api/adminApi";
import type { LeaveType } from "../../api/leaveApi";
import { getLeaveTypeLabel } from "../../utils/leaveLabels";

type LeaveCalendarToolbarProps = {
  month: number;
  year: number;
  departmentId: string;
  leaveTypeId: string;
  status: string;
  years: number[];
  months: string[];
  departments: DepartmentSummary[];
  leaveTypes: LeaveType[];
  onMonthChange: (value: number) => void;
  onYearChange: (value: number) => void;
  onDepartmentChange: (value: string) => void;
  onLeaveTypeChange: (value: string) => void;
  onStatusChange: (value: string) => void;
};

export function LeaveCalendarToolbar({
  month,
  year,
  departmentId,
  leaveTypeId,
  status,
  years,
  months,
  departments,
  leaveTypes,
  onMonthChange,
  onYearChange,
  onDepartmentChange,
  onLeaveTypeChange,
  onStatusChange,
}: LeaveCalendarToolbarProps) {
  return (
    <Card sx={{ mb: 2 }}>
      <CardContent sx={{ py: 2 }}>
        <Grid container spacing={1.5} alignItems="center">
          <Grid item xs={12} sm={6} md={2}>
            <TextField select fullWidth size="small" label="เดือน" value={month} onChange={(event) => onMonthChange(Number(event.target.value))}>
              {months.map((item, index) => (
                <MenuItem key={index + 1} value={index + 1}>
                  {item}
                </MenuItem>
              ))}
            </TextField>
          </Grid>
          <Grid item xs={12} sm={6} md={2}>
            <TextField select fullWidth size="small" label="ปี" value={year} onChange={(event) => onYearChange(Number(event.target.value))}>
              {years.map((item) => (
                <MenuItem key={item} value={item}>
                  {item + 543}
                </MenuItem>
              ))}
            </TextField>
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <TextField select fullWidth size="small" label="หน่วยงาน" value={departmentId} onChange={(event) => onDepartmentChange(event.target.value)}>
              <MenuItem value="">ทุกหน่วยงาน</MenuItem>
              {departments.map((item) => (
                <MenuItem key={item.id} value={item.id}>
                  {item.name}
                </MenuItem>
              ))}
            </TextField>
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <TextField select fullWidth size="small" label="ประเภทการลา" value={leaveTypeId} onChange={(event) => onLeaveTypeChange(event.target.value)}>
              <MenuItem value="">ทุกประเภทลา</MenuItem>
              {leaveTypes.map((item) => (
                <MenuItem key={item.id} value={item.id}>
                  {getLeaveTypeLabel(item.name || item.code)}
                </MenuItem>
              ))}
            </TextField>
          </Grid>
          <Grid item xs={12} sm={6} md={2}>
            <TextField select fullWidth size="small" label="สถานะ" value={status} onChange={(event) => onStatusChange(event.target.value)}>
              <MenuItem value="">ทุกสถานะ</MenuItem>
              <MenuItem value="Pending">รออนุมัติ</MenuItem>
              <MenuItem value="Approved">อนุมัติแล้ว</MenuItem>
              <MenuItem value="Rejected">ไม่อนุมัติ</MenuItem>
              <MenuItem value="Cancelled">ยกเลิก</MenuItem>
            </TextField>
          </Grid>
        </Grid>
      </CardContent>
    </Card>
  );
}
