import ArrowBackOutlinedIcon from "@mui/icons-material/ArrowBackOutlined";
import CalendarMonthOutlinedIcon from "@mui/icons-material/CalendarMonthOutlined";
import CorporateFareOutlinedIcon from "@mui/icons-material/CorporateFareOutlined";
import HealthAndSafetyOutlinedIcon from "@mui/icons-material/HealthAndSafetyOutlined";
import HotelOutlinedIcon from "@mui/icons-material/HotelOutlined";
import OpenInNewOutlinedIcon from "@mui/icons-material/OpenInNewOutlined";
import PersonOutlineOutlinedIcon from "@mui/icons-material/PersonOutlineOutlined";
import ShieldOutlinedIcon from "@mui/icons-material/ShieldOutlined";
import type { SvgIconComponent } from "@mui/icons-material";
import { Alert, Box, Button, Card, CardContent, FormControl, Grid, LinearProgress, MenuItem, Select, Skeleton, Stack, Table, TableBody, TableCell, TableHead, TableRow, Typography } from "@mui/material";
import { alpha, useTheme } from "@mui/material/styles";
import { useQuery } from "@tanstack/react-query";
import dayjs from "dayjs";
import { useMemo, useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import { getMyLeaveBalances, type LeaveBalance } from "../api/leaveApi";
import { getMyProfile } from "../api/profileApi";
import { PageHeader } from "../components/PageHeader";
import { formatThaiDate } from "../utils/dateFormat";
import { getEmploymentTypeLabel } from "../utils/employmentLabels";

const coreLeaveDefinitions = [
  { code: "VACATION_LEAVE", title: "ลาพักผ่อน", icon: HotelOutlinedIcon, color: "#16A34A", emoji: "🛏️" },
  { code: "PERSONAL_LEAVE", title: "ลากิจ", icon: CorporateFareOutlinedIcon, color: "#F59E0B", emoji: "🏠" },
  { code: "SICK_LEAVE", title: "ลาป่วย", icon: HealthAndSafetyOutlinedIcon, color: "#0284C7", emoji: "🙂" },
];

const policySummaryRows = [
  { employmentType: "CIVIL_SERVANT", label: "ข้าราชการ", vacation: "10 หลังครบ 6 เดือน", personal: "45 / ปีแรก 15", sick: "60" },
  { employmentType: "PERMANENT_EMPLOYEE", label: "ลูกจ้างประจำ", vacation: "10 หลังครบ 6 เดือน", personal: "45 / ปีแรก 15", sick: "60" },
  { employmentType: "GOVERNMENT_EMPLOYEE", label: "พนักงานราชการ", vacation: "10 หลังครบ 6 เดือน", personal: "10 หลังครบ 1 ปี", sick: "30" },
  { employmentType: "MOPH_EMPLOYEE", label: "พกส.", vacation: "10 หลังครบ 6 เดือน", personal: "15 / ปีแรก 6", sick: "45" },
  { employmentType: "TEMPORARY_EMPLOYEE_MONTHLY", label: "ลูกจ้างชั่วคราวรายเดือน", vacation: "10 หลังครบ 6 เดือน", personal: "ไม่รับค่าจ้าง", sick: "15 / ปีแรก 8" },
  { employmentType: "TEMPORARY_EMPLOYEE_DAILY", label: "ลูกจ้างชั่วคราวรายวัน", vacation: "10 หลังครบ 6 เดือน", personal: "ไม่รับค่าจ้าง", sick: "15 / ปีแรก 8" },
];

export function LeaveBalancePage() {
  const theme = useTheme();
  const { data = [], isLoading } = useQuery({ queryKey: ["leave-balances", "me"], queryFn: getMyLeaveBalances });
  const { data: profile, isLoading: isProfileLoading } = useQuery({ queryKey: ["me", "profile"], queryFn: getMyProfile });
  const fiscalYears = useMemo(() => [...new Set(data.map((item) => item.year))].sort((a, b) => b - a), [data]);
  const [selectedYear, setSelectedYear] = useState<number | "">("");
  const activeYear = selectedYear || fiscalYears[0] || getCurrentFiscalYear();
  const balances = useMemo(() => data.filter((item) => item.year === activeYear), [activeYear, data]);
  const byCode = useMemo(() => new Map(balances.map((item) => [normalizeLeaveCode(item), item])), [balances]);
  const entitlementWarnings = useMemo(
    () => balances
      .filter((item) => !item.id && item.notes)
      .map((item) => `${item.leaveTypeName}: ${item.notes}`),
    [balances]);
  const employmentStartDate = profile?.employmentStartDate;
  const serviceDuration = calculateServiceDuration(employmentStartDate);
  const currentServiceBand = getServiceBand(serviceDuration.totalMonths);
  const currentEmploymentType = profile?.employmentType ?? "";

  return (
    <>
      <PageHeader title="แดชบอร์ดวันลาคงเหลือ" subtitle="ข้อมูลสิทธิ์การลาและสถานะวันลาคงเหลือของคุณ" />

      <Stack spacing={3}>
        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: { xs: "1fr", md: "minmax(220px, 1fr) minmax(300px, 380px)" },
            alignItems: "end",
            gap: 2,
          }}
        >
          <Button
            component={RouterLink}
            to="/dashboard"
            variant="outlined"
            startIcon={<ArrowBackOutlinedIcon />}
            sx={{
              justifySelf: { xs: "stretch", md: "start" },
              minHeight: 48,
              borderRadius: 2.5,
              px: 2.5,
            }}
          >
            กลับไป Dashboard Hub
          </Button>

          <FormControl sx={{ width: "100%" }}>
            <Typography variant="caption" color="text.secondary" sx={{ mb: 0.5 }}>ปีงบประมาณ</Typography>
            <Select
              size="small"
              value={activeYear}
              onChange={(event) => setSelectedYear(Number(event.target.value))}
              sx={{
                bgcolor: "background.paper",
                borderRadius: 2.5,
                minHeight: 48,
                "& .MuiSelect-select": { py: 1.35 },
              }}
            >
              {(fiscalYears.length ? fiscalYears : [activeYear]).map((year) => (
                <MenuItem key={year} value={year}>{formatFiscalYear(year)}</MenuItem>
              ))}
            </Select>
          </FormControl>
        </Box>

        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: {
              xs: "1fr",
              sm: "repeat(2, minmax(0, 1fr))",
              lg: "repeat(4, minmax(0, 1fr))",
            },
            gap: 2.5,
            width: "100%",
            maxWidth: "100%",
            boxSizing: "border-box",
            px: { xs: 0, md: 1.25 },
          }}
        >
          <Box sx={{ minWidth: 0, display: "flex" }}>
            <ProfileInfoCard
              icon={CalendarMonthOutlinedIcon}
              title="อายุการทำงาน"
              value={isProfileLoading ? undefined : serviceDuration.label}
              subtitle={employmentStartDate ? `นับจากวันเริ่มงาน ${formatThaiDate(employmentStartDate)}` : "ยังไม่ได้ระบุวันเริ่มงาน"}
              isLoading={isProfileLoading}
            />
          </Box>
          <Box sx={{ minWidth: 0, display: "flex" }}>
            <ProfileInfoCard
              icon={PersonOutlineOutlinedIcon}
              title="ประเภทการจ้าง"
              value={getEmploymentTypeLabel(profile?.employmentType)}
              subtitle={profile?.position ? `ตำแหน่ง ${profile.position}` : "ข้อมูลจากประวัติผู้ใช้งาน"}
              isLoading={isProfileLoading}
            />
          </Box>
          <Box sx={{ minWidth: 0, display: "flex" }}>
            <ProfileInfoCard
              icon={ShieldOutlinedIcon}
              title="กลุ่มสิทธิ์การลา"
              value={getEmploymentTypeLabel(profile?.employmentType)}
              subtitle="อ้างอิงจากเงื่อนไขสิทธิ์การลา"
              isLoading={isProfileLoading}
            />
          </Box>
          <Box sx={{ minWidth: 0, display: "flex" }}>
            <ProfileInfoCard
              icon={CorporateFareOutlinedIcon}
              title="หน่วยงาน"
              value={profile?.departmentName ?? "-"}
              subtitle="โรงพยาบาลนาหมื่น"
              isLoading={isProfileLoading}
            />
          </Box>
        </Box>

        {entitlementWarnings.length > 0 && (
          <Alert severity="warning" sx={{ borderRadius: 2.5 }}>
            ระบบยังไม่ได้ตั้งต้นยอดวันลาจริงบางประเภท จึงแสดงค่าคำนวณจาก policy ปัจจุบัน: {entitlementWarnings.join(" / ")}
          </Alert>
        )}

        <Card
          sx={{
            border: `1px solid ${alpha(theme.palette.primary.main, 0.5)}`,
            borderRadius: 3,
            boxShadow: `0 18px 46px ${alpha(theme.palette.primary.dark, 0.08)}`,
            overflow: "hidden",
          }}
        >
          <CardContent sx={{ p: { xs: 2, md: 2.5 } }}>
            <Stack direction={{ xs: "column", md: "row" }} justifyContent="space-between" spacing={2} sx={{ mb: 2 }}>
              <Box>
                <Stack direction="row" alignItems="center" spacing={1}>
                  <Box sx={{ width: 10, height: 18, borderRadius: 99, bgcolor: alpha(theme.palette.primary.main, 0.16) }} />
                  <Typography variant="h6" fontWeight={900}>สรุปวันลาคงเหลือ แยกตามประเภทลา</Typography>
                </Stack>
                <Typography variant="body2" color="text.secondary">ปีงบประมาณ {formatFiscalYear(activeYear)}</Typography>
              </Box>
              <Button component={RouterLink} to="/leave" variant="outlined" startIcon={<OpenInNewOutlinedIcon />} sx={{ borderRadius: 2, alignSelf: { xs: "stretch", md: "flex-start" } }}>
                ดูรายละเอียดทั้งหมด
              </Button>
            </Stack>

            <Grid container spacing={2}>
              {coreLeaveDefinitions.map((definition) => (
                <Grid item xs={12} md={4} key={definition.code}>
                  <BalanceSummaryCard
                    definition={definition}
                    balance={byCode.get(definition.code)}
                    isLoading={isLoading}
                  />
                </Grid>
              ))}
            </Grid>

            <Typography variant="caption" color="text.secondary" sx={{ display: "block", textAlign: "center", mt: 2 }}>
              หมายเหตุ: จำนวนวันอาจมีการเปลี่ยนแปลงตามผลการอนุมัติคำขอลาและคำขอยกเลิกใบลา
            </Typography>
          </CardContent>
        </Card>

        <Card
          sx={{
            border: `1px solid ${alpha(theme.palette.primary.main, 0.42)}`,
            borderRadius: 3,
            boxShadow: `0 18px 46px ${alpha(theme.palette.primary.dark, 0.06)}`,
          }}
        >
          <CardContent sx={{ p: { xs: 2, md: 2.5 } }}>
            <Stack direction="row" alignItems="center" spacing={1} sx={{ mb: 2 }}>
              <Box sx={{ width: 10, height: 18, borderRadius: 99, bgcolor: alpha(theme.palette.primary.main, 0.16) }} />
              <Typography variant="h6" fontWeight={900}>รายละเอียดสิทธิ์การลา</Typography>
            </Stack>

            <Grid container spacing={2.5}>
              <Grid item xs={12} md={5}>
                <Box sx={{ border: `1px solid ${alpha(theme.palette.divider, 0.9)}`, borderRadius: 2.5, p: 2, height: "100%" }}>
                  <Typography fontWeight={900} color="primary" sx={{ mb: 1 }}>เงื่อนไขการคำนวณสิทธิ์</Typography>
                  <InfoLine label="ประเภทการจ้างงาน" value={getEmploymentTypeLabel(profile?.employmentType)} />
                  <InfoLine label="วันที่เริ่มงาน" value={formatThaiDate(employmentStartDate)} />
                  <InfoLine label="วันที่คำนวณสิทธิ์" value={formatThaiDate(new Date())} />
                  <InfoLine label="อายุงาน ณ ปัจจุบัน" value={serviceDuration.label} />
                  <InfoLine label="ปีงบประมาณ" value={formatFiscalYear(activeYear)} />
                  <InfoLine label="กลุ่มอายุงาน" value={currentServiceBand} strong />
                  <Typography variant="caption" color="text.secondary" sx={{ display: "block", mt: 2 }}>
                    ระบบแสดงผลตามข้อมูลสิทธิ์ที่บันทึกในระบบวันลาคงเหลือ หากสิทธิ์ไม่ตรง กรุณาติดต่อ HR หรือผู้ดูแลระบบ
                  </Typography>
                </Box>
              </Grid>

              <Grid item xs={12} md={7}>
                <Box sx={{ border: `1px solid ${alpha(theme.palette.divider, 0.9)}`, borderRadius: 2.5, overflow: "hidden", height: "100%" }}>
                  <Box sx={{ px: 2, py: 1.5, bgcolor: alpha(theme.palette.primary.main, 0.04), borderBottom: `1px solid ${theme.palette.divider}` }}>
                    <Typography fontWeight={900} color="primary">ตารางสิทธิ์การลาตามประเภทบุคลากร</Typography>
                  </Box>
                  <Table size="small">
                    <TableHead>
                      <TableRow>
                        <TableCell>ประเภทบุคลากร</TableCell>
                        <TableCell align="center">ลาพักผ่อน (วัน/ปี)</TableCell>
                        <TableCell align="center">ลากิจ (วัน/ปี)</TableCell>
                        <TableCell align="center">ลาป่วย (วัน/ปี)</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {policySummaryRows.map((row) => (
                        <TableRow
                          key={row.employmentType}
                          sx={{
                            bgcolor: row.employmentType === currentEmploymentType ? alpha(theme.palette.success.main, 0.12) : "transparent",
                          }}
                        >
                          <TableCell>{row.label}</TableCell>
                          <TableCell align="center">{row.vacation}</TableCell>
                          <TableCell align="center">{row.personal}</TableCell>
                          <TableCell align="center">{row.sick}</TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                  <Typography variant="caption" color="text.secondary" sx={{ display: "block", px: 2, py: 1.5 }}>
                    อ้างอิง: policy เริ่มต้นในระบบ เงื่อนไข 6 เดือนใช้เฉพาะลาพักผ่อน และค่าใช้งานจริงอาจเปลี่ยนตาม policy เฉพาะปีงบประมาณ
                  </Typography>
                </Box>
              </Grid>
            </Grid>
          </CardContent>
        </Card>
      </Stack>
    </>
  );
}

function ProfileInfoCard({ icon: Icon, title, value, subtitle, isLoading }: { icon: SvgIconComponent; title: string; value?: string; subtitle: string; isLoading: boolean }) {
  const theme = useTheme();
  return (
    <Card
      sx={{
        width: "100%",
        minHeight: 152,
        height: "100%",
        borderRadius: 3,
        border: `1px solid ${alpha(theme.palette.divider, 0.9)}`,
        boxShadow: `0 12px 28px ${alpha(theme.palette.primary.dark, 0.05)}`,
      }}
    >
      <CardContent sx={{ height: "100%", p: { xs: 2, md: 2.5 }, "&:last-child": { pb: { xs: 2, md: 2.5 } } }}>
        <Stack direction="row" spacing={2} alignItems="flex-start" sx={{ height: "100%" }}>
          <Box sx={{ width: 54, height: 54, borderRadius: "50%", bgcolor: alpha(theme.palette.primary.main, 0.08), color: "primary.main", display: "grid", placeItems: "center", flex: "0 0 auto" }}>
            <Icon />
          </Box>
          <Box sx={{ minWidth: 0, display: "flex", flexDirection: "column", gap: 0.6 }}>
            <Typography variant="body2" color="text.secondary" fontWeight={800} sx={{ lineHeight: 1.25 }}>{title}</Typography>
            {isLoading ? (
              <Skeleton width="78%" height={32} />
            ) : (
              <Typography
                fontWeight={900}
                sx={{
                  fontSize: { xs: "1.1rem", md: "1.18rem" },
                  lineHeight: 1.28,
                  overflowWrap: "anywhere",
                }}
              >
                {value || "-"}
              </Typography>
            )}
            <Typography variant="caption" color="text.secondary" sx={{ lineHeight: 1.55 }}>{subtitle}</Typography>
          </Box>
        </Stack>
      </CardContent>
    </Card>
  );
}

function BalanceSummaryCard({ definition, balance, isLoading }: { definition: (typeof coreLeaveDefinitions)[number]; balance?: LeaveBalance; isLoading: boolean }) {
  const theme = useTheme();
  const total = Math.max((balance?.entitledDays ?? 0) + (balance?.carriedOverDays ?? 0) + (balance?.adjustedDays ?? 0), 0);
  const available = balance?.availableDays ?? 0;
  const percent = total > 0 ? Math.max(0, Math.min(100, (available / total) * 100)) : 0;

  return (
    <Box
      sx={{
        height: "100%",
        border: `1px solid ${alpha(definition.color, 0.22)}`,
        borderRadius: 2.5,
        p: 2,
        background: `linear-gradient(135deg, ${alpha(definition.color, 0.08)} 0%, ${theme.palette.background.paper} 46%)`,
      }}
    >
      {isLoading ? (
        <Skeleton variant="rounded" height={210} />
      ) : (
        <Stack spacing={1.5} sx={{ height: "100%" }}>
          <Stack direction="row" justifyContent="space-between" alignItems="flex-start" spacing={1}>
            <Stack direction="row" spacing={1.25} alignItems="center" sx={{ minWidth: 0 }}>
              <Box sx={{ width: 42, height: 42, borderRadius: "50%", bgcolor: alpha(definition.color, 0.12), display: "grid", placeItems: "center", color: definition.color }}>
                <definition.icon />
              </Box>
              <Box sx={{ minWidth: 0 }}>
                <Typography fontWeight={900}>{definition.emoji} {definition.title}</Typography>
              </Box>
            </Stack>
            <Box sx={{ textAlign: "right", flex: "0 0 auto" }}>
              <Typography sx={{ color: definition.color, fontWeight: 950, fontSize: { xs: "2rem", md: "2.25rem" }, lineHeight: 1 }}>
                {formatDays(available)}
              </Typography>
              <Typography variant="caption" fontWeight={800}>วัน<br />คงเหลือ</Typography>
            </Box>
          </Stack>

          <Stack spacing={0.75}>
            <BalanceLine label="สิทธิ์ประจำปี" value={balance?.entitledDays ?? 0} />
            <BalanceLine label="ยกมาจากปีก่อน" value={balance?.carriedOverDays ?? 0} />
            <BalanceLine label="ใช้ไปแล้ว" value={balance?.usedDays ?? 0} />
            <BalanceLine label="รออนุมัติ" value={balance?.pendingDays ?? 0} />
            <BalanceLine label="คงเหลือ" value={available} strong />
          </Stack>

          <Box sx={{ mt: "auto" }}>
            <LinearProgress
              variant="determinate"
              value={percent}
              sx={{
                height: 8,
                borderRadius: 99,
                bgcolor: alpha(definition.color, 0.14),
                "& .MuiLinearProgress-bar": { bgcolor: definition.color, borderRadius: 99 },
              }}
            />
            <Typography variant="caption" fontWeight={800} sx={{ display: "block", textAlign: "right", mt: 0.5 }}>
              {percent.toLocaleString("th-TH", { maximumFractionDigits: 1 })}%
            </Typography>
          </Box>
        </Stack>
      )}
    </Box>
  );
}

function BalanceLine({ label, value, strong }: { label: string; value: number; strong?: boolean }) {
  return (
    <Stack direction="row" justifyContent="space-between" spacing={1}>
      <Typography variant="body2" color="text.secondary">{label}</Typography>
      <Typography variant="body2" fontWeight={strong ? 900 : 700}>{formatDays(value)} วัน</Typography>
    </Stack>
  );
}

function InfoLine({ label, value, strong }: { label: string; value?: string; strong?: boolean }) {
  return (
    <Stack direction="row" justifyContent="space-between" spacing={2} sx={{ py: 0.6 }}>
      <Typography variant="body2" color="text.secondary">{label}</Typography>
      <Typography variant="body2" fontWeight={strong ? 900 : 700} textAlign="right">{value || "-"}</Typography>
    </Stack>
  );
}

function normalizeLeaveCode(balance: LeaveBalance) {
  const name = `${balance.leaveTypeName ?? ""}`.toUpperCase();
  if (name.includes("VACATION") || name.includes("พักผ่อน")) return "VACATION_LEAVE";
  if (name.includes("PERSONAL") || name.includes("กิจ")) return "PERSONAL_LEAVE";
  if (name.includes("SICK") || name.includes("ป่วย")) return "SICK_LEAVE";
  return name;
}

function formatFiscalYear(year: number) {
  const thaiYear = year + 543;
  return `${thaiYear} (1 ต.ค. ${thaiYear - 1} - 30 ก.ย. ${thaiYear})`;
}

function getCurrentFiscalYear() {
  const today = dayjs();
  return today.month() >= 9 ? today.year() + 1 : today.year();
}

function calculateServiceDuration(startDate?: string | null) {
  if (!startDate || !dayjs(startDate).isValid()) {
    return { label: "-", totalMonths: 0 };
  }

  const start = dayjs(startDate).startOf("day");
  const today = dayjs().startOf("day");
  const totalDays = Math.max(0, today.diff(start, "day"));
  const years = Math.floor(totalDays / 365);
  const months = Math.floor((totalDays % 365) / 30);
  const days = totalDays - years * 365 - months * 30;
  return {
    label: `${years} ปี ${months} เดือน ${days} วัน`,
    totalMonths: years * 12 + months,
  };
}

function getServiceBand(totalMonths: number) {
  if (totalMonths < 12) return "น้อยกว่า 1 ปี";
  if (totalMonths < 60) return "1 ปี - น้อยกว่า 5 ปี";
  if (totalMonths < 120) return "5 ปี - น้อยกว่า 10 ปี";
  return "10 ปีขึ้นไป";
}

function formatDays(value: number) {
  return value.toLocaleString("th-TH", { maximumFractionDigits: 1 });
}
