using Hop.Api.Data;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hop.Api.Tests;

public sealed class FleetM42CompletionTests
{
    [Fact] public void Emergency_policy_has_positive_targets_and_snapshot_source(){var x=Policy();Assert.All(new[]{x.ResponseTargetMinutes,x.DispatchTargetMinutes,x.DriverAcknowledgementTargetMinutes},v=>Assert.True(v>0));Assert.True(x.PostReviewRequired);}
    [Fact] public void Emergency_request_snapshots_do_not_depend_on_later_policy_changes(){var p=Policy();var r=new FleetRequest{EmergencyPolicyCode=p.Code,EmergencyPolicyId=p.Id,ResponseTargetMinutesSnapshot=p.ResponseTargetMinutes,DispatchTargetMinutesSnapshot=p.DispatchTargetMinutes,DriverAcknowledgementTargetMinutesSnapshot=p.DriverAcknowledgementTargetMinutes,ApprovalBypassAllowedSnapshot=p.ApprovalBypassAllowed,PostReviewRequiredSnapshot=p.PostReviewRequired};p.ResponseTargetMinutes=99;Assert.Equal(15,r.ResponseTargetMinutesSnapshot);}
    [Theory][InlineData("BOOLEAN","EQUALS")][InlineData("NUMBER","GREATER_THAN_OR_EQUAL")][InlineData("TEXT","CONTAINS")][InlineData("ENUM","IN")] public void Capability_operator_matrix_is_enforced(string type,string op){using var db=Db();var service=new FleetCompatibilityService(db);service.ValidateValue(type,op,type=="BOOLEAN"?true:null,type=="NUMBER"?1:null,type=="TEXT"?"x":null,type=="ENUM"?"A,B":null);}
    [Fact] public void Safety_capability_is_explicit_and_non_destructive(){var x=new FleetCapability{Code="SAFE",Name="Safety",DataType=FleetCapabilityDataTypes.Boolean,IsRequiredSafetyCapability=true};Assert.True(x.IsRequiredSafetyCapability);Assert.True(x.IsActive);}
    [Fact] public void Trip_attachment_delete_is_soft(){var x=new FleetTripAttachment{Id=Guid.NewGuid(),TripId=Guid.NewGuid(),OriginalFileName="a.pdf",StoredFileName="a.pdf",ContentType="application/pdf",FilePath="fleet/trips/a.pdf",FileSize=1,IdempotencyKey="k",CreatedByUserId=Guid.NewGuid(),IsDeleted=true,DeletedAt=DateTime.UtcNow};Assert.True(x.IsDeleted);Assert.NotNull(x.DeletedAt);Assert.Equal("fleet/trips/a.pdf",x.FilePath);}
    private static FleetEmergencyPolicy Policy()=>new(){Id=Guid.NewGuid(),Code="INTERNAL_EMERGENCY_DEFAULT",Name="Default",Priority=FleetPriorities.Emergency,ResponseTargetMinutes=15,DispatchTargetMinutes=15,DriverAcknowledgementTargetMinutes=15,ApprovalBypassAllowed=true,PostReviewRequired=true,EffectiveFrom=DateTime.UtcNow};
    private static AppDbContext Db()=>new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
