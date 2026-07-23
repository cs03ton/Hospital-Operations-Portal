import ArrowBackOutlinedIcon from "@mui/icons-material/ArrowBackOutlined";
import AttachFileOutlinedIcon from "@mui/icons-material/AttachFileOutlined";
import CheckCircleOutlinedIcon from "@mui/icons-material/CheckCircleOutlined";
import CampaignOutlinedIcon from "@mui/icons-material/CampaignOutlined";
import DownloadOutlinedIcon from "@mui/icons-material/DownloadOutlined";
import { Box, Button, Card, CardContent, CardMedia, Dialog, DialogContent, Divider, Stack, Typography } from "@mui/material";
import { alpha, useTheme } from "@mui/material/styles";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { Link as RouterLink, useLocation, useParams } from "react-router-dom";
import { acknowledgeAnnouncement, getAdminAnnouncementDetail, getAnnouncementDetail } from "../api/announcementsApi";
import type { AnnouncementDetail, AnnouncementFile } from "../api/announcementsApi";
import { httpClient } from "../api/httpClient";
import { LoadingState } from "../components/common/LoadingState";
import { StatusBadge } from "../components/common/StatusBadge";
import { PageHeader } from "../components/PageHeader";
import { useAuthenticatedMediaUrl } from "../hooks/useAuthenticatedMediaUrl";
import { useNotification } from "../hooks/useNotification";
import { brandColors } from "../theme/theme";
import { formatThaiDateTime } from "../utils/dateFormat";

export function AnnouncementDetailPage() {
  const { id } = useParams();
  const location = useLocation();
  const isAdminPreview = location.pathname.startsWith("/admin/announcements/");
  const theme = useTheme();
  const notify = useNotification();
  const queryClient = useQueryClient();
  const [previewImage, setPreviewImage] = useState<{ src: string; title: string } | null>(null);
  const { data, isLoading } = useQuery({
    queryKey: [isAdminPreview ? "admin" : "announcements", "detail", id],
    queryFn: () => isAdminPreview ? getAdminAnnouncementDetail(id ?? "") : getAnnouncementDetail(id ?? ""),
    enabled: Boolean(id),
  });

  const acknowledgeMutation = useMutation({
    mutationFn: () => acknowledgeAnnouncement(id ?? ""),
    onSuccess: async () => {
      notify.showSuccess("รับทราบประกาศเรียบร้อยแล้ว");
      await queryClient.invalidateQueries({ queryKey: ["announcements"] });
    },
    onError: () => notify.showError("ไม่สามารถรับทราบประกาศได้"),
  });

  if (isLoading || !data) {
    return <LoadingState message="กำลังโหลดประกาศ..." />;
  }
  const galleryImages = data.images.filter((image) => !image.isCover);

  return (
    <Box sx={{ maxWidth: 1120, mx: "auto" }}>
      <Stack spacing={3}>
        <PageHeader
          title={isAdminPreview ? "ตัวอย่างประกาศ" : "รายละเอียดประกาศ"}
          subtitle={isAdminPreview ? "ตรวจสอบเนื้อหา สถานะ และกลุ่มเป้าหมายของประกาศ" : "อ่านข่าวสารและบันทึกการรับทราบประกาศ"}
        />
        <Button component={RouterLink} to={isAdminPreview ? "/admin/announcements" : "/announcements"} variant="outlined" startIcon={<ArrowBackOutlinedIcon />} sx={{ alignSelf: "flex-start" }}>
          {isAdminPreview ? "กลับหน้าจัดการประกาศ" : "กลับศูนย์ประกาศ"}
        </Button>

        <Card sx={{ border: `1px solid ${brandColors.border}`, borderTop: `5px solid ${data.priority === "Critical" ? theme.palette.error.main : brandColors.accent}`, borderRadius: 3, boxShadow: `0 14px 34px ${alpha(theme.palette.primary.dark, 0.06)}` }}>
          <AnnouncementCoverImage data={data} />
          <CardContent sx={{ p: { xs: 2.5, md: 4 } }}>
            <Stack spacing={3}>
              <Stack direction={{ xs: "column", sm: "row" }} spacing={2} alignItems={{ xs: "flex-start", sm: "center" }}>
                <Box sx={{ width: 56, height: 56, borderRadius: "50%", display: "grid", placeItems: "center", bgcolor: alpha(theme.palette.primary.main, 0.08), color: "primary.main", flexShrink: 0 }}>
                  <CampaignOutlinedIcon />
                </Box>
                <Box sx={{ minWidth: 0 }}>
                  <Typography variant="h4" fontWeight={900} color="primary.dark">{data.title}</Typography>
                  <Typography color="text.secondary" sx={{ mt: 0.5 }}>{data.summary}</Typography>
                </Box>
              </Stack>

              <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                <StatusBadge domain="announcementPriority" status={data.priority} />
                <StatusBadge domain="announcement" status={data.status} />
                {data.category && <StatusBadge domain="active" status="active" label={data.category.name} />}
                {data.isFeatured && <StatusBadge domain="active" status="active" label="📌 ปักหมุด" />}
                {data.requiresAcknowledgement && <StatusBadge domain="notificationType" status={data.isAcknowledged ? "Information" : "ActionRequired"} label={data.isAcknowledged ? "รับทราบแล้ว" : "ต้องรับทราบ"} />}
              </Stack>

              {(data.tags || data.viewCount > 0 || data.requiresAcknowledgement) && (
                <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                  {data.tags && splitTags(data.tags).map((tag) => (
                    <Box key={tag} component="span" sx={{ px: 1.25, py: 0.4, borderRadius: 999, bgcolor: alpha(theme.palette.primary.main, 0.08), color: "text.secondary", fontWeight: 800, fontSize: 13 }}>
                      🏷️ {tag}
                    </Box>
                  ))}
                  <Box component="span" sx={{ px: 1.25, py: 0.4, borderRadius: 999, bgcolor: alpha(theme.palette.primary.main, 0.08), color: "text.secondary", fontWeight: 800, fontSize: 13 }}>
                    👁️ เข้าชม {data.viewCount.toLocaleString("th-TH")} ครั้ง
                  </Box>
                  {data.requiresAcknowledgement && (
                    <Box component="span" sx={{ px: 1.25, py: 0.4, borderRadius: 999, bgcolor: alpha(theme.palette.success.main, 0.12), color: "success.dark", fontWeight: 800, fontSize: 13 }}>
                      ✅ รับทราบแล้ว {data.acknowledgedCount.toLocaleString("th-TH")} คน
                    </Box>
                  )}
                </Stack>
              )}

              <Typography variant="body2" color="text.secondary">
                เผยแพร่ {formatThaiDateTime(data.publishedAt ?? data.publishAt ?? data.createdAt)}
                {data.createdByName ? ` โดย ${data.createdByName}` : ""}
              </Typography>

              <Divider />

              <Typography sx={{ whiteSpace: "pre-wrap", lineHeight: 1.9, fontSize: 18 }}>{data.body}</Typography>

              {galleryImages.length > 0 && (
                <>
                  <Divider />
                  <Box>
                    <Typography variant="h6" fontWeight={900} color="primary.dark" sx={{ mb: 1.5 }}>รูปภาพประกอบ</Typography>
                    <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", sm: "repeat(2, minmax(0, 1fr))", md: "repeat(3, minmax(0, 1fr))" }, gap: 1.5 }}>
                      {galleryImages.map((image) => (
                        <AnnouncementGalleryImage
                          key={image.id}
                          image={image}
                          title={data.title}
                          onPreview={(src) => setPreviewImage({ src, title: `${data.title} รูปที่ ${image.displayOrder}` })}
                        />
                      ))}
                    </Box>
                  </Box>
                </>
              )}

              {data.files.length > 0 && (
                <>
                  <Divider />
                  <Box>
                    <Typography variant="h6" fontWeight={900} color="primary.dark" sx={{ mb: 1.5 }}>ไฟล์แนบ</Typography>
                    <Stack spacing={1}>
                      {data.files.map((file) => (
                        <Stack key={file.id} direction={{ xs: "column", sm: "row" }} spacing={1.5} alignItems={{ xs: "stretch", sm: "center" }} justifyContent="space-between" sx={{ p: 1.5, border: `1px solid ${brandColors.border}`, borderRadius: 2 }}>
                          <Stack direction="row" spacing={1.25} alignItems="center" sx={{ minWidth: 0 }}>
                            <Box sx={{ width: 36, height: 36, borderRadius: "50%", display: "grid", placeItems: "center", bgcolor: alpha(theme.palette.primary.main, 0.08), color: "primary.main", flexShrink: 0 }}>
                              <AttachFileOutlinedIcon />
                            </Box>
                            <Box sx={{ minWidth: 0 }}>
                              <Typography fontWeight={900} noWrap>{file.originalFileName}</Typography>
                              <Typography variant="caption" color="text.secondary">{formatFileSize(file.fileSize)}</Typography>
                            </Box>
                          </Stack>
                          <Button variant="outlined" startIcon={<DownloadOutlinedIcon />} onClick={() => downloadProtectedFile(file)}>
                            ดาวน์โหลด
                          </Button>
                        </Stack>
                      ))}
                    </Stack>
                  </Box>
                </>
              )}

              {data.targets.length > 0 && (
                <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                  {data.targets.map((target) => (
                    <StatusBadge
                      key={target.id ?? `${target.targetType}-${target.targetValue ?? "all"}`}
                      domain="active"
                      status="active"
                      label={`${translateTargetType(target.targetType)}${target.targetValue ? `: ${target.targetValue}` : ""}`}
                    />
                  ))}
                </Stack>
              )}

              {data.requiresAcknowledgement && !isAdminPreview && (
                <Box sx={{ display: "flex", justifyContent: "flex-end", pt: 1 }}>
                  <Button
                    variant="contained"
                    startIcon={<CheckCircleOutlinedIcon />}
                    disabled={data.isAcknowledged || acknowledgeMutation.isPending}
                    onClick={() => acknowledgeMutation.mutate()}
                  >
                    {data.isAcknowledged ? "รับทราบแล้ว" : "รับทราบประกาศ"}
                  </Button>
                </Box>
              )}
            </Stack>
          </CardContent>
        </Card>
        <Dialog open={Boolean(previewImage)} onClose={() => setPreviewImage(null)} maxWidth="lg" fullWidth>
          <DialogContent sx={{ p: { xs: 1.5, md: 2.5 }, bgcolor: "#111" }}>
            {previewImage && (
              <Stack spacing={1.5}>
                <Typography color="#fff" fontWeight={900}>{previewImage.title}</Typography>
                <Box
                  component="img"
                  src={previewImage.src}
                  alt={previewImage.title}
                  sx={{ width: "100%", maxHeight: "78vh", objectFit: "contain", borderRadius: 2, display: "block" }}
                />
              </Stack>
            )}
          </DialogContent>
        </Dialog>
      </Stack>
    </Box>
  );
}

function splitTags(tags: string) {
  return tags.split(",").map((tag) => tag.trim()).filter(Boolean);
}

function AnnouncementCoverImage({ data }: { data: AnnouncementDetail }) {
  const protectedCoverUrl = data.coverImage?.largeUrl;
  const { mediaUrl: authenticatedCoverUrl } = useAuthenticatedMediaUrl(protectedCoverUrl);
  const coverUrl = authenticatedCoverUrl ?? (!protectedCoverUrl ? data.legacyCoverImageUrl : null);

  if (!coverUrl) return null;

  return (
    <CardMedia
      component="img"
      image={coverUrl}
      alt={data.title}
      sx={{ aspectRatio: { xs: "16 / 10", md: "21 / 9" }, objectFit: "cover", borderBottom: `1px solid ${brandColors.border}` }}
    />
  );
}

function AnnouncementGalleryImage({
  image,
  title,
  onPreview,
}: {
  image: { id: string; thumbnailUrl: string; largeUrl: string; displayOrder: number };
  title: string;
  onPreview: (src: string) => void;
}) {
  const { mediaUrl: thumbnailUrl } = useAuthenticatedMediaUrl(image.thumbnailUrl);
  const { mediaUrl: largeUrl } = useAuthenticatedMediaUrl(image.largeUrl);
  const previewUrl = largeUrl ?? thumbnailUrl;

  return (
    <Box
      component="button"
      type="button"
      disabled={!previewUrl}
      onClick={() => previewUrl && onPreview(previewUrl)}
      sx={{
        display: "block",
        width: "100%",
        p: 0,
        border: `1px solid ${brandColors.border}`,
        borderRadius: 2,
        overflow: "hidden",
        bgcolor: "transparent",
        cursor: previewUrl ? "zoom-in" : "default",
        textAlign: "left",
      }}
    >
      {thumbnailUrl ? (
        <Box component="img" src={thumbnailUrl} alt={`${title} รูปที่ ${image.displayOrder}`} loading="lazy" sx={{ width: "100%", aspectRatio: "16 / 9", objectFit: "cover", display: "block" }} />
      ) : (
        <Box sx={{ width: "100%", aspectRatio: "16 / 9", bgcolor: "rgba(31, 94, 79, 0.08)" }} />
      )}
    </Box>
  );
}

function formatFileSize(size: number) {
  if (size >= 1024 * 1024) return `${(size / 1024 / 1024).toFixed(1)} MB`;
  if (size >= 1024) return `${(size / 1024).toFixed(1)} KB`;
  return `${size} bytes`;
}

async function downloadProtectedFile(file: AnnouncementFile) {
  const response = await httpClient.get<Blob>(file.downloadUrl, { responseType: "blob" });
  const objectUrl = URL.createObjectURL(response.data);
  const link = document.createElement("a");
  link.href = objectUrl;
  link.download = file.originalFileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(objectUrl);
}

function translateTargetType(targetType: string) {
  switch (targetType) {
    case "Everyone":
      return "ทุกคน";
    case "Department":
      return "หน่วยงาน";
    case "User":
      return "ผู้ใช้";
    case "Role":
      return "บทบาท";
    case "Permission":
      return "สิทธิ์";
    default:
      return targetType;
  }
}
