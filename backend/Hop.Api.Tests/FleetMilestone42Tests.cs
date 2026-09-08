using Hop.Api.Data;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hop.Api.Tests;

public sealed class FleetMilestone42Tests
{
    [Theory]
    [InlineData("BOOLEAN","EQUALS",true,null,null,null)]
    [InlineData("NUMBER","GREATER_THAN_OR_EQUAL",null,8d,null,null)]
    [InlineData("TEXT","CONTAINS",null,null,"medical",null)]
    [InlineData("ENUM","IN",null,null,null,"MOUNTAIN,LONG_DISTANCE")]
    public void TypedCapabilityValidation_AcceptsCompatibleOperator(string type,string op,bool? b,double? n,string? text,string? value)
    { using var db=Db();new FleetCompatibilityService(db).ValidateValue(type,op,b,n.HasValue?(decimal)n.Value:null,text,value); }

    [Fact] public void TypedCapabilityValidation_RejectsInvalidOperator(){using var db=Db();Assert.Throws<ArgumentException>(()=>new FleetCompatibilityService(db).ValidateValue("BOOLEAN","CONTAINS",true,null,null,null));}

    [Fact]
    public async Task Compatibility_DistinguishesMandatoryAndOptionalMismatch()
    {
        await using var db=Db();var request=Request();var mandatory=new FleetCapability{Id=Guid.NewGuid(),Code="PassengerCapacity",Name="Capacity",DataType="NUMBER",CreatedByUserId=request.RequesterUserId};var optional=new FleetCapability{Id=Guid.NewGuid(),Code="VIP",Name="VIP",DataType="BOOLEAN",CreatedByUserId=request.RequesterUserId};var vehicle=new FleetVehicle{Id=Guid.NewGuid(),VehicleCode="V",RegistrationNumber="T",VehicleTypeId=Guid.NewGuid(),PassengerCapacity=4,SeatCapacityTotal=5};db.AddRange(request,mandatory,optional,new FleetRequestRequiredCapability{FleetRequestId=request.Id,CapabilityId=mandatory.Id,Operator="GREATER_THAN_OR_EQUAL",RequiredNumericValue=8,IsMandatory=true},new FleetRequestRequiredCapability{FleetRequestId=request.Id,CapabilityId=optional.Id,Operator="EQUALS",RequiredBooleanValue=true,IsMandatory=false},new FleetVehicleCapability{VehicleId=vehicle.Id,CapabilityId=mandatory.Id,NumericValue=4,CreatedByUserId=request.RequesterUserId},new FleetVehicleCapability{VehicleId=vehicle.Id,CapabilityId=optional.Id,BooleanValue=false,CreatedByUserId=request.RequesterUserId});await db.SaveChangesAsync();var result=await new FleetCompatibilityService(db).EvaluateAsync(request.Id,vehicle.Id,DateTime.UtcNow,default);Assert.Equal("NOT_MATCH",result.Status);Assert.Contains(result.Capabilities,x=>x.IsMandatory&&x.Status=="NOT_MATCH");
    }

    [Fact]
    public async Task Compatibility_SafetyMismatchCannotBecomeOverridden()
    {
        await using var db=Db();var request=Request();var cap=new FleetCapability{Id=Guid.NewGuid(),Code="EmergencySupport",Name="Emergency",DataType="BOOLEAN",IsRequiredSafetyCapability=true,CreatedByUserId=request.RequesterUserId};var vehicleId=Guid.NewGuid();db.AddRange(request,cap,new FleetRequestRequiredCapability{FleetRequestId=request.Id,CapabilityId=cap.Id,Operator="EQUALS",RequiredBooleanValue=true,IsMandatory=true},new FleetCompatibilityOverride{FleetRequestId=request.Id,VehicleId=vehicleId,MismatchCapabilityIds=$"[\"{cap.Id}\"]",Reason="not allowed",ApprovedByUserId=request.RequesterUserId});await db.SaveChangesAsync();var result=await new FleetCompatibilityService(db).EvaluateAsync(request.Id,vehicleId,DateTime.UtcNow,default);Assert.Equal("NOT_MATCH",result.Status);Assert.DoesNotContain(result.Capabilities,x=>x.Status=="OVERRIDDEN");
    }

    [Fact] public void EmergencyDefaults_PreserveNormalRequests(){var request=Request();Assert.Equal("NORMAL",request.Priority);Assert.False(request.RequiresPostReview);}
    private static FleetRequest Request()=>new(){Id=Guid.NewGuid(),RequestNo="VH-202608-9001",RequesterUserId=Guid.NewGuid(),CreatedByUserId=Guid.NewGuid(),Purpose="Test",MissionType="GENERAL",Destination="Test",ContactPersonName="Test",ContactPhone="0812345678",DepartureAt=DateTime.UtcNow.AddHours(1),ExpectedReturnAt=DateTime.UtcNow.AddHours(2),PassengerCount=1};
    private static AppDbContext Db()=>new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
