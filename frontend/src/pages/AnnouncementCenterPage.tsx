import CampaignOutlinedIcon from "@mui/icons-material/CampaignOutlined";
import SearchOutlinedIcon from "@mui/icons-material/SearchOutlined";
import VisibilityOutlinedIcon from "@mui/icons-material/VisibilityOutlined";
import {
  Box,
  Button,
  Card,
  CardContent,
  CardMedia,
  InputAdornment,
  MenuItem,
  Stack,
  TablePagination,
  TextField,
  Typography,
} from "@mui/material";
import { alpha, useTheme } from "@mui/material/styles";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import { getAnnouncementFeed } from "../api/announcementsApi";
import type { AnnouncementSummary } from "../api/announcementsApi";
import { EmptyState } from "../components/common/EmptyState";
import { LoadingState } from "../components/common/LoadingState";
import { StatusBadge } from "../components/common/StatusBadge";
import { PageHeader } from "../components/PageHeader";
import { useAuthenticatedMediaUrl } from "../hooks/useAuthenticatedMediaUrl";
import { brandColors } from "../theme/theme";
import { formatThaiDateTime } from "../utils/dateFormat";

const priorityOptions = [
  { value: "", label: "ทุกความสำคัญ" },
  { value: "Critical", label: "เร่งด่วน" },
  { value: "Important", label: "สำคัญ" },
  { value: "Normal", label: "ปกติ" },
];

export function AnnouncementCenterPage() {
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(12);
  const [priority, setPriority] = useState("");
  const [search, setSearch] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");

  const { data, isLoading } = useQuery({
    queryKey: ["announcements", "feed", page, pageSize, priority, appliedSearch],
    queryFn: () => getAnnouncementFeed({
      page: page + 1,
      pageSize,
      priority: priority || undefined,
      search: appliedSearch || undefined,
    }),
  });

  function applySearch() {
    setPage(0);
    setAppliedSearch(search.trim());
  }

  function resetFilters() {
    setPage(0);
    setPriority("");
    setSearch("");
    setAppliedSearch("");
  }

  if (isLoading && !data) {
    return <LoadingState message="กำลังโหลดประกาศ..." />;
  }

  return (
    <Box sx={{ maxWidth: 1440, mx: "auto" }}>
      <Stack spacing={3}>
        <PageHeader title="ศูนย์ข่าวสารและประกาศ" subtitle="ติดตามข่าวสารสำคัญ ประกาศภายใน และข้อมูลที่ต้องรับทราบ" />

        <Card sx={{ border: `1px solid ${brandColors.border}`, borderTop: `5px solid ${brandColors.accent}`, borderRadius: 3 }}>
          <CardContent>
            <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", md: "minmax(0, 1fr) 220px auto" }, gap: 2, alignItems: "center" }}>
              <TextField
                fullWidth
                label="ค้นหาประกาศ"
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                onKeyDown={(event) => {
                  if (event.key === "Enter") applySearch();
                }}
                InputProps={{ startAdornment: <InputAdornment position="start"><SearchOutlinedIcon /></InputAdornment> }}
              />
              <TextField select label="ความสำคัญ" value={priority} onChange={(event) => { setPage(0); setPriority(event.target.value); }}>
                {priorityOptions.map((option) => <MenuItem key={option.value} value={option.value}>{option.label}</MenuItem>)}
              </TextField>
              <Stack direction="row" spacing={1} justifyContent={{ xs: "stretch", md: "flex-end" }}>
                <Button variant="contained" onClick={applySearch}>ค้นหา</Button>
                <Button variant="outlined" onClick={resetFilters}>ล้าง</Button>
              </Stack>
            </Box>
          </CardContent>
        </Card>

        {!data?.items.length ? (
          <EmptyState message="ยังไม่มีประกาศที่เกี่ยวข้องกับคุณ" />
        ) : (
          <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", md: "repeat(2, minmax(0, 1fr))", xl: "repeat(3, minmax(0, 1fr))" }, gap: 2.5 }}>
            {data.items.map((item) => <AnnouncementCard key={item.id} item={item} />)}
          </Box>
        )}

        <Box sx={{ display: "flex", justifyContent: "flex-end" }}>
          <TablePagination
            component="div"
            count={data?.totalItems ?? 0}
            page={page}
            onPageChange={(_, nextPage) => setPage(nextPage)}
            rowsPerPage={pageSize}
            onRowsPerPageChange={(event) => {
              setPage(0);
              setPageSize(Number(event.target.value));
            }}
            rowsPerPageOptions={[6, 12, 24, 48]}
            labelRowsPerPage="จำนวนต่อหน้า"
          />
        </Box>
      </Stack>
    </Box>
  );
}

function AnnouncementCard({ item }: { item: AnnouncementSummary }) {
  const theme = useTheme();
  const protectedCoverUrl = item.coverImage?.thumbnailUrl;
  const { mediaUrl: authenticatedCoverUrl } = useAuthenticatedMediaUrl(protectedCoverUrl);
  const coverUrl = authenticatedCoverUrl ?? (!protectedCoverUrl ? item.legacyCoverImageUrl : null);
  return (
    <Card
      sx={{
        height: "100%",
        border: `1px solid ${brandColors.border}`,
        borderTop: `5px solid ${item.priority === "Critical" ? theme.palette.error.main : item.priority === "Important" ? brandColors.accent : theme.palette.primary.main}`,
        borderRadius: 3,
        boxShadow: `0 14px 34px ${alpha(theme.palette.primary.dark, 0.06)}`,
      }}
    >
      {coverUrl ? (
        <CardMedia
          component="img"
          image={coverUrl}
          alt={item.title}
          loading="lazy"
          sx={{ aspectRatio: "16 / 9", objectFit: "cover", borderBottom: `1px solid ${brandColors.border}` }}
        />
      ) : (
        <Box sx={{ aspectRatio: "16 / 9", display: "grid", placeItems: "center", bgcolor: alpha(theme.palette.primary.main, 0.08), color: "primary.main", borderBottom: `1px solid ${brandColors.border}` }}>
          <CampaignOutlinedIcon sx={{ fontSize: 48 }} />
        </Box>
      )}
      <CardContent sx={{ height: "100%" }}>
        <Stack spacing={2} sx={{ height: "100%" }}>
          <Stack direction="row" spacing={1.5} alignItems="flex-start">
            <Box sx={{ width: 48, height: 48, borderRadius: "50%", display: "grid", placeItems: "center", bgcolor: alpha(theme.palette.primary.main, 0.08), color: "primary.main", flexShrink: 0 }}>
              <CampaignOutlinedIcon />
            </Box>
            <Box sx={{ minWidth: 0 }}>
              <Typography variant="h6" fontWeight={900} sx={{ lineHeight: 1.25 }}>{item.title}</Typography>
              <Typography color="text.secondary" sx={{ mt: 0.5, display: "-webkit-box", WebkitLineClamp: 3, WebkitBoxOrient: "vertical", overflow: "hidden" }}>{item.summary}</Typography>
            </Box>
          </Stack>

          <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
            <StatusBadge domain="announcementPriority" status={item.priority} />
            {item.category && <Box component="span" sx={{ px: 1.25, py: 0.35, borderRadius: 999, bgcolor: alpha(item.category.color ?? brandColors.primary, 0.1), color: item.category.color ?? brandColors.primary, fontWeight: 800, fontSize: 13 }}>{item.category.name}</Box>}
            {item.isFeatured && <Box component="span" sx={{ px: 1.25, py: 0.35, borderRadius: 999, bgcolor: alpha(brandColors.accent, 0.16), color: brandColors.primary, fontWeight: 800, fontSize: 13 }}>📌 ปักหมุด</Box>}
            {!item.isRead && <StatusBadge domain="notificationType" status="ActionRequired" label="ใหม่" />}
            {item.requiresAcknowledgement && !item.isAcknowledged && <StatusBadge domain="notificationPriority" status="High" label="ต้องรับทราบ" />}
          </Stack>

          {item.tags && (
            <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
              {splitTags(item.tags).slice(0, 4).map((tag) => (
                <Box key={tag} component="span" sx={{ px: 1, py: 0.25, borderRadius: 999, bgcolor: alpha(theme.palette.primary.main, 0.08), color: "text.secondary", fontSize: 12, fontWeight: 700 }}>
                  🏷️ {tag}
                </Box>
              ))}
            </Stack>
          )}

          <Typography variant="body2" color="text.secondary">
            เผยแพร่ {formatThaiDateTime(item.publishedAt ?? item.publishAt ?? item.createdAt)}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            👁️ {item.viewCount.toLocaleString("th-TH")} ครั้ง
            {item.requiresAcknowledgement ? ` · ✅ รับทราบ ${item.acknowledgedCount.toLocaleString("th-TH")} คน` : ""}
          </Typography>

          <Box sx={{ mt: "auto" }}>
            <Button component={RouterLink} to={`/announcements/${item.id}`} variant="contained" startIcon={<VisibilityOutlinedIcon />}>
              อ่านรายละเอียด
            </Button>
          </Box>
        </Stack>
      </CardContent>
    </Card>
  );
}

function splitTags(tags: string) {
  return tags.split(",").map((tag) => tag.trim()).filter(Boolean);
}
