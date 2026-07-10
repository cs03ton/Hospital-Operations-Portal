import LockResetOutlinedIcon from "@mui/icons-material/LockResetOutlined";
import VisibilityOffOutlinedIcon from "@mui/icons-material/VisibilityOffOutlined";
import VisibilityOutlinedIcon from "@mui/icons-material/VisibilityOutlined";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  IconButton,
  InputAdornment,
  LinearProgress,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import axios from "axios";
import { FormEvent, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { changePassword, getPasswordPolicy, type PasswordPolicy } from "../api/authApi";
import { PageHeader } from "../components/PageHeader";
import { useAuth } from "../context/AuthContext";
import { useNotification } from "../hooks/useNotification";

type PasswordForm = {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
};

const fallbackPolicy: PasswordPolicy = {
  minimumLength: 8,
  requireUppercase: true,
  requireLowercase: true,
  requireDigit: true,
  requireSpecialCharacter: true,
  disallowUsername: true,
};

export function ChangePasswordPage() {
  const navigate = useNavigate();
  const { clearSession, user } = useAuth();
  const { showError, showSuccess } = useNotification();
  const { data: loadedPolicy } = useQuery({ queryKey: ["me", "password-policy"], queryFn: getPasswordPolicy });
  const policy = loadedPolicy ?? fallbackPolicy;
  const [form, setForm] = useState<PasswordForm>({
    currentPassword: "",
    newPassword: "",
    confirmPassword: "",
  });
  const [showPassword, setShowPassword] = useState<Record<keyof PasswordForm, boolean>>({
    currentPassword: false,
    newPassword: false,
    confirmPassword: false,
  });
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const strength = useMemo(() => calculateStrength(form.newPassword, policy, user?.username), [form.newPassword, policy, user?.username]);
  const validationErrors = useMemo(() => validateForm(form, policy, user?.username), [form, policy, user?.username]);
  const canSubmit = validationErrors.length === 0 && !isSubmitting;

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);

    if (validationErrors.length > 0) {
      const message = validationErrors[0];
      setError(message);
      showError(message);
      return;
    }

    setIsSubmitting(true);
    try {
      await changePassword(form);
      showSuccess("เปลี่ยนรหัสผ่านเรียบร้อยแล้ว กรุณาเข้าสู่ระบบใหม่");
      setForm({ currentPassword: "", newPassword: "", confirmPassword: "" });
      clearSession();
      window.setTimeout(() => navigate("/login", { replace: true }), 1200);
    } catch (err) {
      const message = getErrorMessage(err);
      setError(message);
      setForm({ currentPassword: "", newPassword: "", confirmPassword: "" });
      showError(message);
    } finally {
      setIsSubmitting(false);
    }
  }

  function updateField(field: keyof PasswordForm, value: string) {
    setForm((current) => ({ ...current, [field]: value }));
  }

  function toggleVisibility(field: keyof PasswordForm) {
    setShowPassword((current) => ({ ...current, [field]: !current[field] }));
  }

  return (
    <Box>
      <PageHeader title="เปลี่ยนรหัสผ่าน" subtitle="ยืนยันรหัสผ่านปัจจุบันก่อนตั้งรหัสผ่านใหม่เพื่อความปลอดภัยของบัญชี" />

      <Card sx={{ maxWidth: 720 }}>
        <CardContent sx={{ p: { xs: 3, md: 4 } }}>
          <Stack component="form" spacing={2.5} onSubmit={handleSubmit}>
            <Box>
              <Typography variant="h6" color="primary" gutterBottom>
                เปลี่ยนรหัสผ่านของฉัน
              </Typography>
              <Typography color="text.secondary">
                หลังเปลี่ยนรหัสผ่านสำเร็จ ระบบจะออกจากระบบและให้เข้าสู่ระบบใหม่อีกครั้ง
              </Typography>
            </Box>

            {error && <Alert severity="error">{error}</Alert>}

            <PasswordTextField
              label="รหัสผ่านปัจจุบัน"
              value={form.currentPassword}
              visible={showPassword.currentPassword}
              autoComplete="current-password"
              onChange={(value) => updateField("currentPassword", value)}
              onToggle={() => toggleVisibility("currentPassword")}
              disabled={isSubmitting}
            />

            <PasswordTextField
              label="รหัสผ่านใหม่"
              value={form.newPassword}
              visible={showPassword.newPassword}
              autoComplete="new-password"
              onChange={(value) => updateField("newPassword", value)}
              onToggle={() => toggleVisibility("newPassword")}
              disabled={isSubmitting}
              helperText={`อย่างน้อย ${policy.minimumLength} ตัวอักษร และต้องเป็นไปตาม Password Policy`}
            />

            <Box>
              <Stack direction="row" alignItems="center" spacing={1.5} sx={{ mb: 1 }}>
                <Typography variant="body2" color="text.secondary">
                  ความแข็งแรงของรหัสผ่าน
                </Typography>
                <Chip size="small" color={strength.color} label={strength.label} />
              </Stack>
              <LinearProgress
                variant="determinate"
                value={strength.percent}
                color={strength.color}
                sx={{ height: 8, borderRadius: 999 }}
              />
            </Box>

            <PasswordPolicyChecklist policy={policy} password={form.newPassword} username={user?.username} />

            <PasswordTextField
              label="ยืนยันรหัสผ่านใหม่"
              value={form.confirmPassword}
              visible={showPassword.confirmPassword}
              autoComplete="new-password"
              onChange={(value) => updateField("confirmPassword", value)}
              onToggle={() => toggleVisibility("confirmPassword")}
              disabled={isSubmitting}
            />

            <Alert severity="info">
              ระบบจะไม่บันทึกหรือแสดงรหัสผ่านจริงในหน้าเว็บหรือ Audit Log
            </Alert>

            <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5} justifyContent="flex-end">
              <Button variant="outlined" disabled={isSubmitting} onClick={() => navigate("/profile")}>
                ยกเลิก
              </Button>
              <Button
                type="submit"
                variant="contained"
                disabled={!canSubmit}
                startIcon={<LockResetOutlinedIcon />}
              >
                {isSubmitting ? "กำลังบันทึก..." : "เปลี่ยนรหัสผ่าน"}
              </Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>
    </Box>
  );
}

function PasswordTextField({
  label,
  value,
  visible,
  autoComplete,
  helperText,
  disabled,
  onChange,
  onToggle,
}: {
  label: string;
  value: string;
  visible: boolean;
  autoComplete: string;
  helperText?: string;
  disabled?: boolean;
  onChange: (value: string) => void;
  onToggle: () => void;
}) {
  return (
    <TextField
      label={label}
      type={visible ? "text" : "password"}
      value={value}
      autoComplete={autoComplete}
      helperText={helperText}
      disabled={disabled}
      onChange={(event) => onChange(event.target.value)}
      InputProps={{
        endAdornment: (
          <InputAdornment position="end">
            <IconButton edge="end" onClick={onToggle} aria-label={visible ? "ซ่อนรหัสผ่าน" : "แสดงรหัสผ่าน"}>
              {visible ? <VisibilityOffOutlinedIcon /> : <VisibilityOutlinedIcon />}
            </IconButton>
          </InputAdornment>
        ),
      }}
    />
  );
}

function PasswordPolicyChecklist({ policy, password, username }: { policy: PasswordPolicy; password: string; username?: string }) {
  const items = getPolicyChecks(password, policy, username);
  return (
    <Stack spacing={0.75}>
      {items.map((item) => (
        <Typography key={item.label} variant="body2" color={item.passed ? "success.main" : "text.secondary"}>
          {item.passed ? "✓" : "•"} {item.label}
        </Typography>
      ))}
    </Stack>
  );
}

function validateForm(form: PasswordForm, policy: PasswordPolicy, username?: string) {
  if (!form.currentPassword || !form.newPassword || !form.confirmPassword) {
    return ["กรุณากรอกข้อมูลรหัสผ่านให้ครบถ้วน"];
  }

  if (form.currentPassword === form.newPassword) {
    return ["รหัสผ่านใหม่ต้องแตกต่างจากรหัสผ่านเดิม"];
  }

  if (form.newPassword !== form.confirmPassword) {
    return ["รหัสผ่านใหม่และยืนยันรหัสผ่านใหม่ไม่ตรงกัน"];
  }

  return getPolicyChecks(form.newPassword, policy, username)
    .filter((item) => !item.passed)
    .map((item) => item.label);
}

function getPolicyChecks(password: string, policy: PasswordPolicy, username?: string) {
  return [
    {
      label: `ความยาวอย่างน้อย ${policy.minimumLength} ตัวอักษร`,
      passed: password.length >= policy.minimumLength,
    },
    {
      label: "มีตัวพิมพ์ใหญ่อย่างน้อย 1 ตัว",
      passed: !policy.requireUppercase || /[A-Z]/.test(password),
    },
    {
      label: "มีตัวพิมพ์เล็กอย่างน้อย 1 ตัว",
      passed: !policy.requireLowercase || /[a-z]/.test(password),
    },
    {
      label: "มีตัวเลขอย่างน้อย 1 ตัว",
      passed: !policy.requireDigit || /\d/.test(password),
    },
    {
      label: "มีอักขระพิเศษอย่างน้อย 1 ตัว",
      passed: !policy.requireSpecialCharacter || /[^A-Za-z0-9]/.test(password),
    },
    {
      label: "ไม่มีชื่อผู้ใช้เป็นส่วนหนึ่งของรหัสผ่าน",
      passed: !policy.disallowUsername || !username || !password.toLowerCase().includes(username.toLowerCase()),
    },
  ];
}

function calculateStrength(password: string, policy: PasswordPolicy, username?: string) {
  if (!password) {
    return { label: "อ่อน", percent: 0, color: "error" as const };
  }

  const checks = getPolicyChecks(password, policy, username);
  const score = checks.filter((item) => item.passed).length;
  const percent = Math.round((score / checks.length) * 100);
  if (percent >= 85) {
    return { label: "แข็งแรง", percent, color: "success" as const };
  }

  if (percent >= 55) {
    return { label: "ปานกลาง", percent, color: "warning" as const };
  }

  return { label: "อ่อน", percent, color: "error" as const };
}

function getErrorMessage(error: unknown) {
  if (axios.isAxiosError(error)) {
    return error.response?.data?.message ?? "เปลี่ยนรหัสผ่านไม่สำเร็จ กรุณาลองใหม่อีกครั้ง";
  }

  return "เปลี่ยนรหัสผ่านไม่สำเร็จ กรุณาลองใหม่อีกครั้ง";
}
