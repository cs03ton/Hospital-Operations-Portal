using Hop.Api.Data;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed class LeavePolicyService(AppDbContext db) : ILeavePolicyService
{
    public async Task<LeavePolicyRule?> GetPolicyAsync(Guid userId, Guid leaveTypeId, int fiscalYear, CancellationToken cancellationToken = default)
    {
        var employmentType = await db.Users
            .AsNoTracking()
            .Where(item => item.Id == userId)
            .Select(item => item.EmploymentType)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(employmentType))
        {
            return null;
        }

        return await db.LeavePolicyRules
            .AsNoTracking()
            .Include(item => item.LeaveType)
            .Where(item =>
                item.EmploymentType == employmentType &&
                item.LeaveTypeId == leaveTypeId &&
                item.IsActive &&
                (item.FiscalYear == null || item.FiscalYear == fiscalYear))
            .OrderByDescending(item => item.FiscalYear == fiscalYear)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<decimal> CalculateEntitlementAsync(Guid userId, Guid leaveTypeId, int fiscalYear, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
        {
            return 0;
        }

        var policy = await GetPolicyAsync(userId, leaveTypeId, fiscalYear, cancellationToken);
        if (policy is null)
        {
            return 0;
        }

        return ResolvePolicyEntitlement(user, policy, FiscalYearStartDate(fiscalYear));
    }

    public async Task<LeavePolicyPreviewResult> ValidateLeaveRequestAsync(
        Guid userId,
        Guid leaveTypeId,
        DateOnly startDate,
        DateOnly endDate,
        string? durationType,
        decimal requestedDays,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var notes = new List<string>();
        var normalizedDurationType = LeaveDurationTypes.Normalize(durationType);

        if (string.IsNullOrWhiteSpace(normalizedDurationType))
        {
            errors.Add("ประเภทช่วงเวลาการลาไม่ถูกต้อง");
        }

        if (endDate < startDate)
        {
            errors.Add("วันที่สิ้นสุดต้องไม่น้อยกว่าวันที่เริ่มลา");
        }

        if (LeaveDurationTypes.IsHalfDay(normalizedDurationType) && startDate != endDate)
        {
            errors.Add("การลาครึ่งวันต้องเลือกวันที่เริ่มลาและวันที่สิ้นสุดเป็นวันเดียวกัน");
        }

        var fiscalYear = FiscalYearHelper.GetFiscalYear(startDate);
        var preview = await CalculateAvailableDaysAsync(userId, leaveTypeId, fiscalYear, requestedDays, cancellationToken);
        errors.AddRange(preview.Errors);
        warnings.AddRange(preview.Warnings);
        notes.AddRange(preview.PolicyNotes);
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
        var policy = await GetPolicyAsync(userId, leaveTypeId, fiscalYear, cancellationToken);
        if (user is not null && policy is not null)
        {
            var minimumServiceError = ValidateMinimumService(user, policy, startDate);
            if (minimumServiceError is not null)
            {
                errors.Add(minimumServiceError);
            }
        }
        var leaveType = await db.LeaveTypes.AsNoTracking().FirstOrDefaultAsync(item => item.Id == leaveTypeId, cancellationToken);
        if (user is not null && leaveType is not null)
        {
            var genderError = ValidateGenderRequirement(user, leaveType);
            if (genderError is not null)
            {
                errors.Add(genderError);
            }
        }

        return preview with
        {
            RequestedDays = requestedDays,
            CanSubmit = errors.Count == 0,
            Errors = errors.Distinct().ToList(),
            Warnings = warnings.Distinct().ToList(),
            PolicyNotes = notes.Distinct().ToList()
        };
    }

    public async Task<LeavePolicyPreviewResult> CalculateAvailableDaysAsync(
        Guid userId,
        Guid leaveTypeId,
        int fiscalYear,
        decimal requestedDays = 0,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
        var leaveType = await db.LeaveTypes.AsNoTracking().FirstOrDefaultAsync(item => item.Id == leaveTypeId, cancellationToken);
        var errors = new List<string>();
        var warnings = new List<string>();
        var notes = new List<string>();

        if (user is null || leaveType is null)
        {
            errors.Add("ไม่พบข้อมูลผู้ขอลาหรือประเภทการลา");
            return Empty(null, fiscalYear, requestedDays, errors);
        }

        if (string.IsNullOrWhiteSpace(user.EmploymentType) || user.EmploymentStartDate is null)
        {
            errors.Add("ไม่พบข้อมูลประเภทบุคลากรหรือวันที่เริ่มปฏิบัติงาน กรุณาติดต่อผู้ดูแลระบบ");
            return Empty(user.EmploymentType, fiscalYear, requestedDays, errors);
        }

        var policy = await GetPolicyAsync(userId, leaveTypeId, fiscalYear, cancellationToken);
        if (policy is null)
        {
            errors.Add($"ยังไม่ได้กำหนดสิทธิ์ {leaveType.Name} สำหรับ{EmploymentTypes.GetThaiLabel(user.EmploymentType)}");
            return Empty(user.EmploymentType, fiscalYear, requestedDays, errors);
        }

        if (!policy.IsPaid)
        {
            warnings.Add("ประเภทการลานี้ไม่มีสิทธิได้รับค่าจ้างตาม policy กรุณาตรวจสอบก่อนส่งคำขอ");
        }

        if (!string.IsNullOrWhiteSpace(policy.Notes))
        {
            notes.Add(policy.Notes);
        }

        if (policy.MaxPaidDays is not null && policy.MaxPaidDays.Value < policy.EntitlementDays)
        {
            notes.Add($"ได้รับค่าจ้างไม่เกิน {policy.MaxPaidDays.Value:0.##} วัน ตาม policy");
        }

        if (policy.MaxExtendedDays is not null)
        {
            notes.Add($"กรณีพิเศษอาจพิจารณาได้เพิ่มเติมไม่เกิน {policy.MaxExtendedDays.Value:0.##} วัน ตาม policy");
        }

        var entitlement = ResolvePolicyEntitlement(user, policy, FiscalYearStartDate(fiscalYear));
        var balance = await db.LeaveBalances
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == userId && item.LeaveTypeId == leaveTypeId && item.Year == fiscalYear, cancellationToken);

        var entitled = balance?.EntitledDays ?? entitlement;
        var carriedOver = NormalizeCarryOver(balance?.CarriedOverDays ?? 0, user, policy, fiscalYear);
        var adjusted = balance?.AdjustedDays ?? 0;
        var used = balance?.UsedDays ?? 0;
        var pending = balance?.PendingDays ?? 0;
        var available = FiscalYearHelper.CalculateAvailableDays(entitled, carriedOver, used, pending, adjusted);
        var maxAccumulatedDays = ResolveMaxAccumulatedDays(user, policy, fiscalYear);
        if (maxAccumulatedDays is not null)
        {
            available = Math.Min(available, Math.Max(0, maxAccumulatedDays.Value - used - pending + adjusted));
        }

        if (leaveType.RequiresBalance && entitlement <= 0)
        {
            errors.Add("ประเภทการลานี้ไม่มีสิทธิ์วันลาคงเหลือตามประเภทบุคลากร");
        }

        if (leaveType.RequiresBalance && requestedDays > 0 && available < requestedDays)
        {
            errors.Add(pending > 0
                ? $"วันลาคงเหลือไม่เพียงพอ คงเหลือ {entitled + carriedOver + adjusted - used:0.##} วัน มีคำขอรออนุมัติ {pending:0.##} วัน เหลือใช้ได้ {available:0.##} วัน แต่ขอลา {requestedDays:0.##} วัน"
                : $"วันลาคงเหลือไม่เพียงพอ คงเหลือ {available:0.##} วัน แต่ขอลา {requestedDays:0.##} วัน");
        }

        return new LeavePolicyPreviewResult(
            user.EmploymentType,
            EmploymentTypes.GetThaiLabel(user.EmploymentType),
            fiscalYear,
            entitled,
            carriedOver,
            adjusted,
            used,
            pending,
            leaveType.RequiresBalance ? available : decimal.MaxValue,
            requestedDays,
            leaveType.RequiresBalance,
            errors.Count == 0,
            warnings,
            errors,
            notes);
    }

    public async Task<LeaveCarryOverPolicyResult> CalculateCarryOverAsync(
        Guid userId,
        Guid leaveTypeId,
        int fromFiscalYear,
        decimal endYearRemaining,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
        var leaveType = await db.LeaveTypes.AsNoTracking().FirstOrDefaultAsync(item => item.Id == leaveTypeId, cancellationToken);
        if (user is null || leaveType is null)
        {
            return new LeaveCarryOverPolicyResult(false, 0, 0, Math.Max(endYearRemaining, 0), [], ["ไม่พบข้อมูลผู้ใช้งานหรือประเภทลา"]);
        }

        var toFiscalYear = fromFiscalYear + 1;
        var policy = await GetPolicyAsync(userId, leaveTypeId, toFiscalYear, cancellationToken)
            ?? await GetPolicyAsync(userId, leaveTypeId, fromFiscalYear, cancellationToken);
        if (policy is null)
        {
            return new LeaveCarryOverPolicyResult(false, 0, 0, Math.Max(endYearRemaining, 0), [], ["ยังไม่ได้กำหนดกฎยกยอดสำหรับประเภทบุคลากรนี้"]);
        }

        if (!policy.AllowCarryOver || !leaveType.AllowCarryOver)
        {
            return new LeaveCarryOverPolicyResult(false, 0, 0, Math.Max(endYearRemaining, 0), [], ["ประเภทลาหรือ policy นี้ไม่รองรับการยกยอด"]);
        }

        var cap = ResolveCarryOverCap(user, policy, toFiscalYear, leaveType.Code);
        if (policy.AllowCarryOver)
        {
            cap = ResolveEmploymentCarryOverCap(user, toFiscalYear, cap);
        }
        var carryOverDays = Math.Min(Math.Max(endYearRemaining, 0), cap);
        var forfeitedDays = Math.Max(endYearRemaining - cap, 0);
        var warnings = forfeitedDays > 0
            ? [$"มียอดคงเหลือเกินสิทธิ์ยกยอด ระบบจะตัดออก {forfeitedDays:0.##} วัน"]
            : Array.Empty<string>();

        return new LeaveCarryOverPolicyResult(true, cap, carryOverDays, forfeitedDays, warnings, []);
    }

    public string? ValidateMinimumService(User user, LeavePolicyRule policy, DateOnly asOfDate)
    {
        var requiredMonths = policy.MinServiceMonths ?? (policy.MinServiceYears is null ? null : policy.MinServiceYears.Value * 12);
        if (requiredMonths is null || requiredMonths <= 0)
        {
            return null;
        }

        if (user.EmploymentStartDate is null)
        {
            return "ไม่พบข้อมูลประเภทบุคลากรหรือวันที่เริ่มปฏิบัติงาน กรุณาติดต่อผู้ดูแลระบบ";
        }

        var serviceMonths = CalculateCompletedServiceMonths(user.EmploymentStartDate.Value, asOfDate);
        return serviceMonths >= requiredMonths.Value
            ? null
            : $"ยังไม่ครบเงื่อนไขอายุงาน ต้องมีอายุงานอย่างน้อย {requiredMonths.Value} เดือน ปัจจุบัน {serviceMonths} เดือน";
    }

    public string? ValidateGenderRequirement(User user, LeaveType leaveType)
    {
        var gender = GenderTypes.Normalize(user.Gender);
        return leaveType.Code switch
        {
            "MATERNITY_LEAVE" when gender != GenderTypes.Female => "ประเภทการลาคลอดบุตร ใช้ได้เฉพาะบุคลากรเพศหญิง",
            "ORDINATION_LEAVE" when gender != GenderTypes.Male => "ประเภทการลาบวช ใช้ได้เฉพาะบุคลากรเพศชาย",
            _ => null
        };
    }

    public async Task<IReadOnlyList<string>> ValidateCarryOverAsync(Guid userId, LeaveType leaveType, int fiscalYear, CancellationToken cancellationToken = default)
    {
        var policy = await GetPolicyAsync(userId, leaveType.Id, fiscalYear, cancellationToken);
        if (policy is null)
        {
            return ["ยังไม่ได้กำหนดกฎยกยอดสำหรับประเภทบุคลากรนี้"];
        }

        var balance = await db.LeaveBalances
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == userId && item.LeaveTypeId == leaveType.Id && item.Year == fiscalYear, cancellationToken);
        if (balance is null)
        {
            return [];
        }

        var user = await db.Users.AsNoTracking().FirstAsync(item => item.Id == userId, cancellationToken);
        var normalized = NormalizeCarryOver(balance.CarriedOverDays, user, policy, fiscalYear);
        return normalized < balance.CarriedOverDays
            ? [$"ยอดยกมาจะถูกจำกัดเหลือ {normalized:0.##} วันตาม policy"]
            : [];
    }

    private static LeavePolicyPreviewResult Empty(string? employmentType, int fiscalYear, decimal requestedDays, IReadOnlyList<string> errors)
    {
        return new LeavePolicyPreviewResult(
            employmentType,
            EmploymentTypes.GetThaiLabel(employmentType),
            fiscalYear,
            0,
            0,
            0,
            0,
            0,
            0,
            requestedDays,
            true,
            false,
            [],
            errors,
            []);
    }

    private static decimal ResolvePolicyEntitlement(User user, LeavePolicyRule policy, DateOnly asOfDate)
    {
        if (user.EmploymentStartDate is null)
        {
            return policy.EntitlementDays;
        }

        var serviceMonths = CalculateCompletedServiceMonths(user.EmploymentStartDate.Value, asOfDate);
        if (serviceMonths < 12 && policy.FirstYearEntitlementDays is not null)
        {
            return policy.FirstYearEntitlementDays.Value;
        }

        if (policy.ProrateIfServiceLessThanYear && serviceMonths < 12)
        {
            return Math.Round(policy.EntitlementDays * Math.Max(0, serviceMonths) / 12, 2, MidpointRounding.AwayFromZero);
        }

        return policy.EntitlementDays;
    }

    private static decimal NormalizeCarryOver(decimal carriedOver, User user, LeavePolicyRule policy, int fiscalYear)
    {
        if (!policy.AllowCarryOver)
        {
            return 0;
        }

        var cap = ResolveCarryOverCap(user, policy, fiscalYear, policy.LeaveType?.Code);

        return Math.Min(carriedOver, cap);
    }

    private static decimal ResolveCarryOverCap(User user, LeavePolicyRule policy, int fiscalYear, string? leaveTypeCode)
    {
        var cap = policy.CarryOverMaxDays ?? FiscalYearHelper.CarryOverDefaultMaxDays;
        if (policy.AllowCarryOver)
        {
            cap = ResolveEmploymentCarryOverCap(user, fiscalYear, cap);
        }

        if (policy.MaxAccumulatedDays is not null)
        {
            cap = Math.Min(cap, policy.MaxAccumulatedDays.Value);
        }

        return Math.Max(0, cap);
    }

    private static decimal ResolveEmploymentCarryOverCap(User user, int fiscalYear, decimal currentCap)
    {
        var serviceMonths = user.EmploymentStartDate is null ? 0 : CalculateCompletedServiceMonths(user.EmploymentStartDate.Value, FiscalYearStartDate(fiscalYear));
        return user.EmploymentType switch
        {
            EmploymentTypes.CivilServant => Math.Min(currentCap, serviceMonths >= 120 ? 30 : 20),
            EmploymentTypes.GovernmentEmployee or EmploymentTypes.MophEmployee => Math.Min(currentCap, 15),
            EmploymentTypes.TemporaryEmployeeDaily or EmploymentTypes.TemporaryEmployeeMonthly => 0,
            _ => currentCap
        };
    }

    private static decimal? ResolveMaxAccumulatedDays(User user, LeavePolicyRule policy, int fiscalYear)
    {
        if (policy.LeaveType?.Code != "VACATION_LEAVE")
        {
            return policy.MaxAccumulatedDays;
        }

        var serviceMonths = user.EmploymentStartDate is null ? 0 : CalculateCompletedServiceMonths(user.EmploymentStartDate.Value, FiscalYearStartDate(fiscalYear));
        return user.EmploymentType switch
        {
            EmploymentTypes.CivilServant => serviceMonths >= 120 ? 30 : 20,
            EmploymentTypes.GovernmentEmployee or EmploymentTypes.MophEmployee => 15,
            _ => policy.MaxAccumulatedDays
        };
    }

    private static DateOnly FiscalYearStartDate(int fiscalYear)
    {
        return new DateOnly(fiscalYear - 1, 10, 1);
    }

    private static int CalculateCompletedServiceMonths(DateOnly startDate, DateOnly asOfDate)
    {
        if (asOfDate < startDate)
        {
            return 0;
        }

        var months = ((asOfDate.Year - startDate.Year) * 12) + asOfDate.Month - startDate.Month;
        if (asOfDate.Day < startDate.Day)
        {
            months--;
        }

        return Math.Max(0, months);
    }
}
