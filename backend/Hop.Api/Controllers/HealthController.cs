using Hop.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public ActionResult<ApiResponse<object>> Get()
    {
        return ApiResponse<object>.Ok(new
        {
            status = "Healthy",
            checkedAt = DateTime.UtcNow
        });
    }
}
