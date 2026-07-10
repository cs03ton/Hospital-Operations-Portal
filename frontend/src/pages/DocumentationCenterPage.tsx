import ArticleOutlinedIcon from "@mui/icons-material/ArticleOutlined";
import AutoStoriesOutlinedIcon from "@mui/icons-material/AutoStoriesOutlined";
import HelpOutlineOutlinedIcon from "@mui/icons-material/HelpOutlineOutlined";
import ManageSearchOutlinedIcon from "@mui/icons-material/ManageSearchOutlined";
import NewReleasesOutlinedIcon from "@mui/icons-material/NewReleasesOutlined";
import SearchOutlinedIcon from "@mui/icons-material/SearchOutlined";
import SupervisorAccountOutlinedIcon from "@mui/icons-material/SupervisorAccountOutlined";
import {
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  FormControl,
  InputAdornment,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { alpha, useTheme } from "@mui/material/styles";
import type { SvgIconComponent } from "@mui/icons-material";
import { useQuery } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { Link as RouterLink } from "react-router-dom";
import { getDocumentationList, type DocumentationSummary } from "../api/docsApi";
import { EmptyState } from "../components/common/EmptyState";
import { LoadingState } from "../components/common/LoadingState";
import { PageHeader } from "../components/PageHeader";
import { useAuth } from "../context/AuthContext";
import { brandColors } from "../theme/theme";
import { formatThaiDateTime } from "../utils/dateFormat";

export function DocumentationCenterPage() {
  const theme = useTheme();
  const { user } = useAuth();
  const [search, setSearch] = useState("");
  const [category, setCategory] = useState("all");
  const [role, setRole] = useState("all");
  const { data = [], isLoading } = useQuery({ queryKey: ["documentation"], queryFn: getDocumentationList });
  const canFilterRole = user?.role === "Admin" || user?.role === "SuperAdmin" || user?.permissions?.includes("Documentation.AdminView");

  const categories = useMemo(() => Array.from(new Set(data.map((item) => item.category))).sort(), [data]);
  const roles = useMemo(() => Array.from(new Set(data.flatMap((item) => item.roles))).sort(), [data]);
  const filteredDocs = useMemo(() => {
    const normalizedSearch = search.trim().toLowerCase();
    return data.filter((item) => {
      const matchesSearch =
        !normalizedSearch ||
        item.title.toLowerCase().includes(normalizedSearch) ||
        item.description.toLowerCase().includes(normalizedSearch) ||
        item.category.toLowerCase().includes(normalizedSearch);
      const matchesCategory = category === "all" || item.category === category;
      const matchesRole = role === "all" || item.roles.includes(role);
      return matchesSearch && matchesCategory && matchesRole;
    });
  }, [category, data, role, search]);

  const recentDocs = [...data].sort((a, b) => Date.parse(b.updatedAt) - Date.parse(a.updatedAt)).slice(0, 3);
  const faqDocs = data.filter((item) => item.category === "FAQ");
  const releaseNotes = data.filter((item) => item.category === "Release Notes");

  if (isLoading) {
    return <LoadingState message="กำลังโหลดศูนย์คู่มือ..." />;
  }

  return (
    <Box sx={{ maxWidth: 1440, mx: "auto" }}>
      <Stack spacing={3}>
        <PageHeader title="ศูนย์คู่มือการใช้งาน" subtitle="รวมคู่มือ HOP ตามบทบาทและงานที่เกี่ยวข้อง" />

        <Card
          sx={{
            border: `1px solid ${brandColors.border}`,
            borderTop: `5px solid ${brandColors.accent}`,
            borderRadius: 3,
            boxShadow: `0 14px 34px ${alpha(theme.palette.primary.dark, 0.06)}`,
          }}
        >
          <CardContent>
            <Box
              sx={{
                display: "grid",
                gridTemplateColumns: {
                  xs: "1fr",
                  md: canFilterRole ? "minmax(0, 2fr) minmax(180px, 1fr) minmax(180px, 1fr)" : "minmax(0, 2fr) minmax(180px, 1fr)",
                },
                gap: 2,
                alignItems: "center",
              }}
            >
              <Box>
                <TextField
                  fullWidth
                  label="ค้นหาคู่มือ"
                  value={search}
                  onChange={(event) => setSearch(event.target.value)}
                  InputProps={{
                    startAdornment: (
                      <InputAdornment position="start">
                        <SearchOutlinedIcon />
                      </InputAdornment>
                    ),
                  }}
                />
              </Box>
              <Box>
                <FormControl fullWidth>
                  <InputLabel>หมวดหมู่</InputLabel>
                  <Select label="หมวดหมู่" value={category} onChange={(event) => setCategory(event.target.value)}>
                    <MenuItem value="all">ทั้งหมด</MenuItem>
                    {categories.map((item) => <MenuItem key={item} value={item}>{item}</MenuItem>)}
                  </Select>
                </FormControl>
              </Box>
              {canFilterRole && (
                <Box>
                  <FormControl fullWidth>
                    <InputLabel>บทบาท</InputLabel>
                    <Select label="บทบาท" value={role} onChange={(event) => setRole(event.target.value)}>
                      <MenuItem value="all">ทั้งหมด</MenuItem>
                      {roles.map((item) => <MenuItem key={item} value={item}>{item}</MenuItem>)}
                    </Select>
                  </FormControl>
                </Box>
              )}
            </Box>
          </CardContent>
        </Card>

        {filteredDocs.length === 0 ? (
          <EmptyState message="ไม่พบคู่มือที่ตรงกับเงื่อนไข" />
        ) : (
          <Box
            sx={{
              display: "grid",
              gridTemplateColumns: { xs: "1fr", md: "repeat(2, minmax(0, 1fr))", lg: "repeat(3, minmax(0, 1fr))" },
              gap: { xs: 2, md: 2.5 },
            }}
          >
            {filteredDocs.map((item) => (
              <DocumentationCard key={item.slug} doc={item} />
            ))}
          </Box>
        )}

        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: { xs: "1fr", md: "repeat(3, minmax(0, 1fr))" },
            gap: { xs: 2, md: 2.5 },
            alignItems: "stretch",
          }}
        >
          <MiniDocSection title="อัปเดตล่าสุด" docs={recentDocs} icon={NewReleasesOutlinedIcon} />
          <MiniDocSection title="FAQ" docs={faqDocs} icon={HelpOutlineOutlinedIcon} />
          <MiniDocSection title="Release Notes" docs={releaseNotes} icon={ArticleOutlinedIcon} />
        </Box>
      </Stack>
    </Box>
  );
}

function DocumentationCard({ doc }: { doc: DocumentationSummary }) {
  const theme = useTheme();
  const Icon = iconForCategory(doc.category);
  return (
    <Card
      sx={{
        height: "100%",
        minHeight: 280,
        border: `1px solid ${brandColors.border}`,
        borderTop: `4px solid ${brandColors.accent}`,
        borderRadius: 3,
        boxShadow: `0 12px 28px ${alpha(theme.palette.primary.dark, 0.05)}`,
      }}
    >
      <CardContent sx={{ height: "100%", p: 2.5 }}>
        <Stack spacing={2} sx={{ height: "100%" }}>
          <Stack direction="row" spacing={1.5} alignItems="flex-start">
            <Box sx={{ color: "primary.main", bgcolor: alpha(theme.palette.primary.main, 0.08), borderRadius: "50%", p: 1.25, display: "grid", flexShrink: 0 }}>
              <Icon fontSize="small" />
            </Box>
            <Box sx={{ minWidth: 0 }}>
              <Typography variant="h6" fontWeight={900} sx={{ lineHeight: 1.25 }}>{doc.title}</Typography>
              <Typography color="text.secondary" sx={{ mt: 0.5 }}>{doc.description}</Typography>
            </Box>
          </Stack>
          <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
            <Chip size="small" label={doc.category} />
            <Chip size="small" variant="outlined" label={`อัปเดต ${formatThaiDateTime(doc.updatedAt)}`} />
          </Stack>
          <Box sx={{ mt: "auto" }}>
            <Button component={RouterLink} to={`/docs/${doc.slug}`} variant="contained">
              อ่านคู่มือ
            </Button>
          </Box>
        </Stack>
      </CardContent>
    </Card>
  );
}

function MiniDocSection({ title, docs, icon: Icon }: { title: string; docs: DocumentationSummary[]; icon: SvgIconComponent }) {
  const theme = useTheme();
  return (
    <Card
      sx={{
        height: "100%",
        minHeight: 220,
        border: `1px solid ${brandColors.border}`,
        borderTop: `4px solid ${brandColors.accent}`,
        borderRadius: 3,
        boxShadow: `0 12px 28px ${alpha(theme.palette.primary.dark, 0.05)}`,
      }}
    >
      <CardContent sx={{ height: "100%", p: 2.5 }}>
        <Stack spacing={1.5} sx={{ height: "100%" }}>
          <Stack direction="row" spacing={1} alignItems="center">
            <Icon color="primary" />
            <Typography fontWeight={900}>{title}</Typography>
          </Stack>
          {docs.length === 0 ? (
            <Typography color="text.secondary">ไม่มีรายการที่เข้าถึงได้</Typography>
          ) : (
            <Stack spacing={0.5}>
              {docs.map((doc) => (
                <Button
                  key={doc.slug}
                  component={RouterLink}
                  to={`/docs/${doc.slug}`}
                  variant="text"
                  sx={{
                    justifyContent: "flex-start",
                    px: 0,
                    textAlign: "left",
                    minHeight: 36,
                    "& .MuiButton-startIcon": { flexShrink: 0 },
                  }}
                >
                  <Box component="span" sx={{ overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
                    {doc.title}
                  </Box>
                </Button>
              ))}
            </Stack>
          )}
        </Stack>
      </CardContent>
    </Card>
  );
}

function iconForCategory(category: string) {
  if (category.includes("Approval") || category.includes("Executive")) return SupervisorAccountOutlinedIcon;
  if (category.includes("FAQ")) return HelpOutlineOutlinedIcon;
  if (category.includes("Release")) return NewReleasesOutlinedIcon;
  if (category.includes("Admin")) return ManageSearchOutlinedIcon;
  return AutoStoriesOutlinedIcon;
}
