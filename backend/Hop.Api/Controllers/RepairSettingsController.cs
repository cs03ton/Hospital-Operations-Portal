using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Hop.Api.Controllers;

[ApiController, Route("api/repairs/settings"), Authorize, RepairActiveUser, RequirePermission(RepairPermissions.Manage)]
public sealed class RepairSettingsController(AppDbContext db, IDataProtectionProvider protection, IConfiguration config) : ControllerBase
{
    [HttpGet]
    public async Task<object> Get(CancellationToken ct) => ApiResponse<object>.Ok(new {
        categories = await db.Set<RepairCategory>().AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct),
        groups = await db.LineGroupDestinations.AsNoTracking().Where(x => x.Module == "REPAIR_IT" || x.Module == "REPAIR_GENERAL")
            .Select(x => new { x.Id, x.Module, x.DisplayName, x.EndpointUrl, x.ClientId, x.Status, x.ConcurrencyToken,
                hasSecret = x.ClientSecretProtected != null }).ToListAsync(ct),
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

    [HttpPut("groups/{team}")]
    public async Task<IActionResult> Group(string team, RepairGroupInput input, CancellationToken ct)
    {
        if (team is not ("IT" or "GENERAL")) return BadRequest();
        // Restrict outbound credentials to explicitly configured HTTPS hosts, not arbitrary URLs.
        var allowed = config.GetSection("Repairs:AllowedNotificationHosts").Get<string[]>() ?? [];
        if (!Uri.TryCreate(input.EndpointUrl, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
            uri.IsLoopback || uri.UserInfo != "" || !allowed.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
            return BadRequest(ApiResponse<object>.Fail("Endpoint ต้องเป็น HTTPS และอยู่ใน Repairs:AllowedNotificationHosts"));
        var module = "REPAIR_" + team;
        var row = await db.LineGroupDestinations.SingleOrDefaultAsync(x => x.Module == module, ct);
        if (row is null) return NotFound();
        if (row.ConcurrencyToken != input.ConcurrencyToken) return Conflict();
        if (string.IsNullOrWhiteSpace(input.ClientSecret) && row.ClientSecretProtected == null)
            return BadRequest(ApiResponse<object>.Fail("กรุณาระบุ Client Secret"));
        row.EndpointUrl = uri.AbsoluteUri; row.ClientId = input.ClientId.Trim();
        row.DisplayName = input.DisplayName.Trim();
        if (!string.IsNullOrWhiteSpace(input.ClientSecret))
            row.ClientSecretProtected = protection.CreateProtector("HOP.LineGroupDestinationCredentials.v1").Protect(input.ClientSecret);
        row.Status = input.Enabled ? "Active" : "Disabled";
        row.ConfirmedAt = input.Enabled ? DateTime.UtcNow : row.ConfirmedAt;
        row.ConcurrencyToken = Guid.NewGuid();
        Audit("Repair.DestinationUpdated", "LineGroupDestination", row.Id, $"Module={module}; Enabled={input.Enabled}; SecretChanged={!string.IsNullOrWhiteSpace(input.ClientSecret)}");
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { return Conflict(); }
        return Ok(ApiResponse<object>.Ok(new { row.Id, row.ConcurrencyToken }));
    }

    private void Audit(string action, string entity, Guid id, string detail) => db.AuditLogs.Add(new AuditLog {
        Id = Guid.NewGuid(), UserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
        Action = action, EntityName = entity, EntityId = id.ToString(), Detail = detail
    });
}

public sealed record RepairCategoryInput(Guid? Id, [Required, MaxLength(100)] string Name,
    string TeamCode, bool IsActive, Guid? ConcurrencyToken);
public sealed record RepairGroupInput([Required, MaxLength(100)] string DisplayName,
    [Required, MaxLength(1000)] string EndpointUrl, [Required, MaxLength(300)] string ClientId,
    [MaxLength(2000)] string? ClientSecret, bool Enabled, Guid ConcurrencyToken);
