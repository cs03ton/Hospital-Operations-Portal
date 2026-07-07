import LockOutlinedIcon from "@mui/icons-material/LockOutlined";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { Navigate, useLocation, useNavigate } from "react-router-dom";
import hospitalLogo from "../assets/logo/hospital-logo.png";
import { appName, hospitalName } from "../config/appConfig";
import { useAuth } from "../context/AuthContext";

type LoginForm = {
  username: string;
  password: string;
};

type LocationState = {
  from?: {
    pathname?: string;
    search?: string;
  };
};

export function LoginPage() {
  const { isAuthenticated, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const state = location.state as LocationState | null;
  const [error, setError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginForm>({
    defaultValues: {
      username: "",
      password: "",
    },
  });

  if (isAuthenticated) {
    return <Navigate to="/dashboard" replace />;
  }

  async function onSubmit(values: LoginForm) {
    setError(null);

    try {
      await login(values.username, values.password);
      navigate(`${state?.from?.pathname ?? "/dashboard"}${state?.from?.search ?? ""}`, { replace: true });
    } catch {
      setError("เข้าสู่ระบบไม่สำเร็จ กรุณาตรวจสอบชื่อผู้ใช้และรหัสผ่าน");
    }
  }

  return (
    <Box
      sx={{
        minHeight: "100vh",
        display: "grid",
        placeItems: "center",
        bgcolor: "background.default",
        px: 2,
        py: 4,
      }}
    >
      <Card sx={{ width: "100%", maxWidth: 440 }}>
        <CardContent sx={{ p: { xs: 3, sm: 4 } }}>
          <Stack spacing={3}>
            <Box sx={{ textAlign: "center" }}>
              <Box
                component="img"
                src={hospitalLogo}
                alt={hospitalName}
                sx={{ width: 132, height: 132, objectFit: "contain", mb: 1.5 }}
              />
              <Typography variant="h4">{appName}</Typography>
              <Typography color="text.secondary">
                ระบบบริหารจัดการงานภายใน{hospitalName}
              </Typography>
            </Box>

            {error && <Alert severity="error">{error}</Alert>}

            <Stack component="form" spacing={2.5} onSubmit={handleSubmit(onSubmit)}>
              <TextField
                label="ชื่อผู้ใช้"
                autoComplete="username"
                error={Boolean(errors.username)}
                helperText={errors.username?.message}
                {...register("username", { required: "กรุณากรอกชื่อผู้ใช้" })}
              />
              <TextField
                label="รหัสผ่าน"
                type="password"
                autoComplete="current-password"
                error={Boolean(errors.password)}
                helperText={errors.password?.message}
                {...register("password", { required: "กรุณากรอกรหัสผ่าน" })}
              />
              <Button
                type="submit"
                variant="contained"
                size="large"
                disabled={isSubmitting}
                startIcon={<LockOutlinedIcon />}
              >
                เข้าสู่ระบบ
              </Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>
    </Box>
  );
}
