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
            Name = "ลาพักผ่อน",
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
