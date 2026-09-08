using System.Security.Claims;
using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

public sealed record FleetEmergencyPolicySave(string Code,string Name,string Priority,int ResponseTargetMinutes,int DispatchTargetMinutes,int DriverAcknowledgementTargetMinutes,bool ApprovalBypassAllowed,bool PostReviewRequired,bool IsActive,DateTime EffectiveFrom,DateTime? EffectiveTo,Guid? ConcurrencyToken);

[ApiController,Authorize,Route("api/fleet/emergency-policies")]
public sealed class FleetEmergencyPoliciesController(AppDbContext db):ControllerBase
{
    [HttpGet,RequirePermission(FleetPermissions.EmergencyPolicyView)]
    public async Task<ActionResult<ApiResponse<object>>> List([FromQuery]string? search,[FromQuery]bool? active,[FromQuery]int page=1,[FromQuery]int pageSize=25,CancellationToken ct=default)
    { page=Math.Max(1,page);pageSize=Math.Clamp(pageSize,1,100);var q=db.FleetEmergencyPolicies.AsNoTracking();if(!string.IsNullOrWhiteSpace(search))q=q.Where(x=>x.Code.Contains(search)||x.Name.Contains(search));if(active.HasValue)q=q.Where(x=>x.IsActive==active);var total=await q.CountAsync(ct);var items=await q.OrderBy(x=>x.Priority).ThenBy(x=>x.Code).Skip((page-1)*pageSize).Take(pageSize).ToListAsync(ct);return ApiResponse<object>.Ok(new{items,total,page,pageSize}); }
    [HttpGet("{id:guid}"),RequirePermission(FleetPermissions.EmergencyPolicyView)]
    public async Task<ActionResult<ApiResponse<object>>> Detail(Guid id,CancellationToken ct){var item=await db.FleetEmergencyPolicies.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id,ct);return item is null?NotFound(ApiResponse<object>.Fail("Policy not found.")):ApiResponse<object>.Ok(item);}
    [HttpPost,RequirePermission(FleetPermissions.EmergencyPolicyManage)] public async Task<ActionResult<ApiResponse<object>>> Create(FleetEmergencyPolicySave r,CancellationToken ct){var error=Validate(r);if(error is not null)return BadRequest(ApiResponse<object>.Fail(error));if(await db.FleetEmergencyPolicies.AnyAsync(x=>x.Code==r.Code.Trim(),ct))return Conflict(ApiResponse<object>.Fail("Policy code already exists."));var x=new FleetEmergencyPolicy{Code=r.Code.Trim(),Name=r.Name.Trim(),Priority=r.Priority,ResponseTargetMinutes=r.ResponseTargetMinutes,DispatchTargetMinutes=r.DispatchTargetMinutes,DriverAcknowledgementTargetMinutes=r.DriverAcknowledgementTargetMinutes,ApprovalBypassAllowed=r.ApprovalBypassAllowed,PostReviewRequired=r.PostReviewRequired,IsActive=r.IsActive,EffectiveFrom=r.EffectiveFrom,EffectiveTo=r.EffectiveTo,CreatedByUserId=Actor()};db.Add(x);await db.SaveChangesAsync(ct);return ApiResponse<object>.Ok(x);}
    [HttpPut("{id:guid}"),RequirePermission(FleetPermissions.EmergencyPolicyManage)] public async Task<ActionResult<ApiResponse<object>>> Update(Guid id,FleetEmergencyPolicySave r,CancellationToken ct){var error=Validate(r);if(error is not null)return BadRequest(ApiResponse<object>.Fail(error));var x=await db.FleetEmergencyPolicies.FindAsync([id],ct);if(x is null)return NotFound(ApiResponse<object>.Fail("Policy not found."));if(r.ConcurrencyToken!=x.ConcurrencyToken)return Conflict(ApiResponse<object>.Fail("Policy changed."));x.Name=r.Name.Trim();x.Priority=r.Priority;x.ResponseTargetMinutes=r.ResponseTargetMinutes;x.DispatchTargetMinutes=r.DispatchTargetMinutes;x.DriverAcknowledgementTargetMinutes=r.DriverAcknowledgementTargetMinutes;x.ApprovalBypassAllowed=r.ApprovalBypassAllowed;x.PostReviewRequired=r.PostReviewRequired;x.IsActive=r.IsActive;x.EffectiveFrom=r.EffectiveFrom;x.EffectiveTo=r.EffectiveTo;x.UpdatedAt=DateTime.UtcNow;x.UpdatedByUserId=Actor();x.ConcurrencyToken=Guid.NewGuid();await db.SaveChangesAsync(ct);return ApiResponse<object>.Ok(x);}
    [HttpPost("{id:guid}/disable"),RequirePermission(FleetPermissions.EmergencyPolicyManage)] public async Task<ActionResult<ApiResponse<object>>> Disable(Guid id,[FromBody]FleetWorkflowActionRequest r,CancellationToken ct){var x=await db.FleetEmergencyPolicies.FindAsync([id],ct);if(x is null)return NotFound(ApiResponse<object>.Fail("Policy not found."));if(x.ConcurrencyToken!=r.ConcurrencyToken)return Conflict(ApiResponse<object>.Fail("Policy changed."));x.IsActive=false;x.EffectiveTo??=DateTime.UtcNow;x.UpdatedAt=DateTime.UtcNow;x.UpdatedByUserId=Actor();x.ConcurrencyToken=Guid.NewGuid();await db.SaveChangesAsync(ct);return ApiResponse<object>.Ok(new{x.Id,x.ConcurrencyToken});}
    private static string? Validate(FleetEmergencyPolicySave r){if(string.IsNullOrWhiteSpace(r.Code)||string.IsNullOrWhiteSpace(r.Name))return "Code and name are required.";if(r.Priority is not FleetPriorities.Emergency and not FleetPriorities.Urgent)return "Invalid priority.";if(r.ResponseTargetMinutes<=0||r.DispatchTargetMinutes<=0||r.DriverAcknowledgementTargetMinutes<=0)return "Targets must be positive.";if(r.EffectiveTo<=r.EffectiveFrom)return "EffectiveTo must be after EffectiveFrom.";return null;}
    private Guid Actor()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
