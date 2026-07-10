import ArrowBackOutlinedIcon from "@mui/icons-material/ArrowBackOutlined";
import CloseOutlinedIcon from "@mui/icons-material/CloseOutlined";
import ContentCopyOutlinedIcon from "@mui/icons-material/ContentCopyOutlined";
import DownloadOutlinedIcon from "@mui/icons-material/DownloadOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import PrintOutlinedIcon from "@mui/icons-material/PrintOutlined";
import SaveOutlinedIcon from "@mui/icons-material/SaveOutlined";
import {
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Divider,
  Link,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from "@mui/material";
import { alpha, useTheme } from "@mui/material/styles";
import type { ReactNode } from "react";
import { useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link as RouterLink, useParams } from "react-router-dom";
import { downloadDocumentationPdf, getDocumentationDetail, updateDocumentation } from "../api/docsApi";
import { EmptyState } from "../components/common/EmptyState";
import { LoadingState } from "../components/common/LoadingState";
import { PageHeader } from "../components/PageHeader";
import { useAuth } from "../context/AuthContext";
import { useNotification } from "../hooks/useNotification";
import { brandColors } from "../theme/theme";
import { formatThaiDateTime } from "../utils/dateFormat";

export function DocumentationDetailPage() {
  const theme = useTheme();
  const { slug = "" } = useParams();
  const queryClient = useQueryClient();
  const { user } = useAuth();
  const { showSuccess, showError } = useNotification();
  const [isEditing, setIsEditing] = useState(false);
  const [draftMarkdown, setDraftMarkdown] = useState("");
  const { data, isLoading } = useQuery({
    queryKey: ["documentation", slug],
    queryFn: () => getDocumentationDetail(slug),
    enabled: Boolean(slug),
  });
  const headings = useMemo(() => extractHeadings(data?.contentMarkdown ?? ""), [data?.contentMarkdown]);
  const canManageDocs = Boolean(user?.permissions?.includes("Documentation.Manage") || user?.role === "SuperAdmin");
  const updateMutation = useMutation({
    mutationFn: () => updateDocumentation(slug, draftMarkdown),
    onSuccess: (updated) => {
      if (updated) {
        queryClient.setQueryData(["documentation", slug], updated);
      }
      queryClient.invalidateQueries({ queryKey: ["documentation"] });
      setIsEditing(false);
      showSuccess("บันทึกคู่มือเรียบร้อยแล้ว");
    },
    onError: () => showError("ไม่สามารถบันทึกคู่มือได้ กรุณาตรวจสอบเนื้อหาอีกครั้ง"),
  });
  const pdfMutation = useMutation({
    mutationFn: () => downloadDocumentationPdf(slug),
    onSuccess: (blob) => {
      const url = window.URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = `hop-documentation-${slug}.pdf`;
      anchor.click();
      window.URL.revokeObjectURL(url);
      showSuccess("ดาวน์โหลด PDF คู่มือเรียบร้อยแล้ว");
    },
    onError: () => showError("ไม่สามารถดาวน์โหลด PDF ได้"),
  });

  useEffect(() => {
    if (data?.contentMarkdown && !isEditing) {
      setDraftMarkdown(data.contentMarkdown);
    }
  }, [data?.contentMarkdown, isEditing]);

  if (isLoading) {
    return <LoadingState message="กำลังโหลดคู่มือ..." />;
  }

  if (!data) {
    return <EmptyState message="ไม่พบคู่มือ หรือคุณไม่มีสิทธิ์เข้าถึงคู่มือนี้" />;
  }

  return (
    <Box sx={{ maxWidth: 1440, mx: "auto" }}>
      <Stack spacing={3}>
        <PageHeader title={data.title} subtitle={data.description} />
        <Stack direction={{ xs: "column", md: "row" }} spacing={1} justifyContent="space-between">
          <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
            <Button component={RouterLink} to="/docs" startIcon={<ArrowBackOutlinedIcon />} variant="outlined">
              กลับศูนย์คู่มือ
            </Button>
            <Button startIcon={<PrintOutlinedIcon />} variant="outlined" onClick={() => window.print()}>
              พิมพ์
            </Button>
            <Button startIcon={<DownloadOutlinedIcon />} variant="outlined" disabled={pdfMutation.isPending} onClick={() => pdfMutation.mutate()}>
              ดาวน์โหลด PDF
            </Button>
            <Button
              startIcon={<ContentCopyOutlinedIcon />}
              variant="outlined"
              onClick={async () => {
                try {
                  await navigator.clipboard.writeText(window.location.href);
                  showSuccess("คัดลอกลิงก์คู่มือแล้ว");
                } catch {
                  showError("ไม่สามารถคัดลอกลิงก์ได้");
                }
              }}
            >
              คัดลอกลิงก์
            </Button>
            {canManageDocs && !isEditing && (
              <Button startIcon={<EditOutlinedIcon />} variant="contained" onClick={() => setIsEditing(true)}>
                แก้ไขคู่มือ
              </Button>
            )}
            {isEditing && (
              <>
                <Button startIcon={<SaveOutlinedIcon />} variant="contained" disabled={updateMutation.isPending} onClick={() => updateMutation.mutate()}>
                  บันทึก
                </Button>
                <Button
                  startIcon={<CloseOutlinedIcon />}
                  variant="outlined"
                  disabled={updateMutation.isPending}
                  onClick={() => {
                    setDraftMarkdown(data.contentMarkdown);
                    setIsEditing(false);
                  }}
                >
                  ยกเลิก
                </Button>
              </>
            )}
          </Stack>
          <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
            <Chip label={data.category} />
            <Chip variant="outlined" label={`อัปเดต ${formatThaiDateTime(data.updatedAt)}`} />
          </Stack>
        </Stack>

        <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", lg: "minmax(0, 1fr) 280px" }, gap: 2.5 }}>
          <Card sx={{ border: `1px solid ${brandColors.border}`, borderTop: `4px solid ${brandColors.accent}`, borderRadius: 3 }}>
            <CardContent sx={{ p: { xs: 2, md: 3 } }}>
              {isEditing ? (
                <Stack spacing={2}>
                  <Box sx={{ p: 2, borderRadius: 2, bgcolor: alpha(brandColors.accent, 0.08), border: `1px solid ${alpha(brandColors.accent, 0.32)}` }}>
                    <Typography fontWeight={800}>ข้อควรระวัง</Typography>
                    <Typography color="text.secondary">
                      ห้ามใส่ token, secret, password หรือ connection string จริงในคู่มือ ระบบจะปฏิเสธการบันทึกหากพบค่าในรูปแบบ sensitive assignment
                    </Typography>
                  </Box>
                  <TextField
                    fullWidth
                    multiline
                    minRows={24}
                    label="เนื้อหา Markdown"
                    value={draftMarkdown}
                    onChange={(event) => setDraftMarkdown(event.target.value)}
                    inputProps={{ sx: { fontFamily: "Consolas, monospace", fontSize: 14, lineHeight: 1.6 } }}
                  />
                </Stack>
              ) : (
                <MarkdownContent markdown={data.contentMarkdown} />
              )}
            </CardContent>
          </Card>
          <Card sx={{ alignSelf: "start", borderRadius: 3, position: { lg: "sticky" }, top: { lg: 88 } }}>
            <CardContent>
              <Typography fontWeight={900} color="primary.main">สารบัญ</Typography>
              <Divider sx={{ my: 1.5 }} />
              {headings.length === 0 ? (
                <Typography color="text.secondary">ไม่มีหัวข้อย่อย</Typography>
              ) : (
                <Stack spacing={1}>
                  {headings.map((heading) => (
                    <Link key={heading.id} href={`#${heading.id}`} underline="hover" color={heading.level === 2 ? "primary.main" : "text.secondary"} sx={{ pl: Math.max(0, heading.level - 2) * 1.5 }}>
                      {heading.text}
                    </Link>
                  ))}
                </Stack>
              )}
              <Box sx={{ mt: 2, p: 1.5, borderRadius: 2, bgcolor: alpha(theme.palette.primary.main, 0.06) }}>
                <Typography variant="body2" color="text.secondary">
                  เอกสารนี้แสดงเฉพาะเนื้อหาที่บทบาทของคุณมีสิทธิ์เข้าถึง
                </Typography>
              </Box>
            </CardContent>
          </Card>
        </Box>
      </Stack>
    </Box>
  );
}

function MarkdownContent({ markdown }: { markdown: string }) {
  return (
    <Stack
      spacing={2.25}
      sx={{
        "& p, & li, & td, & th": {
          lineHeight: 1.85,
        },
      }}
    >
      {parseMarkdown(markdown)}
    </Stack>
  );
}

function parseMarkdown(markdown: string) {
  const lines = markdown.replace(/\r\n/g, "\n").split("\n");
  const nodes: ReactNode[] = [];
  let index = 0;

  while (index < lines.length) {
    const line = lines[index];
    const trimmed = line.trim();
    if (!trimmed) {
      index++;
      continue;
    }

    if (trimmed.startsWith("```")) {
      const codeLines: string[] = [];
      index++;
      while (index < lines.length && !lines[index].trim().startsWith("```")) {
        codeLines.push(lines[index]);
        index++;
      }
      index++;
      nodes.push(
        <Box key={nodes.length} component="pre" sx={{ p: 2, borderRadius: 2, bgcolor: "#0f172a", color: "#e2e8f0", overflowX: "auto", fontSize: 14 }}>
          <code>{codeLines.join("\n")}</code>
        </Box>,
      );
      continue;
    }

    if (trimmed.startsWith("|") && index + 1 < lines.length && lines[index + 1].trim().startsWith("|")) {
      const tableLines: string[] = [];
      while (index < lines.length && lines[index].trim().startsWith("|")) {
        tableLines.push(lines[index].trim());
        index++;
      }
      nodes.push(renderTable(tableLines, nodes.length));
      continue;
    }

    const headingMatch = /^(#{1,4})\s+(.+)$/.exec(trimmed);
    if (headingMatch) {
      const level = headingMatch[1].length;
      const text = headingMatch[2];
      const id = slugifyHeading(text);
      nodes.push(
        <Typography
          key={nodes.length}
          id={id}
          variant={level === 1 ? "h4" : level === 2 ? "h5" : "h6"}
          fontWeight={900}
          color={level <= 2 ? "primary.main" : "text.primary"}
          sx={{
            scrollMarginTop: 96,
            mt: level === 1 ? 0 : 2.5,
            mb: 0.75,
            lineHeight: 1.35,
          }}
        >
          {text}
        </Typography>,
      );
      index++;
      continue;
    }

    if (/^[-*]\s+/.test(trimmed) || /^\d+\.\s+/.test(trimmed)) {
      const items: string[] = [];
      const isOrderedList = /^\d+\.\s+/.test(trimmed);
      while (index < lines.length && (/^[-*]\s+/.test(lines[index].trim()) || /^\d+\.\s+/.test(lines[index].trim()))) {
        items.push(lines[index].trim().replace(/^[-*]\s+/, "").replace(/^\d+\.\s+/, ""));
        index++;
      }
      nodes.push(
        <Box
          key={nodes.length}
          component={isOrderedList ? "ol" : "ul"}
          sx={{
            pl: 3.25,
            my: 0.5,
            "& li": {
              mb: 0.9,
              pl: 0.5,
            },
            "& li:last-of-type": {
              mb: 0,
            },
          }}
        >
          {items.map((item) => (
            <li key={item}>
              <Typography component="span" sx={{ lineHeight: 1.85 }}>
                {item}
              </Typography>
            </li>
          ))}
        </Box>,
      );
      continue;
    }

    if (trimmed.startsWith(">")) {
      nodes.push(
        <Box key={nodes.length} sx={{ p: 2, borderLeft: `4px solid ${brandColors.accent}`, bgcolor: alpha(brandColors.accent, 0.08), borderRadius: 2 }}>
          <Typography sx={{ lineHeight: 1.85 }}>{trimmed.replace(/^>\s?/, "")}</Typography>
        </Box>,
      );
      index++;
      continue;
    }

    nodes.push(<Typography key={nodes.length} sx={{ lineHeight: 1.85 }}>{trimmed}</Typography>);
    index++;
  }

  return nodes;
}

function renderTable(lines: string[], key: number) {
  const rows = lines
    .filter((line) => !/^\|\s*-+/.test(line))
    .map((line) => line.split("|").slice(1, -1).map((cell) => cell.trim()));
  const [head = [], ...body] = rows;

  return (
    <Box key={key} sx={{ overflowX: "auto" }}>
      <Table size="small">
        <TableHead>
          <TableRow>{head.map((cell) => <TableCell key={cell} sx={{ fontWeight: 900 }}>{cell}</TableCell>)}</TableRow>
        </TableHead>
        <TableBody>
          {body.map((row, rowIndex) => (
            <TableRow key={rowIndex}>
              {row.map((cell, cellIndex) => <TableCell key={`${rowIndex}-${cellIndex}`}>{cell}</TableCell>)}
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </Box>
  );
}

function extractHeadings(markdown: string) {
  return markdown
    .split(/\r?\n/)
    .map((line) => /^(#{2,4})\s+(.+)$/.exec(line.trim()))
    .filter((match): match is RegExpExecArray => Boolean(match))
    .map((match) => ({ level: match[1].length, text: match[2], id: slugifyHeading(match[2]) }));
}

function slugifyHeading(text: string) {
  return text
    .toLowerCase()
    .replace(/[^\p{L}\p{N}]+/gu, "-")
    .replace(/^-|-$/g, "");
}
