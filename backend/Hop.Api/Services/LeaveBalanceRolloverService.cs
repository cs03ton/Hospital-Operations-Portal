using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed class LeaveBalanceRolloverService(
    AppDbContext db,
    ILeavePolicyService leavePolicyService,
    IAuditLogService auditLogService) : ILeaveBalanceRolloverService
{
    private const string ActionCreated = "Created";
    private const string ActionUpdated = "Updated";
    private const string ActionSkipped = "Skipped";
    private const string ActionBlocked = "Blocked";
    private const string ActionNoChange = "NoChange";

    public async Task<LeaveBalanceRolloverBatchResponse> PreviewAsync(
        LeaveBalanceRolloverFilterRequest request,
        Guid? actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateFiscalYears(request.FromFiscalYear, request.ToFiscalYear);
        var items = await BuildPreviewItemsAsync(request, cancellationToken);
        await auditLogService.WriteAsync(
            actorUserId,
            "LeaveBalance.RolloverPreviewed",
            "LeaveBalance",
            null,
            $"Previewed rollover from {request.FromFiscalYear} to {request.ToFiscalYear}. total={items.Count}.",
            "Success");
        return ToBatchResponse(null, request.FromFiscalYear, request.ToFiscalYear, items);
    }

    public async Task<LeaveBalanceRolloverBatchResponse> ConfirmAsync(
        LeaveBalanceRolloverConfirmBatchRequest request,
        Guid? actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new InvalidOperationException("กรุณาระบุเหตุผลการยกยอด");
        }

        ValidateFiscalYears(request.FromFiscalYear, request.ToFiscalYear);
        var filter = new LeaveBalanceRolloverFilterRequest(
            request.FromFiscalYear,
            request.ToFiscalYear,
            request.DepartmentId,
            request.EmploymentType,
            request.LeaveTypeId,
            request.UserId);
        var items = await BuildPreviewItemsAsync(filter, cancellationToken);
        var run = new LeaveBalanceRolloverRun
        {
            FromFiscalYear = request.FromFiscalYear,
            ToFiscalYear = request.ToFiscalYear,
            Status = "Confirmed",
            FiltersJson = JsonSerializer.Serialize(filter, JsonOptions),
            Total = items.Count,
            CreatedCount = items.Count(item => item.Action == ActionCreated),
            UpdatedCount = items.Count(item => item.Action == ActionUpdated),
            SkippedCount = items.Count(item => item.Action is ActionSkipped or ActionNoChange),
            BlockedCount = items.Count(item => item.Action == ActionBlocked),
            Reason = request.Reason.Trim(),
            CreatedByUserId = actorUserId,
            StartedAt = DateTime.UtcNow
        };

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.LeaveBalanceRolloverRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);

        await auditLogService.WriteAsync(actorUserId, "LeaveBalance.RolloverStarted", "LeaveBalance", run.Id.ToString(), $"Started rollover run from {request.FromFiscalYear} to {request.ToFiscalYear}.", "Success");

        try
        {
            foreach (var item in items.Where(item => item.Action is ActionCreated or ActionUpdated))
            {
                var source = await db.LeaveBalances
                    .AsNoTracking()
                    .FirstOrDefaultAsync(balance =>
                        balance.UserId == item.UserId &&
                        balance.LeaveTypeId == item.LeaveTypeId &&
                        balance.Year == request.FromFiscalYear,
                        cancellationToken);
                if (source is not null)
                {
                    db.LeaveBalanceSnapshots.Add(new LeaveBalanceSnapshot
                    {
                        RolloverRunId = run.Id,
                        UserId = source.UserId,
                        LeaveTypeId = source.LeaveTypeId,
                        FiscalYear = source.Year,
                        EntitlementDays = source.EntitledDays,
                        CarriedOverDays = source.CarriedOverDays,
                        AdjustedDays = source.AdjustedDays,
                        UsedDays = source.UsedDays,
                        PendingDays = source.PendingDays,
                        AvailableDays = FiscalYearHelper.CalculateAvailableDays(source.EntitledDays, source.CarriedOverDays, source.UsedDays, source.PendingDays, source.AdjustedDays),
                        CreatedByUserId = actorUserId
                    });
                }

                var target = await db.LeaveBalances.FirstOrDefaultAsync(balance =>
                    balance.UserId == item.UserId &&
                    balance.LeaveTypeId == item.LeaveTypeId &&
                    balance.Year == request.ToFiscalYear,
                    cancellationToken);

                if (target is null)
                {
                    db.LeaveBalances.Add(new LeaveBalance
                    {
                        UserId = item.UserId,
                        LeaveTypeId = item.LeaveTypeId,
                        Year = request.ToFiscalYear,
                        EntitledDays = item.NewEntitlementDays,
                        CarriedOverDays = item.CarryOverDays,
                        AdjustedDays = 0,
                        UsedDays = 0,
                        PendingDays = 0,
                        Notes = request.Reason.Trim()
                    });
                }
                else
                {
                    target.CarriedOverDays = item.CarryOverDays;
                    target.Notes = request.Reason.Trim();
                    target.UpdatedAt = DateTime.UtcNow;
                }
            }

            run.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            await auditLogService.WriteAsync(actorUserId, "LeaveBalance.RolloverCompleted", "LeaveBalance", run.Id.ToString(), $"Completed rollover run. created={run.CreatedCount}, updated={run.UpdatedCount}, skipped={run.SkippedCount}, blocked={run.BlockedCount}.", "Success");
        }
        catch (Exception ex)
        {
            run.Status = "Failed";
            run.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.RollbackAsync(cancellationToken);
            await auditLogService.WriteAsync(actorUserId, "LeaveBalance.RolloverFailed", "LeaveBalance", run.Id.ToString(), ex.Message, "Failed");
            throw;
        }

        return ToBatchResponse(run.Id, request.FromFiscalYear, request.ToFiscalYear, items);
    }

    public async Task<byte[]> ExportPreviewCsvAsync(
        LeaveBalanceRolloverFilterRequest request,
        Guid? actorUserId,
        CancellationToken cancellationToken = default)
    {
        var response = await PreviewAsync(request, actorUserId, cancellationToken);
        var builder = new StringBuilder();
        builder.AppendLine("employeeName,departmentName,employmentType,leaveType,remaining,cap,carryOver,forfeited,action,reason");
        foreach (var item in response.Items)
        {
            builder.AppendLine(string.Join(",",
            [
                Csv(item.EmployeeName),
                Csv(item.DepartmentName ?? "-"),
                Csv(item.EmploymentTypeName),
                Csv(item.LeaveTypeName),
                item.EndYearRemaining.ToString("0.##"),
                item.CarryOverCap.ToString("0.##"),
                item.CarryOverDays.ToString("0.##"),
                item.ForfeitedDays.ToString("0.##"),
                item.Action,
                Csv(item.Reason)
            ]));
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
    }

    private async Task<IReadOnlyList<LeaveBalanceRolloverItemResponse>> BuildPreviewItemsAsync(
        LeaveBalanceRolloverFilterRequest request,
        CancellationToken cancellationToken)
    {
        var usersQuery = db.Users
            .AsNoTracking()
            .Include(user => user.Department)
            .Where(user => user.IsActive);

        if (request.UserId is not null)
        {
            usersQuery = usersQuery.Where(user => user.Id == request.UserId.Value);
        }

        if (request.DepartmentId is not null)
        {
            usersQuery = usersQuery.Where(user => user.DepartmentId == request.DepartmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.EmploymentType))
        {
            usersQuery = usersQuery.Where(user => user.EmploymentType == request.EmploymentType);
        }

        var users = await usersQuery.OrderBy(user => user.FullName).ToListAsync(cancellationToken);
        var leaveTypesQuery = db.LeaveTypes
            .AsNoTracking()
            .Where(leaveType => leaveType.IsActive && leaveType.RequiresBalance);
        if (request.LeaveTypeId is not null)
        {
            leaveTypesQuery = leaveTypesQuery.Where(leaveType => leaveType.Id == request.LeaveTypeId.Value);
        }
        var leaveTypes = await leaveTypesQuery.OrderBy(leaveType => leaveType.Name).ToListAsync(cancellationToken);
        var fromBalances = await db.LeaveBalances
            .AsNoTracking()
            .Where(balance => balance.Year == request.FromFiscalYear)
            .ToListAsync(cancellationToken);
        var targetBalances = await db.LeaveBalances
            .AsNoTracking()
            .Where(balance => balance.Year == request.ToFiscalYear)
            .ToListAsync(cancellationToken);

        var items = new List<LeaveBalanceRolloverItemResponse>();
        foreach (var user in users)
        {
            foreach (var leaveType in leaveTypes)
            {
                var source = fromBalances.FirstOrDefault(balance => balance.UserId == user.Id && balance.LeaveTypeId == leaveType.Id);
                var existingTarget = targetBalances.FirstOrDefault(balance => balance.UserId == user.Id && balance.LeaveTypeId == leaveType.Id);
                var policy = await leavePolicyService.GetPolicyAsync(user.Id, leaveType.Id, request.ToFiscalYear, cancellationToken)
                    ?? await leavePolicyService.GetPolicyAsync(user.Id, leaveType.Id, request.FromFiscalYear, cancellationToken);
                var entitlement = await leavePolicyService.CalculateEntitlementAsync(user.Id, leaveType.Id, request.ToFiscalYear, cancellationToken);
                var entitled = source?.EntitledDays ?? await leavePolicyService.CalculateEntitlementAsync(user.Id, leaveType.Id, request.FromFiscalYear, cancellationToken);
                var carriedOver = source?.CarriedOverDays ?? 0;
                var adjusted = source?.AdjustedDays ?? 0;
                var used = source?.UsedDays ?? 0;
                var pending = source?.PendingDays ?? 0;
                var endYearRemaining = FiscalYearHelper.CalculateAvailableDays(entitled, carriedOver, used, pending, adjusted);
                var warnings = new List<string>();
                string action;
                string reason;
                decimal carryOverCap = 0;
                decimal carryOverDays = 0;
                decimal forfeitedDays = Math.Max(endYearRemaining, 0);

                if (source is null)
                {
                    action = ActionSkipped;
                    reason = "ไม่พบยอดวันลาปีงบประมาณต้นทาง";
                }
                else if (policy is null)
                {
                    action = ActionBlocked;
                    reason = "ไม่พบ policy สำหรับประเภทบุคลากรและประเภทลานี้";
                }
                else
                {
                    var carry = await leavePolicyService.CalculateCarryOverAsync(user.Id, leaveType.Id, request.FromFiscalYear, endYearRemaining, cancellationToken);
                    carryOverCap = carry.CarryOverCap;
                    carryOverDays = carry.CarryOverDays;
                    forfeitedDays = carry.ForfeitedDays;
                    warnings.AddRange(carry.Warnings);
                    if (carry.Errors.Count > 0)
                    {
                        action = existingTarget is null ? ActionCreated : ActionNoChange;
                        reason = "ประเภทนี้ไม่ยกยอดคงเหลือ แต่สามารถสร้างสิทธิ์ปีใหม่ได้";
                        carryOverDays = 0;
                        forfeitedDays = Math.Max(endYearRemaining, 0);
                    }
                    else if (existingTarget is null)
                    {
                        action = ActionCreated;
                        reason = "สร้างยอดวันลาปีงบประมาณใหม่";
                    }
                    else if (existingTarget.CarriedOverDays == carryOverDays)
                    {
                        action = ActionNoChange;
                        reason = "ยอดปีงบประมาณปลายทางมีอยู่แล้วและยอดยกมาไม่เปลี่ยน";
                    }
                    else
                    {
                        action = ActionUpdated;
                        reason = "อัปเดตเฉพาะยอดยกมาในปีงบประมาณปลายทาง";
                    }
                }

                var newAvailableDays = FiscalYearHelper.CalculateAvailableDays(entitlement, carryOverDays, 0, 0, 0);
                items.Add(new LeaveBalanceRolloverItemResponse(
                    user.Id,
                    user.FullName,
                    user.Department?.Name,
                    user.EmploymentType,
                    EmploymentTypes.GetThaiLabel(user.EmploymentType),
                    leaveType.Id,
                    leaveType.Name,
                    request.FromFiscalYear,
                    request.ToFiscalYear,
                    entitled,
                    carriedOver,
                    adjusted,
                    used,
                    pending,
                    endYearRemaining,
                    carryOverCap,
                    carryOverDays,
                    forfeitedDays,
                    entitlement,
                    newAvailableDays,
                    action,
                    reason,
                    warnings));
            }
        }

        return items;
    }

    private static LeaveBalanceRolloverBatchResponse ToBatchResponse(Guid? runId, int fromFiscalYear, int toFiscalYear, IReadOnlyList<LeaveBalanceRolloverItemResponse> items)
    {
        return new LeaveBalanceRolloverBatchResponse(
            runId,
            fromFiscalYear,
            toFiscalYear,
            items.Select(item => item.UserId).Distinct().Count(),
            items.Count(item => item.Action == ActionCreated),
            items.Count(item => item.Action == ActionUpdated),
            items.Count(item => item.Action is ActionSkipped or ActionNoChange),
            items.Count(item => item.Action == ActionBlocked),
            items);
    }

    private static void ValidateFiscalYears(int fromFiscalYear, int toFiscalYear)
    {
        if (fromFiscalYear > 2400 || toFiscalYear > 2400)
        {
            throw new InvalidOperationException("ปีงบประมาณไม่ถูกต้อง ระบบต้องใช้ปี ค.ศ. ภายใน backend");
        }

        if (fromFiscalYear < 2000 || toFiscalYear < 2000 || toFiscalYear <= fromFiscalYear)
        {
            throw new InvalidOperationException("ปีงบประมาณไม่ถูกต้อง");
        }
    }

    private static string Csv(string value)
    {
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}
