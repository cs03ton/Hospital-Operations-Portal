using Hop.Api.Data;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hop.Api.Tests;

public class LeavePolicyRuleValidationTests
{
    [Fact]
    public async Task ValidateAvailableBalanceAsync_RejectsWhenEmploymentTypeIsMissing()
    {
        await using var db = CreateDbContext();
        var user = await AddUser(db, employmentType: null);
        var leaveType = await AddLeaveType(db, "VACATION_LEAVE", 10);
        var service = new LeaveBalanceValidationService(db, new LeavePolicyService(db));

        var result = await service.ValidateAvailableBalanceAsync(CreateRequest(user.Id, leaveType.Id), leaveType, 1);

        Assert.False(result.IsValid);
        Assert.Contains("ประเภทบุคลากร", result.Message);
    }

    [Fact]
    public async Task ValidateAvailableBalanceAsync_RejectsWhenPolicyIsMissing()
    {
        await using var db = CreateDbContext();
        var user = await AddUser(db, EmploymentTypes.MophEmployee);
        var leaveType = await AddLeaveType(db, "VACATION_LEAVE", 10);
        var service = new LeaveBalanceValidationService(db, new LeavePolicyService(db));

        var result = await service.ValidateAvailableBalanceAsync(CreateRequest(user.Id, leaveType.Id), leaveType, 1);

        Assert.False(result.IsValid);
        Assert.Contains("ยังไม่ได้กำหนดสิทธิ์", result.Message);
    }

    [Fact]
    public async Task ValidateAvailableBalanceAsync_UsesPolicyAndPendingDays()
    {
        await using var db = CreateDbContext();
        var user = await AddUser(db, EmploymentTypes.MophEmployee);
        var leaveType = await AddLeaveType(db, "VACATION_LEAVE", 10);
        db.LeavePolicyRules.Add(new LeavePolicyRule
        {
            EmploymentType = EmploymentTypes.MophEmployee,
            LeaveTypeId = leaveType.Id,
            EntitlementDays = 10,
            IsActive = true
        });
        db.LeaveBalances.Add(new LeaveBalance
        {
            UserId = user.Id,
            LeaveTypeId = leaveType.Id,
            Year = 2027,
            EntitledDays = 10,
            PendingDays = 8,
            UsedDays = 0
        });
        await db.SaveChangesAsync();

        var service = new LeaveBalanceValidationService(db, new LeavePolicyService(db));
        var result = await service.ValidateAvailableBalanceAsync(CreateRequest(user.Id, leaveType.Id), leaveType, 3);

        Assert.False(result.IsValid);
        Assert.Equal(8, result.PendingDays);
        Assert.Equal(2, result.AvailableDays);
    }

    [Fact]
    public async Task ValidateLeaveRequestAsync_RejectsVacationWhenServiceIsLessThanSixMonths()
    {
        await using var db = CreateDbContext();
        var user = await AddUser(db, EmploymentTypes.CivilServant, new DateOnly(2026, 7, 1));
        var leaveType = await AddLeaveType(db, "VACATION_LEAVE", 10);
        db.LeavePolicyRules.Add(new LeavePolicyRule
        {
            EmploymentType = EmploymentTypes.CivilServant,
            LeaveTypeId = leaveType.Id,
            EntitlementDays = 10,
            MinServiceMonths = 6,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var result = await new LeavePolicyService(db).ValidateLeaveRequestAsync(
            user.Id,
            leaveType.Id,
            new DateOnly(2026, 10, 5),
            new DateOnly(2026, 10, 5),
            LeaveDurationTypes.FullDay,
            1);

        Assert.False(result.CanSubmit);
        Assert.Contains(result.Errors, item => item.Contains("อายุงาน"));
        Assert.Contains(result.Errors, item => item.Contains("ลาพักผ่อน"));
        Assert.Contains(result.Errors, item => item.Contains("01/01/2570"));
    }

    [Fact]
    public async Task ValidateLeaveRequestAsync_AllowsVacationWhenServiceIsExactlySixMonths()
    {
        await using var db = CreateDbContext();
        var user = await AddUser(db, EmploymentTypes.CivilServant, new DateOnly(2026, 4, 5));
        var leaveType = await AddLeaveType(db, "VACATION_LEAVE", 10);
        db.LeavePolicyRules.Add(new LeavePolicyRule
        {
            EmploymentType = EmploymentTypes.CivilServant,
            LeaveTypeId = leaveType.Id,
            EntitlementDays = 10,
            MinServiceMonths = 6,
            IsActive = true,
            LeaveType = leaveType
        });
        await db.SaveChangesAsync();

        var result = await new LeavePolicyService(db).ValidateLeaveRequestAsync(
            user.Id,
            leaveType.Id,
            new DateOnly(2026, 10, 5),
            new DateOnly(2026, 10, 5),
            LeaveDurationTypes.FullDay,
            1);

        Assert.True(result.CanSubmit);
    }

    [Fact]
    public async Task ValidateLeaveRequestAsync_AllowsSickLeaveForTemporaryEmployeeBeforeSixMonthsWithinFirstYearLimit()
    {
        await using var db = CreateDbContext();
        var user = await AddUser(db, EmploymentTypes.TemporaryEmployeeMonthly, new DateOnly(2026, 8, 1));
        var leaveType = await AddLeaveType(db, "SICK_LEAVE", 15);
        db.LeavePolicyRules.Add(new LeavePolicyRule
        {
            EmploymentType = EmploymentTypes.TemporaryEmployeeMonthly,
            LeaveTypeId = leaveType.Id,
            EntitlementDays = 15,
            MaxPaidDays = 15,
            FirstYearEntitlementDays = 8,
            FirstYearPaidDays = 8,
            IsActive = true,
            LeaveType = leaveType
        });
        await db.SaveChangesAsync();

        var result = await new LeavePolicyService(db).ValidateLeaveRequestAsync(
            user.Id,
            leaveType.Id,
            new DateOnly(2026, 10, 5),
            new DateOnly(2026, 10, 6),
            LeaveDurationTypes.FullDay,
            2);

        Assert.True(result.CanSubmit);
        Assert.Equal(8, result.EntitlementDays);
    }

    [Fact]
    public async Task ValidateLeaveRequestAsync_AllowsTemporaryEmployeeSickLeaveBeyondProbationPaidLimitWithBreakdown()
    {
        await using var db = CreateDbContext();
        var user = await AddUser(db, EmploymentTypes.TemporaryEmployeeMonthly, new DateOnly(2026, 8, 1));
        var leaveType = await AddLeaveType(db, "SICK_LEAVE", 15);
        db.LeavePolicyRules.Add(new LeavePolicyRule
        {
            EmploymentType = EmploymentTypes.TemporaryEmployeeMonthly,
            LeaveTypeId = leaveType.Id,
            EntitlementDays = 15,
            MaxPaidDays = 15,
            EmployerPaidLimitDays = 15,
            FirstYearEntitlementDays = 8,
            ProbationEntitlementDays = 8,
            FirstYearPaidDays = 8,
            MaximumLeaveDays = 15,
            IsActive = true,
            LeaveType = leaveType
        });
        await db.SaveChangesAsync();

        var result = await new LeavePolicyService(db).ValidateLeaveRequestAsync(
            user.Id,
            leaveType.Id,
            new DateOnly(2026, 10, 5),
            new DateOnly(2026, 10, 13),
            LeaveDurationTypes.FullDay,
            9);

        Assert.True(result.CanSubmit);
        Assert.Contains(result.PaymentSegments, item => item.PaymentSource == "Employer" && item.Days == 8);
        Assert.Contains(result.PaymentSegments, item => item.PaymentStatus == "Unpaid" && item.Days == 1);
    }

    [Fact]
    public async Task ValidateLeaveRequestAsync_RejectsGovernmentEmployeePersonalLeaveBeforeOneYear()
    {
        await using var db = CreateDbContext();
        var user = await AddUser(db, EmploymentTypes.GovernmentEmployee, new DateOnly(2025, 11, 6));
        var leaveType = await AddLeaveType(db, "PERSONAL_LEAVE", 10);
        db.LeavePolicyRules.Add(new LeavePolicyRule
        {
            EmploymentType = EmploymentTypes.GovernmentEmployee,
            LeaveTypeId = leaveType.Id,
            EntitlementDays = 10,
            MinServiceMonths = 12,
            IsActive = true,
            LeaveType = leaveType
        });
        await db.SaveChangesAsync();

        var result = await new LeavePolicyService(db).ValidateLeaveRequestAsync(
            user.Id,
            leaveType.Id,
            new DateOnly(2026, 10, 5),
            new DateOnly(2026, 10, 5),
            LeaveDurationTypes.FullDay,
            1);

        Assert.False(result.CanSubmit);
        Assert.Contains(result.Errors, item => item.Contains("ลากิจส่วนตัว"));
    }

    [Fact]
    public async Task ValidateLeaveRequestAsync_AllowsGovernmentEmployeePersonalLeaveAfterOneYear()
    {
        await using var db = CreateDbContext();
        var user = await AddUser(db, EmploymentTypes.GovernmentEmployee, new DateOnly(2025, 10, 5));
        var leaveType = await AddLeaveType(db, "PERSONAL_LEAVE", 10);
        db.LeavePolicyRules.Add(new LeavePolicyRule
        {
            EmploymentType = EmploymentTypes.GovernmentEmployee,
            LeaveTypeId = leaveType.Id,
            EntitlementDays = 10,
            MinServiceMonths = 12,
            IsActive = true,
            LeaveType = leaveType
        });
        await db.SaveChangesAsync();

        var result = await new LeavePolicyService(db).ValidateLeaveRequestAsync(
            user.Id,
            leaveType.Id,
            new DateOnly(2026, 10, 5),
            new DateOnly(2026, 10, 5),
            LeaveDurationTypes.FullDay,
            1);

        Assert.True(result.CanSubmit);
    }

    [Fact]
    public async Task ValidateLeaveRequestAsync_RejectsCivilServantOrdinationBeforeTwelveMonths()
    {
        await using var db = CreateDbContext();
        var user = await AddUser(db, EmploymentTypes.CivilServant, new DateOnly(2025, 11, 6), GenderTypes.Male);
        var leaveType = await AddLeaveType(db, "ORDINATION_LEAVE", 120, requiresBalance: false);
        db.LeavePolicyRules.Add(new LeavePolicyRule
        {
            EmploymentType = EmploymentTypes.CivilServant,
            LeaveTypeId = leaveType.Id,
            EntitlementDays = 120,
            MinServiceMonths = 12,
            IsActive = true,
            LeaveType = leaveType
        });
        await db.SaveChangesAsync();

        var result = await new LeavePolicyService(db).ValidateLeaveRequestAsync(
            user.Id,
            leaveType.Id,
            new DateOnly(2026, 10, 5),
            new DateOnly(2026, 10, 5),
            LeaveDurationTypes.FullDay,
            1);

        Assert.False(result.CanSubmit);
        Assert.Contains(result.Errors, item => item.Contains("ลาบวช"));
    }

    [Fact]
    public async Task ValidateLeaveRequestAsync_AllowsCivilServantOrdinationAfterTwelveMonths()
    {
        await using var db = CreateDbContext();
        var user = await AddUser(db, EmploymentTypes.CivilServant, new DateOnly(2025, 10, 5), GenderTypes.Male);
        var leaveType = await AddLeaveType(db, "ORDINATION_LEAVE", 120, requiresBalance: false);
        db.LeavePolicyRules.Add(new LeavePolicyRule
        {
            EmploymentType = EmploymentTypes.CivilServant,
            LeaveTypeId = leaveType.Id,
            EntitlementDays = 120,
            MinServiceMonths = 12,
            IsActive = true,
            LeaveType = leaveType
        });
        await db.SaveChangesAsync();

        var result = await new LeavePolicyService(db).ValidateLeaveRequestAsync(
            user.Id,
            leaveType.Id,
            new DateOnly(2026, 10, 5),
            new DateOnly(2026, 10, 5),
            LeaveDurationTypes.FullDay,
            1);

        Assert.True(result.CanSubmit);
    }

    [Fact]
    public async Task ValidateLeaveRequestAsync_RejectsGovernmentEmployeeOrdinationBeforeFourYears()
    {
        await using var db = CreateDbContext();
        var user = await AddUser(db, EmploymentTypes.GovernmentEmployee, new DateOnly(2023, 10, 6), GenderTypes.Male);
        var leaveType = await AddLeaveType(db, "ORDINATION_LEAVE", 120, requiresBalance: false);
        db.LeavePolicyRules.Add(new LeavePolicyRule
        {
            EmploymentType = EmploymentTypes.GovernmentEmployee,
            LeaveTypeId = leaveType.Id,
            EntitlementDays = 120,
            MinServiceYears = 4,
            IsActive = true,
            LeaveType = leaveType
        });
        await db.SaveChangesAsync();

        var result = await new LeavePolicyService(db).ValidateLeaveRequestAsync(
            user.Id,
            leaveType.Id,
            new DateOnly(2026, 10, 5),
            new DateOnly(2026, 10, 5),
            LeaveDurationTypes.FullDay,
            1);

        Assert.False(result.CanSubmit);
        Assert.Contains(result.Errors, item => item.Contains("4 ปี"));
    }

    [Fact]
    public async Task ValidateLeaveRequestAsync_AllowsTemporaryPersonalLeaveWithUnpaidWarning()
    {
        await using var db = CreateDbContext();
        var user = await AddUser(db, EmploymentTypes.TemporaryEmployeeDaily, new DateOnly(2026, 8, 1));
        var leaveType = await AddLeaveType(db, "PERSONAL_LEAVE", 10);
        db.LeavePolicyRules.Add(new LeavePolicyRule
        {
            EmploymentType = EmploymentTypes.TemporaryEmployeeDaily,
            LeaveTypeId = leaveType.Id,
            EntitlementDays = 10,
            IsPaid = false,
            IsActive = true,
            LeaveType = leaveType
        });
        await db.SaveChangesAsync();

        var result = await new LeavePolicyService(db).ValidateLeaveRequestAsync(
            user.Id,
            leaveType.Id,
            new DateOnly(2026, 10, 5),
            new DateOnly(2026, 10, 5),
            LeaveDurationTypes.FullDay,
            1);

        Assert.True(result.CanSubmit);
        Assert.Contains(result.Warnings, item => item.Contains("ไม่มีสิทธิได้รับค่าจ้าง"));
    }

    [Fact]
    public async Task ValidateLeaveRequestAsync_AllowsDailyEmployeeMaternityLeaveWithUnpaidWarning()
    {
        await using var db = CreateDbContext();
        var user = await AddUser(db, EmploymentTypes.TemporaryEmployeeDaily, new DateOnly(2026, 8, 1), GenderTypes.Female);
        var leaveType = await AddLeaveType(db, "MATERNITY_LEAVE", 90, requiresBalance: false);
        db.LeavePolicyRules.Add(new LeavePolicyRule
        {
            EmploymentType = EmploymentTypes.TemporaryEmployeeDaily,
            LeaveTypeId = leaveType.Id,
            EntitlementDays = 90,
            IsPaid = false,
            IsActive = true,
            LeaveType = leaveType
        });
        await db.SaveChangesAsync();

        var result = await new LeavePolicyService(db).ValidateLeaveRequestAsync(
            user.Id,
            leaveType.Id,
            new DateOnly(2026, 10, 5),
            new DateOnly(2026, 10, 5),
            LeaveDurationTypes.FullDay,
            1);

        Assert.True(result.CanSubmit);
        Assert.Contains(result.Warnings, item => item.Contains("ไม่มีสิทธิได้รับค่าจ้าง"));
    }

    [Fact]
    public async Task ValidateLeaveRequestAsync_SplitsCivilServantSickLeaveIntoPaidAndSpecialApproval()
    {
        await using var db = CreateDbContext();
        var user = await AddUser(db, EmploymentTypes.CivilServant, new DateOnly(2020, 10, 1));
        var leaveType = await AddLeaveType(db, "SICK_LEAVE", 60);
        db.LeavePolicyRules.Add(new LeavePolicyRule
        {
            EmploymentType = EmploymentTypes.CivilServant,
            LeaveTypeId = leaveType.Id,
            EntitlementDays = 60,
            AnnualEntitlementDays = 60,
            MaxPaidDays = 60,
            EmployerPaidLimitDays = 60,
            MaxExtendedDays = 120,
            MaximumLeaveDays = 120,
            RequiresSpecialApprovalAfterDays = 60,
            PaymentRuleType = "EmployerPaidThenSpecialApproval",
            IsActive = true,
            LeaveType = leaveType
        });
        db.LeaveBalances.Add(new LeaveBalance
        {
            UserId = user.Id,
            LeaveTypeId = leaveType.Id,
            Year = 2027,
            EntitledDays = 60,
            UsedDays = 55
        });
        await db.SaveChangesAsync();

        var result = await new LeavePolicyService(db).ValidateLeaveRequestAsync(
            user.Id,
            leaveType.Id,
            new DateOnly(2026, 10, 5),
            new DateOnly(2026, 10, 14),
            LeaveDurationTypes.FullDay,
            10);

        Assert.True(result.CanSubmit);
        Assert.True(result.RequiresSpecialApproval);
        Assert.Equal("requires_special_approval", result.LimitStatus);
        Assert.Contains(result.PaymentSegments, item => item.PaymentSource == "Employer" && item.Days == 5);
        Assert.Contains(result.PaymentSegments, item => item.PaymentSource == "SpecialApproval" && item.Days == 5);
    }

    [Fact]
    public async Task ValidateLeaveRequestAsync_RejectsCivilServantSickLeaveBeyondMaximumExtendedDays()
    {
        await using var db = CreateDbContext();
        var user = await AddUser(db, EmploymentTypes.CivilServant, new DateOnly(2020, 10, 1));
        var leaveType = await AddLeaveType(db, "SICK_LEAVE", 60);
        db.LeavePolicyRules.Add(new LeavePolicyRule
        {
            EmploymentType = EmploymentTypes.CivilServant,
            LeaveTypeId = leaveType.Id,
            EntitlementDays = 60,
            EmployerPaidLimitDays = 60,
            MaximumLeaveDays = 120,
            RequiresSpecialApprovalAfterDays = 60,
            IsActive = true,
            LeaveType = leaveType
        });
        db.LeaveBalances.Add(new LeaveBalance
        {
            UserId = user.Id,
            LeaveTypeId = leaveType.Id,
            Year = 2027,
            EntitledDays = 60,
            UsedDays = 115
        });
        await db.SaveChangesAsync();

        var result = await new LeavePolicyService(db).ValidateLeaveRequestAsync(
            user.Id,
            leaveType.Id,
            new DateOnly(2026, 10, 5),
            new DateOnly(2026, 10, 14),
            LeaveDurationTypes.FullDay,
            10);

        Assert.False(result.CanSubmit);
        Assert.Contains(result.Errors, item => item.Contains("วันลาคงเหลือไม่เพียงพอ"));
    }

    [Fact]
    public async Task ValidateLeaveRequestAsync_SplitsGovernmentEmployeeSickLeaveIntoEmployerAndSocialSecurity()
    {
        await using var db = CreateDbContext();
        var user = await AddUser(db, EmploymentTypes.GovernmentEmployee, new DateOnly(2020, 10, 1));
        var leaveType = await AddLeaveType(db, "SICK_LEAVE", 30);
        db.LeavePolicyRules.Add(new LeavePolicyRule
        {
            EmploymentType = EmploymentTypes.GovernmentEmployee,
            LeaveTypeId = leaveType.Id,
            EntitlementDays = 30,
            EmployerPaidLimitDays = 30,
            SocialSecurityMaxDays = 90,
            UsesSocialSecurity = true,
            PaymentRuleType = "EmployerPaidThenSocialSecurity",
            IsActive = true,
            LeaveType = leaveType
        });
        db.LeaveBalances.Add(new LeaveBalance
        {
            UserId = user.Id,
            LeaveTypeId = leaveType.Id,
            Year = 2027,
            EntitledDays = 30,
            UsedDays = 28
        });
        await db.SaveChangesAsync();

        var result = await new LeavePolicyService(db).ValidateLeaveRequestAsync(
            user.Id,
            leaveType.Id,
            new DateOnly(2026, 10, 5),
            new DateOnly(2026, 10, 9),
            LeaveDurationTypes.FullDay,
            5);

        Assert.True(result.CanSubmit);
        Assert.Contains(result.PaymentSegments, item => item.PaymentSource == "Employer" && item.Days == 2);
        Assert.Contains(result.PaymentSegments, item => item.PaymentSource == "SocialSecurity" && item.Days == 3);
    }

    [Fact]
    public async Task ValidateLeaveRequestAsync_SplitsTemporaryProbationSickLeaveIntoPaidAndUnpaid()
    {
        await using var db = CreateDbContext();
        var user = await AddUser(db, EmploymentTypes.TemporaryEmployeeMonthly, new DateOnly(2026, 5, 6));
        var leaveType = await AddLeaveType(db, "SICK_LEAVE", 15);
        db.LeavePolicyRules.Add(new LeavePolicyRule
        {
            EmploymentType = EmploymentTypes.TemporaryEmployeeMonthly,
            LeaveTypeId = leaveType.Id,
            EntitlementDays = 15,
            EmployerPaidLimitDays = 15,
            ProbationEntitlementDays = 8,
            FirstYearEntitlementDays = 8,
            FirstYearPaidDays = 8,
            MaximumLeaveDays = 15,
            IsActive = true,
            LeaveType = leaveType
        });
        db.LeaveBalances.Add(new LeaveBalance
        {
            UserId = user.Id,
            LeaveTypeId = leaveType.Id,
            Year = 2027,
            EntitledDays = 8,
            UsedDays = 7
        });
        await db.SaveChangesAsync();

        var result = await new LeavePolicyService(db).ValidateLeaveRequestAsync(
            user.Id,
            leaveType.Id,
            new DateOnly(2026, 10, 5),
            new DateOnly(2026, 10, 7),
            LeaveDurationTypes.FullDay,
            3);

        Assert.True(result.CanSubmit);
        Assert.Contains(result.PaymentSegments, item => item.PaymentSource == "Employer" && item.Days == 1);
        Assert.Contains(result.PaymentSegments, item => item.PaymentStatus == "Unpaid" && item.Days == 2);
    }

    [Fact]
    public async Task CalculateAvailableDaysAsync_CapsCivilServantVacationAccumulationByServiceYears()
    {
        await using var db = CreateDbContext();
        var user = await AddUser(db, EmploymentTypes.CivilServant, new DateOnly(2020, 10, 1));
        var leaveType = await AddLeaveType(db, "VACATION_LEAVE", 10);
        db.LeavePolicyRules.Add(new LeavePolicyRule
        {
            EmploymentType = EmploymentTypes.CivilServant,
            LeaveTypeId = leaveType.Id,
            EntitlementDays = 10,
            AllowCarryOver = true,
            CarryOverMaxDays = 30,
            MaxAccumulatedDays = 30,
            IsActive = true,
            LeaveType = leaveType
        });
        db.LeaveBalances.Add(new LeaveBalance
        {
            UserId = user.Id,
            LeaveTypeId = leaveType.Id,
            Year = 2027,
            EntitledDays = 10,
            CarriedOverDays = 30
        });
        await db.SaveChangesAsync();

        var result = await new LeavePolicyService(db).CalculateAvailableDaysAsync(user.Id, leaveType.Id, 2027);

        Assert.Equal(20, result.AvailableDays);
    }

    [Fact]
    public async Task ValidateLeaveRequestAsync_ReturnsMaternityPaidPolicyNote()
    {
        await using var db = CreateDbContext();
        var user = await AddUser(db, EmploymentTypes.MophEmployee, gender: GenderTypes.Female);
        var leaveType = await AddLeaveType(db, "MATERNITY_LEAVE", 90, requiresBalance: false);
        db.LeavePolicyRules.Add(new LeavePolicyRule
        {
            EmploymentType = EmploymentTypes.MophEmployee,
            LeaveTypeId = leaveType.Id,
            EntitlementDays = 90,
            MaxPaidDays = 45,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var result = await new LeavePolicyService(db).ValidateLeaveRequestAsync(
            user.Id,
            leaveType.Id,
            new DateOnly(2026, 10, 5),
            new DateOnly(2026, 10, 5),
            LeaveDurationTypes.FullDay,
            1);

        Assert.True(result.CanSubmit);
        Assert.Contains(result.PolicyNotes, item => item.Contains("45"));
    }

    [Fact]
    public async Task ValidateAvailableBalanceAsync_SkipsBalanceForNonQuotaLeaveButKeepsPolicyValidation()
    {
        await using var db = CreateDbContext();
        var user = await AddUser(db, EmploymentTypes.MophEmployee, gender: GenderTypes.Female);
        var leaveType = await AddLeaveType(db, "MATERNITY_LEAVE", 90, requiresBalance: false);
        db.LeavePolicyRules.Add(new LeavePolicyRule
        {
            EmploymentType = EmploymentTypes.MophEmployee,
            LeaveTypeId = leaveType.Id,
            EntitlementDays = 90,
            MaxPaidDays = 45,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var service = new LeaveBalanceValidationService(db, new LeavePolicyService(db));
        var result = await service.ValidateAvailableBalanceAsync(CreateRequest(user.Id, leaveType.Id), leaveType, 90);

        Assert.True(result.IsValid);
        Assert.False(result.RequiresBalance);
        Assert.Equal(decimal.MaxValue, result.AvailableDays);
    }

    [Theory]
    [InlineData(GenderTypes.Male, "ORDINATION_LEAVE", true, null)]
    [InlineData(GenderTypes.Male, "MATERNITY_LEAVE", false, "ประเภทการลาคลอดบุตร ใช้ได้เฉพาะบุคลากรเพศหญิง")]
    [InlineData(GenderTypes.Female, "MATERNITY_LEAVE", true, null)]
    [InlineData(GenderTypes.Female, "ORDINATION_LEAVE", false, "ประเภทการลาบวช ใช้ได้เฉพาะบุคลากรเพศชาย")]
    [InlineData(GenderTypes.Unknown, "MATERNITY_LEAVE", false, "ประเภทการลาคลอดบุตร ใช้ได้เฉพาะบุคลากรเพศหญิง")]
    [InlineData(GenderTypes.Unknown, "ORDINATION_LEAVE", false, "ประเภทการลาบวช ใช้ได้เฉพาะบุคลากรเพศชาย")]
    public async Task ValidateLeaveRequestAsync_EnforcesGenderEligibility(string gender, string leaveTypeCode, bool expectedCanSubmit, string? expectedError)
    {
        await using var db = CreateDbContext();
        var user = await AddUser(db, EmploymentTypes.MophEmployee, gender: gender);
        var leaveType = await AddLeaveType(db, leaveTypeCode, 90);
        db.LeavePolicyRules.Add(new LeavePolicyRule
        {
            EmploymentType = EmploymentTypes.MophEmployee,
            LeaveTypeId = leaveType.Id,
            EntitlementDays = 90,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var result = await new LeavePolicyService(db).ValidateLeaveRequestAsync(
            user.Id,
            leaveType.Id,
            new DateOnly(2026, 10, 5),
            new DateOnly(2026, 10, 5),
            LeaveDurationTypes.FullDay,
            1);

        Assert.Equal(expectedCanSubmit, result.CanSubmit);
        if (expectedError is not null)
        {
            Assert.Contains(expectedError, result.Errors);
        }
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<User> AddUser(AppDbContext db, string? employmentType, DateOnly? startDate = null, string gender = GenderTypes.Unknown)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Test User",
            Username = $"user-{Guid.NewGuid():N}",
            PasswordHash = "hash",
            EmploymentType = employmentType,
            EmploymentStartDate = startDate ?? new DateOnly(2024, 10, 1),
            Gender = gender,
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task<LeaveType> AddLeaveType(AppDbContext db, string code, decimal defaultDays, bool requiresBalance = true)
    {
        var leaveType = new LeaveType
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = code switch
            {
                "SICK_LEAVE" => "ลาป่วย",
                "PERSONAL_LEAVE" => "ลากิจส่วนตัว",
                "MATERNITY_LEAVE" => "ลาคลอดบุตร",
                "ORDINATION_LEAVE" => "ลาบวช",
                _ => "ลาพักผ่อน"
            },
            DefaultDaysPerYear = defaultDays,
            RequiresBalance = requiresBalance,
            IsActive = true
        };
        db.LeaveTypes.Add(leaveType);
        await db.SaveChangesAsync();
        return leaveType;
    }

    private static LeaveRequest CreateRequest(Guid userId, Guid leaveTypeId)
    {
        return new LeaveRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LeaveTypeId = leaveTypeId,
            StartDate = new DateOnly(2026, 10, 5),
            EndDate = new DateOnly(2026, 10, 5),
            DurationType = LeaveDurationTypes.FullDay,
            TotalDays = 1,
            Reason = "Test"
        };
    }
}
