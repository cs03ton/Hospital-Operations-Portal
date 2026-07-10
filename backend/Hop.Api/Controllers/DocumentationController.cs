using System.Security.Claims;
using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/docs")]
[Authorize]
[RequireAnyPermission("Documentation.View", "Documentation.AdminView", "Documentation.Manage")]
public class DocumentationController(
    AppDbContext db,
    IAuditLogService auditLogService,
    IDocumentationService documentationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DocumentationSummaryResponse>>>> Get(CancellationToken cancellationToken)
    {
        var access = await BuildAccessContext(cancellationToken);
        var docs = await documentationService.GetDocumentsAsync(access, cancellationToken);
        return ApiResponse<IReadOnlyList<DocumentationSummaryResponse>>.Ok(docs);
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<ApiResponse<DocumentationDetailResponse>>> GetDetail(string slug, CancellationToken cancellationToken)
    {
        var access = await BuildAccessContext(cancellationToken);
        var doc = await documentationService.GetDocumentAsync(slug, access, cancellationToken);
        if (doc is null)
        {
            return NotFound(ApiResponse<DocumentationDetailResponse>.Fail("ไม่พบคู่มือ หรือคุณไม่มีสิทธิ์เข้าถึงคู่มือนี้"));
        }

        return ApiResponse<DocumentationDetailResponse>.Ok(doc);
    }

    [HttpPut("{slug}")]
    [RequirePermission("Documentation.Manage")]
    public async Task<ActionResult<ApiResponse<DocumentationDetailResponse>>> Update(
        string slug,
        UpdateDocumentationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ContentMarkdown))
        {
            return BadRequest(ApiResponse<DocumentationDetailResponse>.Fail("กรุณาระบุเนื้อหาคู่มือ"));
        }

        var access = await BuildAccessContext(cancellationToken);
        try
        {
            var doc = await documentationService.UpdateDocumentAsync(slug, request.ContentMarkdown, access, cancellationToken);
            if (doc is null)
            {
                return NotFound(ApiResponse<DocumentationDetailResponse>.Fail("ไม่พบคู่มือ หรือคุณไม่มีสิทธิ์แก้ไขคู่มือนี้"));
            }

            await auditLogService.WriteAsync(GetCurrentUserId(), "Documentation.Updated", "Documentation", slug, $"Updated documentation {slug}.", "Success", HttpContext);
            return ApiResponse<DocumentationDetailResponse>.Ok(doc, "บันทึกคู่มือเรียบร้อยแล้ว");
        }
        catch (InvalidOperationException ex)
        {
            await auditLogService.WriteAsync(GetCurrentUserId(), "Documentation.UpdateRejected", "Documentation", slug, ex.Message, "Failed", HttpContext);
            return BadRequest(ApiResponse<DocumentationDetailResponse>.Fail(ex.Message));
        }
    }

    [HttpGet("{slug}/pdf")]
    public async Task<IActionResult> DownloadPdf(string slug, CancellationToken cancellationToken)
    {
        var access = await BuildAccessContext(cancellationToken);
        var pdfBytes = await documentationService.GeneratePdfAsync(slug, access, cancellationToken);
        if (pdfBytes is null)
        {
            return NotFound(ApiResponse<string>.Fail("ไม่พบคู่มือ หรือคุณไม่มีสิทธิ์ดาวน์โหลดคู่มือนี้"));
        }

        var safeSlug = slug.Replace("/", "-", StringComparison.Ordinal).Replace("\\", "-", StringComparison.Ordinal);
        return File(pdfBytes, "application/pdf", $"hop-documentation-{safeSlug}.pdf");
    }

    private async Task<DocumentationAccessContext> BuildAccessContext(CancellationToken cancellationToken)
    {
        var roles = User.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (Guid.TryParse(userIdValue, out var userId))
        {
            var assignedPermissions = await db.UserRoles
                .AsNoTracking()
                .Where(userRole => userRole.UserId == userId)
                .SelectMany(userRole => userRole.Role!.RolePermissions)
                .Where(rolePermission => rolePermission.Permission != null && rolePermission.Permission.IsActive)
                .Select(rolePermission => rolePermission.Permission!.Code)
                .ToListAsync(cancellationToken);

            foreach (var permission in assignedPermissions)
            {
                permissions.Add(permission);
            }
        }

        return new DocumentationAccessContext(roles, permissions);
    }

    private Guid? GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }
}
