using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Hop.Api.Controllers;

[ApiController, Route("api/repairs/settings"), Authorize, RepairActiveUser, RequirePermission(RepairPermissions.Manage)]
public sealed class RepairSettingsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<object> Get(CancellationToken ct) => ApiResponse<object>.Ok(new {
        categories = await db.Set<RepairCategory>().AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct),
        deliveries = await db.Set<RepairDispatch>().AsNoTracking().OrderByDescending(x => x.AvailableAt).Take(50)
            .Select(x => new { x.Id, x.RequestId, x.TeamCode, x.Status, x.Attempts, x.ErrorCode, x.SentAt }).ToListAsync(ct)
    });

    [HttpPost("categories")]
    public async Task<IActionResult> Category(RepairCategoryInput input, CancellationToken ct)
    {
        if (!await db.Set<RepairTeam>().AnyAsync(x => x.Code == input.TeamCode, ct)) return BadRequest();
        var row = input.Id is null ? new RepairCategory() : await db.Set<RepairCategory>().FindAsync([input.Id.Value], ct);
        if (row is null) return NotFound();
        if (input.Id != null && row.ConcurrencyToken != input.ConcurrencyToken) return Conflict();
        if (await db.Set<RepairCategory>().AnyAsync(x => x.Name == input.Name.Trim() && x.Id != row.Id, ct)) return Conflict();
        if (input.Id is null) db.Add(row);
        row.Name = input.Name.Trim(); row.TeamCode = input.TeamCode; row.IsActive = input.IsActive; row.ConcurrencyToken = Guid.NewGuid();
        Audit("Repair.CategoryUpdated", "RepairCategory", row.Id, $"Team={row.TeamCode}; Active={row.IsActive}");
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { return Conflict(); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { return Conflict(); }
        return Ok(ApiResponse<object>.Ok(row));
    }

    private void Audit(string action, string entity, Guid id, string detail) => db.AuditLogs.Add(new AuditLog {
        Id = Guid.NewGuid(), UserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
        Action = action, EntityName = entity, EntityId = id.ToString(), Detail = detail
    });
}

public sealed record RepairCategoryInput(Guid? Id, [Required, MaxLength(100)] string Name,
    string TeamCode, bool IsActive, Guid? ConcurrencyToken);
