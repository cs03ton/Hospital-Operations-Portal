using System.Security.Claims;
using Hop.Api.Authorization;
using Hop.Api.DTOs;
using Hop.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hop.Api.Controllers;

[ApiController, Authorize, Route("api/correspondence")]
[RequirePermission(CorrespondenceAccess.Permission)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CorrespondenceController(CorrespondenceMemoService memos, CorrespondenceMemoRenderer renderer, ILogger<CorrespondenceController>? logger = null) : ControllerBase
{
    [HttpGet("options")]
    public async Task<ActionResult<ApiResponse<MemoOptions>>> Options(CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor)) return Unauthorized();
        try { return ApiResponse<MemoOptions>.Ok(await memos.OptionsAsync(actor, ct)); } catch (UnauthorizedAccessException) { return Unauthorized(); }
    }
    [HttpPost("render"), RequestSizeLimit(524288)]
    public async Task<ActionResult<ApiResponse<MemoFiles>>> Render(MemoRenderRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor)) return Unauthorized();
        try
        {
            var memo = await memos.ResolveAsync(actor, request, ct); ct.ThrowIfCancellationRequested();
            return ApiResponse<MemoFiles>.Ok(renderer.Render(memo));
        }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
        catch (ArgumentException ex) { return BadRequest(ApiResponse<MemoFiles>.Fail(ex.Message)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            // Rendering diagnostics can contain document text. Never log the exception or request.
            logger?.LogError("Memo rendering failed. ErrorType={ErrorType}", ex.GetType().Name);
            return StatusCode(500, ApiResponse<MemoFiles>.Fail("สร้างไฟล์ไม่สำเร็จ กรุณาลองใหม่หรือติดต่อผู้ดูแลระบบ"));
        }
    }
    [HttpGet("departments/{departmentId:guid}/people")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MemoPerson>>>> DepartmentPeople(Guid departmentId, CancellationToken ct, [FromQuery] bool includeOtherDepartments = false)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor)) return Unauthorized();
        try { return ApiResponse<IReadOnlyList<MemoPerson>>.Ok(await memos.DepartmentPeopleAsync(actor, departmentId, ct, includeOtherDepartments)); }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
        catch (ArgumentException ex) { return BadRequest(ApiResponse<IReadOnlyList<MemoPerson>>.Fail(ex.Message)); }
    }
}
