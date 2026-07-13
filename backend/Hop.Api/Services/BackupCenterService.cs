using System.Diagnostics;
using System.Security.Cryptography;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed class BackupCenterService(
    AppDbContext db,
    IConfiguration configuration,
    IWebHostEnvironment environment,
    IAuditLogService auditLogService,
    ILogger<BackupCenterService> logger) : IBackupCenterService
{
    private const string RestoreConfirmationText = "RESTORE HOP";
    private const string ApplyRetentionConfirmationText = "APPLY RETENTION";

    public async Task<BackupOverviewResponse> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        await SyncFileSystemBackupsAsync(cancellationToken);

        var backups = db.BackupRuns
            .AsNoTracking()
            .Include(item => item.CreatedByUser)
            .Include(item => item.VerifiedByUser)
            .Where(item => item.DeletedAt == null);

        var lastSuccessful = await backups
            .Where(item => item.Status == BackupStatuses.Success || item.Status == BackupStatuses.Verified)
            .OrderByDescending(item => item.CompletedAt ?? item.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var lastFailed = await backups
            .Where(item => item.Status == BackupStatuses.Failed || item.Status == BackupStatuses.VerificationFailed)
            .OrderByDescending(item => item.CompletedAt ?? item.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var lastVerified = await backups
            .Where(item => item.Status == BackupStatuses.Verified)
            .OrderByDescending(item => item.VerifiedAt ?? item.CompletedAt ?? item.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var totalSize = await backups.SumAsync(item => item.FileSizeBytes, cancellationToken);
        var lastRestore = await db.RestoreRuns
            .AsNoTracking()
            .Include(item => item.BackupRun)
            .Include(item => item.CreatedByUser)
            .Where(item => item.Status == RestoreStatuses.Success)
            .OrderByDescending(item => item.CompletedAt ?? item.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return new BackupOverviewResponse(
            lastSuccessful is null ? null : ToBackupResponse(lastSuccessful),
            lastFailed is null ? null : ToBackupResponse(lastFailed),
            lastVerified is null ? null : ToBackupResponse(lastVerified),
            lastRestore is null ? null : ToRestoreResponse(lastRestore),
            totalSize,
            ResolveBackupRoot(),
            GetRetentionPolicy());
    }

    public async Task<PagedResponse<BackupRunResponse>> GetBackupsAsync(BackupQuery query, CancellationToken cancellationToken = default)
    {
        await SyncFileSystemBackupsAsync(cancellationToken);

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var items = db.BackupRuns
            .AsNoTracking()
            .Include(item => item.CreatedByUser)
            .Include(item => item.VerifiedByUser)
            .Where(item => item.DeletedAt == null || query.Status == BackupStatuses.Deleted);

        if (!string.IsNullOrWhiteSpace(query.Type))
        {
            items = items.Where(item => item.BackupType == query.Type);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            items = items.Where(item => item.Status == query.Status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            items = items.Where(item => item.FileName.ToLower().Contains(search));
        }

        if (query.DateFrom is not null)
        {
            items = items.Where(item => item.StartedAt >= query.DateFrom.Value);
        }

        if (query.DateTo is not null)
        {
            items = items.Where(item => item.StartedAt <= query.DateTo.Value);
        }

        items = ApplyBackupSort(items, query.Sort, query.Direction);
        var total = await items.CountAsync(cancellationToken);
        var result = await items
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<BackupRunResponse>(
            result.Select(ToBackupResponse).ToList(),
            page,
            pageSize,
            total,
            (int)Math.Ceiling(total / (double)pageSize));
    }

    public async Task<BackupRunDetailResponse?> GetBackupAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await SyncFileSystemBackupsAsync(cancellationToken);
        var backup = await LoadBackupAsync(id, cancellationToken);
        if (backup is null)
        {
            return null;
        }

        var preview = BuildRestorePreview(backup);
        return new BackupRunDetailResponse(
            ToBackupResponse(backup),
            ReadLogSummary(backup),
            preview.CanRestore,
            backup.Status == BackupStatuses.Verified,
            preview.Warnings,
            preview.Errors);
    }

    public async Task<RestorePreviewResponse?> PreviewRestoreAsync(Guid id, Guid? userId, CancellationToken cancellationToken = default)
    {
        await SyncFileSystemBackupsAsync(cancellationToken);
        var backup = await LoadBackupAsync(id, cancellationToken);
        if (backup is null)
        {
            return null;
        }

        var preview = BuildRestorePreview(backup);
        db.RestoreRuns.Add(new RestoreRun
        {
            Id = Guid.NewGuid(),
            BackupRunId = backup.Id,
            RestoreType = backup.BackupType,
            TargetEnvironment = environment.EnvironmentName,
            Status = RestoreStatuses.Previewed,
            Reason = "Restore preview",
            CreatedByUserId = userId,
            ConfirmationMethod = "Preview",
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(userId, "Backup.RestorePreviewed", "BackupRun", backup.Id.ToString(), backup.FileName);

        return preview;
    }

    public async Task<RestoreRunResponse?> RestoreAsync(Guid id, RestoreRequest request, Guid? userId, CancellationToken cancellationToken = default)
    {
        await SyncFileSystemBackupsAsync(cancellationToken);
        var backup = await LoadBackupAsync(id, cancellationToken);
        if (backup is null)
        {
            return null;
        }

        if (!string.Equals(request.ConfirmationText?.Trim(), RestoreConfirmationText, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"กรุณาพิมพ์คำยืนยันว่า {RestoreConfirmationText}");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new InvalidOperationException("กรุณาระบุเหตุผลในการ restore");
        }

        var preview = BuildRestorePreview(backup);
        if (!preview.CanRestore)
        {
            throw new InvalidOperationException(string.Join(" ", preview.Errors));
        }

        if (environment.IsProduction() && backup.Status != BackupStatuses.Verified)
        {
            throw new InvalidOperationException("Production restore ต้องใช้ backup ที่ผ่านการตรวจสอบแล้วเท่านั้น");
        }

        var restoreMode = string.IsNullOrWhiteSpace(request.RestoreMode) ? RestoreTypes.TestDatabase : request.RestoreMode.Trim();
        var isInPlace = string.Equals(restoreMode, "InPlace", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(restoreMode, RestoreTypes.Full, StringComparison.OrdinalIgnoreCase);

        if (environment.IsProduction() && isInPlace && !request.RestoreDatabase && !request.RestoreStorage)
        {
            throw new InvalidOperationException("กรุณาเลือกข้อมูลที่จะ restore");
        }

        var run = new RestoreRun
        {
            Id = Guid.NewGuid(),
            BackupRunId = backup.Id,
            RestoreType = request.RestoreDatabase && request.RestoreStorage ? RestoreTypes.Full : request.RestoreDatabase ? RestoreTypes.Database : RestoreTypes.Storage,
            TargetEnvironment = environment.EnvironmentName,
            TargetDatabase = string.IsNullOrWhiteSpace(request.TargetDatabase) ? null : request.TargetDatabase.Trim(),
            Status = RestoreStatuses.Running,
            Reason = request.Reason.Trim(),
            StartedAt = DateTime.UtcNow,
            CreatedByUserId = userId,
            ConfirmationMethod = RestoreConfirmationText
        };
        db.RestoreRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            // Safety-first Phase 1 implementation: perform script-level dry-run/validation only.
            // Production in-place restore remains a maintenance-window shell/runbook action.
            var validation = await VerifyArchiveAsync(backup, cancellationToken);
            run.Status = validation.Success ? RestoreStatuses.Success : RestoreStatuses.Failed;
            run.ErrorMessage = validation.Success ? null : validation.Message;
            run.CompletedAt = DateTime.UtcNow;
            run.DurationMs = (long)(run.CompletedAt.Value - run.StartedAt).TotalMilliseconds;
            await db.SaveChangesAsync(cancellationToken);

            await auditLogService.WriteAsync(
                userId,
                validation.Success ? "Backup.RestoreConfirmed" : "Backup.RestoreFailed",
                "RestoreRun",
                run.Id.ToString(),
                $"{backup.FileName}; Mode={restoreMode}; Reason={request.Reason}",
                validation.Success ? "Success" : "Failed");
        }
        catch (Exception ex)
        {
            run.Status = RestoreStatuses.Failed;
            run.ErrorMessage = SanitizeMessage(ex.Message);
            run.CompletedAt = DateTime.UtcNow;
            run.DurationMs = (long)(run.CompletedAt.Value - run.StartedAt).TotalMilliseconds;
            await db.SaveChangesAsync(cancellationToken);
            await auditLogService.WriteAsync(userId, "Backup.RestoreFailed", "RestoreRun", run.Id.ToString(), ex.Message, "Failed");
        }

        var loaded = await db.RestoreRuns
            .AsNoTracking()
            .Include(item => item.BackupRun)
            .Include(item => item.CreatedByUser)
            .FirstAsync(item => item.Id == run.Id, cancellationToken);
        return ToRestoreResponse(loaded);
    }

    public async Task<PagedResponse<RestoreRunResponse>> GetRestoreRunsAsync(BackupQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var items = db.RestoreRuns
            .AsNoTracking()
            .Include(item => item.BackupRun)
            .Include(item => item.CreatedByUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            items = items.Where(item => item.Status == query.Status);
        }

        if (!string.IsNullOrWhiteSpace(query.Type))
        {
            items = items.Where(item => item.RestoreType == query.Type);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            items = items.Where(item => item.BackupRun != null && item.BackupRun.FileName.ToLower().Contains(search));
        }

        var total = await items.CountAsync(cancellationToken);
        var result = await items
            .OrderByDescending(item => item.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<RestoreRunResponse>(
            result.Select(ToRestoreResponse).ToList(),
            page,
            pageSize,
            total,
            (int)Math.Ceiling(total / (double)pageSize));
    }

    public async Task<RetentionPreviewResponse> PreviewRetentionAsync(Guid? userId, CancellationToken cancellationToken = default)
    {
        await SyncFileSystemBackupsAsync(cancellationToken);
        var items = await CalculateRetentionItemsAsync(cancellationToken);
        await auditLogService.WriteAsync(userId, "Backup.RetentionPreviewed", "BackupRun", null, $"Delete={items.Count(item => item.Action == "Delete")}");
        return ToRetentionPreview(items);
    }

    public async Task<ApplyRetentionResponse> ApplyRetentionAsync(ApplyRetentionRequest request, Guid? userId, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(request.ConfirmationText?.Trim(), ApplyRetentionConfirmationText, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"กรุณาพิมพ์คำยืนยันว่า {ApplyRetentionConfirmationText}");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new InvalidOperationException("กรุณาระบุเหตุผลในการลบ backup ตาม retention policy");
        }

        await SyncFileSystemBackupsAsync(cancellationToken);
        var items = await CalculateRetentionItemsAsync(cancellationToken);
        var deleteItems = items.Where(item => item.Action == "Delete").ToList();
        var deletedCount = 0;
        var freedBytes = 0L;

        foreach (var item in deleteItems)
        {
            var backup = await db.BackupRuns.FirstOrDefaultAsync(run => run.Id == item.BackupId, cancellationToken);
            if (backup is null || backup.Status == BackupStatuses.Deleted)
            {
                continue;
            }

            try
            {
                if (File.Exists(backup.FilePath))
                {
                    File.Delete(backup.FilePath);
                }

                backup.Status = BackupStatuses.Deleted;
                backup.DeletedAt = DateTime.UtcNow;
                backup.DeletedByUserId = userId;
                backup.UpdatedAt = DateTime.UtcNow;
                deletedCount++;
                freedBytes += backup.FileSizeBytes;
                await auditLogService.WriteAsync(userId, "Backup.FileDeleted", "BackupRun", backup.Id.ToString(), backup.FileName);
            }
            catch (Exception ex)
            {
                backup.ErrorMessage = SanitizeMessage(ex.Message);
                backup.UpdatedAt = DateTime.UtcNow;
                await auditLogService.WriteAsync(userId, "Backup.RetentionFailed", "BackupRun", backup.Id.ToString(), ex.Message, "Failed");
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(userId, "Backup.RetentionApplied", "BackupRun", null, $"{request.Reason}; Deleted={deletedCount}; FreedBytes={freedBytes}");

        return new ApplyRetentionResponse(deletedCount, freedBytes, deleteItems);
    }

    public async Task<BackupVerificationResponse?> VerifyBackupAsync(Guid id, Guid? userId, CancellationToken cancellationToken = default)
    {
        await SyncFileSystemBackupsAsync(cancellationToken);
        var backup = await db.BackupRuns.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (backup is null)
        {
            return null;
        }

        var checksum = await ComputeSha256Async(backup.FilePath, cancellationToken);
        var verification = await VerifyArchiveAsync(backup, cancellationToken);
        backup.Checksum = checksum;
        backup.VerifiedAt = DateTime.UtcNow;
        backup.VerifiedByUserId = userId;
        backup.Status = verification.Success ? BackupStatuses.Verified : BackupStatuses.VerificationFailed;
        backup.ErrorMessage = verification.Success ? null : verification.Message;
        backup.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        await auditLogService.WriteAsync(
            userId,
            verification.Success ? "Backup.Verified" : "Backup.VerificationFailed",
            "BackupRun",
            backup.Id.ToString(),
            verification.Message,
            verification.Success ? "Success" : "Failed");

        return new BackupVerificationResponse(backup.Id, backup.Status, verification.Message, checksum, backup.VerifiedAt.Value);
    }

    private async Task SyncFileSystemBackupsAsync(CancellationToken cancellationToken)
    {
        var backupRoot = ResolveBackupRoot();
        var postgresDir = Path.Combine(backupRoot, "postgres");
        var storageDir = Path.Combine(backupRoot, "storage");
        foreach (var file in EnumerateBackupFiles(postgresDir, BackupTypes.Database).Concat(EnumerateBackupFiles(storageDir, BackupTypes.Storage)))
        {
            if (await db.BackupRuns.AnyAsync(item => item.FilePath == file.Path, cancellationToken))
            {
                continue;
            }

            var info = new FileInfo(file.Path);
            db.BackupRuns.Add(new BackupRun
            {
                Id = Guid.NewGuid(),
                BackupType = file.Type,
                Status = BackupStatuses.Success,
                FileName = info.Name,
                FilePath = info.FullName,
                FileSizeBytes = info.Length,
                StartedAt = info.LastWriteTimeUtc,
                CompletedAt = info.LastWriteTimeUtc,
                DurationMs = 0,
                CreatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private IEnumerable<(string Path, string Type)> EnumerateBackupFiles(string directory, string type)
    {
        if (!Directory.Exists(directory))
        {
            yield break;
        }

        var patterns = type == BackupTypes.Database
            ? ["hopdb_*.backup", "hop_db_*.dump"]
            : new[] { "hop_uploads_*.tar.gz", "hop_storage_*.tar.gz" };

        foreach (var pattern in patterns)
        {
            foreach (var path in Directory.EnumerateFiles(directory, pattern, SearchOption.TopDirectoryOnly))
            {
                yield return (Path.GetFullPath(path), type);
            }
        }
    }

    private RestorePreviewResponse BuildRestorePreview(BackupRun backup)
    {
        var warnings = new List<string>();
        var errors = new List<string>();
        if (backup.DeletedAt is not null || backup.Status == BackupStatuses.Deleted)
        {
            errors.Add("backup นี้ถูกลบแล้ว");
        }

        if (!File.Exists(backup.FilePath))
        {
            errors.Add("ไม่พบไฟล์ backup บน server");
        }

        if (backup.FileSizeBytes <= 0)
        {
            errors.Add("ไฟล์ backup มีขนาด 0 byte");
        }

        if (environment.IsProduction() && backup.Status != BackupStatuses.Verified)
        {
            warnings.Add("Production restore ต้อง verify backup ก่อน");
        }

        if (backup.Status == BackupStatuses.Failed || backup.Status == BackupStatuses.VerificationFailed)
        {
            errors.Add("backup นี้มีสถานะล้มเหลวหรือ verify ไม่ผ่าน");
        }

        if (backup.BackupType == BackupTypes.Storage)
        {
            warnings.Add("Storage restore ควร restore ไป temp path และตรวจไฟล์ก่อน sync ทับ production");
        }

        return new RestorePreviewResponse(
            errors.Count == 0,
            warnings,
            errors,
            ToBackupResponse(backup),
            environment.EnvironmentName,
            environment.IsProduction() ? "TestDatabase or maintenance-window in-place restore" : "TestDatabase",
            TryGetFreeDiskBytes(ResolveBackupRoot()));
    }

    private async Task<List<RetentionPreviewItemResponse>> CalculateRetentionItemsAsync(CancellationToken cancellationToken)
    {
        var policy = GetRetentionPolicy();
        var backups = await db.BackupRuns
            .AsNoTracking()
            .Where(item => item.DeletedAt == null && item.Status != BackupStatuses.Deleted)
            .OrderByDescending(item => item.CompletedAt ?? item.StartedAt)
            .ToListAsync(cancellationToken);
        var latestBackupIds = backups
            .GroupBy(item => item.BackupType)
            .Select(group => group.First().Id)
            .ToHashSet();
        var restoredBackupIds = await db.RestoreRuns
            .AsNoTracking()
            .Where(item => item.Status == RestoreStatuses.Success)
            .Select(item => item.BackupRunId)
            .ToListAsync(cancellationToken);
        var restoredSet = restoredBackupIds.ToHashSet();
        var cutoff = DateTime.UtcNow.AddDays(-policy.DailyDays);
        var failedCutoff = DateTime.UtcNow.AddDays(-policy.KeepFailedDays);

        return backups.Select(item =>
        {
            var createdAt = item.CompletedAt ?? item.StartedAt;
            if (item.Status == BackupStatuses.Running)
            {
                return ToRetentionItem(item, "Protected", "กำลังทำงาน");
            }

            if (latestBackupIds.Contains(item.Id))
            {
                return ToRetentionItem(item, "Protected", "เก็บ backup ล่าสุดของแต่ละประเภท");
            }

            if (restoredSet.Contains(item.Id))
            {
                return ToRetentionItem(item, "Protected", "เคยใช้ restore สำเร็จ");
            }

            if (policy.KeepVerified && item.Status == BackupStatuses.Verified)
            {
                return ToRetentionItem(item, "Protected", "backup ผ่านการ verify");
            }

            if ((item.Status == BackupStatuses.Failed || item.Status == BackupStatuses.VerificationFailed) && createdAt >= failedCutoff)
            {
                return ToRetentionItem(item, "Keep", $"เก็บ failed backup {policy.KeepFailedDays} วัน");
            }

            if (createdAt >= cutoff)
            {
                return ToRetentionItem(item, "Keep", $"อยู่ใน daily retention {policy.DailyDays} วัน");
            }

            return ToRetentionItem(item, "Delete", $"เกิน daily retention {policy.DailyDays} วัน");
        }).ToList();
    }

    private RetentionPreviewResponse ToRetentionPreview(IReadOnlyList<RetentionPreviewItemResponse> items)
    {
        var deleteItems = items.Where(item => item.Action == "Delete").ToList();
        return new RetentionPreviewResponse(
            items.Count,
            items.Count(item => item.Action != "Delete"),
            deleteItems.Count,
            deleteItems.Sum(item => item.FileSizeBytes),
            items);
    }

    private RetentionPreviewItemResponse ToRetentionItem(BackupRun item, string action, string reason)
    {
        return new RetentionPreviewItemResponse(
            item.Id,
            item.FileName,
            item.CompletedAt ?? item.StartedAt,
            item.BackupType,
            item.Status,
            action,
            reason,
            item.FileSizeBytes);
    }

    private BackupRetentionPolicyResponse GetRetentionPolicy()
    {
        return new BackupRetentionPolicyResponse(
            configuration.GetValue("BackupRetention:DailyDays", 14),
            configuration.GetValue("BackupRetention:WeeklyWeeks", 8),
            configuration.GetValue("BackupRetention:MonthlyMonths", 12),
            configuration.GetValue("BackupRetention:KeepVerified", true),
            configuration.GetValue("BackupRetention:KeepFailedDays", 7));
    }

    private async Task<(bool Success, string Message)> VerifyArchiveAsync(BackupRun backup, CancellationToken cancellationToken)
    {
        if (!File.Exists(backup.FilePath))
        {
            return (false, "ไม่พบไฟล์ backup");
        }

        if (backup.BackupType == BackupTypes.Database)
        {
            return await RunProcessAsync("pg_restore", ["--list", backup.FilePath], cancellationToken);
        }

        return await RunProcessAsync("tar", ["-tzf", backup.FilePath], cancellationToken);
    }

    private async Task<(bool Success, string Message)> RunProcessAsync(string fileName, IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            foreach (var arg in args)
            {
                process.StartInfo.ArgumentList.Add(arg);
            }

            process.Start();
            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            var output = $"{await outputTask} {await errorTask}".Trim();
            return process.ExitCode == 0
                ? (true, SanitizeMessage(output.Length == 0 ? "ตรวจสอบไฟล์สำเร็จ" : output))
                : (false, SanitizeMessage(output.Length == 0 ? $"{fileName} failed." : output));
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            logger.LogWarning(ex, "Backup command {Command} is not available", fileName);
            return (false, $"{fileName} ไม่พร้อมใช้งานบน server");
        }
    }

    private async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private IQueryable<BackupRun> ApplyBackupSort(IQueryable<BackupRun> query, string? sort, string? direction)
    {
        var desc = !string.Equals(direction, "asc", StringComparison.OrdinalIgnoreCase);
        return (sort ?? "startedAt").ToLowerInvariant() switch
        {
            "filename" => desc ? query.OrderByDescending(item => item.FileName) : query.OrderBy(item => item.FileName),
            "filesizebytes" or "size" => desc ? query.OrderByDescending(item => item.FileSizeBytes) : query.OrderBy(item => item.FileSizeBytes),
            "status" => desc ? query.OrderByDescending(item => item.Status) : query.OrderBy(item => item.Status),
            "type" => desc ? query.OrderByDescending(item => item.BackupType) : query.OrderBy(item => item.BackupType),
            _ => desc ? query.OrderByDescending(item => item.StartedAt) : query.OrderBy(item => item.StartedAt)
        };
    }

    private async Task<BackupRun?> LoadBackupAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.BackupRuns
            .AsNoTracking()
            .Include(item => item.CreatedByUser)
            .Include(item => item.VerifiedByUser)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    private BackupRunResponse ToBackupResponse(BackupRun item)
    {
        return new BackupRunResponse(
            item.Id,
            item.BackupType,
            item.Status,
            item.FileName,
            SanitizeRelativePath(item.FilePath),
            item.FileSizeBytes,
            item.Checksum,
            item.StartedAt,
            item.CompletedAt,
            item.DurationMs,
            SanitizeMessage(item.ErrorMessage),
            item.CreatedByUser?.FullName ?? item.CreatedByUser?.Username,
            item.VerifiedAt,
            item.VerifiedByUser?.FullName ?? item.VerifiedByUser?.Username,
            item.DeletedAt);
    }

    private RestoreRunResponse ToRestoreResponse(RestoreRun item)
    {
        return new RestoreRunResponse(
            item.Id,
            item.BackupRunId,
            item.BackupRun?.FileName ?? "-",
            item.RestoreType,
            item.TargetEnvironment,
            item.TargetDatabase,
            item.Status,
            item.Reason,
            item.StartedAt,
            item.CompletedAt,
            item.DurationMs,
            SanitizeMessage(item.ErrorMessage),
            item.CreatedByUser?.FullName ?? item.CreatedByUser?.Username,
            item.ConfirmationMethod,
            item.PreRestoreBackupRunId);
    }

    private string ResolveBackupRoot()
    {
        return configuration["Backup:RootPath"] ??
            configuration["BACKUP_ROOT"] ??
            "/opt/hop/backups";
    }

    private string SanitizeRelativePath(string path)
    {
        var root = ResolveBackupRoot();
        try
        {
            return Path.GetRelativePath(root, path).Replace('\\', '/');
        }
        catch
        {
            return Path.GetFileName(path);
        }
    }

    private string ReadLogSummary(BackupRun backup)
    {
        var logDir = Path.Combine(ResolveBackupRoot(), "logs");
        if (!Directory.Exists(logDir))
        {
            return "ยังไม่พบ log directory";
        }

        var datePrefix = backup.StartedAt.ToString("yyyyMMdd");
        var log = Directory.EnumerateFiles(logDir, $"backup_{datePrefix}*.log")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
        if (log is null)
        {
            return "ยังไม่พบ log ของ backup นี้";
        }

        return string.Join(Environment.NewLine, File.ReadLines(log).TakeLast(20)).Trim();
    }

    private long? TryGetFreeDiskBytes(string path)
    {
        try
        {
            var root = Path.GetPathRoot(Path.GetFullPath(path));
            return string.IsNullOrWhiteSpace(root) ? null : new DriveInfo(root).AvailableFreeSpace;
        }
        catch
        {
            return null;
        }
    }

    private static string SanitizeMessage(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sanitized = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return sanitized.Length > 1000 ? sanitized[..1000] : sanitized;
    }
}
