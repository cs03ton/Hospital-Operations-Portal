using System.Text.Json;
using Hop.Api.Data;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed record FleetCapabilityMatch(Guid CapabilityId,string Code,string Requirement,string Status,string Reason,bool IsMandatory,bool IsSafety);
public sealed record FleetCompatibilityResult(Guid RequestId,Guid VehicleId,string Status,IReadOnlyList<FleetCapabilityMatch> Capabilities);
public interface IFleetCompatibilityService
{
    Task<FleetCompatibilityResult> EvaluateAsync(Guid requestId, Guid vehicleId, DateTime atUtc, CancellationToken ct);
    void ValidateValue(string dataType,string operation,bool? booleanValue,decimal? numericValue,string? textValue,string? enumValue);
}

public sealed class FleetCompatibilityService(AppDbContext db) : IFleetCompatibilityService
{
    public async Task<FleetCompatibilityResult> EvaluateAsync(Guid requestId, Guid vehicleId, DateTime atUtc, CancellationToken ct)
    {
        var requirements=await db.FleetRequestRequiredCapabilities.AsNoTracking().Include(x=>x.Capability).Where(x=>x.FleetRequestId==requestId).ToListAsync(ct);
        var values=await db.FleetVehicleCapabilities.AsNoTracking().Where(x=>x.VehicleId==vehicleId&&x.IsActive&&x.EffectiveFrom<=atUtc&&(x.EffectiveTo==null||x.EffectiveTo>atUtc)).ToDictionaryAsync(x=>x.CapabilityId,ct);
        var overrides=await db.FleetCompatibilityOverrides.AsNoTracking().Where(x=>x.FleetRequestId==requestId&&x.VehicleId==vehicleId).OrderByDescending(x=>x.CreatedAt).ToListAsync(ct);
        var overridden=overrides.SelectMany(x=>JsonSerializer.Deserialize<Guid[]>(x.MismatchCapabilityIds)??[]).ToHashSet();
        var results=new List<FleetCapabilityMatch>();
        foreach(var requirement in requirements)
        {
            var capability=requirement.Capability!; values.TryGetValue(capability.Id,out var value);
            var matched=value is not null&&Matches(capability.DataType,requirement,value);
            var isOverridden=!matched&&!capability.IsRequiredSafetyCapability&&overridden.Contains(capability.Id);
            results.Add(new(capability.Id,capability.Code,Describe(requirement),matched?FleetCompatibilityStatuses.Match:isOverridden?FleetCompatibilityStatuses.Overridden:FleetCompatibilityStatuses.NotMatch,matched?"ตรงตามความต้องการ":isOverridden?"ได้รับการอนุมัติ override":"รถไม่มีค่าความสามารถที่ตรงตามเงื่อนไข",requirement.IsMandatory,capability.IsRequiredSafetyCapability));
        }
        var blocked=results.Any(x=>x.IsMandatory&&x.Status==FleetCompatibilityStatuses.NotMatch);
        var optional=results.Any(x=>!x.IsMandatory&&x.Status==FleetCompatibilityStatuses.NotMatch);
        var hasOverride=results.Any(x=>x.Status==FleetCompatibilityStatuses.Overridden);
        var status=blocked?FleetCompatibilityStatuses.NotMatch:optional?FleetCompatibilityStatuses.PartialMatch:hasOverride?FleetCompatibilityStatuses.Overridden:FleetCompatibilityStatuses.Match;
        return new(requestId,vehicleId,status,results);
    }

    public void ValidateValue(string dataType,string operation,bool? booleanValue,decimal? numericValue,string? textValue,string? enumValue)
    {
        if(!FleetCapabilityDataTypes.All.Contains(dataType)||!FleetCapabilityOperators.All.Contains(operation))throw new ArgumentException("Invalid capability data type or operator.");
        var count=(booleanValue.HasValue?1:0)+(numericValue.HasValue?1:0)+(string.IsNullOrWhiteSpace(textValue)?0:1)+(string.IsNullOrWhiteSpace(enumValue)?0:1);
        if(count!=1)throw new ArgumentException("Exactly one typed capability value is required.");
        if(dataType==FleetCapabilityDataTypes.Boolean&&(operation!=FleetCapabilityOperators.Equals||!booleanValue.HasValue))throw new ArgumentException("BOOLEAN supports EQUALS and BooleanValue only.");
        if(dataType==FleetCapabilityDataTypes.Number&&(!numericValue.HasValue||operation is FleetCapabilityOperators.Contains or FleetCapabilityOperators.In))throw new ArgumentException("NUMBER requires NumericValue and a numeric operator.");
        if(dataType==FleetCapabilityDataTypes.Text&&(string.IsNullOrWhiteSpace(textValue)||operation is FleetCapabilityOperators.GreaterThanOrEqual or FleetCapabilityOperators.LessThanOrEqual or FleetCapabilityOperators.In))throw new ArgumentException("TEXT supports EQUALS or CONTAINS.");
        if(dataType==FleetCapabilityDataTypes.Enum&&(string.IsNullOrWhiteSpace(enumValue)||operation is not (FleetCapabilityOperators.Equals or FleetCapabilityOperators.In)))throw new ArgumentException("ENUM supports EQUALS or IN.");
    }
    private static bool Matches(string type,FleetRequestRequiredCapability r,FleetVehicleCapability v)=>type switch
    {
        FleetCapabilityDataTypes.Boolean=>v.BooleanValue==r.RequiredBooleanValue,
        FleetCapabilityDataTypes.Number=>r.Operator switch{FleetCapabilityOperators.Equals=>v.NumericValue==r.RequiredNumericValue,FleetCapabilityOperators.GreaterThanOrEqual=>v.NumericValue>=r.RequiredNumericValue,FleetCapabilityOperators.LessThanOrEqual=>v.NumericValue<=r.RequiredNumericValue,_=>false},
        FleetCapabilityDataTypes.Text=>r.Operator==FleetCapabilityOperators.Contains&&(v.TextValue?.Contains(r.RequiredTextValue??"",StringComparison.OrdinalIgnoreCase)??false)||r.Operator==FleetCapabilityOperators.Equals&&string.Equals(v.TextValue,r.RequiredTextValue,StringComparison.OrdinalIgnoreCase),
        FleetCapabilityDataTypes.Enum=>r.Operator==FleetCapabilityOperators.In?(r.RequiredEnumValue??"").Split(',').Contains(v.EnumValue,StringComparer.OrdinalIgnoreCase):string.Equals(v.EnumValue,r.RequiredEnumValue,StringComparison.OrdinalIgnoreCase),
        _=>false
    };
    private static string Describe(FleetRequestRequiredCapability r)=>$"{r.Capability?.Code} {r.Operator} {r.RequiredBooleanValue?.ToString()??r.RequiredNumericValue?.ToString()??r.RequiredTextValue??r.RequiredEnumValue}";
}
