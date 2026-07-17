using System.Text.Json;
using Hop.Api.Interfaces;
using Hop.Api.Models;

namespace Hop.Api.Services;

public static class LeaveLineFlexMessageTemplates
{
    private const string DetailBlue = "#2563EB";
    private const string ApproveGreen = "#16A34A";
    private const string RejectRed = "#DC2626";
    private const string CancelledGray = "#6B7280";
    private const string HeaderGreen = "#064E3B";
    private const string HeaderGreenSoft = "#0B6B4A";
    private const string Gold = "#D4AF37";
    private const string GoldMuted = "#F3D08A";
    private const string Surface = "#FFFFFF";
    private const string SoftSurface = "#F8FAFC";
    private const string TextPrimary = "#1F2937";
    private const string TextMuted = "#6B7280";

    public static string BuildPendingApprovalCard(LeaveRequest request, string publicAppUrl, UserAvatarInfo? avatar = null)
    {
        return BuildPendingCard(
            request,
            publicAppUrl,
            avatar,
            "คำขอลารออนุมัติ",
            "รออนุมัติ",
            "#C8A96B");
    }

    public static string BuildNextApproverCard(LeaveRequest request, string publicAppUrl, UserAvatarInfo? avatar = null)
    {
        return BuildPendingCard(
            request,
            publicAppUrl,
            avatar,
            "คำขอลารออนุมัติขั้นถัดไป",
            "รออนุมัติจากคุณ",
            "#C8A96B");
    }

    public static string BuildApprovedCard(LeaveRequest request, string publicAppUrl, UserAvatarInfo? avatar = null)
    {
        return BuildResultCard(request, publicAppUrl, avatar, "คำขอลาอนุมัติแล้ว", "อนุมัติแล้ว", ApproveGreen);
    }

    public static string BuildApprovedToRequesterCard(LeaveRequest request, string publicAppUrl, UserAvatarInfo? avatar = null)
    {
        return BuildApprovedCard(request, publicAppUrl, avatar);
    }

    public static string BuildRejectedCard(LeaveRequest request, string publicAppUrl, UserAvatarInfo? avatar = null)
    {
        return BuildResultCard(request, publicAppUrl, avatar, "คำขอลาไม่อนุมัติ", "ไม่อนุมัติ", RejectRed);
    }

    public static string BuildRejectedToRequesterCard(LeaveRequest request, string publicAppUrl, UserAvatarInfo? avatar = null)
    {
        return BuildRejectedCard(request, publicAppUrl, avatar);
    }

    public static string BuildCancelledCard(LeaveRequest request, string publicAppUrl, UserAvatarInfo? avatar = null)
    {
        return BuildResultCard(request, publicAppUrl, avatar, "คำขอลาถูกยกเลิก", "ยกเลิกแล้ว", CancelledGray);
    }

    public static string BuildReturnedForRevisionCard(LeaveRequest request, string publicAppUrl, UserAvatarInfo? avatar = null)
    {
        return BuildResultCard(request, publicAppUrl, avatar, "คำขอลาถูกตีกลับ", "ตีกลับรอแก้ไข", "#D97706");
    }

    public static string BuildRevisionCancelledCard(LeaveRequest request, string publicAppUrl, UserAvatarInfo? avatar = null)
    {
        return BuildResultCard(request, publicAppUrl, avatar, "คำขอลาถูกยกเลิก", "ยกเลิกแล้ว", CancelledGray);
    }

    public static string BuildCancellationSubmittedCard(LeaveCancellationRequest request, string publicAppUrl)
    {
        return BuildCancellationCard(request, publicAppUrl, "คำขอยกเลิกใบลารออนุมัติ", "กรุณาพิจารณาอนุมัติคำขอยกเลิกใบลา", "รออนุมัติ", "#C8A96B", true);
    }

    public static string BuildCancellationApprovedCard(LeaveCancellationRequest request, string publicAppUrl)
    {
        return BuildCancellationCard(request, publicAppUrl, "คำขอยกเลิกใบลาอนุมัติแล้ว", "ใบลาเดิมถูกยกเลิกและคืนยอดวันลาแล้ว", "อนุมัติแล้ว", ApproveGreen, false);
    }

    public static string BuildCancellationRejectedCard(LeaveCancellationRequest request, string publicAppUrl)
    {
        return BuildCancellationCard(request, publicAppUrl, "คำขอยกเลิกใบลาไม่อนุมัติ", "คำขอยกเลิกใบลาไม่ได้รับการอนุมัติ", "ไม่อนุมัติ", RejectRed, false);
    }

    public static string BuildCancellationReturnedCard(LeaveCancellationRequest request, string publicAppUrl)
    {
        return BuildCancellationCard(request, publicAppUrl, "คำขอยกเลิกใบลาถูกตีกลับ", "กรุณาแก้ไขและส่งคำขอใหม่อีกครั้ง", "ตีกลับรอแก้ไข", "#D97706", false);
    }

    public static string BuildCancellationCancelledCard(LeaveCancellationRequest request, string publicAppUrl)
    {
        return BuildCancellationCard(request, publicAppUrl, "คำขอยกเลิกใบลาถูกยกเลิก", "คำขอยกเลิกใบลาถูกยกเลิกแล้ว", "ยกเลิกแล้ว", CancelledGray, false);
    }

    private static string BuildPendingCard(LeaveRequest request, string publicAppUrl, UserAvatarInfo? avatar, string header, string status, string statusColor)
    {
        var detailUrl = BuildUrl(publicAppUrl, $"/leave/{request.Id}");
        var approveUrl = BuildUrl(publicAppUrl, $"/line/leave-approval/{request.Id}?action=approve");
        var rejectUrl = BuildUrl(publicAppUrl, $"/line/leave-approval/{request.Id}?action=reject");
        var footerContents = new List<object>
        {
            new
            {
                type = "box",
                layout = "horizontal",
                spacing = "md",
                contents = new object[]
                {
                    ActionButton("อนุมัติ", approveUrl, ApproveGreen, 1),
                    ActionButton("ไม่อนุมัติ", rejectUrl, RejectRed, 1)
                }
            },
            ActionButton("ดูรายละเอียด", detailUrl, DetailBlue)
        };
        return BuildPayload(request, publicAppUrl, avatar, header, "กรุณาพิจารณาอนุมัติคำขอลา", status, statusColor, footerContents, true);
    }

    private static string BuildResultCard(LeaveRequest request, string publicAppUrl, UserAvatarInfo? avatar, string header, string status, string statusColor)
    {
        var detailUrl = BuildUrl(publicAppUrl, $"/leave/{request.Id}");
        var footerContents = new List<object>
        {
            ActionButton("ดูรายละเอียด", detailUrl, DetailBlue)
        };
        var subtitle = status switch
        {
            "อนุมัติแล้ว" => "คำขอได้รับการอนุมัติเรียบร้อย",
            "ไม่อนุมัติ" => "คำขอไม่ได้รับการอนุมัติ",
            "ยกเลิกแล้ว" => "คำขอถูกยกเลิกเรียบร้อย",
            _ => "ตรวจสอบสถานะคำขอลา"
        };
        return BuildPayload(request, publicAppUrl, avatar, header, subtitle, status, statusColor, footerContents, false);
    }

    private static string BuildCancellationCard(
        LeaveCancellationRequest request,
        string publicAppUrl,
        string header,
        string subtitle,
        string status,
        string statusColor,
        bool includeApprovalProgress)
    {
        var detailUrl = BuildUrl(publicAppUrl, $"/leave/cancellations/{request.Id}");
        var footerContents = new List<object>
        {
            ActionButton("ดูรายละเอียด", detailUrl, DetailBlue)
        };

        return BuildPayload(
            RequestCode(request),
            request.RequesterUser,
            request.RequesterUser?.Department?.Name,
            request.LeaveType?.Name,
            request.OriginalLeaveRequest?.StartDate ?? DateOnly.FromDateTime(request.CreatedAt),
            request.OriginalLeaveRequest?.EndDate ?? DateOnly.FromDateTime(request.CreatedAt),
            request.OriginalLeaveDays,
            request.Reason,
            publicAppUrl,
            null,
            header,
            subtitle,
            status,
            statusColor,
            footerContents,
            includeApprovalProgress,
            request.Approvals.Select(item => new ApprovalSnapshot(item.StepOrder, item.StepName, item.Status)),
            "คำขอยกเลิกใบลา");
    }

    private static string BuildPayload(
        LeaveRequest request,
        string publicAppUrl,
        UserAvatarInfo? avatar,
        string header,
        string subtitle,
        string status,
        string statusColor,
        List<object> footerContents,
        bool includeApprovalProgress)
    {
        return BuildPayload(
            RequestCode(request),
            request.User,
            request.User?.Department?.Name,
            request.LeaveType?.Name,
            request.StartDate,
            request.EndDate,
            request.TotalDays,
            request.Reason,
            publicAppUrl,
            avatar,
            header,
            subtitle,
            status,
            statusColor,
            footerContents,
            includeApprovalProgress,
            request.Approvals.Select(item => new ApprovalSnapshot(item.StepOrder, item.StepName, item.Status)),
            "คำขอลา");
    }

    private static string BuildPayload(
        string requestCode,
        User? requester,
        string? departmentName,
        string? leaveTypeName,
        DateOnly startDate,
        DateOnly endDate,
        decimal totalDays,
        string? reason,
        string publicAppUrl,
        UserAvatarInfo? avatar,
        string header,
        string subtitle,
        string status,
        string statusColor,
        List<object> footerContents,
        bool includeApprovalProgress,
        IEnumerable<ApprovalSnapshot> approvals,
        string requestCategory)
    {
        var approvalList = approvals.ToList();
        var currentStep = approvalList
            .OrderBy(item => item.StepOrder)
            .FirstOrDefault(item => item.Status == "Pending")?.StepName ?? "-";
        var bodyContents = new List<object>
        {
            InfoRow("เลขที่คำขอ", requestCode, Gold),
            InfoRow("ผู้ขอ", requester?.FullName ?? "-", TextPrimary),
            InfoRow("หน่วยงาน", departmentName ?? "-", TextPrimary),
            InfoRow("ประเภทการลา", leaveTypeName ?? "-", TextPrimary),
            InfoRow("วันที่ลา", $"{FormatDate(startDate)} ถึง {FormatDate(endDate)}", TextPrimary),
            InfoRow("จำนวนวัน", $"{totalDays:0.##} วัน", TextPrimary)
        };

        if (!string.IsNullOrWhiteSpace(reason) && status != "อนุมัติแล้ว")
        {
            bodyContents.Add(InfoRow("เหตุผล", Truncate(reason, 120), TextPrimary));
        }

        bodyContents.Add(StatusPanel(status, statusColor, includeApprovalProgress ? currentStep : null));

        if (includeApprovalProgress)
        {
            bodyContents.Add(ApprovalProgressPanel(currentStep));
        }

        var payload = new
        {
            to = string.Empty,
            messages = new object[]
            {
                new
                {
                    type = "flex",
                    altText = $"{header} {requestCode}",
                    contents = new
                    {
                        type = "bubble",
                        size = "mega",
                        hero = HeaderSection(requester, requestCode, header, subtitle, statusColor, avatar, requestCategory),
                        body = new
                        {
                            type = "box",
                            layout = "vertical",
                            backgroundColor = Surface,
                            paddingAll = "20px",
                            spacing = "md",
                            contents = bodyContents.ToArray()
                        },
                        footer = new
                        {
                            type = "box",
                            layout = "vertical",
                            spacing = "md",
                            paddingAll = "16px",
                            contents = footerContents.ToArray()
                        }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(payload);
    }

    private static object HeaderSection(User? requester, string requestCode, string title, string subtitle, string accentColor, UserAvatarInfo? avatar, string requestCategory)
    {
        return new
        {
            type = "box",
            layout = "vertical",
            backgroundColor = title.Contains("ไม่อนุมัติ", StringComparison.Ordinal) ? "#991B1B" : HeaderGreen,
            paddingAll = "20px",
            spacing = "md",
            contents = new object[]
            {
                new
                {
                    type = "box",
                    layout = "horizontal",
                    spacing = "md",
                    contents = new object[]
                    {
                        Avatar(requester, avatar),
                        new
                        {
                            type = "box",
                            layout = "vertical",
                            spacing = "xs",
                            flex = 4,
                            contents = new object[]
                            {
                                Text(title, "#FFFFFF", "xl", "bold", wrap: true),
                                Text(subtitle, "#F8FAFC", "sm", "regular", wrap: true)
                            }
                        }
                    }
                },
                new
                {
                    type = "separator",
                    color = accentColor == RejectRed ? "#FCA5A5" : Gold
                },
                Text($"เลขที่คำขอ {requestCode}", "#FDE68A", "md", "bold", wrap: true)
            }
        };
    }

    private static object Avatar(User? user, UserAvatarInfo? avatar)
    {
        var imageUrl = avatar?.ImageUrl;
        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            return new
            {
                type = "image",
                url = imageUrl,
                size = "56px",
                aspectRatio = "1:1",
                aspectMode = "cover",
                flex = 1
            };
        }

        return new
        {
            type = "box",
            layout = "vertical",
            width = "56px",
            height = "56px",
            cornerRadius = "28px",
            backgroundColor = Surface,
            borderColor = Gold,
            borderWidth = "2px",
            justifyContent = "center",
            alignItems = "center",
            contents = new object[]
            {
                new
                {
                    type = "text",
                    text = avatar?.Initials ?? Initials(user?.FullName),
                    color = HeaderGreenSoft,
                    align = "center",
                    weight = "bold",
                    size = "xl"
                }
            }
        };
    }

    private static object InfoRow(string label, string value, string valueColor)
    {
        return new
        {
            type = "box",
            layout = "horizontal",
            spacing = "sm",
            contents = new object[]
            {
                Text("...", Gold, "sm", "bold", 1),
                Text(label, TextMuted, "sm", "regular", 4),
                Text(":", Gold, "sm", "bold", 1),
                Text(value, valueColor, "sm", "bold", 7, true)
            }
        };
    }

    private static object Text(string text, string color, string size, string weight, int? flex = null, bool wrap = false, string? align = null)
    {
        var item = new Dictionary<string, object?>
        {
            ["type"] = "text",
            ["text"] = text,
            ["color"] = color,
            ["size"] = size,
            ["weight"] = weight,
            ["wrap"] = wrap
        };
        if (flex is not null)
        {
            item["flex"] = flex.Value;
        }

        if (!string.IsNullOrWhiteSpace(align))
        {
            item["align"] = align;
        }

        return item;
    }

    private static object StatusPanel(string status, string color, string? currentStep)
    {
        return new
        {
            type = "box",
            layout = "vertical",
            margin = "md",
            paddingAll = "12px",
            cornerRadius = "lg",
            backgroundColor = color == RejectRed ? "#FEF2F2" : color == ApproveGreen ? "#F0FDF4" : "#FFFBEB",
            borderColor = color == RejectRed ? "#FCA5A5" : color == ApproveGreen ? "#86EFAC" : GoldMuted,
            borderWidth = "1px",
            spacing = "xs",
            contents = new object[]
            {
                new
                {
                    type = "box",
                    layout = "horizontal",
                    spacing = "sm",
                    contents = new object[]
                    {
                        Text(StatusIcon(status), color, "lg", "bold", 1),
                        Text("สถานะ", TextMuted, "sm", "regular", 3),
                        Text(status, color, "sm", "bold", 7, true)
                    }
                },
                Text(currentStep is null ? StatusDescription(status) : $"ขั้นตอนปัจจุบัน: {currentStep}", color, "xs", "regular", wrap: true)
            }
        };
    }

    private static object ApprovalProgressPanel(string currentStep)
    {
        return new
        {
            type = "box",
            layout = "vertical",
            margin = "md",
            paddingAll = "12px",
            cornerRadius = "lg",
            backgroundColor = "#FFFBEB",
            borderColor = GoldMuted,
            borderWidth = "1px",
            spacing = "sm",
            contents = new object[]
            {
                new
                {
                    type = "box",
                    layout = "horizontal",
                    contents = new object[]
                    {
                        Text("สถานะปัจจุบัน", TextPrimary, "sm", "bold", 5),
                        Text(currentStep == "-" ? "รอการอนุมัติ" : currentStep, "#B7791F", "sm", "bold", 7, true, "end")
                    }
                },
                Text("1  ─────  2  ─────  3", Gold, "sm", "bold", wrap: true, align: "center"),
                Text("หัวหน้าแผนก        ผอ.ฝ่าย        สำเร็จ", TextMuted, "xs", "regular", wrap: true, align: "center")
            }
        };
    }

    private static object ActionButton(string label, string uri, string color, int? flex = null)
    {
        var button = new Dictionary<string, object?>
        {
            ["type"] = "button",
            ["style"] = "primary",
            ["height"] = "sm",
            ["color"] = color,
            ["action"] = new
            {
                type = "uri",
                label,
                uri
            }
        };
        if (flex is not null)
        {
            button["flex"] = flex.Value;
        }

        return button;
    }

    private static string BuildUrl(string publicAppUrl, string path)
    {
        var baseUrl = publicAppUrl.TrimEnd('/');
        return $"{baseUrl}{path}";
    }

    private static string RequestCode(LeaveRequest request)
    {
        return request.RequestNumber ?? request.Id.ToString("N")[..8].ToUpperInvariant();
    }

    private static string RequestCode(LeaveCancellationRequest request)
    {
        return request.CancellationRequestNumber ?? request.Id.ToString("N")[..8].ToUpperInvariant();
    }

    private static string FormatDate(DateOnly date)
    {
        return $"{date.Day:00}/{date.Month:00}/{date.Year + 543}";
    }

    private static string TranslateDuration(string? durationType)
    {
        return durationType switch
        {
            "HALF_DAY_AM" => "ครึ่งวัน (เช้า)",
            "HALF_DAY_PM" => "ครึ่งวัน (บ่าย)",
            _ => "เต็มวัน"
        };
    }

    private static string Initials(string? fullname)
    {
        if (string.IsNullOrWhiteSpace(fullname))
        {
            return "H";
        }

        var parts = fullname.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            return parts[0][0].ToString();
        }

        return $"{parts[0][0]}{parts[^1][0]}";
    }

    private static string StatusIcon(string status)
    {
        return status switch
        {
            "อนุมัติแล้ว" => "✅",
            "ไม่อนุมัติ" => "❌",
            "ยกเลิกแล้ว" => "◼",
            _ => "⏳"
        };
    }

    private static string StatusDescription(string status)
    {
        return status switch
        {
            "อนุมัติแล้ว" => "คำขอได้รับการอนุมัติเรียบร้อย",
            "ไม่อนุมัติ" => "คำขอไม่ได้รับการอนุมัติ",
            "ยกเลิกแล้ว" => "คำขอถูกยกเลิกเรียบร้อย",
            _ => "กรุณาตรวจสอบคำขอลา"
        };
    }

    private static string Truncate(string value, int maxLength)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "-"
            : value.Length <= maxLength ? value : $"{value[..maxLength]}...";
    }

    private sealed record ApprovalSnapshot(int StepOrder, string? StepName, string Status);
}
