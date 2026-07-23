import ArrowBackOutlinedIcon from "@mui/icons-material/ArrowBackOutlined";
import AttachFileOutlinedIcon from "@mui/icons-material/AttachFileOutlined";
import CampaignOutlinedIcon from "@mui/icons-material/CampaignOutlined";
import DeleteOutlineOutlinedIcon from "@mui/icons-material/DeleteOutlineOutlined";
import ImageOutlinedIcon from "@mui/icons-material/ImageOutlined";
import NotificationsActiveOutlinedIcon from "@mui/icons-material/NotificationsActiveOutlined";
import PushPinOutlinedIcon from "@mui/icons-material/PushPinOutlined";
import SaveOutlinedIcon from "@mui/icons-material/SaveOutlined";
import TaskAltOutlinedIcon from "@mui/icons-material/TaskAltOutlined";
import {
  Alert,
  Autocomplete,
  Box,
  Button,
  Card,
  CardContent,
  Checkbox,
  Chip,
  Dialog,
  DialogContent,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import type { ReactNode } from "react";
import { Link as RouterLink, useNavigate, useParams } from "react-router-dom";
import {
  createAnnouncement,
  deleteAnnouncementFile,
  deleteAnnouncementImage,
  getAdminAnnouncementDetail,
  getAnnouncementCategories,
  reorderAnnouncementImages,
  updateAnnouncement,
  uploadAnnouncementAttachments,
  uploadAnnouncementImage,
} from "../api/announcementsApi";
import type { AnnouncementFile, AnnouncementImage, AnnouncementPayload } from "../api/announcementsApi";
import { getDepartments, getPermissions, getRoles, getUsers } from "../api/adminApi";
import type { DepartmentSummary, PermissionSummary, RoleSummary, UserSummary } from "../api/adminApi";
import { LoadingState } from "../components/common/LoadingState";
import { PageHeader } from "../components/PageHeader";
import { useAuthenticatedMediaUrl } from "../hooks/useAuthenticatedMediaUrl";
import { useNotification } from "../hooks/useNotification";
import { brandColors } from "../theme/theme";

type TargetType = "Everyone" | "Role" | "Department" | "User" | "Permission";

type FormState = {
  title: string;
  summary: string;
  body: string;
  tags: string;
  priority: string;
  categoryId: string;
  publishAt: string;
  expiresAt: string;
  targetType: TargetType;
  targetValues: string[];
  isFeatured: boolean;
  showAsPopup: boolean;
  showAsBanner: boolean;
  requiresAcknowledgement: boolean;
  notifyInApp: boolean;
  notifyViaLine: boolean;
};

const defaultState: FormState = {
  title: "",
  summary: "",
  body: "",
  tags: "",
  priority: "Normal",
  categoryId: "",
  publishAt: "",
  expiresAt: "",
  targetType: "Everyone",
  targetValues: [],
  isFeatured: false,
  showAsPopup: false,
  showAsBanner: false,
  requiresAcknowledgement: false,
  notifyInApp: true,
  notifyViaLine: false,
};

export function AdminAnnouncementFormPage() {
  const { id } = useParams();
  const isEdit = Boolean(id);
  const [form, setForm] = useState<FormState>(defaultState);
  const [coverFile, setCoverFile] = useState<File | null>(null);
  const [galleryFiles, setGalleryFiles] = useState<File[]>([]);
  const [attachmentFiles, setAttachmentFiles] = useState<File[]>([]);
  const [coverPreviewUrl, setCoverPreviewUrl] = useState<string | null>(null);
  const [galleryPreviewUrls, setGalleryPreviewUrls] = useState<string[]>([]);
  const [uploadError, setUploadError] = useState<string | null>(null);
  const navigate = useNavigate();
  const notify = useNotification();
  const queryClient = useQueryClient();
  const { data: categories = [] } = useQuery({ queryKey: ["admin", "announcement-categories"], queryFn: getAnnouncementCategories });
  const { data: departments = [] } = useQuery({ queryKey: ["departments"], queryFn: getDepartments });
  const { data: users = [] } = useQuery({ queryKey: ["users"], queryFn: getUsers });
  const { data: roles = [] } = useQuery({ queryKey: ["roles"], queryFn: getRoles });
  const { data: permissions = [] } = useQuery({ queryKey: ["permissions"], queryFn: getPermissions });
  const { data: detail, isLoading } = useQuery({
    queryKey: ["admin", "announcements", "detail", id],
    queryFn: () => getAdminAnnouncementDetail(id ?? ""),
    enabled: isEdit,
  });

  useEffect(() => {
    if (!detail) return;
    const firstTarget = detail.targets[0];
    const normalizedTargetType = (firstTarget?.targetType ?? "Everyone") as TargetType;
    const targetValues = normalizedTargetType === "Everyone"
      ? []
      : detail.targets
        .filter((target) => target.targetType === normalizedTargetType && target.targetValue)
        .map((target) => target.targetValue!)
        .filter(Boolean);

    setForm({
      title: detail.title,
      summary: detail.summary,
      body: detail.body,
      tags: detail.tags ?? "",
      priority: detail.priority,
      categoryId: detail.category?.id ?? "",
      publishAt: toInputDateTime(detail.publishAt),
      expiresAt: toInputDateTime(detail.expiresAt),
      targetType: normalizedTargetType,
      targetValues,
      isFeatured: detail.isFeatured,
      showAsPopup: detail.showAsPopup,
      showAsBanner: detail.showAsBanner,
      requiresAcknowledgement: detail.requiresAcknowledgement,
      notifyInApp: detail.notifyInApp,
      notifyViaLine: detail.notifyViaLine,
    });
  }, [detail]);

  useEffect(() => {
    if (!coverFile) {
      setCoverPreviewUrl(null);
      return;
    }
    const url = URL.createObjectURL(coverFile);
    setCoverPreviewUrl(url);
    return () => URL.revokeObjectURL(url);
  }, [coverFile]);

  useEffect(() => {
    const urls = galleryFiles.map((file) => URL.createObjectURL(file));
    setGalleryPreviewUrls(urls);
    return () => urls.forEach((url) => URL.revokeObjectURL(url));
  }, [galleryFiles]);

  const saveMutation = useMutation({
    mutationFn: (payload: AnnouncementPayload) => isEdit ? updateAnnouncement(id ?? "", payload) : createAnnouncement(payload),
    onSuccess: async (saved) => {
      notify.showSuccess(isEdit ? "บันทึกประกาศเรียบร้อยแล้ว" : "เพิ่มประกาศเรียบร้อยแล้ว");
      setUploadError(null);
      try {
        await uploadPendingMedia(saved.id);
      } catch (error) {
        const message = error instanceof Error ? error.message : "อัปโหลดสื่อประกาศบางรายการไม่สำเร็จ";
        setUploadError(message);
        notify.showWarning("บันทึกประกาศแล้ว แต่มีไฟล์บางรายการอัปโหลดไม่สำเร็จ");
        return;
      }
      await queryClient.invalidateQueries({ queryKey: ["admin", "announcements"] });
      window.setTimeout(() => navigate("/admin/announcements"), 800);
    },
    onError: () => notify.showError("ไม่สามารถบันทึกประกาศได้ กรุณาตรวจสอบข้อมูลอีกครั้ง"),
  });

  function update<K extends keyof FormState>(key: K, value: FormState[K]) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  function updateTargetType(value: TargetType) {
    setForm((current) => ({ ...current, targetType: value, targetValues: [] }));
  }

  function submit() {
    if (form.targetType !== "Everyone" && form.targetValues.length === 0) {
      notify.showError("กรุณาเลือกค่าเป้าหมายอย่างน้อย 1 รายการ");
      return;
    }

    const payload: AnnouncementPayload = {
      title: form.title,
      summary: form.summary || undefined,
      body: form.body,
      tags: form.tags || null,
      priority: form.priority,
      categoryId: form.categoryId || null,
      publishAt: fromInputDateTime(form.publishAt),
      expiresAt: fromInputDateTime(form.expiresAt),
      isFeatured: form.isFeatured,
      showAsPopup: form.showAsPopup,
      showAsBanner: form.showAsBanner,
      requiresAcknowledgement: form.requiresAcknowledgement,
      notifyInApp: form.notifyInApp,
      notifyViaLine: form.notifyViaLine,
      targets: buildTargetPayload(form.targetType, form.targetValues),
    };
    saveMutation.mutate(payload);
  }

  async function uploadPendingMedia(announcementId: string) {
    if (coverFile) {
      await uploadAnnouncementImage(announcementId, coverFile, true, 0);
    }

    for (const [index, file] of galleryFiles.entries()) {
      await uploadAnnouncementImage(announcementId, file, false, index + 1);
    }

    if (attachmentFiles.length > 0) {
      await uploadAnnouncementAttachments(announcementId, attachmentFiles);
    }
  }

  async function removeExistingImage(image: AnnouncementImage) {
    if (!window.confirm("ต้องการลบรูปภาพนี้หรือไม่")) return;
    await deleteAnnouncementImage(image.id);
    notify.showSuccess("ลบรูปภาพเรียบร้อยแล้ว");
    await queryClient.invalidateQueries({ queryKey: ["admin", "announcements", "detail", id] });
  }

  async function removeExistingFile(file: AnnouncementFile) {
    if (!window.confirm("ต้องการลบไฟล์แนบนี้หรือไม่")) return;
    await deleteAnnouncementFile(file.id);
    notify.showSuccess("ลบไฟล์แนบเรียบร้อยแล้ว");
    await queryClient.invalidateQueries({ queryKey: ["admin", "announcements", "detail", id] });
  }

  async function moveExistingImage(imageId: string, direction: -1 | 1) {
    if (!detail) return;
    const gallery = detail.images
      .filter((image) => !image.isCover)
      .sort((left, right) => left.displayOrder - right.displayOrder);
    const index = gallery.findIndex((image) => image.id === imageId);
    const nextIndex = index + direction;
    if (index < 0 || nextIndex < 0 || nextIndex >= gallery.length) return;

    const nextGallery = [...gallery];
    [nextGallery[index], nextGallery[nextIndex]] = [nextGallery[nextIndex], nextGallery[index]];
    await reorderAnnouncementImages(detail.id, nextGallery.map((image, itemIndex) => ({ imageId: image.id, displayOrder: itemIndex + 1 })));
    notify.showSuccess("จัดลำดับรูปภาพเรียบร้อยแล้ว");
    await queryClient.invalidateQueries({ queryKey: ["admin", "announcements", "detail", id] });
  }

  if (isLoading) {
    return <LoadingState message="กำลังโหลดประกาศ..." />;
  }

  return (
    <Box sx={{ maxWidth: 1120, mx: "auto" }}>
      <Stack spacing={3}>
        <PageHeader title={isEdit ? "แก้ไขประกาศ" : "เพิ่มประกาศ"} subtitle="กำหนดเนื้อหา ความสำคัญ กลุ่มเป้าหมาย และรูปแบบการแสดงผล" />
        <Button component={RouterLink} to="/admin/announcements" variant="outlined" startIcon={<ArrowBackOutlinedIcon />} sx={{ alignSelf: "flex-start" }}>
          กลับหน้าจัดการประกาศ
        </Button>
        <Card sx={{ border: `1px solid ${brandColors.border}`, borderTop: `5px solid ${brandColors.accent}`, borderRadius: 3 }}>
          <CardContent sx={{ p: { xs: 2.5, md: 4 } }}>
            <Stack spacing={2.5}>
              <TextField required label="หัวข้อประกาศ" value={form.title} onChange={(event) => update("title", event.target.value)} />
              <TextField label="สรุปสั้น" value={form.summary} onChange={(event) => update("summary", event.target.value)} multiline minRows={2} />
              <TextField
                label="🏷️ Tag"
                value={form.tags}
                onChange={(event) => update("tags", event.target.value)}
                placeholder="เช่น ประชุม, งานบุคลากร, ด่วน"
                helperText="คั่นแต่ละ tag ด้วยเครื่องหมาย comma ระบบจะแสดงเป็นป้ายกำกับในหน้าประกาศ"
              />
              <TextField required label="เนื้อหาประกาศ" value={form.body} onChange={(event) => update("body", event.target.value)} multiline minRows={8} />
              <AnnouncementMediaSection
                existingImages={detail?.images ?? []}
                existingFiles={detail?.files ?? []}
                coverFile={coverFile}
                coverPreviewUrl={coverPreviewUrl}
                galleryFiles={galleryFiles}
                galleryPreviewUrls={galleryPreviewUrls}
                attachmentFiles={attachmentFiles}
                onCoverChange={setCoverFile}
                onGalleryChange={setGalleryFiles}
                onAttachmentsChange={setAttachmentFiles}
                onRemoveExistingImage={removeExistingImage}
                onRemoveExistingFile={removeExistingFile}
                onMoveExistingImage={moveExistingImage}
              />
              {uploadError && <Alert severity="warning">{uploadError}</Alert>}
              <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", md: "repeat(2, minmax(0, 1fr))" }, gap: 2 }}>
                <TextField select label="ความสำคัญ" value={form.priority} onChange={(event) => update("priority", event.target.value)}>
                  <MenuItem value="Normal">ปกติ</MenuItem>
                  <MenuItem value="Important">สำคัญ</MenuItem>
                  <MenuItem value="Critical">เร่งด่วน</MenuItem>
                </TextField>
                <TextField select label="หมวดหมู่" value={form.categoryId} onChange={(event) => update("categoryId", event.target.value)}>
                  <MenuItem value="">ไม่ระบุ</MenuItem>
                  {categories.map((category) => <MenuItem key={category.id} value={category.id}>{category.name}</MenuItem>)}
                </TextField>
                <TextField type="datetime-local" label="เวลาเผยแพร่" value={form.publishAt} onChange={(event) => update("publishAt", event.target.value)} InputLabelProps={{ shrink: true }} />
                <TextField type="datetime-local" label="หมดอายุ" value={form.expiresAt} onChange={(event) => update("expiresAt", event.target.value)} InputLabelProps={{ shrink: true }} />
                <TextField select label="กลุ่มเป้าหมาย" value={form.targetType} onChange={(event) => updateTargetType(event.target.value as TargetType)}>
                  <MenuItem value="Everyone">ทุกคน</MenuItem>
                  <MenuItem value="Role">บทบาท</MenuItem>
                  <MenuItem value="Department">หน่วยงาน</MenuItem>
                  <MenuItem value="User">ผู้ใช้</MenuItem>
                  <MenuItem value="Permission">สิทธิ์</MenuItem>
                </TextField>
                <AnnouncementTargetPicker
                  targetType={form.targetType}
                  targetValues={form.targetValues}
                  onChange={(values) => update("targetValues", values)}
                  departments={departments}
                  users={users}
                  roles={roles}
                  permissions={permissions}
                />
              </Box>
              <Card variant="outlined" sx={{ borderRadius: 3, bgcolor: "rgba(250, 248, 242, 0.48)" }}>
                <CardContent>
                  <Stack spacing={2}>
                    <Stack direction="row" spacing={1.25} alignItems="flex-start">
                      <Box sx={{ width: 44, height: 44, borderRadius: "50%", display: "grid", placeItems: "center", bgcolor: "rgba(31, 94, 79, 0.08)", color: "primary.main", flexShrink: 0 }}>
                        <CampaignOutlinedIcon />
                      </Box>
                      <Box>
                        <Typography fontWeight={900} color="primary.dark">รูปแบบการแสดงผลประกาศ</Typography>
                        <Typography color="text.secondary">
                          กำหนดว่าประกาศนี้ควรแสดงแบบใดบนหน้าเว็บ และต้องให้ผู้รับกดรับทราบหรือไม่
                        </Typography>
                      </Box>
                    </Stack>
                    <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", sm: "repeat(2, minmax(0, 1fr))", lg: "repeat(4, minmax(0, 1fr))" }, gap: 1.5 }}>
                      <AnnouncementOptionTile
                        checked={form.isFeatured}
                        onChange={(checked) => update("isFeatured", checked)}
                        icon={<PushPinOutlinedIcon />}
                        title="ปักหมุด / ประกาศเด่น"
                        description="แสดงให้เห็นเด่นในศูนย์ประกาศ"
                      />
                      <AnnouncementOptionTile
                        checked={form.showAsPopup}
                        onChange={(checked) => update("showAsPopup", checked)}
                        icon={<CampaignOutlinedIcon />}
                        title="แสดง Popup"
                        description="แจ้งเตือนแบบหน้าต่างเมื่อเข้าใช้งาน"
                      />
                      <AnnouncementOptionTile
                        checked={form.showAsBanner}
                        onChange={(checked) => update("showAsBanner", checked)}
                        icon={<CampaignOutlinedIcon />}
                        title="แสดง Banner"
                        description="แสดงแถบประกาศในหน้า Dashboard"
                      />
                      <AnnouncementOptionTile
                        checked={form.requiresAcknowledgement}
                        onChange={(checked) => update("requiresAcknowledgement", checked)}
                        icon={<TaskAltOutlinedIcon />}
                        title="ต้องกดรับทราบ"
                        description="ใช้ติดตามจำนวนผู้รับทราบประกาศ"
                      />
                    </Box>
                  </Stack>
                </CardContent>
              </Card>
              <Card variant="outlined" sx={{ borderRadius: 3, bgcolor: "rgba(31, 94, 79, 0.035)" }}>
                <CardContent>
                  <Stack spacing={2}>
                    <Stack direction="row" spacing={1.25} alignItems="flex-start">
                      <Box sx={{ width: 44, height: 44, borderRadius: "50%", display: "grid", placeItems: "center", bgcolor: "rgba(31, 94, 79, 0.08)", color: "primary.main", flexShrink: 0 }}>
                        <NotificationsActiveOutlinedIcon />
                      </Box>
                      <Box>
                        <Typography fontWeight={900} color="primary.dark">ช่องทางแจ้งเตือนประกาศ</Typography>
                        <Typography color="text.secondary">
                          ประกาศยังแสดงในศูนย์ประกาศเสมอ ตัวเลือกนี้ใช้กำหนดว่าจะสร้างแจ้งเตือนเพิ่มเติมให้ผู้รับหรือไม่
                        </Typography>
                      </Box>
                    </Stack>
                    <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", md: "repeat(2, minmax(0, 1fr))" }, gap: 1.5 }}>
                      <AnnouncementOptionTile
                        checked={form.notifyInApp}
                        onChange={(checked) => update("notifyInApp", checked)}
                        icon={<NotificationsActiveOutlinedIcon />}
                        title="Notification Bell"
                        description="สร้างรายการแจ้งเตือนในระบบให้ผู้รับ"
                      />
                      <AnnouncementOptionTile
                        checked={form.notifyViaLine}
                        onChange={(checked) => update("notifyViaLine", checked)}
                        icon={<NotificationsActiveOutlinedIcon />}
                        title="LINE Official Account"
                        description="ส่ง Flex Card ประกาศไปยัง LINE ของผู้รับ"
                      />
                    </Box>
                    {form.priority === "Critical" && !form.notifyViaLine && (
                      <Alert severity="warning">ประกาศเร่งด่วนควรเปิด LINE ด้วย เพื่อให้ผู้รับเห็นเร็วขึ้น</Alert>
                    )}
                    {!form.notifyInApp && !form.notifyViaLine && (
                      <Alert severity="info">ประกาศนี้จะไม่ส่งแจ้งเตือนเพิ่มเติม แต่ยังเปิดอ่านได้จากศูนย์ประกาศและหน้า Dashboard</Alert>
                    )}
                  </Stack>
                </CardContent>
              </Card>
              <Box sx={{ display: "flex", justifyContent: "flex-end" }}>
                <Button variant="contained" startIcon={<SaveOutlinedIcon />} disabled={saveMutation.isPending} onClick={submit}>
                  บันทึกข้อมูล
                </Button>
              </Box>
            </Stack>
          </CardContent>
        </Card>
      </Stack>
    </Box>
  );
}

function AnnouncementOptionTile({
  checked,
  onChange,
  icon,
  title,
  description,
}: {
  checked: boolean;
  onChange: (checked: boolean) => void;
  icon: ReactNode;
  title: string;
  description: string;
}) {
  return (
    <Box
      component="label"
      sx={{
        display: "flex",
        alignItems: "flex-start",
        gap: 1.25,
        minHeight: 96,
        border: `1px solid ${checked ? brandColors.primary : brandColors.border}`,
        borderRadius: 2.5,
        px: 1.75,
        py: 1.5,
        bgcolor: checked ? "rgba(31, 94, 79, 0.06)" : "#fff",
        cursor: "pointer",
        transition: "border-color 160ms ease, background-color 160ms ease, box-shadow 160ms ease",
        "&:hover": {
          borderColor: brandColors.accent,
          boxShadow: "0 10px 24px rgba(20, 67, 56, 0.08)",
        },
      }}
    >
      <Checkbox
        checked={checked}
        onChange={(event) => onChange(event.target.checked)}
        sx={{ p: 0.25, mt: 0.1, color: brandColors.primary }}
      />
      <Box sx={{ width: 34, height: 34, borderRadius: "50%", display: "grid", placeItems: "center", bgcolor: checked ? "rgba(31, 94, 79, 0.1)" : "rgba(228, 224, 215, 0.45)", color: checked ? "primary.main" : "text.secondary", flexShrink: 0 }}>
        {icon}
      </Box>
      <Box sx={{ minWidth: 0 }}>
        <Typography fontWeight={900} sx={{ lineHeight: 1.25 }}>
          {title}
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mt: 0.35, lineHeight: 1.45 }}>
          {description}
        </Typography>
      </Box>
    </Box>
  );
}

function AnnouncementMediaSection({
  existingImages,
  existingFiles,
  coverFile,
  coverPreviewUrl,
  galleryFiles,
  galleryPreviewUrls,
  attachmentFiles,
  onCoverChange,
  onGalleryChange,
  onAttachmentsChange,
  onRemoveExistingImage,
  onRemoveExistingFile,
  onMoveExistingImage,
}: {
  existingImages: AnnouncementImage[];
  existingFiles: AnnouncementFile[];
  coverFile: File | null;
  coverPreviewUrl: string | null;
  galleryFiles: File[];
  galleryPreviewUrls: string[];
  attachmentFiles: File[];
  onCoverChange: (file: File | null) => void;
  onGalleryChange: (files: File[]) => void;
  onAttachmentsChange: (files: File[]) => void;
  onRemoveExistingImage: (image: AnnouncementImage) => void;
  onRemoveExistingFile: (file: AnnouncementFile) => void;
  onMoveExistingImage: (imageId: string, direction: -1 | 1) => void;
}) {
  const coverImage = existingImages.find((image) => image.isCover);
  const galleryImages = existingImages.filter((image) => !image.isCover);
  const [preview, setPreview] = useState<{ src: string; label: string } | null>(null);

  return (
    <Card variant="outlined" sx={{ borderRadius: 3, bgcolor: "rgba(250, 248, 242, 0.55)" }}>
      <CardContent>
        <Stack spacing={2.5}>
          <Box>
            <Typography variant="h6" fontWeight={900} color="primary.dark">รูปภาพและไฟล์แนบ</Typography>
            <Typography color="text.secondary">รองรับ JPG, PNG, WebP และไฟล์เอกสาร ขนาดไม่เกิน 10 MB ต่อไฟล์ สามารถกดเลือกไฟล์หรือลากไฟล์มาวางในกรอบได้</Typography>
          </Box>

          <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", md: "1.1fr 1fr" }, gap: 2 }}>
            <UploadPanel
              title="รูปหน้าปก"
              helper="ใช้แสดงใน Card, Dashboard และหน้ารายละเอียด"
              icon={<ImageOutlinedIcon />}
              accept="image/jpeg,image/png,image/webp"
              multiple={false}
              onFiles={(files) => onCoverChange(files[0] ?? null)}
            >
              {(coverPreviewUrl || coverImage) ? (
                <ExistingCoverPreview
                  coverPreviewUrl={coverPreviewUrl}
                  coverImage={coverImage}
                  onCoverChange={onCoverChange}
                  onRemoveExistingImage={onRemoveExistingImage}
                />
              ) : (
                <Typography color="text.secondary">ยังไม่มีรูปหน้าปก</Typography>
              )}
            </UploadPanel>

            <UploadPanel
              title="ไฟล์แนบ"
              helper="รองรับ PDF, DOCX, XLSX, PPTX, ZIP"
              icon={<AttachFileOutlinedIcon />}
              accept=".pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.zip"
              multiple
              onFiles={(files) => onAttachmentsChange([...attachmentFiles, ...files])}
            >
              <Stack spacing={1}>
                {existingFiles.map((file) => (
                  <MediaListItem key={file.id} label={file.originalFileName} helper={formatFileSize(file.fileSize)} onRemove={() => onRemoveExistingFile(file)} />
                ))}
                {attachmentFiles.map((file, index) => (
                  <MediaListItem key={`${file.name}-${index}`} label={file.name} helper={formatFileSize(file.size)} onRemove={() => onAttachmentsChange(attachmentFiles.filter((_, itemIndex) => itemIndex !== index))} />
                ))}
                {existingFiles.length === 0 && attachmentFiles.length === 0 && <Typography color="text.secondary">ยังไม่มีไฟล์แนบ</Typography>}
              </Stack>
            </UploadPanel>
          </Box>

          <UploadPanel
            title="Gallery Images"
            helper="อัปโหลดได้หลายรูปสำหรับแสดงในหน้ารายละเอียด"
            icon={<ImageOutlinedIcon />}
            accept="image/jpeg,image/png,image/webp"
            multiple
            onFiles={(files) => onGalleryChange([...galleryFiles, ...files])}
          >
            <Box sx={{ display: "grid", gridTemplateColumns: { xs: "repeat(2, minmax(0, 1fr))", md: "repeat(4, minmax(0, 1fr))" }, gap: 1.5 }}>
              {galleryImages.map((image, index) => (
                <GalleryPreview
                  key={image.id}
                  protectedSrc={image.thumbnailUrl}
                  label={`รูปที่ ${image.displayOrder}`}
                  onRemove={() => onRemoveExistingImage(image)}
                  onPreview={(src) => setPreview({ src, label: `รูปที่ ${image.displayOrder}` })}
                  onMoveLeft={index === 0 ? undefined : () => onMoveExistingImage(image.id, -1)}
                  onMoveRight={index === galleryImages.length - 1 ? undefined : () => onMoveExistingImage(image.id, 1)}
                />
              ))}
              {galleryPreviewUrls.map((url, index) => (
                <GalleryPreview key={url} src={url} label={galleryFiles[index]?.name ?? `รูปใหม่ ${index + 1}`} onPreview={(src) => setPreview({ src, label: galleryFiles[index]?.name ?? `รูปใหม่ ${index + 1}` })} onRemove={() => onGalleryChange(galleryFiles.filter((_, itemIndex) => itemIndex !== index))} />
              ))}
            </Box>
            {galleryImages.length === 0 && galleryFiles.length === 0 && <Typography color="text.secondary">ยังไม่มีรูป Gallery</Typography>}
          </UploadPanel>
          <Dialog open={Boolean(preview)} onClose={() => setPreview(null)} maxWidth="md" fullWidth>
            <DialogContent sx={{ p: 2 }}>
              {preview && (
                <Stack spacing={1.5}>
                  <Typography fontWeight={900}>{preview.label}</Typography>
                  <Box component="img" src={preview.src} alt={preview.label} sx={{ width: "100%", maxHeight: "75vh", objectFit: "contain", borderRadius: 2, bgcolor: "#111" }} />
                </Stack>
              )}
            </DialogContent>
          </Dialog>
        </Stack>
      </CardContent>
    </Card>
  );
}

function UploadPanel({
  title,
  helper,
  icon,
  accept,
  multiple,
  onFiles,
  children,
}: {
  title: string;
  helper: string;
  icon: ReactNode;
  accept: string;
  multiple?: boolean;
  onFiles: (files: File[]) => void;
  children: ReactNode;
}) {
  const [dragging, setDragging] = useState(false);
  return (
    <Box
      onDragOver={(event) => { event.preventDefault(); setDragging(true); }}
      onDragLeave={() => setDragging(false)}
      onDrop={(event) => {
        event.preventDefault();
        setDragging(false);
        onFiles(Array.from(event.dataTransfer.files));
      }}
      sx={{
        border: `1px dashed ${dragging ? brandColors.accent : brandColors.border}`,
        borderRadius: 3,
        p: 2,
        bgcolor: dragging ? "rgba(200, 169, 107, 0.12)" : "#fff",
      }}
    >
      <Stack spacing={1.5}>
        <Stack direction="row" spacing={1.25} alignItems="flex-start">
          <Box sx={{ width: 38, height: 38, borderRadius: "50%", display: "grid", placeItems: "center", color: "primary.main", bgcolor: "rgba(31, 94, 79, 0.08)", flexShrink: 0 }}>{icon}</Box>
          <Box sx={{ minWidth: 0 }}>
            <Typography fontWeight={900}>{title}</Typography>
            <Typography variant="body2" color="text.secondary">{helper}</Typography>
            <Typography variant="caption" color="text.secondary">ลากไฟล์มาวางในกรอบนี้ หรือกดปุ่ม “เลือกไฟล์”</Typography>
          </Box>
        </Stack>
        <Button component="label" variant="outlined" size="small" sx={{ alignSelf: "flex-start" }}>
          เลือกไฟล์
          <input hidden type="file" accept={accept} multiple={multiple} onChange={(event) => onFiles(Array.from(event.target.files ?? []))} />
        </Button>
        {children}
      </Stack>
    </Box>
  );
}

function ExistingCoverPreview({
  coverPreviewUrl,
  coverImage,
  onCoverChange,
  onRemoveExistingImage,
}: {
  coverPreviewUrl: string | null;
  coverImage?: AnnouncementImage;
  onCoverChange: (file: File | null) => void;
  onRemoveExistingImage: (image: AnnouncementImage) => void;
}) {
  const { mediaUrl } = useAuthenticatedMediaUrl(coverPreviewUrl ? null : coverImage?.mediumUrl);
  const imageUrl = coverPreviewUrl ?? mediaUrl;

  return (
    <Box sx={{ position: "relative" }}>
      {imageUrl ? (
        <Box
          component="img"
          src={imageUrl}
          alt="รูปหน้าปกประกาศ"
          sx={{ width: "100%", aspectRatio: "16 / 9", objectFit: "cover", borderRadius: 2, border: `1px solid ${brandColors.border}` }}
        />
      ) : (
        <Box sx={{ width: "100%", aspectRatio: "16 / 9", borderRadius: 2, bgcolor: "rgba(31, 94, 79, 0.08)", border: `1px solid ${brandColors.border}` }} />
      )}
      <Button
        size="small"
        color="error"
        variant="outlined"
        startIcon={<DeleteOutlineOutlinedIcon />}
        onClick={() => coverPreviewUrl ? onCoverChange(null) : coverImage && onRemoveExistingImage(coverImage)}
        sx={{ mt: 1 }}
      >
        ลบรูปหน้าปก
      </Button>
    </Box>
  );
}

function GalleryPreview({
  src,
  protectedSrc,
  label,
  onRemove,
  onPreview,
  onMoveLeft,
  onMoveRight,
}: {
  src?: string;
  protectedSrc?: string;
  label: string;
  onRemove: () => void;
  onPreview?: (src: string) => void;
  onMoveLeft?: () => void;
  onMoveRight?: () => void;
}) {
  const { mediaUrl } = useAuthenticatedMediaUrl(src ? null : protectedSrc);
  const imageUrl = src ?? mediaUrl;
  return (
    <Box>
      {imageUrl ? (
        <Box
          component="img"
          src={imageUrl}
          alt={label}
          onClick={() => onPreview?.(imageUrl)}
          sx={{ width: "100%", aspectRatio: "16 / 9", objectFit: "cover", borderRadius: 2, border: `1px solid ${brandColors.border}`, cursor: onPreview ? "zoom-in" : "default" }}
        />
      ) : (
        <Box sx={{ width: "100%", aspectRatio: "16 / 9", borderRadius: 2, bgcolor: "rgba(31, 94, 79, 0.08)", border: `1px solid ${brandColors.border}` }} />
      )}
      <Stack direction="row" spacing={0.5} alignItems="center" justifyContent="space-between" sx={{ mt: 0.75 }}>
        <Typography variant="caption" noWrap>{label}</Typography>
        <Stack direction="row" spacing={0.5}>
          {onMoveLeft && <Button size="small" onClick={onMoveLeft}>ก่อน</Button>}
          {onMoveRight && <Button size="small" onClick={onMoveRight}>หลัง</Button>}
          <Button size="small" color="error" onClick={onRemove}>ลบ</Button>
        </Stack>
      </Stack>
    </Box>
  );
}

function MediaListItem({ label, helper, onRemove }: { label: string; helper: string; onRemove: () => void }) {
  return (
    <Stack direction="row" spacing={1} alignItems="center" justifyContent="space-between" sx={{ border: `1px solid ${brandColors.border}`, borderRadius: 2, px: 1.25, py: 0.9 }}>
      <Box sx={{ minWidth: 0 }}>
        <Typography fontWeight={800} noWrap>{label}</Typography>
        <Typography variant="caption" color="text.secondary">{helper}</Typography>
      </Box>
      <Button size="small" color="error" onClick={onRemove}>ลบ</Button>
    </Stack>
  );
}

function formatFileSize(size: number) {
  if (size >= 1024 * 1024) return `${(size / 1024 / 1024).toFixed(1)} MB`;
  if (size >= 1024) return `${(size / 1024).toFixed(1)} KB`;
  return `${size} bytes`;
}

function toInputDateTime(value?: string | null) {
  if (!value) return "";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "";
  return date.toISOString().slice(0, 16);
}

function fromInputDateTime(value: string) {
  return value ? new Date(value).toISOString() : null;
}

type TargetOption = {
  value: string;
  label: string;
  helper?: string;
};

function AnnouncementTargetPicker({
  targetType,
  targetValues,
  onChange,
  departments,
  users,
  roles,
  permissions,
}: {
  targetType: TargetType;
  targetValues: string[];
  onChange: (values: string[]) => void;
  departments: DepartmentSummary[];
  users: UserSummary[];
  roles: RoleSummary[];
  permissions: PermissionSummary[];
}) {
  const options = buildTargetOptions(targetType, departments, users, roles, permissions);
  const selectedOptions = targetValues.map((value) => options.find((option) => option.value === value) ?? { value, label: value });

  if (targetType === "Everyone") {
    return (
      <TextField
        disabled
        label="ค่าเป้าหมาย"
        value="ประกาศนี้จะแสดงให้ผู้ใช้งานทุกคน"
        helperText="ไม่ต้องเลือกค่าเพิ่มเติมเมื่อกลุ่มเป้าหมายเป็นทุกคน"
      />
    );
  }

  return (
    <Autocomplete
      multiple
      disableCloseOnSelect
      options={options}
      value={selectedOptions}
      isOptionEqualToValue={(option, value) => option.value === value.value}
      getOptionLabel={(option) => option.label}
      onChange={(_, values) => onChange(values.map((item) => item.value))}
      renderTags={(values, getTagProps) =>
        values.map((option, index) => {
          const tagProps = getTagProps({ index });
          return <Chip {...tagProps} key={option.value} label={option.label} size="small" />;
        })
      }
      renderOption={(props, option, { selected }) => (
        <Box component="li" {...props} key={option.value} sx={{ alignItems: "flex-start !important", gap: 1 }}>
          <Checkbox checked={selected} sx={{ p: 0.25, mt: 0.25 }} />
          <Box>
            <Box sx={{ fontWeight: 700 }}>{option.label}</Box>
            {option.helper && <Box sx={{ color: "text.secondary", fontSize: 13 }}>{option.helper}</Box>}
          </Box>
        </Box>
      )}
      renderInput={(params) => (
        <TextField
          {...params}
          label={getTargetPickerLabel(targetType)}
          placeholder={getTargetPickerPlaceholder(targetType)}
          helperText={getTargetPickerHelper(targetType)}
        />
      )}
    />
  );
}

function buildTargetOptions(
  targetType: TargetType,
  departments: DepartmentSummary[],
  users: UserSummary[],
  roles: RoleSummary[],
  permissions: PermissionSummary[],
): TargetOption[] {
  switch (targetType) {
    case "Department":
      return departments
        .filter((item) => item.isActive)
        .map((item) => ({
          value: item.id,
          label: item.name,
          helper: `${item.usersCount.toLocaleString("th-TH")} ผู้ใช้`,
        }))
        .sort(compareOptionLabel);
    case "User":
      return users
        .filter((item) => item.isActive)
        .map((item) => ({
          value: item.id,
          label: item.fullname || item.username,
          helper: [item.username, item.department, item.roles.join(", ")].filter(Boolean).join(" · "),
        }))
        .sort(compareOptionLabel);
    case "Role":
      return roles
        .filter((item) => item.isActive)
        .map((item) => ({
          value: item.name,
          label: translateRoleName(item.name),
          helper: `${item.name} · ${item.usersCount.toLocaleString("th-TH")} ผู้ใช้`,
        }))
        .sort(compareOptionLabel);
    case "Permission":
      return permissions
        .filter((item) => item.isActive)
        .map((item) => ({
          value: item.code,
          label: item.code,
          helper: `${item.name} · ${item.group}`,
        }))
        .sort(compareOptionLabel);
    default:
      return [];
  }
}

function buildTargetPayload(targetType: TargetType, targetValues: string[]) {
  if (targetType === "Everyone") {
    return [{ targetType: "Everyone", targetValue: null }];
  }

  return Array.from(new Set(targetValues.filter(Boolean))).map((targetValue) => ({
    targetType,
    targetValue,
  }));
}

function compareOptionLabel(left: TargetOption, right: TargetOption) {
  return left.label.localeCompare(right.label, "th");
}

function getTargetPickerLabel(targetType: TargetType) {
  switch (targetType) {
    case "Department":
      return "เลือกหน่วยงาน";
    case "User":
      return "เลือกผู้ใช้";
    case "Role":
      return "เลือกบทบาท";
    case "Permission":
      return "เลือกสิทธิ์";
    default:
      return "ค่าเป้าหมาย";
  }
}

function getTargetPickerPlaceholder(targetType: TargetType) {
  switch (targetType) {
    case "Department":
      return "ค้นหาและเลือกได้หลายหน่วยงาน";
    case "User":
      return "ค้นหาชื่อ Username หรือหน่วยงาน";
    case "Role":
      return "เลือกบทบาท เช่น Staff, Head, Admin, Director";
    case "Permission":
      return "ค้นหารหัสสิทธิ์ เช่น LeaveRequest.ViewOwn";
    default:
      return "";
  }
}

function getTargetPickerHelper(targetType: TargetType) {
  switch (targetType) {
    case "Department":
      return "เลือกได้หลายหน่วยงาน ระบบจะส่งประกาศให้ผู้ใช้ในหน่วยงานที่เลือก";
    case "User":
      return "เลือกบุคคลจากผู้ใช้งานทั้งหมดในระบบได้หลายคน";
    case "Role":
      return "เลือกบทบาทได้หลายบทบาท เช่น เจ้าหน้าที่ หัวหน้างาน ผู้ดูแลระบบ ผู้อำนวยการ";
    case "Permission":
      return "เลือกสิทธิ์ได้หลายรายการ สำหรับประกาศเฉพาะผู้มีสิทธิ์นั้น";
    default:
      return "";
  }
}

function translateRoleName(roleName: string) {
  switch (roleName) {
    case "Staff":
      return "เจ้าหน้าที่";
    case "DepartmentHead":
      return "หัวหน้างาน";
    case "Director":
      return "ผู้อำนวยการ";
    case "Admin":
      return "ผู้ดูแลระบบ";
    case "SuperAdmin":
      return "ผู้ดูแลระบบสูงสุด";
    case "LeaveAdmin":
      return "เจ้าหน้าที่ระบบลา";
    default:
      return roleName;
  }
}
