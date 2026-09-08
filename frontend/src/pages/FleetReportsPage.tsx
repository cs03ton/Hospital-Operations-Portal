import AccessTimeOutlinedIcon from "@mui/icons-material/AccessTimeOutlined";
import AltRouteOutlinedIcon from "@mui/icons-material/AltRouteOutlined";
import CalendarMonthOutlinedIcon from "@mui/icons-material/CalendarMonthOutlined";
import DownloadOutlinedIcon from "@mui/icons-material/DownloadOutlined";
import FactCheckOutlinedIcon from "@mui/icons-material/FactCheckOutlined";
import LocalShippingOutlinedIcon from "@mui/icons-material/LocalShippingOutlined";
import PendingActionsOutlinedIcon from "@mui/icons-material/PendingActionsOutlined";
import PeopleAltOutlinedIcon from "@mui/icons-material/PeopleAltOutlined";
import PersonOutlineOutlinedIcon from "@mui/icons-material/PersonOutlineOutlined";
import RefreshOutlinedIcon from "@mui/icons-material/RefreshOutlined";
import SearchOutlinedIcon from "@mui/icons-material/SearchOutlined";
import StarRoundedIcon from "@mui/icons-material/StarRounded";
import ReportProblemOutlinedIcon from "@mui/icons-material/ReportProblemOutlined";
import TrendingUpOutlinedIcon from "@mui/icons-material/TrendingUpOutlined";
import { Box, Button, Card, CardContent, Chip, Grid, InputAdornment, LinearProgress, MenuItem, Pagination, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, TextField, Typography } from "@mui/material";
import { alpha } from "@mui/material/styles";
import { useQuery } from "@tanstack/react-query";
import dayjs from "dayjs";
import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { exportFleetReport, getFleetCalendar, getFleetDepartmentReport, getFleetDriverReport, getFleetFeedbackAttention, getFleetFeedbackDriverReport, getFleetFeedbackDriverTrend, getFleetFeedbackManagementSummary, getFleetFeedbackTripReport, getFleetFeedbackVehicleReport, getFleetKpis, getFleetRouteReport, type FleetCalendarEvent, type FleetDriverReport } from "../api/fleetApi";
import { AppDatePicker } from "../components/common/AppDatePicker";
import { EmptyState } from "../components/common/EmptyState";
import { LoadingState } from "../components/common/LoadingState";
import { PageHeader } from "../components/PageHeader";
import { formatThaiDateTime } from "../utils/dateFormat";
import { getFleetStatusLabel } from "../utils/fleetLabels";
import { usePermission } from "../context/PermissionContext";

const pageSize = 10;

export function FleetReportsPage() {
  const { hasPermission } = usePermission();
  const [startDate, setStartDate] = useState(dayjs().startOf("month").format("YYYY-MM-DD"));
  const [endDate, setEndDate] = useState(dayjs().endOf("month").format("YYYY-MM-DD"));
  const [status, setStatus] = useState("");
  const [keyword, setKeyword] = useState("");
  const [page, setPage] = useState(1);
  const params = useMemo(() => {
    const value = new URLSearchParams({ startDate, endDate });
    if (status) value.set("status", status);
    return value;
  }, [endDate, startDate, status]);
  const calendarParams = useMemo(() => {
    const value = new URLSearchParams({ start: startDate, end: endDate });
    value.set("eventTypes", "REQUEST");
    if (status) value.set("statuses", status);
    return value;
  }, [endDate, startDate, status]);
  const kpis = useQuery({ queryKey: ["fleet-report-kpis", params.toString()], queryFn: () => getFleetKpis(params) });
  const departments = useQuery({ queryKey: ["fleet-report-departments", params.toString()], queryFn: () => getFleetDepartmentReport(params) });
  const drivers = useQuery({ queryKey: ["fleet-report-drivers", params.toString()], queryFn: () => getFleetDriverReport(params) });
  const routes = useQuery({ queryKey: ["fleet-report-routes", params.toString()], queryFn: () => getFleetRouteReport(params) });
  const requests = useQuery({ queryKey: ["fleet-report-requests", calendarParams.toString()], queryFn: () => getFleetCalendar(calendarParams) });
  const requestRows = useMemo(() => (requests.data ?? []).filter((event) => {
    const text = `${event.title} ${event.metadata?.requestNo ?? ""} ${event.metadata?.department ?? ""} ${event.metadata?.destination ?? ""}`.toLowerCase();
    return !keyword.trim() || text.includes(keyword.trim().toLowerCase());
  }), [keyword, requests.data]);
  const visibleRows = requestRows.slice((page - 1) * pageSize, page * pageSize);
  const statusCounts = useMemo(() => (requests.data ?? []).reduce<Record<string, number>>((result, event) => { result[event.status] = (result[event.status] ?? 0) + 1; return result; }, {}), [requests.data]);
  const reportSummary = useMemo(() => {
    const rows = requests.data ?? [];
    const passengers = rows.reduce((sum, row) => sum + Number(row.metadata?.passengerCount ?? 0), 0);
    const hours = rows.reduce((sum, row) => sum + Math.max(0, dayjs(row.endAt).diff(dayjs(row.startAt), "minute")) / 60, 0);
    const active = rows.filter(row => !["DRAFT", "REJECTED", "CANCELLED", "ABORTED"].includes(row.status)).length;
    const daily = Array.from({ length: dayjs(endDate).diff(dayjs(startDate), "day") + 1 }, (_, index) => {
      const date = dayjs(startDate).add(index, "day");
      return { label: date.format("D"), value: rows.filter(row => dayjs(row.startAt).isSame(date, "day")).length };
    });
    return { passengers, hours, utilization: rows.length ? Math.round(active / rows.length * 100) : 0, daily };
  }, [endDate, requests.data, startDate]);
  const isLoading = kpis.isLoading || departments.isLoading || drivers.isLoading || routes.isLoading || requests.isLoading;
  const hasError = kpis.isError || departments.isError || drivers.isError || routes.isError || requests.isError;

  const resetFilters = () => { setStartDate(dayjs().startOf("month").format("YYYY-MM-DD")); setEndDate(dayjs().endOf("month").format("YYYY-MM-DD")); setStatus(""); setKeyword(""); setPage(1); };
  const download = async () => {
    const blob = await exportFleetReport(new URLSearchParams({ ...Object.fromEntries(params), reportType: "summary" }));
    const url = URL.createObjectURL(blob); const anchor = document.createElement("a"); anchor.href = url; anchor.download = `รายงานการใช้รถ-${startDate}-${endDate}.csv`; anchor.click(); URL.revokeObjectURL(url);
  };

  return <Stack spacing={2.25}>
    <PageHeader title="รายงานการใช้รถ" subtitle="สรุปภาพรวม วิเคราะห์การใช้งาน และค้นหารายการคำขอรถ" />
    <Card><CardContent><Grid container spacing={1.5} alignItems="center">
      <Grid item xs={12} sm={6} md={3}><AppDatePicker label="วันที่เริ่มต้น" value={startDate} onChange={(value) => { setStartDate(value); setPage(1); }} /></Grid>
      <Grid item xs={12} sm={6} md={3}><AppDatePicker label="วันที่สิ้นสุด" value={endDate} onChange={(value) => { setEndDate(value); setPage(1); }} /></Grid>
      <Grid item xs={12} sm={6} md={2}><TextField select fullWidth size="small" label="สถานะคำขอ" value={status} onChange={(event) => { setStatus(event.target.value); setPage(1); }}><MenuItem value="">ทุกสถานะ</MenuItem>{Object.keys(statusCounts).map((value) => <MenuItem key={value} value={value}>{getFleetStatusLabel(value)}</MenuItem>)}</TextField></Grid>
      <Grid item xs={12} sm={6} md={4}><Stack direction="row" spacing={1}><Button fullWidth variant="outlined" startIcon={<RefreshOutlinedIcon />} onClick={resetFilters}>ล้างตัวกรอง</Button><Button fullWidth variant="contained" startIcon={<DownloadOutlinedIcon />} onClick={download}>ส่งออก CSV</Button></Stack></Grid>
    </Grid></CardContent></Card>

    {isLoading && <LoadingState message="กำลังจัดทำรายงานการใช้รถ..." />}
    {hasError && <Card><CardContent><Typography color="error">ไม่สามารถโหลดรายงานได้ กรุณาลองใหม่อีกครั้ง</Typography></CardContent></Card>}
    {!isLoading && !hasError && <>
      <Grid container spacing={1.5} sx={{ width: "calc(100% + 12px)", ml: "-12px" }}>
        <KpiCard icon={<CalendarMonthOutlinedIcon />} label="จำนวนเที่ยวทั้งหมด" value={`${kpis.data?.totalRequests ?? 0} เที่ยว`} color="#1E8E4A" />
        <KpiCard icon={<AltRouteOutlinedIcon />} label="ระยะทางรวม" value="ยังไม่มีข้อมูล กม." color="#287BC1" compact />
        <KpiCard icon={<AccessTimeOutlinedIcon />} label="ชั่วโมงการใช้งานรวม" value={`${reportSummary.hours.toLocaleString("th-TH", { maximumFractionDigits: 1 })} ชม.`} color="#7651C9" />
        <KpiCard icon={<PeopleAltOutlinedIcon />} label="ผู้ใช้บริการรวม" value={`${reportSummary.passengers.toLocaleString("th-TH")} คน`} color="#D88923" />
        <KpiCard icon={<LocalShippingOutlinedIcon />} label="อัตราการใช้งานรถ" value={`${reportSummary.utilization}%`} color="#238B8B" />
      </Grid>
      <Grid container spacing={2} sx={{ width: "calc(100% + 16px)", ml: "-16px" }}>
        <Grid item xs={12} lg={8.5}><Grid container spacing={2}>
          <Grid item xs={12} md={7}><Card sx={{ height: "100%" }}><CardContent><SectionTitle title="การใช้งานรถตามวัน" subtitle="จำนวนเที่ยวในช่วงวันที่เลือก" /><DailyTrend rows={reportSummary.daily} /></CardContent></Card></Grid>
          <Grid item xs={12} md={5}><Card sx={{ height: "100%" }}><CardContent><SectionTitle title="การใช้งานตามสถานะ" subtitle="สัดส่วนคำขอใช้รถ" /><StatusDonut counts={statusCounts} /></CardContent></Card></Grid>
          <Grid item xs={12}><Card><CardContent><SectionTitle title="การใช้งานรถตามหน่วยงาน (Top 5)" subtitle="จำนวนเที่ยวแยกตามหน่วยงาน" />{(departments.data ?? []).length ? <BarList rows={(departments.data ?? []).sort((a, b) => b.requestCount - a.requestCount).slice(0, 5).map((item) => ({ label: item.department || "ไม่ระบุหน่วยงาน", value: item.requestCount }))} /> : <Typography color="text.secondary">ไม่มีข้อมูลในช่วงเวลานี้</Typography>}</CardContent></Card></Grid>
        </Grid></Grid>
        <Grid item xs={12} lg={3.5}><Stack spacing={2}>
          <Card><CardContent><SectionTitle title="สรุปสถานะการจอง" subtitle="รายการตามสถานะ" /><StatusSummary counts={statusCounts} /></CardContent></Card>
          <Card><CardContent><SectionTitle title="จุดหมายที่ใช้บริการบ่อย" subtitle="5 อันดับแรก" /><Stack spacing={1.25}>{(routes.data ?? []).sort((a, b) => b.requestCount - a.requestCount).slice(0, 5).map((item, index) => <Stack key={item.destination} direction="row" spacing={1.25} alignItems="center"><Chip size="small" label={index + 1} color={index === 0 ? "primary" : "default"} /><Box flex={1}><Typography variant="body2" fontWeight={700}>{item.destination || "ไม่ระบุจุดหมาย"}</Typography><Typography variant="caption" color="text.secondary">{item.requestCount} เที่ยว · เสร็จ {item.completedTripCount}</Typography></Box></Stack>)}</Stack></CardContent></Card>
          <Card><CardContent><SectionTitle title="สรุประยะทางตามช่วง" subtitle="ข้อมูลเลขไมล์จากการปิดงาน" /><Typography variant="body2" color="text.secondary">ระยะทางรวม</Typography><Typography variant="h5" fontWeight={900}>ยังไม่มีข้อมูล</Typography></CardContent></Card>
        </Stack></Grid>
      </Grid>
      <Card><CardContent><Stack direction={{ xs: "column", md: "row" }} justifyContent="space-between" spacing={1.5} sx={{ mb: 2 }}><SectionTitle title="รายละเอียดคำขอใช้รถ" subtitle={`พบ ${requestRows.length.toLocaleString("th-TH")} รายการ`} /><TextField size="small" placeholder="ค้นหาเลขคำขอ หน่วยงาน หรือปลายทาง" value={keyword} onChange={(event) => { setKeyword(event.target.value); setPage(1); }} sx={{ minWidth: { md: 360 } }} InputProps={{ startAdornment: <InputAdornment position="start"><SearchOutlinedIcon /></InputAdornment> }} /></Stack>
        {!requestRows.length ? <EmptyState title="ไม่พบข้อมูลการใช้รถ" description="ลองเปลี่ยนช่วงวันที่หรือสถานะคำขอ" /> : <><TableContainer><Table size="small"><TableHead><TableRow><TableCell>เลขคำขอ</TableCell><TableCell>วันเวลาเดินทาง</TableCell><TableCell>หน่วยงาน</TableCell><TableCell>จุดหมาย</TableCell><TableCell align="center">ผู้ร่วมเดินทาง</TableCell><TableCell>สถานะ</TableCell><TableCell align="right">รายละเอียด</TableCell></TableRow></TableHead><TableBody>{visibleRows.map((event) => <ReportRow key={event.id} event={event} />)}</TableBody></Table></TableContainer><Stack direction={{ xs: "column", sm: "row" }} justifyContent="space-between" alignItems="center" spacing={1.5} sx={{ mt: 2 }}><Typography variant="body2" color="text.secondary">แสดง {(page - 1) * pageSize + 1}–{Math.min(page * pageSize, requestRows.length)} จาก {requestRows.length} รายการ</Typography><Pagination page={page} count={Math.max(1, Math.ceil(requestRows.length / pageSize))} onChange={(_, value) => setPage(value)} color="primary" /></Stack></>}
      </CardContent></Card>
      {(drivers.data ?? []).length > 0 && <Card><CardContent><SectionTitle title="สรุปงานพนักงานขับรถ" subtitle="เปรียบเทียบจำนวนงาน ความคืบหน้า และผลงานที่เสร็จสิ้นของแต่ละคน" /><Grid container spacing={1.75} sx={{ mt: 0.25 }}>{(drivers.data ?? []).map((item, index) => <Grid item xs={12} sm={6} lg={4} key={item.driverUserId}><DriverStatisticCard item={item} index={index} /></Grid>)}</Grid></CardContent></Card>}
      {hasPermission("FleetFeedback.ViewManagement") && <FeedbackManagementReport startDate={startDate} endDate={endDate} />}
    </>}
  </Stack>;
}

function FeedbackManagementReport({ startDate, endDate }: { startDate:string; endDate:string }) {
  const [page,setPage]=useState(1); const [requestNo,setRequestNo]=useState(""); const [incident,setIncident]=useState(""); const [trendDriverId,setTrendDriverId]=useState("");
  const params=useMemo(()=>({startDate,endDate,requestNo:requestNo||undefined,hasIncident:incident===""?undefined:incident==="true",page,pageSize:10}),[endDate,incident,page,requestNo,startDate]);
  const trips=useQuery({queryKey:["fleet-feedback-report","trips",params],queryFn:()=>getFleetFeedbackTripReport(params)});
  const drivers=useQuery({queryKey:["fleet-feedback-report","drivers",startDate,endDate],queryFn:()=>getFleetFeedbackDriverReport({startDate,endDate,page:1,pageSize:100})});
  const vehicles=useQuery({queryKey:["fleet-feedback-report","vehicles",startDate,endDate],queryFn:()=>getFleetFeedbackVehicleReport({startDate,endDate,page:1,pageSize:100})});
  const summary=useQuery({queryKey:["fleet-feedback-report","summary",startDate,endDate],queryFn:()=>getFleetFeedbackManagementSummary({startDate,endDate})});
  const attention=useQuery({queryKey:["fleet-feedback-report","attention",startDate,endDate],queryFn:()=>getFleetFeedbackAttention({startDate,endDate,page:1,pageSize:5})});
  const trend=useQuery({queryKey:["fleet-feedback-report","driver-trend",trendDriverId,startDate,endDate],queryFn:()=>getFleetFeedbackDriverTrend(trendDriverId,{startDate,endDate}),enabled:Boolean(trendDriverId)});
  return <Card><CardContent><Stack spacing={2}>
    <Stack direction={{xs:"column",md:"row"}} justifyContent="space-between" spacing={1}><SectionTitle title="รายงาน Feedback การเดินทาง" subtitle="แสดงผลรวมโดยไม่เปิดเผยตัวตนผู้ประเมิน"/><Stack direction={{xs:"column",sm:"row"}} spacing={1}><TextField size="small" label="ค้นหาเลขคำขอ" value={requestNo} onChange={e=>{setRequestNo(e.target.value);setPage(1)}}/><TextField select size="small" label="เหตุการณ์" value={incident} onChange={e=>{setIncident(e.target.value);setPage(1)}} sx={{minWidth:150}}><MenuItem value="">ทั้งหมด</MenuItem><MenuItem value="true">มีเหตุการณ์</MenuItem><MenuItem value="false">ไม่มีเหตุการณ์</MenuItem></TextField></Stack></Stack>
    {(trips.isLoading||drivers.isLoading||vehicles.isLoading||summary.isLoading||attention.isLoading)&&<LinearProgress/>}
    {(trips.isError||drivers.isError||vehicles.isError||summary.isError||attention.isError)&&<Typography color="error">ไม่สามารถโหลดรายงาน Feedback ได้</Typography>}
    {summary.data&&<Grid container spacing={1.5}><FeedbackKpi label="Feedback ที่ได้รับ" value={`${summary.data.feedbackCount} รายการ`}/><FeedbackKpi label="อัตราการตอบกลับ" value={`${summary.data.responseRate}%`}/><FeedbackKpi label="คะแนนรวมเฉลี่ย" value={summary.data.overallAverage==null?"–":`${summary.data.overallAverage.toFixed(1)}/5`}/><FeedbackKpi label="ความปลอดภัยเฉลี่ย" value={summary.data.safetyAverage==null?"–":`${summary.data.safetyAverage.toFixed(1)}/5`}/><FeedbackKpi label="แจ้งเหตุการณ์" value={`${summary.data.incidentCount} รายการ`}/><FeedbackKpi label="ควรตรวจสอบ" value={`${summary.data.attentionCount} รายการ`}/></Grid>}
    {trips.data&&<>
      {!trips.data.items.length?<EmptyState title="ยังไม่มี Feedback ในช่วงเวลานี้" description="Feedback เป็นทางเลือกและไม่กระทบการปิดทริป"/>:<TableContainer><Table size="small"><TableHead><TableRow><TableCell>เลขคำขอ</TableCell><TableCell>รถ / คนขับ</TableCell><TableCell align="center">ตอบกลับ</TableCell><TableCell align="center">คะแนนรวม</TableCell><TableCell align="center">ความปลอดภัย</TableCell><TableCell align="center">เหตุการณ์</TableCell></TableRow></TableHead><TableBody>{trips.data.items.map(x=><TableRow key={x.tripId} hover><TableCell><Typography fontWeight={800}>{x.requestNo}</Typography><Typography variant="caption" color="text.secondary">{x.destination}</Typography></TableCell><TableCell>{x.vehicle}<br/><Typography variant="caption">{x.driver}</Typography></TableCell><TableCell align="center">{x.feedbackCount}/{x.eligibleParticipants}<br/><Typography variant="caption">{x.responseRate}%</Typography></TableCell><TableCell align="center"><Score value={x.overallAverage}/></TableCell><TableCell align="center"><Score value={x.safetyAverage}/></TableCell><TableCell align="center"><Chip size="small" color={x.incidentCount?"warning":"default"} label={`${x.incidentCount} รายการ`}/></TableCell></TableRow>)}</TableBody></Table></TableContainer>}
      <Pagination page={page} count={Math.max(1,trips.data.totalPages)} onChange={(_,v)=>setPage(v)} color="primary" sx={{alignSelf:"center"}}/></>}
    <Grid container spacing={2}><Grid item xs={12} md={6}><FeedbackSummaryList title="สรุปตามคนขับ" rows={(drivers.data?.items??[]).map(x=>({id:x.driverUserId,label:x.driver,detail:`${x.feedbackCount} Feedback จาก ${x.eligibleParticipants} คน`,score:x.overallAverage}))}/></Grid><Grid item xs={12} md={6}><FeedbackSummaryList title="สรุปตามรถ" rows={(vehicles.data?.items??[]).map(x=>({id:x.vehicleId,label:x.vehicle,detail:`${x.feedbackCount} Feedback · ${x.trips} ทริป`,score:x.vehicleConditionAverage}))}/></Grid></Grid>
    {attention.data&&<Box sx={{p:2,border:"1px solid",borderColor:attention.data.totalItems?"warning.light":"divider",borderRadius:3,bgcolor:attention.data.totalItems?alpha("#D88923",.055):"transparent"}}><Stack direction="row" spacing={1} alignItems="center" sx={{mb:1.5}}><ReportProblemOutlinedIcon color={attention.data.totalItems?"warning":"disabled"}/><Box><Typography fontWeight={900}>Feedback ที่ควรตรวจสอบ</Typography><Typography variant="caption" color="text.secondary">มีเหตุการณ์ หรือคะแนนรวม/ความปลอดภัยไม่เกิน {summary.data?.attentionThreshold??2}</Typography></Box></Stack>{attention.data.items.length?<Stack spacing={1}>{attention.data.items.map(x=><Box key={x.feedbackId} sx={{p:1.5,borderRadius:2,bgcolor:"background.paper",border:"1px solid",borderColor:"divider"}}><Stack direction={{xs:"column",md:"row"}} justifyContent="space-between" spacing={1}><Box><Typography fontWeight={900}>{x.requestNo} · {x.destination}</Typography><Typography variant="body2" color="text.secondary">{formatThaiDateTime(x.completedAt)} · {x.vehicle} · คนขับ {x.driver}</Typography>{x.comment&&<Typography variant="body2" sx={{mt:.5}}>“{x.comment}”</Typography>}</Box><Stack direction="row" spacing={.75} alignItems="center"><Chip size="small" color="warning" label={`รวม ${x.overallRating}/5`}/><Chip size="small" color={x.safetyRating<=2?"error":"default"} label={`ปลอดภัย ${x.safetyRating}/5`}/>{x.hasIncident&&<Chip size="small" color="error" label="มีเหตุการณ์"/>}</Stack></Stack></Box>)}</Stack>:<Typography color="text.secondary">ไม่มี Feedback ที่ต้องตรวจสอบในช่วงเวลานี้</Typography>}</Box>}
    <Box sx={{p:2,border:"1px solid",borderColor:"divider",borderRadius:3}}><Stack direction={{xs:"column",sm:"row"}} justifyContent="space-between" spacing={1.5} alignItems={{sm:"center"}}><Stack direction="row" spacing={1} alignItems="center"><TrendingUpOutlinedIcon color="primary"/><Box><Typography fontWeight={900}>แนวโน้มคะแนนคนขับ</Typography><Typography variant="caption" color="text.secondary">คะแนนรายเดือน ไม่จัดอันดับพนักงานขับรถ</Typography></Box></Stack><TextField select size="small" label="เลือกคนขับ" value={trendDriverId} onChange={e=>setTrendDriverId(e.target.value)} sx={{minWidth:240}}><MenuItem value="">เลือกเพื่อดูแนวโน้ม</MenuItem>{(drivers.data?.items??[]).map(x=><MenuItem key={x.driverUserId} value={x.driverUserId}>{x.driver}</MenuItem>)}</TextField></Stack>{trend.isFetching&&<LinearProgress sx={{mt:2}}/>}{trendDriverId&&!trend.isFetching&&<FeedbackTrend rows={trend.data??[]}/>}</Box>
  </Stack></CardContent></Card>;
}
function FeedbackKpi({label,value}:{label:string;value:string}){return <Grid item xs={12} sm={4}><Box sx={{p:2,borderRadius:3,bgcolor:alpha("#C79A3B",.1),border:"1px solid",borderColor:alpha("#C79A3B",.3)}}><Typography variant="caption" color="text.secondary">{label}</Typography><Typography variant="h6" fontWeight={900}>{value}</Typography></Box></Grid>}
function Score({value}:{value?:number|null}){return value==null?<Typography color="text.secondary">–</Typography>:<Stack direction="row" spacing={.3} justifyContent="center" alignItems="center"><StarRoundedIcon sx={{fontSize:18,color:"#C79A3B"}}/><Typography fontWeight={900}>{value.toFixed(1)}</Typography></Stack>}
function FeedbackSummaryList({title,rows}:{title:string;rows:Array<{id:string;label:string;detail:string;score?:number|null}>}){return <Box sx={{p:2,border:"1px solid",borderColor:"divider",borderRadius:3,height:"100%"}}><Typography fontWeight={900} sx={{mb:1}}>{title}</Typography><Stack spacing={1}>{rows.slice(0,5).map(x=><Stack key={x.id} direction="row" justifyContent="space-between" alignItems="center"><Box><Typography variant="body2" fontWeight={800}>{x.label}</Typography><Typography variant="caption" color="text.secondary">{x.detail}</Typography></Box><Score value={x.score}/></Stack>)}{!rows.length&&<Typography variant="body2" color="text.secondary">ยังไม่มีข้อมูล</Typography>}</Stack></Box>}
function FeedbackTrend({rows}:{rows:Array<{year:number;month:number;feedbackCount:number;overallAverage?:number|null;safetyAverage?:number|null;punctualityAverage?:number|null}>}){if(!rows.length)return <Typography color="text.secondary" sx={{mt:2}}>ยังไม่มีข้อมูลแนวโน้มในช่วงเวลานี้</Typography>;return <Stack direction="row" spacing={1} sx={{mt:2,overflowX:"auto",pb:1}}>{rows.map(x=><Box key={`${x.year}-${x.month}`} sx={{minWidth:150,p:1.5,borderRadius:2,bgcolor:alpha("#176B55",.06),border:"1px solid",borderColor:alpha("#176B55",.18)}}><Typography fontWeight={900}>{String(x.month).padStart(2,"0")}/{x.year+543}</Typography><Typography variant="caption" color="text.secondary">{x.feedbackCount} Feedback</Typography><Stack spacing={.35} sx={{mt:1}}><Typography variant="body2">คะแนนรวม <b>{x.overallAverage?.toFixed(1)??"–"}</b></Typography><Typography variant="body2">ความปลอดภัย <b>{x.safetyAverage?.toFixed(1)??"–"}</b></Typography><Typography variant="body2">ตรงต่อเวลา <b>{x.punctualityAverage?.toFixed(1)??"–"}</b></Typography></Stack></Box>)}</Stack>}

function KpiCard({ icon, label, value, color }: { icon: React.ReactNode; label: string; value: string; color: string; compact?: boolean }) { return <Grid item xs={12} sm={6} md={4} lg><Card sx={{ height: "100%" }}><CardContent><Stack direction="row" spacing={1.25} alignItems="center"><Box sx={{ width: 44, height: 44, flex: "0 0 auto", display: "grid", placeItems: "center", borderRadius: 2.5, color, bgcolor: `${color}18` }}>{icon}</Box><Box minWidth={0}><Typography variant="body2" color="text.secondary" noWrap>{label}</Typography><Typography variant="h5" fontWeight={900} noWrap sx={{ fontSize: { xs: "1.25rem", xl: "1.5rem" } }}>{value}</Typography></Box></Stack></CardContent></Card></Grid>; }
function DriverStatisticCard({ item, index }: { item: FleetDriverReport; index: number }) {
  const colors = ["#176B55", "#287BC1", "#7651C9", "#D88923", "#238B8B", "#B55252"];
  const color = colors[index % colors.length];
  const activeJobs = Math.max(0, item.jobCount - item.completedTrips);
  const completionRate = item.jobCount ? Math.round(item.completedTrips / item.jobCount * 100) : 0;
  const initials = item.driver.trim().split(/\s+/).slice(-2).map(part => part.charAt(0)).join("").slice(0, 2) || "–";

  return <Box sx={{ height: "100%", p: 2, border: "1px solid", borderColor: alpha(color, 0.25), borderRadius: 3, background: `linear-gradient(145deg, #fff 0%, ${alpha(color, 0.055)} 100%)`, boxShadow: `0 10px 26px ${alpha(color, 0.08)}`, transition: "transform .18s ease, box-shadow .18s ease", "&:hover": { transform: "translateY(-3px)", boxShadow: `0 16px 32px ${alpha(color, 0.15)}` } }}>
    <Stack direction="row" spacing={1.5} alignItems="center">
      <Box sx={{ width: 50, height: 50, flexShrink: 0, display: "grid", placeItems: "center", borderRadius: "16px", color: "white", fontWeight: 900, fontSize: "1.05rem", background: `linear-gradient(135deg, ${color}, ${alpha(color, 0.72)})`, boxShadow: `0 8px 18px ${alpha(color, 0.24)}` }}>{initials}</Box>
      <Box minWidth={0} flex={1}>
        <Typography fontWeight={900} color="text.primary" noWrap title={item.driver}>{item.driver}</Typography>
        <Stack direction="row" spacing={0.6} alignItems="center"><PersonOutlineOutlinedIcon sx={{ fontSize: 15, color }} /><Typography variant="caption" color="text.secondary">พนักงานขับรถ</Typography></Stack>
      </Box>
      <Chip size="small" label={`${completionRate}%`} sx={{ bgcolor: alpha(color, 0.12), color, fontWeight: 900, border: `1px solid ${alpha(color, 0.22)}` }} />
    </Stack>

    <Box sx={{ display: "grid", gridTemplateColumns: "repeat(3, minmax(0, 1fr))", gap: 1, my: 2 }}>
      <DriverMetric icon={<LocalShippingOutlinedIcon />} label="รับงาน" value={item.jobCount} color={color} />
      <DriverMetric icon={<FactCheckOutlinedIcon />} label="เสร็จสิ้น" value={item.completedTrips} color="#1E8E4A" />
      <DriverMetric icon={<PendingActionsOutlinedIcon />} label="ดำเนินการ" value={activeJobs} color="#D88923" />
    </Box>

    <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 0.65 }}><Typography variant="caption" color="text.secondary">ความสำเร็จของงาน</Typography><Typography variant="caption" fontWeight={800} sx={{ color }}>{item.completedTrips}/{item.jobCount} งาน</Typography></Stack>
    <LinearProgress variant="determinate" value={completionRate} sx={{ height: 8, borderRadius: 6, bgcolor: alpha(color, 0.1), "& .MuiLinearProgress-bar": { borderRadius: 6, background: `linear-gradient(90deg, ${color}, ${alpha(color, 0.68)})` } }} />
  </Box>;
}
function DriverMetric({ icon, label, value, color }: { icon: React.ReactNode; label: string; value: number; color: string }) { return <Box sx={{ p: 1, textAlign: "center", borderRadius: 2, bgcolor: alpha(color, 0.075), border: `1px solid ${alpha(color, 0.14)}` }}><Box sx={{ color, display: "flex", justifyContent: "center", "& svg": { fontSize: 20 } }}>{icon}</Box><Typography fontWeight={900} lineHeight={1.2}>{value.toLocaleString("th-TH")}</Typography><Typography variant="caption" color="text.secondary" noWrap>{label}</Typography></Box>; }
function SectionTitle({ title, subtitle }: { title: string; subtitle: string }) { return <Box sx={{ mb: 1.75 }}><Typography fontWeight={900}>{title}</Typography><Typography variant="caption" color="text.secondary">{subtitle}</Typography></Box>; }
function BarList({ rows }: { rows: Array<{ label: string; value: number }> }) { const maximum = Math.max(...rows.map((row) => row.value), 1); return <Stack spacing={1.25}>{rows.map((row) => <Box key={row.label}><Stack direction="row" justifyContent="space-between" spacing={1}><Typography variant="body2" noWrap>{row.label}</Typography><Typography variant="body2" fontWeight={800}>{row.value}</Typography></Stack><Box sx={{ mt: 0.5, height: 8, bgcolor: "grey.100", borderRadius: 5, overflow: "hidden" }}><Box sx={{ height: "100%", width: `${row.value / maximum * 100}%`, bgcolor: "success.main", borderRadius: 5 }} /></Box></Box>)}</Stack>; }
function DailyTrend({ rows }: { rows: Array<{ label: string; value: number }> }) {
  const maximum = Math.max(...rows.map(row => row.value), 1);
  const visible = rows.length > 31 ? rows.filter((_, index) => index % Math.ceil(rows.length / 31) === 0) : rows;
  return <Box sx={{ height: 230, display: "flex", alignItems: "flex-end", gap: 0.5, pt: 2, borderBottom: 1, borderColor: "divider" }}>{visible.map((row, index) => <Box key={`${row.label}-${index}`} sx={{ flex: 1, minWidth: 5, height: "100%", display: "flex", flexDirection: "column", justifyContent: "flex-end", alignItems: "center" }}><Box title={`${row.value} เที่ยว`} sx={{ width: "70%", minHeight: row.value ? 8 : 2, height: `${Math.max(2, row.value / maximum * 88)}%`, borderRadius: "4px 4px 0 0", bgcolor: index % 2 ? "success.light" : "success.main", transition: "height .2s" }} /><Typography variant="caption" color="text.secondary" sx={{ mt: 0.5, fontSize: 10 }}>{visible.length <= 16 || index % 2 === 0 ? row.label : ""}</Typography></Box>)}</Box>;
}
function StatusDonut({ counts }: { counts: Record<string, number> }) {
  const entries = Object.entries(counts).sort((a, b) => b[1] - a[1]); const total = entries.reduce((sum, [, count]) => sum + count, 0);
  let cursor = 0; const segments = entries.map(([status, count]) => { const start = cursor; cursor += total ? count / total * 100 : 0; return `${statusColor(status)} ${start}% ${cursor}%`; });
  return <Stack direction={{ xs: "column", sm: "row", md: "column" }} spacing={2} alignItems="center"><Box sx={{ width: 150, height: 150, borderRadius: "50%", background: total ? `conic-gradient(${segments.join(",")})` : "grey.100", display: "grid", placeItems: "center" }}><Box sx={{ width: 92, height: 92, borderRadius: "50%", bgcolor: "background.paper", display: "grid", placeItems: "center", textAlign: "center" }}><Box><Typography variant="h5" fontWeight={900}>{total}</Typography><Typography variant="caption">เที่ยว</Typography></Box></Box></Box><StatusSummary counts={counts} compact /></Stack>;
}
function StatusSummary({ counts, compact = false }: { counts: Record<string, number>; compact?: boolean }) {
  const entries = Object.entries(counts).sort((a, b) => b[1] - a[1]); const total = entries.reduce((sum, [, count]) => sum + count, 0);
  return <Stack spacing={compact ? 0.65 : 1}>{entries.length ? entries.slice(0, compact ? 5 : 8).map(([status, count]) => <Stack key={status} direction="row" justifyContent="space-between" spacing={1}><Stack direction="row" spacing={0.8} alignItems="center"><Box sx={{ width: 8, height: 8, borderRadius: "50%", bgcolor: statusColor(status) }} /><Typography variant="body2">{getFleetStatusLabel(status)}</Typography></Stack><Typography variant="body2" fontWeight={800}>{count} <Typography component="span" variant="caption" color="text.secondary">({total ? Math.round(count / total * 100) : 0}%)</Typography></Typography></Stack>) : <Typography variant="body2" color="text.secondary">ไม่มีข้อมูล</Typography>}</Stack>;
}
function ReportRow({ event }: { event: FleetCalendarEvent }) { const data = event.metadata ?? {}; return <TableRow hover><TableCell><Typography fontWeight={800} color="primary">{String(data.requestNo ?? event.title).split(" · ")[0]}</Typography></TableCell><TableCell>{formatThaiDateTime(event.startAt)}</TableCell><TableCell>{String(data.department ?? "-")}</TableCell><TableCell>{String(data.destination ?? "-")}</TableCell><TableCell align="center">{String(data.passengerCount ?? "-")}</TableCell><TableCell><Chip size="small" label={getFleetStatusLabel(event.status)} sx={{ bgcolor: `${statusColor(event.status)}18`, color: statusColor(event.status), fontWeight: 800 }} /></TableCell><TableCell align="right"><Button component={Link} to={event.detailUrl} size="small">เปิดดู</Button></TableCell></TableRow>; }
function statusColor(status: string) { if (status === "COMPLETED" || status === "APPROVED") return "#1E8E4A"; if (status === "REJECTED" || status === "CANCELLED" || status === "ABORTED") return "#D64545"; if (status.includes("PENDING")) return "#7651C9"; if (status === "IN_PROGRESS" || status === "READY") return "#287BC1"; return "#6B7280"; }
