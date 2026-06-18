using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController(AppDbContext db) : ControllerBase
{
    [HttpGet("summary")]
    [RequirePermission("Dashboard.View")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryResponse>>> GetSummary()
    {
        var totalUsers = await db.Users.CountAsync(user => user.IsActive);
        var totalDepartments = await db.Departments.CountAsync(department => department.IsActive);

        return ApiResponse<DashboardSummaryResponse>.Ok(new DashboardSummaryResponse(
            totalUsers,
            totalDepartments,
            PendingApprovals: 0,
            OpenRepairRequests: 0,
            ActiveBorrowRequests: 0,
            InventoryItems: 0
        ));
    }
}
