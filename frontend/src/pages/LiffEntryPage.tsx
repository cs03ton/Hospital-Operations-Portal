import CloseIcon from "@mui/icons-material/Close";
import LoginIcon from "@mui/icons-material/Login";
import RefreshIcon from "@mui/icons-material/Refresh";
import { Alert, Box, Button, Card, CardContent, CircularProgress, Stack, Typography } from "@mui/material";
import axios from "axios";
import { useEffect, useMemo, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import hospitalLogo from "../assets/logo/hospital-logo.png";
import { appName, hospitalName } from "../config/appConfig";
import { useAuth } from "../context/AuthContext";
import { closeLiffWindow, ensureLiffLogin, getLiffIdToken } from "../services/liffService";
import { sanitizeInternalReturnUrl } from "../utils/returnUrl";

type LiffState = "loading" | "unlinked" | "error";

export function LiffEntryPage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const { loginWithLiff } = useAuth();
  const [state, setState] = useState<LiffState>("loading");
  const [message, setMessage] = useState("กำลังเชื่อมต่อกับ LINE...");
  const returnUrl = useMemo(() => sanitizeInternalReturnUrl(searchParams.get("returnUrl")), [searchParams]);

  useEffect(() => {
    let cancelled = false;

    async function run() {
      try {
        setState("loading");
        setMessage("กำลังเชื่อมต่อกับ LINE...");
        const loggedIn = await ensureLiffLogin();
        if (!loggedIn || cancelled) {
          return;
        }

        setMessage("กำลังเข้าสู่ระบบ HOP...");
        const idToken = await getLiffIdToken();
        await loginWithLiff(idToken, returnUrl);
        if (!cancelled) {
          navigate(returnUrl, { replace: true });
        }
      } catch (error) {
        if (cancelled) {
          return;
        }

        const code = axios.isAxiosError(error) ? error.response?.data?.message : null;
        if (code === "LINE_ACCOUNT_NOT_LINKED") {
          setState("unlinked");
          setMessage("บัญชี LINE นี้ยังไม่ได้เชื่อมกับ HOP");
          return;
        }

        setState("error");
        setMessage(code === "LINE_TOKEN_INVALID"
          ? "เซสชัน LINE หมดอายุ กรุณาเข้าสู่ระบบใหม่"
          : "ไม่สามารถตรวจสอบบัญชี LINE ได้ กรุณาลองใหม่");
      }
    }

    run();
    return () => {
      cancelled = true;
    };
  }, [loginWithLiff, navigate, returnUrl]);

  return (
    <Box
      sx={{
        minHeight: "100vh",
        display: "grid",
        placeItems: "center",
        bgcolor: "background.default",
        px: 2,
        py: "calc(24px + env(safe-area-inset-top))",
      }}
    >
      <Card sx={{ width: "100%", maxWidth: 460, borderTop: "4px solid", borderTopColor: "primary.main" }}>
        <CardContent sx={{ p: { xs: 3, sm: 4 } }}>
          <Stack spacing={3} alignItems="center" textAlign="center">
            <Box component="img" src={hospitalLogo} alt={hospitalName} sx={{ width: 104, height: 104, objectFit: "contain" }} />
            <Box>
              <Typography variant="h4">{appName}</Typography>
              <Typography color="text.secondary">เข้าสู่ระบบผ่าน LINE สำหรับ{hospitalName}</Typography>
            </Box>

            {state === "loading" ? <CircularProgress /> : null}
            <Alert severity={state === "loading" ? "info" : state === "unlinked" ? "warning" : "error"} sx={{ width: "100%", textAlign: "left" }}>
              {message}
            </Alert>

            {state === "unlinked" ? (
              <Stack spacing={1.5} sx={{ width: "100%" }}>
                <Typography color="text.secondary">
                  กรุณาเข้าสู่ระบบ HOP ด้วยบัญชีเดิมก่อน แล้วไปที่ข้อมูลส่วนตัวเพื่อเชื่อมบัญชี LINE
                </Typography>
                <Button variant="contained" size="large" startIcon={<LoginIcon />} onClick={() => navigate(`/login?returnUrl=${encodeURIComponent("/profile?lineLink=liff")}`, { replace: true })}>
                  เข้าสู่ระบบ HOP เพื่อเชื่อมบัญชี
                </Button>
                <Button variant="outlined" size="large" startIcon={<CloseIcon />} onClick={() => void closeLiffWindow()}>
                  ปิดหน้าต่าง
                </Button>
              </Stack>
            ) : null}

            {state === "error" ? (
              <Stack spacing={1.5} sx={{ width: "100%" }}>
                <Button variant="contained" size="large" startIcon={<RefreshIcon />} onClick={() => window.location.reload()}>
                  ลองใหม่
                </Button>
                <Button variant="outlined" size="large" startIcon={<CloseIcon />} onClick={() => void closeLiffWindow()}>
                  ปิดหน้าต่าง
                </Button>
              </Stack>
            ) : null}
          </Stack>
        </CardContent>
      </Card>
    </Box>
  );
}
