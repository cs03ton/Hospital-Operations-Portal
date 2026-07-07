using System.Diagnostics;
using Hop.Api.Authorization;
using Hop.Api.DTOs;
using Hop.Api.Data;
using Hop.Api.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/admin/health")]
[RequirePermission("SystemSettings.View")]
public class AdminHealthController(
    AppDbContext db,
    IConfiguration configuration,
    IWebHostEnvironment environment,
    LineConfigurationResolver lineConfiguration) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<AdminHealthResponse>>> Get(CancellationToken cancellationToken)
    {
        var database = await CheckDatabase(cancellationToken);
        var storage = CheckStorage();
        var line = await CheckLine(cancellationToken);
        var queue = await CheckQueue(cancellationToken);
        var disk = CheckDisk();
        var backup = CheckBackup();
        var version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0";

        var response = new AdminHealthResponse(
            Api: new HealthComponentResponse("Healthy"),
            Database: database,
            Storage: storage,
            Line: line,
            Queue: queue,
            Disk: disk,
            Backup: backup,
            Version: version,
            Environment: environment.EnvironmentName,
            CurrentTimeServer: DateTime.UtcNow);

        return ApiResponse<AdminHealthResponse>.Ok(response);
    }

    private async Task<HealthComponentResponse> CheckDatabase(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var canConnect = await db.Database.CanConnectAsync(cancellationToken);
            stopwatch.Stop();
            return canConnect
                ? new HealthComponentResponse("Healthy", LatencyMs: stopwatch.ElapsedMilliseconds)
                : new HealthComponentResponse("Unhealthy", "ไม่สามารถเชื่อมต่อฐานข้อมูลได้", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception)
        {
            stopwatch.Stop();
            return new HealthComponentResponse("Unhealthy", "ไม่สามารถตรวจสอบฐานข้อมูลได้", stopwatch.ElapsedMilliseconds);
        }
    }

    private StorageHealthResponse CheckStorage()
    {
        var rootPath = configuration["Storage:RootPath"] ?? configuration["STORAGE_ROOT_PATH"];
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            return new StorageHealthResponse("Unhealthy", false, "ยังไม่ได้ตั้งค่า Storage__RootPath");
        }

        try
        {
            Directory.CreateDirectory(rootPath);
            var testFile = Path.Combine(rootPath, $".health-{Guid.NewGuid():N}.tmp");
            System.IO.File.WriteAllText(testFile, DateTime.UtcNow.ToString("O"));
            System.IO.File.Delete(testFile);
            return new StorageHealthResponse("Healthy", true);
        }
        catch (Exception)
        {
            return new StorageHealthResponse("Unhealthy", false, "ไม่สามารถเขียนไฟล์ใน storage ได้");
        }
    }

    private async Task<LineHealthResponse> CheckLine(CancellationToken cancellationToken)
    {
        DateTime? lastSuccess = null;
        DateTime? lastFailure = null;

        try
        {
            lastSuccess = await db.LineDeliveryLogs
                .AsNoTracking()
                .Where(item => item.Status == "Sent" || item.Status == "Success")
                .OrderByDescending(item => item.SentAt ?? item.UpdatedAt ?? item.CreatedAt)
                .Select(item => item.SentAt ?? item.UpdatedAt ?? item.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
            lastFailure = await db.LineDeliveryLogs
                .AsNoTracking()
                .Where(item => item.Status == "Failed")
                .OrderByDescending(item => item.UpdatedAt ?? item.CreatedAt)
                .Select(item => item.UpdatedAt ?? item.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception)
        {
            return new LineHealthResponse("Warning", lineConfiguration.Enabled, null, null, "ไม่สามารถอ่านประวัติการส่ง LINE ได้");
        }

        var status = !lineConfiguration.Enabled
            ? "Disabled"
            : lineConfiguration.HasAccessToken && lineConfiguration.HasChannelSecret
                ? "Healthy"
                : "Warning";
        var message = status switch
        {
            "Disabled" => "LINE Messaging API ปิดใช้งาน",
            "Warning" => "LINE เปิดใช้งานแต่ตั้งค่า token หรือ secret ยังไม่ครบ",
            _ => null
        };

        return new LineHealthResponse(
            status,
            lineConfiguration.Enabled,
            lastSuccess == default ? null : lastSuccess,
            lastFailure == default ? null : lastFailure,
            message);
    }

    private async Task<QueueHealthResponse> CheckQueue(CancellationToken cancellationToken)
    {
        var lineRetryEnabled = configuration.GetValue("LineRetry:Enabled", false);
        var approvalEscalationEnabled = configuration.GetValue("ApprovalEscalation:Enabled", false);

        try
        {
            var now = DateTime.UtcNow;
            var deliveryStats = await db.LineDeliveryLogs
                .AsNoTracking()
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    Pending = group.Count(item => item.Status == "Queued"),
                    Failed = group.Count(item => item.Status == "Failed"),
                    PendingRetries = group.Count(item =>
                        (item.Status == "Queued" || item.Status == "Failed") &&
                        (item.NextRetryAt == null || item.NextRetryAt <= now)),
                    LastSuccess = group
                        .Where(item => item.Status == "Sent" || item.Status == "Success")
                        .Max(item => (DateTime?)(item.SentAt ?? item.UpdatedAt ?? item.CreatedAt)),
                    LastFailure = group
                        .Where(item => item.Status == "Failed")
                        .Max(item => (DateTime?)(item.UpdatedAt ?? item.CreatedAt))
                })
                .FirstOrDefaultAsync(cancellationToken);

            var pending = deliveryStats?.Pending ?? 0;
            var failed = deliveryStats?.Failed ?? 0;
            var pendingRetries = deliveryStats?.PendingRetries ?? 0;
            var status = failed > 0
                ? "Warning"
                : pending > 100
                    ? "Warning"
                    : "Healthy";
            var message = status == "Warning"
                ? $"LINE queue มี pending {pending} รายการ, failed {failed} รายการ, retry พร้อมส่ง {pendingRetries} รายการ"
                : lineRetryEnabled || approvalEscalationEnabled
                    ? "worker พร้อมใช้งาน"
                    : "worker ยังปิดใช้งานตาม configuration";

            return new QueueHealthResponse(
                status,
                lineRetryEnabled,
                approvalEscalationEnabled,
                pending,
                failed,
                pendingRetries,
                deliveryStats?.LastSuccess,
                deliveryStats?.LastFailure,
                message);
        }
        catch (Exception)
        {
            return new QueueHealthResponse(
                "Warning",
                lineRetryEnabled,
                approvalEscalationEnabled,
                0,
                0,
                0,
                null,
                null,
                "ไม่สามารถตรวจสอบ queue หรือ worker ได้");
        }
    }

    private DiskHealthResponse CheckDisk()
    {
        try
        {
            var rootPath = configuration["Storage:RootPath"] ?? configuration["STORAGE_ROOT_PATH"] ?? AppContext.BaseDirectory;
            var root = Path.GetPathRoot(Path.GetFullPath(rootPath));
            if (string.IsNullOrWhiteSpace(root))
            {
                return new DiskHealthResponse("Unknown", null, "ไม่สามารถตรวจสอบ disk root ได้");
            }

            var drive = new DriveInfo(root);
            var usedPercent = drive.TotalSize <= 0
                ? null
                : (double?)Math.Round((1 - (double)drive.AvailableFreeSpace / drive.TotalSize) * 100, 2);
            var status = usedPercent is null
                ? "Unknown"
                : usedPercent >= 90
                    ? "Unhealthy"
                    : usedPercent >= 80
                        ? "Warning"
                        : "Healthy";

            return new DiskHealthResponse(status, usedPercent);
        }
        catch (Exception)
        {
            return new DiskHealthResponse("Unknown", null, "ไม่สามารถตรวจสอบ disk usage ได้");
        }
    }

    private BackupHealthResponse CheckBackup()
    {
        var backupRoot = configuration["Backup:RootPath"] ?? configuration["BACKUP_ROOT"] ?? "backups";
        try
        {
            if (!Directory.Exists(backupRoot))
            {
                return new BackupHealthResponse("Warning", null, "ยังไม่พบโฟลเดอร์ backup");
            }

            var lastBackup = Directory
                .EnumerateFiles(backupRoot, "*", SearchOption.AllDirectories)
                .Select(path => new FileInfo(path))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .Select(file => (DateTime?)file.LastWriteTimeUtc)
                .FirstOrDefault();

            return lastBackup is null
                ? new BackupHealthResponse("Warning", null, "ยังไม่พบไฟล์ backup")
                : new BackupHealthResponse("Healthy", lastBackup);
        }
        catch (Exception)
        {
            return new BackupHealthResponse("Warning", null, "ไม่สามารถตรวจสอบ backup ได้");
        }
    }
}
