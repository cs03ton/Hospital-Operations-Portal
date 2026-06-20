import DownloadOutlinedIcon from "@mui/icons-material/DownloadOutlined";
import RecyclingOutlinedIcon from "@mui/icons-material/RecyclingOutlined";
import { Alert, Button, Card, CardContent, Stack, TextField, Typography } from "@mui/material";
import { useMutation } from "@tanstack/react-query";
import { useState } from "react";
import { getAuditLogExportUrl, runAuditRetention } from "../api/securityApi";
import { AppDatePicker } from "../components/common/AppDatePicker";
import { PageHeader } from "../components/PageHeader";

export function AuditLogExportPage() {
  const [action, setAction] = useState("");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const retentionMutation = useMutation({ mutationFn: runAuditRetention });

  function downloadCsv() {
    const params = new URLSearchParams();
    if (action) params.set("action", action);
    if (from) params.set("from", from);
    if (to) params.set("to", to);
    window.open(`${getAuditLogExportUrl()}?${params.toString()}`, "_blank", "noopener,noreferrer");
  }

  return (
    <>
      <PageHeader title="ส่งออกบันทึกการใช้งาน" subtitle="ดาวน์โหลด Audit Log เป็นไฟล์ CSV และสั่งรันนโยบายเก็บรักษาข้อมูล" />
      <Card>
        <CardContent>
          <Stack spacing={2}>
            <Typography variant="h6">ตัวกรองสำหรับส่งออก</Typography>
            <TextField label="การกระทำ" value={action} onChange={(event) => setAction(event.target.value)} />
            <AppDatePicker label="ตั้งแต่วันที่" value={from} onChange={setFrom} />
            <AppDatePicker label="ถึงวันที่" value={to} onChange={setTo} />
            <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5}>
              <Button variant="contained" startIcon={<DownloadOutlinedIcon />} onClick={downloadCsv}>
                ดาวน์โหลด CSV
              </Button>
              <Button variant="outlined" color="warning" startIcon={<RecyclingOutlinedIcon />} onClick={() => retentionMutation.mutate()} disabled={retentionMutation.isPending}>
                รัน Audit Retention
              </Button>
            </Stack>
            {retentionMutation.isSuccess && <Alert severity="success">ลบรายการหมดอายุแล้ว {retentionMutation.data.deletedCount} รายการ</Alert>}
            {retentionMutation.isError && <Alert severity="error">รัน Audit Retention ไม่สำเร็จ</Alert>}
          </Stack>
        </CardContent>
      </Card>
    </>
  );
}
