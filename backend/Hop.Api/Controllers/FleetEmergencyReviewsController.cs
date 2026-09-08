using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController,Authorize,Route("api/fleet/emergency-reviews")]
public sealed class FleetEmergencyReviewsController(AppDbContext db):ControllerBase
{
    [HttpGet,RequirePermission(FleetPermissions.EmergencyReview)]
    public async Task<ActionResult<ApiResponse<object>>> Queue([FromQuery]string? search,[FromQuery]string? status,[FromQuery]bool? slaBreached,[FromQuery]DateTime? from,[FromQuery]DateTime? to,[FromQuery]int page=1,[FromQuery]int pageSize=25,CancellationToken ct=default)
    {
        page=Math.Max(1,page);pageSize=Math.Clamp(pageSize,1,100);var now=DateTime.UtcNow;
        var q=db.FleetRequests.AsNoTracking().Where(x=>x.Priority==FleetPriorities.Emergency&&x.Status==FleetRequestStatuses.Completed);
        if(!string.IsNullOrWhiteSpace(search))q=q.Where(x=>x.RequestNo.Contains(search)||x.Destination.Contains(search));if(status=="PENDING")q=q.Where(x=>x.RequiresPostReview);else if(status=="COMPLETED")q=q.Where(x=>!x.RequiresPostReview);if(from.HasValue)q=q.Where(x=>x.CreatedAt>=from);if(to.HasValue)q=q.Where(x=>x.CreatedAt<to);
        if(slaBreached.HasValue)q=q.Where(x=>x.SubmittedAt!=null&&x.Assignments.Any()&&(x.Assignments.Min(a=>a.AssignedAt)>x.SubmittedAt.Value.AddMinutes(x.ResponseTargetMinutesSnapshot??15))==slaBreached.Value);
        var total=await q.CountAsync(ct);var items=await q.OrderByDescending(x=>x.RequiresPostReview).ThenByDescending(x=>x.CreatedAt).Skip((page-1)*pageSize).Take(pageSize).Select(x=>new{x.Id,x.RequestNo,x.Priority,x.Status,x.CreatedAt,x.RequiresPostReview,x.EmergencyPolicyCode,ResponseTargetMinutes=x.ResponseTargetMinutesSnapshot??15,SlaBreached=x.SubmittedAt!=null&&x.Assignments.Any()&&x.Assignments.Min(a=>a.AssignedAt)>x.SubmittedAt.Value.AddMinutes(x.ResponseTargetMinutesSnapshot??15),BypassUsed=x.StatusHistories.Any(h=>h.Action=="FleetEmergency.ApprovalBypassed"),Review=x.RequiresPostReview?null:x.StatusHistories.Where(h=>h.Action=="FleetEmergency.PostReviewCompleted").OrderByDescending(h=>h.CreatedAt).Select(h=>new{h.ActorUserId,h.CreatedAt}).FirstOrDefault()}).ToListAsync(ct);return ApiResponse<object>.Ok(new{items,total,page,pageSize,generatedAt=now});
    }

    [HttpGet("{id:guid}"),RequirePermission(FleetPermissions.EmergencyReview)]
    public async Task<ActionResult<ApiResponse<object>>> Detail(Guid id,CancellationToken ct)
    {
        var x=await db.FleetRequests.AsNoTracking().Where(x=>x.Id==id&&x.Priority==FleetPriorities.Emergency).Select(x=>new{x.Id,x.RequestNo,x.Priority,x.Status,x.EmergencyReason,x.IncidentLocation,x.EmergencyPolicyCode,x.ResponseTargetMinutesSnapshot,x.DispatchTargetMinutesSnapshot,x.DriverAcknowledgementTargetMinutesSnapshot,x.ApprovalBypassAllowedSnapshot,x.PostReviewRequiredSnapshot,x.RequiresPostReview,x.SubmittedAt,x.CreatedAt,x.ConcurrencyToken,Timeline=x.StatusHistories.OrderBy(h=>h.CreatedAt).Select(h=>new{h.Action,h.FromStatus,h.ToStatus,h.Reason,h.ActorUserId,h.CreatedAt}),Assignment=x.Assignments.Where(a=>a.IsActive).Select(a=>new{a.Id,a.VehicleId,a.DriverUserId,a.AssignedAt}).FirstOrDefault(),Trip=db.FleetTripRecords.Where(t=>t.FleetRequestId==x.Id).Select(t=>new{t.ActualStartAt,t.ActualEndAt,t.StartMileage,t.EndMileage}).FirstOrDefault(),CompatibilityOverrides=db.FleetCompatibilityOverrides.Where(o=>o.FleetRequestId==x.Id).OrderBy(o=>o.CreatedAt).Select(o=>new{o.Id,o.VehicleId,o.AssignmentId,o.MismatchCapabilityIds,o.Reason,o.ApprovedByUserId,o.CreatedAt}).ToList(),Review=db.FleetEmergencyPostReviews.Where(r=>r.FleetRequestId==x.Id).Select(r=>new{r.Id,r.Outcome,r.WasBypassAppropriate,r.ResponseTimeAssessment,r.SafetyIssues,r.FollowUpActions,r.Notes,r.ReviewedByUserId,r.ReviewedAt}).FirstOrDefault()}).SingleOrDefaultAsync(ct);return x is null?NotFound(ApiResponse<object>.Fail("Emergency request not found.")):ApiResponse<object>.Ok(x);
    }
}
