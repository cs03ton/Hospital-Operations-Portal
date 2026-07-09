using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Hop.Api.Services;

public sealed class LeavePdfService(IWebHostEnvironment environment, IConfiguration configuration) : ILeavePdfService
{
    private static readonly object FontRegistrationLock = new();
    private static readonly HashSet<string> RegisteredFontKeys = [];

    public byte[] GenerateLeaveRequestPdf(LeaveRequest leaveRequest, LeavePdfRenderContext context)
    {
        return GenerateQuestPdf(leaveRequest, context);
    }

    private byte[] GenerateQuestPdf(LeaveRequest leaveRequest, LeavePdfRenderContext context)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var templateSettings = ResolveDocumentSettings(TryLoadTemplateConfig()?.DocumentSettings);
        var fontFamily = RegisterThaiFont(templateSettings.FontFamily);
        var values = BuildFieldValues(leaveRequest, context);
        var approvals = leaveRequest.Approvals.OrderBy(item => item.StepOrder).ToList();
        var logoPath = TryResolveLogoPath();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20, Unit.Millimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(style => style
                    .FontFamily(fontFamily)
                    .FontSize(templateSettings.FontSize)
                    .LineHeight((float)templateSettings.LineHeight));

                page.Header().Element(header => ComposeHeader(header, values, logoPath, fontFamily));
                page.Content().PaddingTop(8).Column(column =>
                {
                    column.Spacing(8);
                    column.Item().Element(item => ComposeEmployeeSection(item, values));
                    column.Item().Element(item => ComposeLeaveSection(item, values));
                    column.Item().Element(item => ComposeBalanceSection(item, values));
                    column.Item().Element(item => ComposeReasonSection(item, values));
                    column.Item().Element(item => ComposeAttachmentSection(item, values));
                    column.Item().Element(item => ComposeApprovalSection(item, approvals));
                    column.Item().Element(item => ComposeApproverCommentsSection(item, values));
                    column.Item().Element(item => ComposeFinalApprovalSection(item, values));
                });
                page.Footer().Element(footer => ComposeFooter(footer, values));
            });
        }).GeneratePdf();
    }

    private IReadOnlyList<PdfLine>? TryBuildTemplateLines(LeaveRequest leaveRequest, LeavePdfRenderContext context)
    {
        var template = TryLoadTemplateConfig();
        if (template is null)
        {
            return null;
        }

        var values = BuildFieldValues(leaveRequest, context);
        var documentSettings = ResolveDocumentSettings(template.DocumentSettings);
        var lines = new List<PdfLine>();
        foreach (var item in template.StaticText)
        {
            lines.Add(new PdfLine(
                item.Text,
                item.X,
                item.Y,
                ResolveFontSize(item.FontSize, documentSettings),
                ResolveFontFamily(item.FontFamily, documentSettings)));
        }

        foreach (var field in template.Fields)
        {
            var value = values.GetValueOrDefault(field.Key, "-");
            var text = string.IsNullOrWhiteSpace(field.Label) ? value : $"{field.Label}: {value}";
            lines.Add(new PdfLine(
                text,
                field.X,
                field.Y,
                ResolveFontSize(field.FontSize, documentSettings),
                ResolveFontFamily(field.FontFamily, documentSettings)));
        }

        if (template.ApprovalRows is not null)
        {
            var y = template.ApprovalRows.StartY;
            var approvalFontSize = ResolveFontSize(template.ApprovalRows.FontSize, documentSettings);
            var approvalFontFamily = ResolveFontFamily(template.ApprovalRows.FontFamily, documentSettings);
            var approvalRowHeight = template.ApprovalRows.RowHeight
                ?? approvalFontSize * ResolveLineHeight(template.ApprovalRows.LineHeight, documentSettings);
            foreach (var approval in leaveRequest.Approvals.OrderBy(item => item.StepOrder).Take(template.ApprovalRows.MaxRows))
            {
                var rowValues = BuildApprovalValues(approval);
                var text = template.ApprovalRows.Format;
                foreach (var pair in rowValues)
                {
                    text = text.Replace($"{{{{{pair.Key}}}}}", pair.Value, StringComparison.OrdinalIgnoreCase);
                }

                lines.Add(new PdfLine(text, template.ApprovalRows.X, y, approvalFontSize, approvalFontFamily));
                y -= approvalRowHeight;
            }
        }

        return lines.Count == 0 ? null : lines;
    }

    private LeaveFormTemplateConfig? TryLoadTemplateConfig()
    {
        var configuredPath = configuration["LeavePdf:TemplateConfigPath"];
        var candidates = new[]
        {
            configuredPath,
            Path.Combine(environment.ContentRootPath, "storage", "templates", "leave", "leave_form_template.json"),
            Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", "storage", "templates", "leave", "leave_form_template.json"))
        };

        foreach (var candidate in candidates.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            if (!File.Exists(candidate))
            {
                continue;
            }

            try
            {
                var json = File.ReadAllText(candidate);
                return JsonSerializer.Deserialize<LeaveFormTemplateConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    private static int ResolveFontSize(int? fontSize, DocumentTemplateSettings settings)
    {
        return fontSize is > 0 ? fontSize.Value : settings.FontSize;
    }

    private DocumentTemplateSettings ResolveDocumentSettings(DocumentTemplateSettings? templateSettings)
    {
        var settings = templateSettings ?? new DocumentTemplateSettings();
        settings.FontFamily = configuration["LeavePdf:FontFamily"] ?? configuration["LEAVE_PDF_FONT_FAMILY"] ?? settings.FontFamily;
        settings.FontSize = configuration.GetValue("LeavePdf:FontSize", configuration.GetValue("LEAVE_PDF_FONT_SIZE", settings.FontSize));
        settings.LineHeight = configuration.GetValue("LeavePdf:LineHeight", configuration.GetValue("LEAVE_PDF_LINE_HEIGHT", settings.LineHeight));
        return settings;
    }

    private static double ResolveLineHeight(double? lineHeight, DocumentTemplateSettings settings)
    {
        return lineHeight is > 0 ? lineHeight.Value : settings.LineHeight;
    }

    private static string ResolveFontFamily(string? fontFamily, DocumentTemplateSettings settings)
    {
        return string.IsNullOrWhiteSpace(fontFamily) ? settings.FontFamily : fontFamily.Trim();
    }

    private PdfImage? TryLoadLogo()
    {
        foreach (var candidate in GetLogoPathCandidates())
        {
            if (File.Exists(candidate) && PngImageReader.TryReadRgb(candidate, out var image))
            {
                return image;
            }
        }

        return null;
    }

    private string? TryResolveLogoPath()
    {
        return GetLogoPathCandidates().FirstOrDefault(File.Exists);
    }

    private IEnumerable<string> GetLogoPathCandidates()
    {
        var configuredPaths = new[]
        {
            configuration["Branding:LogoPath"],
            configuration["Hospital:LogoPath"],
            configuration["HOSPITAL_LOGO_PATH"],
            configuration["HOP_HOSPITAL_LOGO_PATH"]
        };

        foreach (var configuredPath in configuredPaths)
        {
            foreach (var resolvedPath in ResolveLogoConfigPath(configuredPath))
            {
                yield return resolvedPath;
            }
        }

        var fixedCandidates = new[]
        {
            Path.Combine(environment.ContentRootPath, "assets", "logo", "hospital-logo.png"),
            Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "assets", "logo", "hospital-logo.png")),
            "/opt/hop/backend/assets/logo/hospital-logo.png",
            "/opt/hop/assets/logo/hospital-logo.png",
            "/var/www/hop/assets/logo/hospital-logo.png",
            Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", "frontend", "src", "assets", "logo", "hospital-logo.png"))
        };

        foreach (var candidate in fixedCandidates)
        {
            yield return candidate;
        }

        foreach (var directory in new[]
        {
            Path.Combine(environment.ContentRootPath, "assets"),
            Path.Combine(environment.ContentRootPath, "assets", "logo"),
            "/var/www/hop/assets",
            "/var/www/hop/assets/logo"
        })
        {
            foreach (var candidate in FindLogoFiles(directory))
            {
                yield return candidate;
            }
        }
    }

    private IEnumerable<string> ResolveLogoConfigPath(string? configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            yield break;
        }

        var trimmedPath = configuredPath.Trim();
        yield return trimmedPath;

        if (!Path.IsPathRooted(trimmedPath))
        {
            yield return Path.GetFullPath(Path.Combine(environment.ContentRootPath, trimmedPath));
            yield break;
        }

        if (trimmedPath.StartsWith("/assets/", StringComparison.OrdinalIgnoreCase))
        {
            var relativeWebPath = trimmedPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            yield return Path.Combine("/var/www/hop", relativeWebPath);
            yield return Path.Combine(environment.ContentRootPath, relativeWebPath);
        }
    }

    private static IEnumerable<string> FindLogoFiles(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(directory, "hospital-logo*.png", SearchOption.TopDirectoryOnly)
            .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase);
    }

    private string RegisterThaiFont(string preferredFontFamily)
    {
        var configuredPath = configuration["LeavePdf:FontPath"] ?? configuration["LEAVE_PDF_FONT_PATH"];
        var fontCandidates = new[]
        {
            configuredPath,
            Path.Combine(environment.ContentRootPath, "assets", "fonts", "THSarabunPSK.ttf"),
            Path.Combine(environment.ContentRootPath, "assets", "fonts", "TH SarabunPSK.ttf"),
            Path.Combine(environment.ContentRootPath, "assets", "fonts", "THSarabunNew.ttf"),
            Path.Combine(environment.ContentRootPath, "assets", "fonts", "NotoSansThai-Regular.ttf"),
            Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", "assets", "fonts", "THSarabunPSK.ttf")),
            Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", "assets", "fonts", "TH SarabunPSK.ttf")),
            Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", "assets", "fonts", "THSarabunNew.ttf")),
            Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", "assets", "fonts", "NotoSansThai-Regular.ttf")),
            @"C:\Windows\Fonts\THSarabunPSK.ttf",
            @"C:\Windows\Fonts\TH SarabunPSK.ttf",
            @"C:\Windows\Fonts\THSarabunNew.ttf",
            @"C:\Windows\Fonts\THSarabun.ttf",
            @"C:\Windows\Fonts\tahoma.ttf",
            @"C:\Windows\Fonts\LeelawUI.ttf"
        };

        var fontPath = fontCandidates
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .FirstOrDefault(File.Exists);

        if (fontPath is null)
        {
            return string.IsNullOrWhiteSpace(preferredFontFamily) ? "Tahoma" : preferredFontFamily;
        }

        var fontKey = $"HOPThai::{Path.GetFileNameWithoutExtension(fontPath)}";
        lock (FontRegistrationLock)
        {
            if (RegisteredFontKeys.Add(fontKey))
            {
                using var stream = File.OpenRead(fontPath);
                FontManager.RegisterFontWithCustomName(fontKey, stream);
            }
        }

        return fontKey;
    }

    private static void ComposeHeader(IContainer container, IReadOnlyDictionary<string, string> values, string? logoPath, string fontFamily)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.ConstantItem(62).Height(58).Element(logo =>
                {
                    if (!string.IsNullOrWhiteSpace(logoPath) && File.Exists(logoPath))
                    {
                        logo.Image(logoPath).FitArea();
                    }
                    else
                    {
                        logo.Border(1).BorderColor(Colors.Grey.Lighten2).AlignCenter().AlignMiddle().Text("LOGO").FontSize(10);
                    }
                });
                row.RelativeItem().PaddingLeft(12).Column(headerText =>
                {
                    headerText.Item().Text(values["hospitalName"]).FontFamily(fontFamily).FontSize(18).Bold();
                    headerText.Item().Text("Hospital Operations Portal").FontSize(12);
                    headerText.Item().Text("ใบคำขอลา").FontSize(20).Bold();
                });
                row.ConstantItem(170).AlignRight().Column(meta =>
                {
                    meta.Item().Text($"เลขที่คำขอ: {values["requestNumber"]}").FontSize(13).SemiBold();
                    meta.Item().Text($"วันที่ยื่น: {values["submittedAt"]}").FontSize(13);
                });
            });

            column.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
        });
    }

    private static void ComposeEmployeeSection(IContainer container, IReadOnlyDictionary<string, string> values)
    {
        Section(container, "ข้อมูลผู้ขอลา", body =>
        {
            body.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Element(item => LabelValue(item, "รหัสพนักงาน", values["employeeCode"]));
                    row.RelativeItem().Element(item => LabelValue(item, "ชื่อ-นามสกุล", values["requesterName"]));
                });
                column.Item().Row(row =>
                {
                    row.RelativeItem().Element(item => LabelValue(item, "ตำแหน่ง", values["position"]));
                    row.RelativeItem().Element(item => LabelValue(item, "หน่วยงาน", values["departmentName"]));
                });
                column.Item().Row(row =>
                {
                    row.RelativeItem().Element(item => LabelValue(item, "เบอร์โทรศัพท์", values["phoneNumber"]));
                    row.RelativeItem().Element(item => LabelValue(item, "อีเมล", values["email"]));
                });
                column.Item().Element(item => LabelValue(item, "ที่อยู่ระหว่างลา", values["leaveContactAddress"]));
            });
        });
    }

    private static void ComposeLeaveSection(IContainer container, IReadOnlyDictionary<string, string> values)
    {
        Section(container, "ข้อมูลการลา", body =>
        {
            body.Column(column =>
            {
                column.Spacing(4);
                column.Item().Text("ประเภทการลา").FontSize(14).Bold();
                column.Item().Text(values["leaveTypeCheckboxLine1"]).FontSize(14);
                column.Item().Text(values["leaveTypeCheckboxLine2"]).FontSize(14);
                column.Item().Text("รูปแบบการลา").FontSize(14).Bold();
                column.Item().Text(values["durationCheckboxes"]).FontSize(14);
                column.Item().Row(row =>
                {
                    row.RelativeItem().Element(item => LabelValue(item, "ตั้งแต่วันที่", values["startDate"]));
                    row.RelativeItem().Element(item => LabelValue(item, "ถึงวันที่", values["endDate"]));
                    row.RelativeItem().Element(item => LabelValue(item, "จำนวนวันลา", $"{values["totalDays"]} วัน"));
                });
                column.Item().Row(row =>
                {
                    row.RelativeItem().Element(item => LabelValue(item, "วันทำการ", values["workingDays"]));
                    row.RelativeItem().Element(item => LabelValue(item, "วันหยุดราชการ", values["holidayDays"]));
                    row.RelativeItem().Element(item => LabelValue(item, "วันเสาร์-อาทิตย์", values["weekendDays"]));
                });
            });
        });
    }

    private static void ComposeBalanceSection(IContainer container, IReadOnlyDictionary<string, string> values)
    {
        Section(container, "วันลาคงเหลือ", body =>
        {
            body.Column(column =>
            {
                column.Spacing(6);
                column.Item().Text($"ประเภทลา: {values["balanceLeaveTypeName"]} | {values["balanceYearLabel"]}")
                    .FontSize(13)
                    .SemiBold();
                column.Item().Row(row =>
                {
                    row.RelativeItem().Element(item => LabelValue(item, $"ก่อนลา ({values["balanceLeaveTypeName"]})", values["balanceBefore"]));
                    row.RelativeItem().Element(item => LabelValue(item, "ใช้ครั้งนี้", values["balanceUsedThisRequest"]));
                    row.RelativeItem().Element(item => LabelValue(item, "รออนุมัติ", values["balancePending"]));
                    row.RelativeItem().Element(item => LabelValue(item, "คงเหลือหลังอนุมัติ", values["balanceAfterApproval"]));
                });
            });
        });
    }

    private static void ComposeReasonSection(IContainer container, IReadOnlyDictionary<string, string> values)
    {
        Section(container, "เหตุผลการลา", body =>
        {
            body.MinHeight(42).Text(values["reason"]).FontSize(14);
        });
    }

    private static void ComposeAttachmentSection(IContainer container, IReadOnlyDictionary<string, string> values)
    {
        Section(container, "เอกสารแนบ", body =>
        {
            body.Column(column =>
            {
                column.Item().Text(values["attachmentCheckboxes"]).FontSize(14);
                column.Item().Text($"จำนวนไฟล์แนบ: {values["attachmentCount"]}").FontSize(14);
            });
        });
    }

    private static void ComposeApprovalSection(IContainer container, IReadOnlyList<LeaveApproval> approvals)
    {
        Section(container, "การอนุมัติ", body =>
        {
            if (approvals.Count == 0)
            {
                body.Text("ยังไม่มีข้อมูลสายการอนุมัติ").FontSize(14);
                return;
            }

            body.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(42);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1.2f);
                });

                HeaderCell(table, "ขั้น");
                HeaderCell(table, "ผู้อนุมัติ");
                HeaderCell(table, "ตำแหน่ง");
                HeaderCell(table, "สถานะ");
                HeaderCell(table, "วันที่ดำเนินการ");
                HeaderCell(table, "หมายเหตุ");

                foreach (var approval in approvals)
                {
                    var item = BuildApprovalValues(approval);
                    BodyCell(table, item["stepOrder"]);
                    BodyCell(table, item["approverName"]);
                    BodyCell(table, item["approverPosition"]);
                    BodyCell(table, item["status"]);
                    BodyCell(table, item["actionAt"]);
                    BodyCell(table, item["remark"]);
                }
            });
        });
    }

    private static void ComposeApproverCommentsSection(IContainer container, IReadOnlyDictionary<string, string> values)
    {
        Section(container, "ความเห็นผู้อนุมัติ", body =>
        {
            body.Row(row =>
            {
                row.RelativeItem().Element(item => ApprovalCommentCard(
                    item,
                    "ความเห็นหัวหน้า",
                    values["headApproverName"],
                    values["headApprovalStatus"],
                    values["headApprovalActionAt"],
                    values["headApprovalRemark"]));

                row.RelativeItem().Element(item => ApprovalCommentCard(
                    item,
                    "ความเห็นผู้อำนวยการ",
                    values["directorApproverName"],
                    values["directorApprovalStatus"],
                    values["directorApprovalActionAt"],
                    values["directorApprovalRemark"]));
            });
        });
    }

    private static void ComposeFinalApprovalSection(IContainer container, IReadOnlyDictionary<string, string> values)
    {
        Section(container, "ผลการพิจารณา", body =>
        {
            body.Row(row =>
            {
                row.RelativeItem().Text(values["finalApprovalCheckboxes"]).FontSize(14);
                row.RelativeItem().Element(item => LabelValue(item, "ชื่อผู้อนุมัติ", values["finalApproverName"]));
                row.RelativeItem().Element(item => LabelValue(item, "วันที่อนุมัติ", values["finalActionAt"]));
            });
        });
    }

    private static void ApprovalCommentCard(IContainer container, string title, string approverName, string status, string actionAt, string remark)
    {
        container.PaddingRight(6)
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(6)
            .Column(column =>
            {
                column.Spacing(3);
                column.Item().Text(title).FontSize(14).Bold();
                column.Item().Text($"ผู้อนุมัติ: {approverName}").FontSize(12);
                column.Item().Text($"สถานะ: {status}").FontSize(12);
                column.Item().Text($"วันที่ดำเนินการ: {actionAt}").FontSize(12);
                column.Item().Text($"ความเห็น: {remark}").FontSize(12);
            });
    }

    private static void ComposeFooter(IContainer container, IReadOnlyDictionary<string, string> values)
    {
        container.AlignCenter().Text(text =>
        {
            text.DefaultTextStyle(style => style.FontSize(10).FontColor(Colors.Grey.Darken1));
            text.Span($"สร้างโดย Hospital Operations Portal | Version {values["applicationVersion"]} | Generated At {values["generatedAt"]} | Copyright © {values["hospitalName"]}");
        });
    }

    private static void Section(IContainer container, string title, Action<IContainer> content)
    {
        container.Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(8)
            .Column(column =>
            {
                column.Spacing(6);
                column.Item().Text(title).FontSize(16).Bold();
                column.Item().Element(content);
            });
    }

    private static void LabelValue(IContainer container, string label, string value)
    {
        container.PaddingRight(6).Column(column =>
        {
            column.Item().Text(label).FontSize(10).FontColor(Colors.Grey.Darken1);
            column.Item().Text(string.IsNullOrWhiteSpace(value) ? "-" : value).FontSize(14);
        });
    }

    private static void HeaderCell(TableDescriptor table, string text)
    {
        table.Cell().Background(Colors.Grey.Lighten3).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(4).Text(text).FontSize(11).Bold();
    }

    private static void BodyCell(TableDescriptor table, string text)
    {
        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(string.IsNullOrWhiteSpace(text) ? "-" : text).FontSize(10);
    }

    private static IReadOnlyList<PdfLine> BuildLines(LeaveRequest leaveRequest, LeavePdfRenderContext context)
    {
        var values = BuildFieldValues(leaveRequest, context);
        var approvals = leaveRequest.Approvals.OrderBy(item => item.StepOrder).ToList();

        var lines = new List<PdfLine>
        {
            new(values["hospitalName"], 50, 790, 18),
            new("Hospital Operations Portal", 50, 770, 14),
            new("ใบคำขอลา", 50, 746, 20),
            new($"เลขที่คำขอ: {values["requestNumber"]}", 50, 724, 14),
            new($"วันที่ยื่น: {values["submittedAt"]}", 330, 724, 14),
            new("ข้อมูลผู้ขอลา", 50, 696, 16),
            new($"รหัสพนักงาน: {values["employeeCode"]}", 70, 674, 14),
            new($"ชื่อ-นามสกุล: {values["requesterName"]}", 270, 674, 14),
            new($"ตำแหน่ง: {values["position"]}", 70, 654, 14),
            new($"หน่วยงาน: {values["departmentName"]}", 270, 654, 14),
            new($"เบอร์โทรศัพท์: {values["phoneNumber"]}", 70, 634, 14),
            new($"อีเมล: {values["email"]}", 270, 634, 14),
            new($"ที่อยู่ระหว่างลา: {values["leaveContactAddress"]}", 70, 614, 14),
            new("ประเภทการลา", 50, 586, 16),
            new(values["leaveTypeCheckboxLine1"], 70, 564, 14),
            new(values["leaveTypeCheckboxLine2"], 70, 544, 14),
            new("รูปแบบและระยะเวลาการลา", 50, 516, 16),
            new(values["durationCheckboxes"], 70, 494, 14),
            new($"ตั้งแต่วันที่: {values["startDate"]}   ถึงวันที่: {values["endDate"]}   จำนวนวันลา: {values["totalDays"]} วัน", 70, 474, 14),
            new($"วันทำการ: {values["workingDays"]}   วันหยุดราชการ: {values["holidayDays"]}   วันเสาร์-อาทิตย์: {values["weekendDays"]}", 70, 454, 14),
            new("วันลาคงเหลือ", 50, 426, 16),
            new($"ประเภทลา: {values["balanceLeaveTypeName"]} | {values["balanceYearLabel"]}", 70, 408, 13),
            new($"ก่อนลา ({values["balanceLeaveTypeName"]}): {values["balanceBefore"]}   ใช้ครั้งนี้: {values["balanceUsedThisRequest"]}   รออนุมัติ: {values["balancePending"]}   คงเหลือหลังอนุมัติ: {values["balanceAfterApproval"]}", 70, 392, 14),
            new("เหตุผลการลา", 50, 376, 16),
            new(values["reason"], 70, 354, 14),
            new("เอกสารแนบ", 50, 326, 16),
            new($"{values["attachmentCheckboxes"]}   จำนวนไฟล์แนบ: {values["attachmentCount"]}", 70, 304, 14),
            new("สายอนุมัติ", 50, 276, 16)
        };

        if (approvals.Count == 0)
        {
            lines.Add(new("ยังไม่มีข้อมูลสายการอนุมัติ", 70, 254, 14));
        }
        else
        {
            var y = 254;
            foreach (var approval in approvals)
            {
                var approvalValues = BuildApprovalValues(approval);
                lines.Add(new(
                    $"ขั้นที่ {approvalValues["stepOrder"]}: {approvalValues["stepName"]} | {approvalValues["approverName"]} | {approvalValues["approverPosition"]} | {approvalValues["status"]} | {approvalValues["actionAt"]}",
                    70,
                    y,
                    13));
                y -= 16;
                lines.Add(new($"ความเห็น: {approvalValues["remark"]}", 90, y, 12));
                y -= 18;
                if (y < 154)
                {
                    break;
                }
            }
        }

        lines.Add(new($"ความเห็นหัวหน้า: {values["headApprovalRemark"]}", 70, 140, 13));
        lines.Add(new($"ผู้อนุมัติ: {values["headApproverName"]} | สถานะ: {values["headApprovalStatus"]} | วันที่: {values["headApprovalActionAt"]}", 90, 124, 12));
        lines.Add(new($"ความเห็นผู้อำนวยการ: {values["directorApprovalRemark"]}", 70, 108, 13));
        lines.Add(new($"ผู้อนุมัติ: {values["directorApproverName"]} | สถานะ: {values["directorApprovalStatus"]} | วันที่: {values["directorApprovalActionAt"]}", 90, 92, 12));
        lines.Add(new("ผลการพิจารณา", 50, 76, 16));
        lines.Add(new($"{values["finalApprovalCheckboxes"]}   ผู้อนุมัติขั้นสุดท้าย: {values["finalApproverName"]}   วันที่: {values["finalActionAt"]}", 70, 62, 13));
        lines.Add(new($"สร้างโดย Hospital Operations Portal Version {values["applicationVersion"]} | Generated At {values["generatedAt"]} | Copyright © {values["hospitalName"]}", 50, 42, 10));

        return lines;
    }

    private static Dictionary<string, string> BuildFieldValues(LeaveRequest leaveRequest, LeavePdfRenderContext context)
    {
        var requester = leaveRequest.User?.FullName ?? "-";
        var employeeCode = leaveRequest.User?.EmployeeCode ?? "-";
        var department = leaveRequest.User?.Department?.Name ?? "-";
        var position = !string.IsNullOrWhiteSpace(leaveRequest.User?.Position)
            ? leaveRequest.User.Position
            : leaveRequest.User?.UserRoles.Select(item => item.Role?.Name).FirstOrDefault(item => !string.IsNullOrWhiteSpace(item)) ?? "-";
        var leaveType = leaveRequest.LeaveType?.Name ?? "-";
        var submittedAt = leaveRequest.SubmittedAt is null ? "-" : FormatDateTime(leaveRequest.SubmittedAt.Value);
        var businessDays = CountBusinessDays(leaveRequest.StartDate, leaveRequest.EndDate, context.Holidays);
        var weekendDays = CountWeekendDays(leaveRequest.StartDate, leaveRequest.EndDate);
        var holidayDays = context.Holidays.Count;
        var balanceBefore = context.LeaveBalance is null ? (decimal?)null : context.LeaveBalance.EntitledDays - context.LeaveBalance.UsedDays - context.LeaveBalance.PendingDays;
        decimal? balanceAfterApproval = balanceBefore is null ? null : balanceBefore.Value - leaveRequest.TotalDays;
        var balanceYear = context.LeaveBalance?.Year ?? FiscalYearHelper.ResolveBalanceYear(leaveRequest.StartDate, leaveRequest.LeaveType ?? new LeaveType());
        var balanceYearLabel = leaveRequest.LeaveType?.UseFiscalYear == true
            ? $"ปีงบประมาณ {balanceYear + 543}"
            : $"ปี {balanceYear + 543}";
        var finalApproval = leaveRequest.Approvals
            .OrderByDescending(item => item.StepOrder)
            .FirstOrDefault(item => item.Status is "Approved" or "Rejected");
        var headApproval = ResolveHeadApproval(leaveRequest.Approvals);
        var directorApproval = ResolveDirectorApproval(leaveRequest.Approvals);

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["hospitalName"] = context.HospitalName,
            ["applicationVersion"] = context.ApplicationVersion,
            ["generatedAt"] = FormatDateTime(DateTime.UtcNow),
            ["requestNumber"] = leaveRequest.RequestNumber ?? "-",
            ["requesterName"] = requester,
            ["employeeCode"] = employeeCode,
            ["position"] = position,
            ["departmentName"] = department,
            ["phoneNumber"] = leaveRequest.User?.PhoneNumber ?? "-",
            ["email"] = leaveRequest.User?.Email ?? "-",
            ["leaveContactAddress"] = leaveRequest.User?.LeaveContactAddress ?? "-",
            ["leaveTypeName"] = leaveType,
            ["leaveTypeCheckboxLine1"] = BuildLeaveTypeCheckboxLine(leaveRequest, 1),
            ["leaveTypeCheckboxLine2"] = BuildLeaveTypeCheckboxLine(leaveRequest, 2),
            ["startDate"] = FormatDate(leaveRequest.StartDate),
            ["endDate"] = FormatDate(leaveRequest.EndDate),
            ["totalDays"] = leaveRequest.TotalDays.ToString("0.##", CultureInfo.InvariantCulture),
            ["durationType"] = TranslateDurationType(leaveRequest.DurationType),
            ["durationCheckboxes"] = BuildDurationCheckboxes(leaveRequest.DurationType),
            ["workingDays"] = businessDays.ToString(CultureInfo.InvariantCulture),
            ["holidayDays"] = holidayDays.ToString(CultureInfo.InvariantCulture),
            ["weekendDays"] = weekendDays.ToString(CultureInfo.InvariantCulture),
            ["balanceBefore"] = FormatDecimalOrDash(balanceBefore),
            ["balanceUsedThisRequest"] = leaveRequest.TotalDays.ToString("0.##", CultureInfo.InvariantCulture),
            ["balancePending"] = FormatDecimalOrDash(context.LeaveBalance?.PendingDays),
            ["balanceAfterApproval"] = FormatDecimalOrDash(balanceAfterApproval),
            ["balanceLeaveTypeName"] = leaveType,
            ["balanceYearLabel"] = balanceYearLabel,
            ["reason"] = leaveRequest.Reason,
            ["attachmentCount"] = leaveRequest.Attachments.Count.ToString(CultureInfo.InvariantCulture),
            ["attachmentCheckboxes"] = BuildAttachmentCheckboxes(leaveRequest),
            ["submittedAt"] = submittedAt,
            ["status"] = TranslateStatus(leaveRequest.Status),
            ["currentApproverName"] = leaveRequest.CurrentApprover?.FullName ?? "-",
            ["finalApprovalCheckboxes"] = BuildFinalApprovalCheckboxes(leaveRequest.Status),
            ["finalApproverName"] = finalApproval?.Approver?.FullName ?? "-",
            ["finalApproverPosition"] = ResolvePosition(finalApproval?.Approver),
            ["finalActionAt"] = finalApproval?.ActionAt is null ? "-" : FormatDateTime(finalApproval.ActionAt.Value),
            ["finalRemark"] = string.IsNullOrWhiteSpace(finalApproval?.Remark) ? "-" : finalApproval.Remark,
            ["headApproverName"] = headApproval?.Approver?.FullName ?? "-",
            ["headApprovalStatus"] = headApproval is null ? "-" : TranslateStatus(headApproval.Status),
            ["headApprovalActionAt"] = headApproval?.ActionAt is null ? "-" : FormatDateTime(headApproval.ActionAt.Value),
            ["headApprovalRemark"] = string.IsNullOrWhiteSpace(headApproval?.Remark) ? "-" : headApproval.Remark,
            ["directorApproverName"] = directorApproval?.Approver?.FullName ?? "-",
            ["directorApprovalStatus"] = directorApproval is null ? "-" : TranslateStatus(directorApproval.Status),
            ["directorApprovalActionAt"] = directorApproval?.ActionAt is null ? "-" : FormatDateTime(directorApproval.ActionAt.Value),
            ["directorApprovalRemark"] = string.IsNullOrWhiteSpace(directorApproval?.Remark) ? "-" : directorApproval.Remark
        };
    }

    private static Dictionary<string, string> BuildApprovalValues(LeaveApproval approval)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["stepOrder"] = approval.StepOrder.ToString(CultureInfo.InvariantCulture),
            ["stepName"] = approval.StepName ?? "ขั้นอนุมัติ",
            ["approverName"] = approval.Approver?.FullName ?? "-",
            ["approverPosition"] = ResolvePosition(approval.Approver),
            ["status"] = TranslateStatus(approval.Status),
            ["actionAt"] = approval.ActionAt is null ? "-" : FormatDateTime(approval.ActionAt.Value),
            ["remark"] = string.IsNullOrWhiteSpace(approval.Remark) ? "-" : approval.Remark
        };
    }

    private static LeaveApproval? ResolveHeadApproval(IEnumerable<LeaveApproval> approvals)
    {
        return approvals
            .OrderBy(item => item.StepOrder)
            .FirstOrDefault(item => ContainsAny(item.StepName, "หัวหน้า", "Head"))
            ?? approvals.OrderBy(item => item.StepOrder).FirstOrDefault();
    }

    private static LeaveApproval? ResolveDirectorApproval(IEnumerable<LeaveApproval> approvals)
    {
        return approvals
            .OrderByDescending(item => item.StepOrder)
            .FirstOrDefault(item => ContainsAny(item.StepName, "ผู้อำนวยการ", "Director"))
            ?? approvals.OrderByDescending(item => item.StepOrder).FirstOrDefault();
    }

    private static bool ContainsAny(string? value, params string[] candidates)
    {
        return !string.IsNullOrWhiteSpace(value)
            && candidates.Any(candidate => value.Contains(candidate, StringComparison.OrdinalIgnoreCase));
    }

    private static string ResolvePosition(User? user)
    {
        if (!string.IsNullOrWhiteSpace(user?.Position))
        {
            return user.Position;
        }

        return user?.UserRoles.Select(item => item.Role?.Name).FirstOrDefault(item => !string.IsNullOrWhiteSpace(item)) ?? "-";
    }

    private static string BuildLeaveTypeCheckboxLine(LeaveRequest leaveRequest, int lineNumber)
    {
        var options = lineNumber == 1
            ? new[]
            {
                ("sick", "ลาป่วย"),
                ("personal", "ลากิจส่วนตัว"),
                ("annual", "ลาพักผ่อน"),
                ("maternity", "ลาคลอดบุตร"),
                ("ordination", "ลาอุปสมบท")
            }
            : new[]
            {
                ("paternity", "ลาไปช่วยภริยาคลอดบุตร"),
                ("study", "ลาศึกษาต่อ / อบรม"),
                ("official", "ลาไปปฏิบัติราชการ"),
                ("international", "ลาไปต่างประเทศ"),
                ("other", "อื่น ๆ")
            };

        return string.Join("   ", options.Select(item => $"{Checkbox(IsLeaveType(leaveRequest, item.Item1, item.Item2))} {item.Item2}"));
    }

    private static bool IsLeaveType(LeaveRequest leaveRequest, string code, string thaiName)
    {
        var leaveTypeCode = NormalizeCode(leaveRequest.LeaveType?.Code);
        var leaveTypeName = leaveRequest.LeaveType?.Name ?? string.Empty;
        string[] expectedCodes = code switch
        {
            "sick" => ["SICK", "SICKLEAVE", "SICK_LEAVE"],
            "personal" => ["PERSONAL", "PERSONALLEAVE", "PERSONAL_LEAVE"],
            "annual" => ["ANNUAL", "ANNUALLEAVE", "VACATION", "VACATIONLEAVE", "VACATION_LEAVE"],
            "maternity" => ["MATERNITY", "MATERNITYLEAVE", "MATERNITY_LEAVE"],
            "ordination" => ["ORDINATION", "ORDINATIONLEAVE", "ORDINATION_LEAVE", "MONK", "MONKHOOD"],
            "paternity" => ["PATERNITY", "PATERNITYLEAVE", "PATERNITY_LEAVE"],
            "study" => ["STUDY", "STUDYLEAVE", "STUDY_LEAVE", "TRAINING", "TRAINING_LEAVE"],
            "official" => ["OFFICIAL", "OFFICIALLEAVE", "OFFICIAL_LEAVE", "DUTY", "DUTY_LEAVE"],
            "international" => ["INTERNATIONAL", "INTERNATIONALLEAVE", "INTERNATIONAL_LEAVE", "ABROAD", "ABROAD_LEAVE"],
            _ => [NormalizeCode(code)]
        };

        if (expectedCodes.Any(item => leaveTypeCode.Equals(NormalizeCode(item), StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (leaveTypeName.Contains(thaiName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return code switch
        {
            "sick" => leaveTypeName.Contains("ป่วย", StringComparison.OrdinalIgnoreCase),
            "personal" => leaveTypeName.Contains("กิจ", StringComparison.OrdinalIgnoreCase),
            "annual" => leaveTypeName.Contains("พักผ่อน", StringComparison.OrdinalIgnoreCase),
            "maternity" => leaveTypeName.Contains("คลอด", StringComparison.OrdinalIgnoreCase),
            "ordination" => leaveTypeName.Contains("อุปสมบท", StringComparison.OrdinalIgnoreCase),
            "paternity" => leaveTypeName.Contains("ภริยา", StringComparison.OrdinalIgnoreCase) || leaveTypeName.Contains("ช่วย", StringComparison.OrdinalIgnoreCase),
            "study" => leaveTypeName.Contains("ศึกษา", StringComparison.OrdinalIgnoreCase) || leaveTypeName.Contains("อบรม", StringComparison.OrdinalIgnoreCase),
            "official" => leaveTypeName.Contains("ปฏิบัติราชการ", StringComparison.OrdinalIgnoreCase),
            "international" => leaveTypeName.Contains("ต่างประเทศ", StringComparison.OrdinalIgnoreCase),
            _ => code == "other" && !IsKnownLeaveType(leaveTypeCode, leaveTypeName)
        };
    }

    private static string BuildDurationCheckboxes(string? durationType)
    {
        var normalized = NormalizeDurationTypeForPdf(durationType);
        return string.Join("   ", new[]
        {
            $"{Checkbox(normalized == LeaveDurationTypes.FullDay)} เต็มวัน",
            $"{Checkbox(normalized == LeaveDurationTypes.HalfDayAm)} ครึ่งวัน (เช้า)",
            $"{Checkbox(normalized == LeaveDurationTypes.HalfDayPm)} ครึ่งวัน (บ่าย)"
        });
    }

    private static string BuildAttachmentCheckboxes(LeaveRequest leaveRequest)
    {
        var attachments = leaveRequest.Attachments.ToList();
        var hasMedicalCertificate = IsLeaveType(leaveRequest, "sick", "ลาป่วย") &&
            attachments.Any(item => ContainsAny(item.FileName, "medical", "doctor", "certificate", "ใบรับรอง", "แพทย์"));
        var hasInvitation = attachments.Any(item => ContainsAny(item.FileName, "invite", "invitation", "หนังสือเชิญ", "เชิญ"));
        var hasOfficialDocument = attachments.Any(item => ContainsAny(item.FileName, "official", "ราชการ", "คำสั่ง", "หนังสือ"));
        var hasOtherAttachments = attachments.Count > 0 && !hasMedicalCertificate && !hasInvitation && !hasOfficialDocument;

        return string.Join("   ", new[]
        {
            $"{Checkbox(hasMedicalCertificate)} ใบรับรองแพทย์",
            $"{Checkbox(hasInvitation)} หนังสือเชิญ",
            $"{Checkbox(hasOfficialDocument)} เอกสารราชการ",
            $"{Checkbox(hasOtherAttachments)} อื่น ๆ"
        });
    }

    private static string BuildFinalApprovalCheckboxes(string status)
    {
        var normalized = NormalizeCode(status);
        return $"{Checkbox(normalized == "APPROVED")} อนุมัติ   {Checkbox(normalized == "REJECTED")} ไม่อนุมัติ";
    }

    private static string Checkbox(bool isChecked)
    {
        return isChecked ? "[X]" : "[ ]";
    }

    private static string NormalizeDurationTypeForPdf(string? durationType)
    {
        var normalized = NormalizeCode(durationType);
        return normalized switch
        {
            "" or "FULLDAY" or "FULL_DAY" or "FULL" or "เต็มวัน" => LeaveDurationTypes.FullDay,
            "HALFDAYAM" or "HALF_DAY_AM" or "MORNING" or "AM" or "เช้า" => LeaveDurationTypes.HalfDayAm,
            "HALFDAYPM" or "HALF_DAY_PM" or "AFTERNOON" or "PM" or "บ่าย" => LeaveDurationTypes.HalfDayPm,
            _ => LeaveDurationTypes.Normalize(durationType)
        };
    }

    private static bool IsKnownLeaveType(string normalizedCode, string leaveTypeName)
    {
        var knownCodes = new[]
        {
            "SICK", "SICKLEAVE", "SICK_LEAVE",
            "PERSONAL", "PERSONALLEAVE", "PERSONAL_LEAVE",
            "ANNUAL", "ANNUALLEAVE", "VACATION", "VACATIONLEAVE", "VACATION_LEAVE",
            "MATERNITY", "MATERNITYLEAVE", "MATERNITY_LEAVE",
            "ORDINATION", "ORDINATIONLEAVE", "ORDINATION_LEAVE",
            "PATERNITY", "PATERNITYLEAVE", "PATERNITY_LEAVE",
            "STUDY", "STUDYLEAVE", "STUDY_LEAVE", "TRAINING", "TRAINING_LEAVE",
            "OFFICIAL", "OFFICIALLEAVE", "OFFICIAL_LEAVE",
            "INTERNATIONAL", "INTERNATIONALLEAVE", "INTERNATIONAL_LEAVE", "ABROAD", "ABROAD_LEAVE"
        };

        return knownCodes.Any(item => normalizedCode.Equals(NormalizeCode(item), StringComparison.OrdinalIgnoreCase)) ||
            ContainsAny(leaveTypeName, "ป่วย", "กิจ", "พักผ่อน", "คลอด", "อุปสมบท", "ภริยา", "ศึกษา", "อบรม", "ปฏิบัติราชการ", "ต่างประเทศ");
    }

    private static string NormalizeCode(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().Replace("-", "_", StringComparison.Ordinal).ToUpperInvariant();
    }

    private static int CountBusinessDays(DateOnly startDate, DateOnly endDate, IReadOnlyList<LeaveHoliday> holidays)
    {
        var holidayDates = holidays.Select(item => item.HolidayDate).ToHashSet();
        var count = 0;
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || holidayDates.Contains(date))
            {
                continue;
            }

            count++;
        }

        return count;
    }

    private static int CountWeekendDays(DateOnly startDate, DateOnly endDate)
    {
        var count = 0;
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                count++;
            }
        }

        return count;
    }

    private static string FormatDecimalOrDash(decimal? value)
    {
        return value is null ? "-" : value.Value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static string FormatDate(DateOnly date)
    {
        var buddhistYear = date.Year + 543;
        return $"{date.Day:00}/{date.Month:00}/{buddhistYear}";
    }

    private static string FormatDateTime(DateTime value)
    {
        var local = value.ToLocalTime();
        return $"{local.Day:00}/{local.Month:00}/{local.Year + 543} {local:HH:mm}";
    }

    private static string TranslateStatus(string status)
    {
        return status switch
        {
            "Draft" => "แบบร่าง",
            "Pending" => "รออนุมัติ",
            "Approved" => "อนุมัติแล้ว",
            "Rejected" => "ไม่อนุมัติ",
            "Cancelled" => "ยกเลิก",
            "Waiting" => "รอดำเนินการ",
            "Skipped" => "ข้ามขั้นตอน",
            _ => status
        };
    }

    private static string TranslateDurationType(string? durationType)
    {
        return durationType switch
        {
            "HALF_DAY_AM" => "ครึ่งวัน (เช้า)",
            "HALF_DAY_PM" => "ครึ่งวัน (บ่าย)",
            "FULL_DAY" or null or "" => "เต็มวัน",
            _ => durationType
        };
    }
}

public sealed class LeaveFormTemplateConfig
{
    public DocumentTemplateSettings? DocumentSettings { get; set; }
    public List<TemplateText> StaticText { get; set; } = [];
    public List<TemplateField> Fields { get; set; } = [];
    public ApprovalRowsTemplate? ApprovalRows { get; set; }
}

public sealed class DocumentTemplateSettings
{
    public string FontFamily { get; set; } = "TH SarabunPSK";
    public int FontSize { get; set; } = 16;
    public double LineHeight { get; set; } = 1.2;
}

public sealed class TemplateText
{
    public string Text { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public string? FontFamily { get; set; }
    public int? FontSize { get; set; }
    public double? LineHeight { get; set; }
}

public sealed class TemplateField
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public string? FontFamily { get; set; }
    public int? FontSize { get; set; }
    public double? LineHeight { get; set; }
}

public sealed class ApprovalRowsTemplate
{
    public double X { get; set; }
    public double StartY { get; set; }
    public double? RowHeight { get; set; }
    public string? FontFamily { get; set; }
    public int? FontSize { get; set; }
    public double? LineHeight { get; set; }
    public int MaxRows { get; set; } = 8;
    public string Format { get; set; } = "{{stepOrder}}. {{stepName}} - {{approverName}} - {{status}} - {{actionAt}} - {{remark}}";
}

public sealed record PdfLine(string Text, double X, double Y, int FontSize, string? FontFamily = null);

public sealed record PdfImage(int Width, int Height, byte[] RgbBytes);

public static class SimplePdfWriter
{
    public static byte[] CreateA4(IReadOnlyList<PdfLine> lines, PdfImage? logo)
    {
        return CreateA4Pages([lines], logo);
    }

    public static byte[] CreateA4Pages(IReadOnlyList<IReadOnlyList<PdfLine>> pages, PdfImage? logo)
    {
        if (pages.Count == 0)
        {
            pages = [[new PdfLine(string.Empty, 50, 790, 10)]];
        }

        var fontFamily = pages
            .SelectMany(page => page)
            .Select(line => line.FontFamily)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            ?? "TH SarabunPSK";
        var pdfFontName = ToPdfFontName(fontFamily);
        var hasLogo = logo is not null;
        var pageCount = pages.Count;
        var fontObjectNumber = 3 + pageCount;
        var cidFontObjectNumber = fontObjectNumber + 1;
        var unicodeMapObjectNumber = fontObjectNumber + 2;
        var imageObjectNumber = hasLogo ? fontObjectNumber + 3 : 0;
        var contentStartObjectNumber = fontObjectNumber + 3 + (hasLogo ? 1 : 0);
        var pageResources = hasLogo
            ? $"<< /Font << /F1 {fontObjectNumber} 0 R >> /XObject << /Im1 {imageObjectNumber} 0 R >> >>"
            : $"<< /Font << /F1 {fontObjectNumber} 0 R >> >>";
        var kids = string.Join(' ', Enumerable.Range(3, pageCount).Select(number => $"{number} 0 R"));
        var unicodeMap = Encoding.ASCII.GetBytes("/CIDInit /ProcSet findresource begin\n12 dict begin\nbegincmap\n/CIDSystemInfo << /Registry (Adobe) /Ordering (UCS) /Supplement 0 >> def\n/CMapName /Adobe-Identity-UCS def\n/CMapType 2 def\n1 begincodespacerange\n<0000> <FFFF>\nendcodespacerange\n1 beginbfrange\n<0000> <FFFF> <0000>\nendbfrange\nendcmap\nCMapName currentdict /CMap defineresource pop\nend\nend");

        var objects = new List<byte[]>
        {
            Encoding.ASCII.GetBytes("<< /Type /Catalog /Pages 2 0 R >>"),
            Encoding.ASCII.GetBytes($"<< /Type /Pages /Kids [{kids}] /Count {pageCount} >>")
        };

        for (var index = 0; index < pageCount; index++)
        {
            objects.Add(Encoding.ASCII.GetBytes($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources {pageResources} /Contents {contentStartObjectNumber + index} 0 R >>"));
        }

        objects.Add(Encoding.ASCII.GetBytes($"<< /Type /Font /Subtype /Type0 /BaseFont /{pdfFontName} /Encoding /Identity-H /DescendantFonts [{cidFontObjectNumber} 0 R] /ToUnicode {unicodeMapObjectNumber} 0 R >>"));
        objects.Add(Encoding.ASCII.GetBytes($"<< /Type /Font /Subtype /CIDFontType2 /BaseFont /{pdfFontName} /CIDSystemInfo << /Registry (Adobe) /Ordering (Identity) /Supplement 0 >> /DW 1000 >>"));
        objects.Add(WrapStream(unicodeMap));

        if (logo is not null)
        {
            objects.Add(WrapImage(logo));
        }

        foreach (var page in pages)
        {
            objects.Add(WrapStream(Encoding.ASCII.GetBytes(BuildPageContent(page, logo))));
        }

        return BuildPdf(objects);
    }

    private static string BuildPageContent(IReadOnlyList<PdfLine> lines, PdfImage? logo)
    {
        var content = new StringBuilder();
        if (logo is null)
        {
            content.AppendLine("q");
            content.AppendLine("0.95 0.95 0.95 rg 480 760 65 50 re f");
            content.AppendLine("0 0 0 RG 480 760 65 50 re S");
            content.AppendLine("Q");
        }
        else
        {
            content.AppendLine("q 55 0 0 55 485 758 cm /Im1 Do Q");
        }

        foreach (var line in lines)
        {
            content.Append("BT /F1 ")
                .Append(line.FontSize)
                .Append(" Tf 1 0 0 1 ")
                .Append(line.X.ToString(CultureInfo.InvariantCulture))
                .Append(' ')
                .Append(line.Y.ToString(CultureInfo.InvariantCulture))
                .Append(" Tm ")
                .Append(ToPdfHexString(line.Text))
                .AppendLine(" Tj ET");
        }

        return content.ToString();
    }

    private static string ToPdfFontName(string fontFamily)
    {
        var chars = fontFamily.Where(char.IsLetterOrDigit).ToArray();
        return chars.Length == 0 ? "THSarabunPSK" : new string(chars);
    }

    private static string ToPdfHexString(string value)
    {
        var bytes = Encoding.BigEndianUnicode.GetBytes(value);
        return "<FEFF" + Convert.ToHexString(bytes) + ">";
    }

    private static byte[] WrapStream(byte[] content)
    {
        var header = Encoding.ASCII.GetBytes($"<< /Length {content.Length} >>\nstream\n");
        var footer = Encoding.ASCII.GetBytes("\nendstream");
        return [.. header, .. content, .. footer];
    }

    private static byte[] WrapImage(PdfImage image)
    {
        var compressed = Compress(image.RgbBytes);
        var header = Encoding.ASCII.GetBytes(
            $"<< /Type /XObject /Subtype /Image /Width {image.Width} /Height {image.Height} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /FlateDecode /Length {compressed.Length} >>\nstream\n");
        var footer = Encoding.ASCII.GetBytes("\nendstream");
        return [.. header, .. compressed, .. footer];
    }

    private static byte[] Compress(byte[] bytes)
    {
        using var output = new MemoryStream();
        using (var deflate = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            deflate.Write(bytes);
        }

        return output.ToArray();
    }

    private static byte[] BuildPdf(IReadOnlyList<byte[]> objects)
    {
        using var stream = new MemoryStream();
        var header = Encoding.ASCII.GetBytes("%PDF-1.7\n%\xE2\xE3\xCF\xD3\n");
        stream.Write(header);

        var offsets = new List<long> { 0 };
        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(stream.Position);
            WriteAscii(stream, $"{index + 1} 0 obj\n");
            stream.Write(objects[index]);
            WriteAscii(stream, "\nendobj\n");
        }

        var xrefOffset = stream.Position;
        WriteAscii(stream, $"xref\n0 {objects.Count + 1}\n");
        WriteAscii(stream, "0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            WriteAscii(stream, $"{offset:0000000000} 00000 n \n");
        }

        WriteAscii(stream, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");
        return stream.ToArray();
    }

    private static void WriteAscii(Stream stream, string value)
    {
        stream.Write(Encoding.ASCII.GetBytes(value));
    }
}

public static class PngImageReader
{
    public static bool TryReadRgb(string path, out PdfImage? image)
    {
        image = null;
        try
        {
            var bytes = File.ReadAllBytes(path);
            if (!bytes.Take(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            {
                return false;
            }

            var offset = 8;
            var width = 0;
            var height = 0;
            byte bitDepth = 0;
            byte colorType = 0;
            using var idat = new MemoryStream();

            while (offset < bytes.Length)
            {
                var length = ReadInt32(bytes, offset);
                var type = Encoding.ASCII.GetString(bytes, offset + 4, 4);
                var dataOffset = offset + 8;
                if (type == "IHDR")
                {
                    width = ReadInt32(bytes, dataOffset);
                    height = ReadInt32(bytes, dataOffset + 4);
                    bitDepth = bytes[dataOffset + 8];
                    colorType = bytes[dataOffset + 9];
                }
                else if (type == "IDAT")
                {
                    idat.Write(bytes, dataOffset, length);
                }
                else if (type == "IEND")
                {
                    break;
                }

                offset += length + 12;
            }

            if (width <= 0 || height <= 0 || bitDepth != 8 || colorType is not (2 or 6))
            {
                return false;
            }

            var channelCount = colorType == 6 ? 4 : 3;
            var decompressed = Decompress(idat.ToArray());
            var stride = width * channelCount;
            var previous = new byte[stride];
            var current = new byte[stride];
            var rgb = new byte[width * height * 3];
            var inputOffset = 0;
            var outputOffset = 0;

            for (var y = 0; y < height; y++)
            {
                var filter = decompressed[inputOffset++];
                Array.Copy(decompressed, inputOffset, current, 0, stride);
                inputOffset += stride;
                Unfilter(current, previous, filter, channelCount);

                for (var x = 0; x < width; x++)
                {
                    var pixelOffset = x * channelCount;
                    rgb[outputOffset++] = current[pixelOffset];
                    rgb[outputOffset++] = current[pixelOffset + 1];
                    rgb[outputOffset++] = current[pixelOffset + 2];
                }

                (previous, current) = (current, previous);
            }

            image = new PdfImage(width, height, rgb);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static byte[] Decompress(byte[] bytes)
    {
        using var input = new MemoryStream(bytes);
        using var zlib = new ZLibStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        zlib.CopyTo(output);
        return output.ToArray();
    }

    private static void Unfilter(byte[] current, byte[] previous, byte filter, int bytesPerPixel)
    {
        for (var i = 0; i < current.Length; i++)
        {
            var left = i >= bytesPerPixel ? current[i - bytesPerPixel] : (byte)0;
            var up = previous[i];
            var upperLeft = i >= bytesPerPixel ? previous[i - bytesPerPixel] : (byte)0;
            current[i] = filter switch
            {
                0 => current[i],
                1 => (byte)(current[i] + left),
                2 => (byte)(current[i] + up),
                3 => (byte)(current[i] + ((left + up) / 2)),
                4 => (byte)(current[i] + Paeth(left, up, upperLeft)),
                _ => current[i]
            };
        }
    }

    private static byte Paeth(byte left, byte up, byte upperLeft)
    {
        var p = left + up - upperLeft;
        var pa = Math.Abs(p - left);
        var pb = Math.Abs(p - up);
        var pc = Math.Abs(p - upperLeft);
        if (pa <= pb && pa <= pc)
        {
            return left;
        }

        return pb <= pc ? up : upperLeft;
    }

    private static int ReadInt32(byte[] bytes, int offset)
    {
        return (bytes[offset] << 24) |
            (bytes[offset + 1] << 16) |
            (bytes[offset + 2] << 8) |
            bytes[offset + 3];
    }
}
