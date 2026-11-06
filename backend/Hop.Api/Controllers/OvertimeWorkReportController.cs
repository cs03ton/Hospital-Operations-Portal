using System.Security.Claims;
using Hop.Api.Authorization;
using Hop.Api.DTOs;
using Hop.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hop.Api.Controllers;

[ApiController, Authorize, Route("api/correspondence/overtime-report")]
[RequirePermission(CorrespondenceAccess.Permission)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class OvertimeWorkReportController(OvertimeWorkReportService reports, OvertimeWorkReportRenderer renderer, OvertimePaymentService payments, OvertimePaymentRenderer paymentRenderer, ILogger<OvertimeWorkReportController> logger) : ControllerBase
{
    [HttpGet("payment-options")]
    public async Task<ActionResult<ApiResponse<OvertimePaymentOptions>>> PaymentOptions([FromQuery] string month, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor)) return Unauthorized();
        try { return ApiResponse<OvertimePaymentOptions>.Ok(await payments.OptionsAsync(actor, month, ct)); }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
        catch (ArgumentException ex) { return BadRequest(ApiResponse<OvertimePaymentOptions>.Fail(ex.Message)); }
    }
    [HttpPost("render-bundle"), RequestSizeLimit(524288)]
    public async Task<ActionResult<ApiResponse<OvertimeReportBundle>>> RenderBundle(OvertimeBundleRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor)) return Unauthorized();
        try
        {
            var payment = await payments.ResolveAsync(actor, request, ct); ct.ThrowIfCancellationRequested();
            var work = renderer.Render(payment.Report, payment.Days, payment.MonthStart); ct.ThrowIfCancellationRequested();
            var evidence = paymentRenderer.Render(payment); ct.ThrowIfCancellationRequested();
            return ApiResponse<OvertimeReportBundle>.Ok(new(work, evidence));
        }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
        catch (ArgumentException ex) { return BadRequest(ApiResponse<OvertimeReportBundle>.Fail(ex.Message)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            logger.LogError("Overtime bundle rendering failed. ErrorType={ErrorType}", ex.GetType().Name);
            return StatusCode(500, ApiResponse<OvertimeReportBundle>.Fail("สร้างเอกสารทั้งสองชุดไม่สำเร็จ กรุณาลองใหม่หรือติดต่อผู้ดูแลระบบ"));
        }
    }
    [HttpPost("render"), RequestSizeLimit(524288)]
    public async Task<ActionResult<ApiResponse<MemoFiles>>> Render(OvertimeReportRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor)) return Unauthorized();
        try { var report = await reports.ResolveAsync(actor, request, ct); ct.ThrowIfCancellationRequested(); return ApiResponse<MemoFiles>.Ok(renderer.Render(report)); }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
        catch (ArgumentException ex) { return BadRequest(ApiResponse<MemoFiles>.Fail(ex.Message)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            // Document text may appear in rendering exceptions. Log the error type only.
            logger.LogError("Overtime report rendering failed. ErrorType={ErrorType}", ex.GetType().Name);
            return StatusCode(500, ApiResponse<MemoFiles>.Fail("สร้างรายงานไม่สำเร็จ กรุณาลองใหม่หรือติดต่อผู้ดูแลระบบ"));
        }
    }
}
