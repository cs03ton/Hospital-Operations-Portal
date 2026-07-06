import { Alert, Avatar, Box, Button, Card, CardContent, Chip, Dialog, DialogActions, DialogContent, DialogTitle, Divider, Stack, TextField, Typography } from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { isAxiosError } from "axios";
import { useEffect, useRef, useState } from "react";
import { useForm } from "react-hook-form";
import {
  createMyLineConnectToken,
  deleteMyProfileImage,
  getMyLineBinding,
  getMyProfile,
  sendMyLineTestMessage,
  unbindMyLine,
  updateMyProfile,
  uploadMyProfileImage,
  type UpdateUserProfileRequest,
} from "../api/profileApi";
import { PageHeader } from "../components/PageHeader";
import { useAuth } from "../context/AuthContext";
import { useNotification } from "../hooks/useNotification";
import { brandColors } from "../theme/theme";
import { getEmploymentTypeLabel } from "../utils/employmentLabels";
import { getGenderLabel } from "../utils/genderLabels";
import { toAbsoluteMediaUrl } from "../utils/mediaUrl";

type ProfileFormValues = {
  fullname: string;
  position: string;
  email: string;
  phoneNumber: string;
  leaveContactAddress: string;
};

type LineConfirmAction = "disconnect" | "connectAnother" | null;

export function ProfilePage() {
  const queryClient = useQueryClient();
  const { showError, showSuccess } = useNotification();
  const { refreshUser } = useAuth();
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const [lineConfirmAction, setLineConfirmAction] = useState<LineConfirmAction>(null);
  const { data: profile, isLoading } = useQuery({ queryKey: ["me", "profile"], queryFn: getMyProfile });
  const { data: lineBinding, isLoading: isLineLoading } = useQuery({ queryKey: ["me", "profile", "line"], queryFn: getMyLineBinding });
  const { register, handleSubmit, reset, formState: { errors } } = useForm<ProfileFormValues>({
    defaultValues: {
      fullname: "",
      position: "",
      email: "",
      phoneNumber: "",
      leaveContactAddress: "",
    },
  });

  useEffect(() => {
    if (profile) {
      reset({
        fullname: profile.fullname,
        position: profile.position ?? "",
        email: profile.email ?? "",
        phoneNumber: profile.phoneNumber ?? "",
        leaveContactAddress: profile.leaveContactAddress ?? "",
      });
    }
  }, [profile, reset]);

  const mutation = useMutation({
    mutationFn: (values: UpdateUserProfileRequest) => updateMyProfile(values),
    onSuccess: async () => {
      showSuccess("บันทึกข้อมูลส่วนตัวเรียบร้อยแล้ว");
      await queryClient.invalidateQueries({ queryKey: ["me", "profile"] });
      await refreshUser();
    },
  });

  function onSubmit(values: ProfileFormValues) {
    mutation.mutate({
      fullname: values.fullname.trim(),
      position: normalizeOptional(values.position),
      email: normalizeOptional(values.email),
      phoneNumber: normalizeOptional(values.phoneNumber),
      leaveContactAddress: normalizeOptional(values.leaveContactAddress),
    });
  }

  const uploadImageMutation = useMutation({
    mutationFn: uploadMyProfileImage,
    onSuccess: async () => {
      showSuccess("อัปโหลดรูปโปรไฟล์เรียบร้อยแล้ว");
      await queryClient.invalidateQueries({ queryKey: ["me", "profile"] });
      await refreshUser();
      if (fileInputRef.current) {
        fileInputRef.current.value = "";
      }
    },
    onError: (error) => showError(getImageApiErrorMessage(error, "อัปโหลดรูปโปรไฟล์ไม่สำเร็จ")),
  });

  const deleteImageMutation = useMutation({
    mutationFn: deleteMyProfileImage,
    onSuccess: async () => {
      showSuccess("ลบรูปโปรไฟล์เรียบร้อยแล้ว");
      await queryClient.invalidateQueries({ queryKey: ["me", "profile"] });
      await refreshUser();
    },
    onError: (error) => showError(getImageApiErrorMessage(error, "ลบรูปโปรไฟล์ไม่สำเร็จ")),
  });

  const createPairingCodeMutation = useMutation({
    mutationFn: createMyLineConnectToken,
    onSuccess: async () => {
      showSuccess("สร้างรหัสเชื่อมต่อ LINE เรียบร้อยแล้ว");
      await queryClient.invalidateQueries({ queryKey: ["me", "profile", "line"] });
    },
    onError: (error) => showError(getImageApiErrorMessage(error, "สร้างรหัสเชื่อมต่อ LINE ไม่สำเร็จ")),
  });

  const unbindLineMutation = useMutation({
    mutationFn: unbindMyLine,
    onSuccess: async () => {
      showSuccess("ยกเลิกการเชื่อมต่อ LINE เรียบร้อยแล้ว");
      await queryClient.invalidateQueries({ queryKey: ["me", "profile", "line"] });
      await queryClient.invalidateQueries({ queryKey: ["me", "profile"] });
      await refreshUser();
    },
    onError: (error) => showError(getImageApiErrorMessage(error, "ยกเลิกการเชื่อมต่อ LINE ไม่สำเร็จ")),
  });

  const connectAnotherLineMutation = useMutation({
    mutationFn: async () => {
      await unbindMyLine();
      return createMyLineConnectToken();
    },
    onSuccess: async () => {
      showSuccess("สร้างรหัสเชื่อมต่อ LINE ใหม่เรียบร้อยแล้ว");
      setLineConfirmAction(null);
      await queryClient.invalidateQueries({ queryKey: ["me", "profile", "line"] });
      await queryClient.invalidateQueries({ queryKey: ["me", "profile"] });
      await refreshUser();
    },
    onError: (error) => showError(getImageApiErrorMessage(error, "เริ่มเชื่อมต่อ LINE ใหม่ไม่สำเร็จ")),
  });

  const sendLineTestMutation = useMutation({
    mutationFn: sendMyLineTestMessage,
    onSuccess: () => showSuccess("ส่งข้อความทดสอบ LINE สำเร็จ"),
    onError: (error) => showError(getImageApiErrorMessage(error, "ส่งข้อความทดสอบ LINE ไม่สำเร็จ")),
  });

  function handleProfileImageChange(file?: File) {
    if (!file) {
      return;
    }

    if (!["image/jpeg", "image/png", "image/webp"].includes(file.type)) {
      showError("รองรับเฉพาะไฟล์ JPG, PNG หรือ WEBP เท่านั้น");
      return;
    }

    if (file.size > 2 * 1024 * 1024) {
      showError("ไฟล์รูปโปรไฟล์ต้องมีขนาดไม่เกิน 2 MB");
      return;
    }

    uploadImageMutation.mutate(file);
  }

  const latestLinePairingCode = createPairingCodeMutation.data ?? connectAnotherLineMutation.data;

  function handleConfirmLineAction() {
    if (lineConfirmAction === "disconnect") {
      unbindLineMutation.mutate(undefined, {
        onSuccess: () => setLineConfirmAction(null),
      });
      return;
    }

    if (lineConfirmAction === "connectAnother") {
      connectAnotherLineMutation.mutate();
    }
  }

  return (
    <>
      <PageHeader title="ข้อมูลส่วนตัวของฉัน" subtitle="แก้ไขข้อมูลสำหรับระบบลาและแบบฟอร์มใบลา" />
      <Stack spacing={2}>
        {mutation.isError && <Alert severity="error">{getApiErrorMessage(mutation.error)}</Alert>}
        <Card>
          <CardContent>
            <Stack direction={{ xs: "column", md: "row" }} spacing={2.5} alignItems={{ xs: "flex-start", md: "center" }}>
              <Avatar
                src={toAbsoluteMediaUrl(profile?.profileImageUrl)}
                sx={{ width: 80, height: 80, bgcolor: brandColors.accent, color: brandColors.primaryDark, fontSize: 28 }}
              >
                {(profile?.fullname ?? "U").slice(0, 1)}
              </Avatar>
              <Stack spacing={1}>
                <Typography variant="h6">{profile?.fullname ?? "กำลังโหลด..."}</Typography>
                <Typography variant="body2" color="text.secondary">
                  ข้อมูลส่วนตัวสำหรับระบบลาและแบบฟอร์มใบลา
                </Typography>
                <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
                  <input
                    ref={fileInputRef}
                    type="file"
                    accept="image/jpeg,image/png,image/webp"
                    hidden
                    onChange={(event) => handleProfileImageChange(event.target.files?.[0])}
                  />
                  <Button
                    type="button"
                    variant="outlined"
                    disabled={uploadImageMutation.isPending || isLoading}
                    onClick={() => fileInputRef.current?.click()}
                  >
                    {profile?.hasProfileImage ? "เปลี่ยนรูป" : "อัปโหลดรูปโปรไฟล์"}
                  </Button>
                  <Button
                    type="button"
                    color="error"
                    variant="outlined"
                    disabled={!profile?.hasProfileImage || deleteImageMutation.isPending || isLoading}
                    onClick={() => deleteImageMutation.mutate()}
                  >
                    ลบรูป
                  </Button>
                </Stack>
                <Typography variant="caption" color="text.secondary">
                  รองรับ JPG, PNG, WEBP ขนาดไม่เกิน 2 MB
                </Typography>
              </Stack>
            </Stack>
          </CardContent>
        </Card>

        <Card>
          <CardContent>
            <Stack spacing={2}>
              <Stack direction={{ xs: "column", sm: "row" }} justifyContent="space-between" alignItems={{ xs: "flex-start", sm: "center" }} spacing={1}>
                <Stack spacing={0.5}>
                  <Typography variant="h6">การเชื่อมต่อ LINE</Typography>
                  <Typography variant="body2" color="text.secondary">
                    ใช้สำหรับรับแจ้งเตือนคำขอลาและสถานะอนุมัติผ่าน LINE OA
                  </Typography>
                </Stack>
                <Chip
                  color={lineBinding?.isBound ? "success" : lineBinding?.status === "PairingCodeActive" ? "warning" : "default"}
                  label={lineBinding?.isBound ? "เชื่อมต่อแล้ว" : lineBinding?.status === "PairingCodeActive" ? "รอผูกบัญชี" : "ยังไม่ได้เชื่อมต่อ"}
                />
              </Stack>

              <Divider />

              {lineBinding?.isBound ? (
                <Stack direction={{ xs: "column", md: "row" }} spacing={2} alignItems={{ xs: "flex-start", md: "center" }}>
                  <Avatar src={lineBinding.pictureUrl ?? undefined} sx={{ width: 56, height: 56, bgcolor: brandColors.accent, color: brandColors.primaryDark }}>
                    {(lineBinding.displayName ?? profile?.fullname ?? "L").slice(0, 1)}
                  </Avatar>
                  <Stack spacing={0.5} sx={{ flex: 1 }}>
                    <Typography fontWeight={700}>{lineBinding.displayName ?? "LINE User"}</Typography>
                    <Typography variant="body2" color="text.secondary">LINE User ID: {lineBinding.lineUserIdMasked ?? "-"}</Typography>
                    <Typography variant="body2" color="text.secondary">เชื่อมต่อเมื่อ: {formatDateTime(lineBinding.boundAt)}</Typography>
                  </Stack>
                  <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
                    <Button variant="outlined" disabled={sendLineTestMutation.isPending} onClick={() => sendLineTestMutation.mutate()}>
                      ส่งข้อความทดสอบถึงฉัน
                    </Button>
                    <Button variant="outlined" disabled={connectAnotherLineMutation.isPending || unbindLineMutation.isPending} onClick={() => setLineConfirmAction("connectAnother")}>
                      เชื่อมต่อ LINE บัญชีอื่น
                    </Button>
                    <Button color="error" variant="outlined" disabled={unbindLineMutation.isPending || connectAnotherLineMutation.isPending} onClick={() => setLineConfirmAction("disconnect")}>
                      ยกเลิกการเชื่อมต่อ LINE
                    </Button>
                  </Stack>
                </Stack>
              ) : (
                <Stack spacing={1.5}>
                  <Alert severity="info">
                    1. เพิ่มเพื่อน LINE OA ของโรงพยาบาล 2. ส่งรหัสนี้ไปในแชท LINE OA 3. ระบบจะเชื่อมบัญชีให้อัตโนมัติ
                  </Alert>
                  {(latestLinePairingCode || lineBinding?.expiresAt) && (
                    <Stack
                      direction={{ xs: "column", md: "row" }}
                      spacing={2}
                      sx={{
                        border: `1px dashed ${brandColors.accent}`,
                        borderRadius: 2,
                        p: 2,
                        bgcolor: "rgba(200, 169, 107, 0.08)",
                      }}
                    >
                      {latestLinePairingCode?.qrCodePayload && (
                        <Box
                          component="img"
                          alt="QR Code สำหรับเชื่อมต่อ LINE"
                          src={buildQrCodeUrl(latestLinePairingCode.qrCodePayload)}
                          sx={{
                            width: 180,
                            height: 180,
                            borderRadius: 2,
                            border: "1px solid",
                            borderColor: "divider",
                            bgcolor: "common.white",
                            p: 1,
                          }}
                        />
                      )}
                      <Stack spacing={1} sx={{ flex: 1 }}>
                        <Typography variant="body2" color="text.secondary">รหัสเชื่อมต่อ LINE</Typography>
                        <Typography variant="h5" fontWeight={800} color="primary.main">
                          {latestLinePairingCode?.shortCode ?? "รหัสเดิมยังใช้งานอยู่"}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          หมดอายุ: {formatDateTime(latestLinePairingCode?.expiresAt ?? lineBinding?.expiresAt)}
                        </Typography>
                        <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
                          <Button
                            variant="outlined"
                            disabled={!latestLinePairingCode?.shortCode}
                            onClick={() => copyLineShortCode(latestLinePairingCode?.shortCode, showSuccess, showError)}
                          >
                            คัดลอกรหัส
                          </Button>
                          <Button
                            variant="outlined"
                            disabled={!latestLinePairingCode?.lineAddFriendUrl}
                            onClick={() => latestLinePairingCode?.lineAddFriendUrl && window.open(latestLinePairingCode.lineAddFriendUrl, "_blank", "noopener,noreferrer")}
                          >
                            เปิด LINE OA
                          </Button>
                        </Stack>
                      </Stack>
                    </Stack>
                  )}
                  <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
                    <Button
                      variant="contained"
                      disabled={createPairingCodeMutation.isPending || isLineLoading}
                      onClick={() => createPairingCodeMutation.mutate()}
                    >
                      {latestLinePairingCode ? "สร้างรหัสใหม่" : "เชื่อมต่อ LINE"}
                    </Button>
                  </Stack>
                </Stack>
              )}
            </Stack>
          </CardContent>
        </Card>

        <Card>
          <CardContent>
            <Stack component="form" spacing={2.5} onSubmit={handleSubmit(onSubmit)}>
              <Stack spacing={0.5}>
                <Typography variant="h6">ข้อมูลส่วนตัว</Typography>
                <Typography variant="body2" color="text.secondary">
                  แก้ไขข้อมูลที่ใช้ในระบบลาและเอกสารใบลา
                </Typography>
              </Stack>

              <Box
                sx={{
                  display: "grid",
                  gap: 2,
                  gridTemplateColumns: { xs: "1fr", md: "repeat(2, minmax(0, 1fr))" },
                  width: "100%",
                }}
              >
                <Box>
                  <TextField
                    fullWidth
                    label="ชื่อ-นามสกุล"
                    InputLabelProps={{ shrink: true }}
                    disabled={isLoading}
                    error={Boolean(errors.fullname)}
                    helperText={errors.fullname?.message}
                    {...register("fullname", { required: "กรุณากรอกชื่อ-นามสกุล" })}
                  />
                </Box>

                <Box>
                  <TextField fullWidth label="ตำแหน่ง" InputLabelProps={{ shrink: true }} disabled={isLoading} {...register("position")} />
                </Box>

                <Box>
                  <TextField
                    fullWidth
                    label="เพศ"
                    InputLabelProps={{ shrink: true }}
                    value={getGenderLabel(profile?.gender)}
                    disabled
                  />
                </Box>

                <Box>
                  <TextField
                    fullWidth
                    label="ประเภทพนักงาน"
                    InputLabelProps={{ shrink: true }}
                    value={getEmploymentTypeLabel(profile?.employmentType)}
                    disabled
                  />
                </Box>

                <Box>
                  <TextField
                    fullWidth
                    label="วันที่เริ่มปฏิบัติงาน"
                    InputLabelProps={{ shrink: true }}
                    value={formatDate(profile?.employmentStartDate)}
                    disabled
                  />
                </Box>

                <Box>
                  <TextField
                    fullWidth
                    label="อีเมล"
                    InputLabelProps={{ shrink: true }}
                    disabled={isLoading}
                    error={Boolean(errors.email)}
                    helperText={errors.email?.message}
                    {...register("email", {
                      pattern: {
                        value: /^[^@\s]+@[^@\s]+\.[^@\s]+$/,
                        message: "รูปแบบอีเมลไม่ถูกต้อง",
                      },
                    })}
                  />
                </Box>

                <Box>
                  <TextField
                    fullWidth
                    label="เบอร์โทรศัพท์"
                    InputLabelProps={{ shrink: true }}
                    disabled={isLoading}
                    error={Boolean(errors.phoneNumber)}
                    helperText={errors.phoneNumber?.message}
                    {...register("phoneNumber", {
                      pattern: {
                        value: /^[0-9+\-\s()]{6,30}$/,
                        message: "รูปแบบเบอร์โทรศัพท์ไม่ถูกต้อง",
                      },
                    })}
                  />
                </Box>

                <Box sx={{ gridColumn: "1 / -1" }}>
                  <TextField
                    fullWidth
                    multiline
                    minRows={3}
                    label="ที่อยู่ระหว่างลา / ที่อยู่ติดต่อ"
                    InputLabelProps={{ shrink: true }}
                    disabled={isLoading}
                    {...register("leaveContactAddress")}
                  />
                </Box>
              </Box>

              <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5}>
                <Button type="submit" variant="contained" disabled={mutation.isPending || isLoading}>
                  บันทึกข้อมูล
                </Button>
                <Button type="button" variant="outlined" disabled={isLoading} onClick={() => profile && reset({
                  fullname: profile.fullname,
                  position: profile.position ?? "",
                  email: profile.email ?? "",
                  phoneNumber: profile.phoneNumber ?? "",
                  leaveContactAddress: profile.leaveContactAddress ?? "",
                })}>
                  ยกเลิก
                </Button>
              </Stack>
            </Stack>
          </CardContent>
        </Card>
      </Stack>
      <Dialog open={lineConfirmAction !== null} onClose={() => setLineConfirmAction(null)} maxWidth="xs" fullWidth>
        <DialogTitle>
          {lineConfirmAction === "connectAnother" ? "เชื่อมต่อ LINE บัญชีอื่น" : "ยกเลิกการเชื่อมต่อ LINE"}
        </DialogTitle>
        <DialogContent>
          <Typography color="text.secondary">
            {lineConfirmAction === "connectAnother"
              ? "ระบบจะยกเลิกการเชื่อมต่อ LINE ปัจจุบัน แล้วสร้างรหัสใหม่สำหรับผูกบัญชี LINE อื่น คุณต้องพิมพ์รหัสใหม่ใน LINE OA ภายใน 10 นาที"
              : "ระบบจะยกเลิกการเชื่อมต่อ LINE กับบัญชีนี้ และจะไม่ส่งแจ้งเตือนไปยัง LINE เดิมจนกว่าจะเชื่อมต่อใหม่"}
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setLineConfirmAction(null)} disabled={unbindLineMutation.isPending || connectAnotherLineMutation.isPending}>
            ยกเลิก
          </Button>
          <Button
            variant="contained"
            color={lineConfirmAction === "disconnect" ? "error" : "primary"}
            onClick={handleConfirmLineAction}
            disabled={unbindLineMutation.isPending || connectAnotherLineMutation.isPending}
          >
            ยืนยัน
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}

function getImageApiErrorMessage(error: unknown, fallback: string) {
  if (isAxiosError<{ message?: string }>(error)) {
    return error.response?.data?.message ?? fallback;
  }

  return fallback;
}

function formatDate(value?: string | null) {
  if (!value) {
    return "-";
  }

  const [year, month, day] = value.slice(0, 10).split("-");
  return year && month && day ? `${day}/${month}/${year}` : value;
}

function normalizeOptional(value?: string | null) {
  return value?.trim() ? value.trim() : null;
}

function getApiErrorMessage(error: unknown) {
  if (isAxiosError<{ message?: string }>(error)) {
    return error.response?.data?.message ?? "บันทึกข้อมูลส่วนตัวไม่สำเร็จ";
  }

  return "บันทึกข้อมูลส่วนตัวไม่สำเร็จ";
}

function formatDateTime(value?: string | null) {
  if (!value) {
    return "-";
  }

  return new Intl.DateTimeFormat("th-TH", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }).format(new Date(value));
}

function buildQrCodeUrl(payload: string) {
  return `https://api.qrserver.com/v1/create-qr-code/?size=220x220&margin=12&data=${encodeURIComponent(payload)}`;
}

async function copyLineShortCode(code: string | undefined, onSuccess: (message: string) => void, onError: (message: string) => void) {
  if (!code) {
    return;
  }

  try {
    await navigator.clipboard.writeText(code);
    onSuccess("คัดลอกรหัสเชื่อมต่อ LINE แล้ว");
  } catch {
    onError("คัดลอกรหัสไม่สำเร็จ");
  }
}
