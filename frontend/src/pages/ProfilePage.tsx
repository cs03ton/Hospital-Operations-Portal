import { Alert, Avatar, Box, Button, Card, CardContent, Stack, TextField, Typography } from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { isAxiosError } from "axios";
import { useEffect, useRef } from "react";
import { useForm } from "react-hook-form";
import { deleteMyProfileImage, getMyProfile, updateMyProfile, uploadMyProfileImage, type UpdateUserProfileRequest } from "../api/profileApi";
import { PageHeader } from "../components/PageHeader";
import { useAuth } from "../context/AuthContext";
import { useNotification } from "../hooks/useNotification";
import { brandColors } from "../theme/theme";
import { toAbsoluteMediaUrl } from "../utils/mediaUrl";

type ProfileFormValues = {
  fullname: string;
  position: string;
  email: string;
  phoneNumber: string;
  leaveContactAddress: string;
};

export function ProfilePage() {
  const queryClient = useQueryClient();
  const { showError, showSuccess } = useNotification();
  const { refreshUser } = useAuth();
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const { data: profile, isLoading } = useQuery({ queryKey: ["me", "profile"], queryFn: getMyProfile });
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
    </>
  );
}

function getImageApiErrorMessage(error: unknown, fallback: string) {
  if (isAxiosError<{ message?: string }>(error)) {
    return error.response?.data?.message ?? fallback;
  }

  return fallback;
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
