using Hop.Api.Data;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public static class FleetReferPolicy
{
    public const string MissionType = "ส่งต่อผู้ป่วย (Refer)";
    public const string VehicleTypeCode = "AMBULANCE";
    public static bool IsRefer(FleetRequest request) => request.MissionType.Trim() == MissionType;

    public static async Task<string?> ApplyAsync(AppDbContext db, FleetRequest request, CancellationToken ct)
    {
        if (!IsRefer(request))
        {
            request.RequestedVehicleTypeId = null;
            if (request.Priority != FleetPriorities.Emergency)
                request.Priority = request.IsUrgent ? FleetPriorities.Urgent : FleetPriorities.Normal;
            return null;
        }
        var typeId = await db.FleetVehicleTypes.Where(x => x.Code == VehicleTypeCode && x.IsActive)
            .Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
        if (typeId is null) return "ไม่พบประเภทรถพยาบาลที่เปิดใช้งาน กรุณาติดต่อผู้ดูแลเพื่อตรวจสอบทะเบียนรถ";
        request.RequestedVehicleTypeId = typeId;
        request.IsUrgent = true;
        request.Priority = FleetPriorities.Urgent;
        if (string.IsNullOrWhiteSpace(request.UrgentReason)) request.UrgentReason = MissionType;
        return null;
    }
}
