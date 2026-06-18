using Hop.Api.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<object>> Get()
    {
        return ApiResponse<object>.Ok(new
        {
            service = "Hop.Api",
            status = "Healthy",
            checkedAt = DateTime.UtcNow
        });
    }
}
