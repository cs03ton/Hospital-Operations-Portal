using System.Security.Claims;
using Hop.Api.Authorization;
using Hop.Api.DTOs;
using Hop.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hop.Api.Controllers;

[ApiController, Authorize, Route("api/correspondence/cho11")]
[RequirePermission(CorrespondenceAccess.Permission)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class Cho11Controller(Cho11Service service, Cho11Renderer renderer, ILogger<Cho11Controller> logger) : ControllerBase
{
    [HttpGet("options")]
    public async Task<ActionResult<ApiResponse<Cho11Options>>> Options(CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor)) return Unauthorized();
        try { return ApiResponse<Cho11Options>.Ok(await service.OptionsAsync(actor, ct)); }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
    }
    [HttpPost("render"), RequestSizeLimit(16384)]
    public async Task<ActionResult<ApiResponse<MemoFiles>>> Render(Cho11Request input, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor)) return Unauthorized();
        try {
            var report = await service.ResolveAsync(actor, input, ct); ct.ThrowIfCancellationRequested();
            return ApiResponse<MemoFiles>.Ok(renderer.Render(report));
        }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
        catch (ArgumentException ex) { return BadRequest(ApiResponse<MemoFiles>.Fail(ex.Message)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) {
            logger.LogError("Cho11 rendering failed. ErrorType={ErrorType}", ex.GetType().Name);
            return StatusCode(500, ApiResponse<MemoFiles>.Fail("สร้างเอกสาร ฉ.11 ไม่สำเร็จ กรุณาลองใหม่หรือติดต่อผู้ดูแลระบบ"));
        }
    }
}
