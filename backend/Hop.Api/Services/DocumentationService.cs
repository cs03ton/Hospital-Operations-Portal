using System.Text.RegularExpressions;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Hop.Api.Services;

public sealed class DocumentationService(
    IConfiguration configuration,
    IWebHostEnvironment environment,
    ILogger<DocumentationService> logger) : IDocumentationService
{
    private static readonly object FontRegistrationLock = new();
    private static readonly HashSet<string> RegisteredFontKeys = [];
    private static readonly Regex SlugPattern = new("^[a-z0-9-]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex SensitiveAssignmentPattern = new(
        @"(?i)\b(token|secret|password|connectionstring|jwt__key|access[_-]?token|channel[_-]?secret)\b\s*[:=]\s*[^`\s]+",
        RegexOptions.Compiled);

    private static readonly IReadOnlyList<DocumentationDefinition> Definitions =
    [
        new("staff-guide", "staff.md", "คู่มือผู้ใช้งานทั่วไป", "วิธีเข้าสู่ระบบ ขอลา ติดตามสถานะ และเชื่อมต่อ LINE", "User Guide", ["Staff", "DepartmentHead", "Director", "Admin", "SuperAdmin"]),
        new("head-guide", "head.md", "คู่มือหัวหน้าหน่วยงาน", "วิธีดูงานรออนุมัติ ตรวจคำขอ และอนุมัติ/ไม่อนุมัติ", "Approval Guide", ["DepartmentHead", "Admin", "SuperAdmin"]),
        new("director-guide", "director.md", "คู่มือผู้อำนวยการ/ผู้อนุมัติ", "งานอนุมัติขั้นสุดท้าย Executive Dashboard และ Leave Analytics", "Executive Guide", ["Director", "Admin", "SuperAdmin"]),
        new("admin-guide", "admin.md", "คู่มือผู้ดูแลระบบ", "การดูแลผู้ใช้ สิทธิ์ ระบบลา LINE Health Center และ Backup", "Admin Guide", ["Admin", "SuperAdmin"]),
        new("faq", "faq.md", "FAQ", "คำถามที่พบบ่อยและแนวทางแก้ไขเบื้องต้น", "FAQ", ["Staff", "DepartmentHead", "Director", "Admin", "SuperAdmin"]),
        new("release-notes", "release-notes.md", "Release Notes", "สรุปความสามารถของ HOP Phase 1 และ Phase 1.5", "Release Notes", ["Admin", "SuperAdmin"])
    ];

    public async Task<IReadOnlyList<DocumentationSummaryResponse>> GetDocumentsAsync(
        DocumentationAccessContext access,
        CancellationToken cancellationToken = default)
    {
        var docsRoot = ResolveDocumentationRoot();
        var results = new List<DocumentationSummaryResponse>();

        foreach (var definition in Definitions.Where(item => CanAccess(item, access)))
        {
            var path = ResolveDocumentPath(docsRoot, definition);
            results.Add(new DocumentationSummaryResponse(
                definition.Slug,
                definition.Title,
                definition.Description,
                definition.Category,
                definition.Roles,
                GetUpdatedAt(path)));
        }

        return await Task.FromResult(results.OrderBy(item => item.Category).ThenBy(item => item.Title).ToList());
    }

    public async Task<DocumentationDetailResponse?> GetDocumentAsync(
        string slug,
        DocumentationAccessContext access,
        CancellationToken cancellationToken = default)
    {
        if (!SlugPattern.IsMatch(slug))
        {
            return null;
        }

        var definition = Definitions.FirstOrDefault(item => string.Equals(item.Slug, slug, StringComparison.OrdinalIgnoreCase));
        if (definition is null || !CanAccess(definition, access))
        {
            return null;
        }

        var docsRoot = ResolveDocumentationRoot();
        var path = ResolveDocumentPath(docsRoot, definition);
        if (!File.Exists(path))
        {
            logger.LogWarning("Documentation file {FileName} for slug {Slug} was not found.", definition.FileName, definition.Slug);
            return null;
        }

        var markdown = await File.ReadAllTextAsync(path, cancellationToken);
        return new DocumentationDetailResponse(
            definition.Slug,
            definition.Title,
            definition.Description,
            definition.Category,
            definition.Roles,
            SanitizeMarkdown(markdown),
            GetUpdatedAt(path));
    }

    public async Task<DocumentationDetailResponse?> UpdateDocumentAsync(
        string slug,
        string contentMarkdown,
        DocumentationAccessContext access,
        CancellationToken cancellationToken = default)
    {
        if (!access.Permissions.Contains("Documentation.Manage") && !access.Roles.Contains("SuperAdmin"))
        {
            return null;
        }

        if (!SlugPattern.IsMatch(slug))
        {
            return null;
        }

        var definition = Definitions.FirstOrDefault(item => string.Equals(item.Slug, slug, StringComparison.OrdinalIgnoreCase));
        if (definition is null || !CanAccess(definition, access))
        {
            return null;
        }

        if (SensitiveAssignmentPattern.IsMatch(contentMarkdown))
        {
            throw new InvalidOperationException("Markdown contains secret-like assignments. Remove token, secret, password, or connection string values before saving.");
        }

        var docsRoot = ResolveDocumentationRoot();
        Directory.CreateDirectory(docsRoot);
        var path = ResolveDocumentPath(docsRoot, definition);
        var content = RemoveScriptBlocks(contentMarkdown).TrimEnd() + Environment.NewLine;
        await File.WriteAllTextAsync(path, content, cancellationToken);
        return await GetDocumentAsync(slug, access, cancellationToken);
    }

    public async Task<byte[]?> GeneratePdfAsync(
        string slug,
        DocumentationAccessContext access,
        CancellationToken cancellationToken = default)
    {
        var doc = await GetDocumentAsync(slug, access, cancellationToken);
        if (doc is null)
        {
            return null;
        }

        QuestPDF.Settings.License = LicenseType.Community;
        var fontFamily = RegisterThaiFont();
        var lines = doc.ContentMarkdown.Replace("\r\n", "\n").Split('\n');

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(18, Unit.Millimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(style => style.FontFamily(fontFamily).FontSize(16).LineHeight(1.2f));
                page.Header().Column(column =>
                {
                    column.Item().Text(doc.Title).FontSize(20).Bold().FontColor("#0F766E");
                    column.Item().Text($"{doc.Category} · อัปเดต {doc.UpdatedAt:dd/MM/yyyy HH:mm}").FontSize(12).FontColor("#64748B");
                    column.Item().PaddingTop(6).LineHorizontal(1).LineColor("#C8A96B");
                });
                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Spacing(6);
                    foreach (var line in lines)
                    {
                        ComposePdfLine(column, line);
                    }
                });
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Hospital Operations Portal Documentation Center").FontSize(10).FontColor("#64748B");
                });
            });
        }).GeneratePdf();
    }

    private static bool CanAccess(DocumentationDefinition definition, DocumentationAccessContext access)
    {
        if (access.Roles.Contains("Admin") ||
            access.Roles.Contains("SuperAdmin") ||
            access.Permissions.Contains("Documentation.AdminView") ||
            access.Permissions.Contains("Documentation.Manage"))
        {
            return true;
        }

        return definition.Roles.Any(role => access.Roles.Contains(role));
    }

    private string ResolveDocumentationRoot()
    {
        var configuredRoot = configuration["Documentation:RootPath"] ?? configuration["DOCUMENTATION_ROOT_PATH"];
        var candidates = new[]
        {
            configuredRoot,
            Path.Combine(environment.ContentRootPath, "docs", "user-guide"),
            Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", "docs", "user-guide")),
            Path.Combine(Directory.GetCurrentDirectory(), "docs", "user-guide"),
            Path.Combine(AppContext.BaseDirectory, "docs", "user-guide")
        };

        var root = candidates
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => Path.GetFullPath(item!))
            .FirstOrDefault(Directory.Exists);

        return root ?? Path.GetFullPath(configuredRoot ?? Path.Combine(environment.ContentRootPath, "docs", "user-guide"));
    }

    private static string ResolveDocumentPath(string docsRoot, DocumentationDefinition definition)
    {
        var fullRoot = Path.GetFullPath(docsRoot);
        var path = Path.GetFullPath(Path.Combine(fullRoot, definition.FileName));
        if (!path.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid documentation path.");
        }

        return path;
    }

    private static DateTime GetUpdatedAt(string path)
    {
        return File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.UtcNow;
    }

    private static string SanitizeMarkdown(string markdown)
    {
        var withoutScripts = RemoveScriptBlocks(markdown);
        return SensitiveAssignmentPattern.Replace(withoutScripts, match =>
        {
            var separator = match.Value.Contains('=') ? "=" : ":";
            var key = match.Value.Split(separator[0], 2)[0].Trim();
            return $"{key}{separator} [REDACTED]";
        });
    }

    private static string RemoveScriptBlocks(string markdown)
    {
        return Regex.Replace(markdown, @"(?is)<script.*?>.*?</script>", string.Empty);
    }

    private string RegisterThaiFont()
    {
        var configuredPath = configuration["Documentation:PdfFontPath"]
            ?? configuration["DOCUMENTATION_PDF_FONT_PATH"]
            ?? configuration["LeavePdf:FontPath"]
            ?? configuration["LEAVE_PDF_FONT_PATH"];
        var configuredFamily = configuration["Documentation:PdfFontFamily"]
            ?? configuration["DOCUMENTATION_PDF_FONT_FAMILY"]
            ?? configuration["LeavePdf:FontFamily"]
            ?? configuration["LEAVE_PDF_FONT_FAMILY"]
            ?? "TH SarabunPSK";
        var candidates = new[]
        {
            configuredPath,
            Path.Combine(environment.ContentRootPath, "assets", "fonts", "TH SarabunPSK.ttf"),
            Path.Combine(environment.ContentRootPath, "assets", "fonts", "THSarabunPSK.ttf"),
            Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", "assets", "fonts", "TH SarabunPSK.ttf")),
            Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", "assets", "fonts", "THSarabunPSK.ttf")),
            @"C:\Windows\Fonts\TH SarabunPSK.ttf",
            @"C:\Windows\Fonts\THSarabunPSK.ttf",
            "/usr/share/fonts/truetype/thai/THSarabunPSK.ttf"
        };

        foreach (var candidate in candidates.Where(item => !string.IsNullOrWhiteSpace(item)))
        {
            if (!File.Exists(candidate))
            {
                continue;
            }

            var fontKey = $"{configuredFamily}:{Path.GetFullPath(candidate)}";
            lock (FontRegistrationLock)
            {
                if (!RegisteredFontKeys.Contains(fontKey))
                {
                    using var stream = File.OpenRead(candidate);
                    FontManager.RegisterFontWithCustomName(configuredFamily, stream);
                    RegisteredFontKeys.Add(fontKey);
                }
            }

            return configuredFamily;
        }

        return Fonts.Arial;
    }

    private static void ComposePdfLine(ColumnDescriptor column, string line)
    {
        var trimmed = line.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            column.Item().Height(4);
            return;
        }

        if (trimmed.StartsWith("# "))
        {
            column.Item().PaddingTop(4).Text(trimmed[2..]).FontSize(20).Bold().FontColor("#0F766E");
            return;
        }

        if (trimmed.StartsWith("## "))
        {
            column.Item().PaddingTop(8).Text(trimmed[3..]).FontSize(18).Bold().FontColor("#0F766E");
            return;
        }

        if (trimmed.StartsWith("### "))
        {
            column.Item().PaddingTop(6).Text(trimmed[4..]).FontSize(16).Bold();
            return;
        }

        if (trimmed.StartsWith("- "))
        {
            column.Item().Text($"• {trimmed[2..]}").FontSize(16);
            return;
        }

        if (Regex.IsMatch(trimmed, @"^\d+\.\s+"))
        {
            column.Item().Text(trimmed).FontSize(16);
            return;
        }

        if (trimmed.StartsWith(">"))
        {
            column.Item().Padding(6).Background("#FAF8F2").Text(trimmed.TrimStart('>', ' ')).FontSize(15).Italic();
            return;
        }

        if (trimmed.StartsWith("|"))
        {
            column.Item().Text(trimmed.Replace("|", "  ")).FontSize(14).FontColor("#475569");
            return;
        }

        if (trimmed.StartsWith("```"))
        {
            return;
        }

        column.Item().Text(trimmed).FontSize(16);
    }

    private sealed record DocumentationDefinition(
        string Slug,
        string FileName,
        string Title,
        string Description,
        string Category,
        IReadOnlyList<string> Roles);
}
