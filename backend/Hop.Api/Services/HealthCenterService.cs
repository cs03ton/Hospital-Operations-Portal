using System.Diagnostics;
using Hop.Api.Configuration;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed class HealthCenterService(
    AppDbContext db,
    IConfiguration configuration,
    IWebHostEnvironment environment,
    LineConfigurationResolver lineConfiguration) : IHealthCenterService
{
    private static readonly DateTime StartedAtUtc = DateTime.UtcNow;

    public async Task<AdminHealthResponse> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        var api = new HealthComponentResponse(
            "Healthy",
            UptimeSeconds: (long)Math.Max(0, (DateTime.UtcNow - StartedAtUtc).TotalSeconds));
        var database = await CheckDatabaseAsync(cancellationToken);
        var storage = CheckStorage();
        var line = await CheckLineAsync(cancellationToken);
        var queue = await CheckQueueAsync(cancellationToken);
        var disk = CheckDisk();
        var memory = CheckMemory();
        var cpu = CheckCpu();
        var backup = CheckBackup();
        var version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0";
        var overallStatus = CalculateOverallStatus([
            api.Status,
            database.Status,
            storage.Status,
            NormalizeStatus(line.Status),
            queue.Status,
            disk.Status,
            memory.Status,
            cpu.Status,
            backup.Status
        ]);

        return new AdminHealthResponse(
            overallStatus,
            DateTimeOffset.Now,
            api,
            database,
            storage,
            line,
            queue,
            disk,
            memory,
            cpu,
            backup,
            version,
            environment.EnvironmentName,
            DateTime.UtcNow,
            ResolveGitCommit(),
            TimeZoneInfo.Local.Id);
    }

    public async Task<HealthComponentResponse> CheckDatabaseAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            if (db.Database.IsRelational())
            {
                await db.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
            }
            else
            {
                await db.Database.CanConnectAsync(cancellationToken);
            }

            stopwatch.Stop();
            var status = stopwatch.ElapsedMilliseconds > 100 ? "Warning" : "Healthy";
            var message = stopwatch.ElapsedMilliseconds > 100 ? "Database latency is higher than 100ms." : null;
            return new HealthComponentResponse(status, message, stopwatch.ElapsedMilliseconds, Provider: db.Database.ProviderName ?? "Unknown");
        }
        catch (Exception)
        {
            stopwatch.Stop();
            return new HealthComponentResponse("Unhealthy", "ไม่สามารถตรวจสอบฐานข้อมูลได้", stopwatch.ElapsedMilliseconds, Provider: ResolveDatabaseProviderName());
        }
    }

    public StorageHealthResponse CheckStorage()
    {
        var rootPath = ResolveStoragePath();
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            return new StorageHealthResponse("Unhealthy", false, "ยังไม่ได้ตั้งค่า Storage__RootPath", null);
        }

        try
        {
            Directory.CreateDirectory(rootPath);
            var testFile = Path.Combine(rootPath, $".health-{Guid.NewGuid():N}.tmp");
            File.WriteAllText(testFile, DateTime.UtcNow.ToString("O"));
            File.Delete(testFile);
            return new StorageHealthResponse("Healthy", true, "เขียนไฟล์ได้", rootPath);
        }
        catch (Exception)
        {
            return new StorageHealthResponse("Unhealthy", false, "ไม่สามารถเขียนไฟล์ใน storage ได้", rootPath);
        }
    }

    public async Task<LineHealthResponse> CheckLineAsync(CancellationToken cancellationToken = default)
    {
        DateTime? lastSuccess = null;
        DateTime? lastFailure = null;
        string? lastError = null;

        try
        {
            lastSuccess = await db.LineDeliveryLogs
                .AsNoTracking()
                .Where(item => item.Status == "Sent" || item.Status == "Success")
                .OrderByDescending(item => item.SentAt ?? item.UpdatedAt ?? item.CreatedAt)
                .Select(item => item.SentAt ?? item.UpdatedAt ?? item.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
            var lastFailedLog = await db.LineDeliveryLogs
                .AsNoTracking()
                .Where(item => item.Status == "Failed")
                .OrderByDescending(item => item.UpdatedAt ?? item.CreatedAt)
                .Select(item => new { At = item.UpdatedAt ?? item.CreatedAt, item.ResponseDetail })
                .FirstOrDefaultAsync(cancellationToken);
            lastFailure = lastFailedLog?.At;
            lastError = SanitizeMessage(lastFailedLog?.ResponseDetail);
        }
        catch (Exception)
        {
            return new LineHealthResponse(
                "Warning",
                lineConfiguration.Enabled,
                null,
                null,
                "ไม่สามารถอ่านประวัติการส่ง LINE ได้",
                lineConfiguration.HasAccessToken,
                lineConfiguration.HasChannelSecret);
        }

        var status = !lineConfiguration.Enabled
            ? "Warning"
            : lineConfiguration.HasAccessToken && lineConfiguration.HasChannelSecret
                ? "Healthy"
                : "Unhealthy";

        if (status == "Healthy" && lastFailure is not null && (lastSuccess is null || lastFailure > lastSuccess))
        {
            status = "Warning";
        }

        var message = status switch
        {
            "Warning" when !lineConfiguration.Enabled => "LINE Messaging API ปิดใช้งาน",
            "Warning" => "พบการส่ง LINE ล้มเหลวล่าสุด",
            "Unhealthy" => "LINE เปิดใช้งานแต่ตั้งค่า token หรือ secret ยังไม่ครบ",
            _ => null
        };

        return new LineHealthResponse(
            status,
            lineConfiguration.Enabled,
            lastSuccess == default ? null : lastSuccess,
            lastFailure == default ? null : lastFailure,
            message,
            lineConfiguration.HasAccessToken,
            lineConfiguration.HasChannelSecret,
            lastError);
    }

    public DiskHealthResponse CheckDisk()
    {
        try
        {
            var rootPath = ResolveStoragePath() ?? AppContext.BaseDirectory;
            var root = Path.GetPathRoot(Path.GetFullPath(rootPath));
            if (string.IsNullOrWhiteSpace(root))
            {
                return new DiskHealthResponse("Unknown", null, "ไม่สามารถตรวจสอบ disk root ได้");
            }

            var drive = new DriveInfo(root);
            if (drive.TotalSize <= 0)
            {
                return new DiskHealthResponse("Unknown", null, "ไม่สามารถอ่านขนาด disk ได้");
            }

            var totalGb = BytesToGb(drive.TotalSize);
            var freeGb = BytesToGb(drive.AvailableFreeSpace);
            var usedGb = Math.Max(0, totalGb - freeGb);
            var usedPercent = Math.Round((1 - (double)drive.AvailableFreeSpace / drive.TotalSize) * 100, 2);
            var status = usedPercent > 90
                ? "Unhealthy"
                : usedPercent >= 80
                    ? "Warning"
                    : "Healthy";

            return new DiskHealthResponse(status, usedPercent, null, totalGb, usedGb, freeGb);
        }
        catch (Exception)
        {
            return new DiskHealthResponse("Unknown", null, "ไม่สามารถตรวจสอบ disk usage ได้");
        }
    }

    public MemoryHealthResponse CheckMemory()
    {
        try
        {
            var totalMb = GetTotalMemoryMb();
            var processUsedMb = Math.Round(Environment.WorkingSet / 1024d / 1024d, 2);
            if (totalMb is null || totalMb <= 0)
            {
                return new MemoryHealthResponse("Unknown", null, processUsedMb, null, null, "ไม่สามารถอ่านหน่วยความจำรวมของระบบได้");
            }

            var availableMb = GetAvailableMemoryMb();
            var usedMb = availableMb is null
                ? processUsedMb
                : Math.Max(0, totalMb.Value - availableMb.Value);
            var usedPercent = Math.Round(usedMb / totalMb.Value * 100, 2);
            double? availablePercent = availableMb is null ? null : Math.Round(availableMb.Value / totalMb.Value * 100, 2);
            var status = availablePercent is null
                ? "Unknown"
                : availablePercent < 10
                    ? "Unhealthy"
                    : availablePercent < 20
                        ? "Warning"
                        : "Healthy";

            return new MemoryHealthResponse(status, totalMb, Math.Round(usedMb, 2), availableMb, usedPercent);
        }
        catch (Exception)
        {
            return new MemoryHealthResponse("Unknown", null, null, null, null, "ไม่สามารถตรวจสอบ memory ได้");
        }
    }

    public CpuHealthResponse CheckCpu()
    {
        var loadAverage = ReadLoadAverage();
        return new CpuHealthResponse(
            "Healthy",
            Environment.ProcessorCount,
            loadAverage,
            loadAverage is null ? "ไม่พบ load average บนระบบปฏิบัติการนี้" : null);
    }

    public BackupHealthResponse CheckBackup()
    {
        var backupRoot = configuration["Backup:RootPath"] ?? configuration["BACKUP_ROOT"] ?? "backups";
        try
        {
            if (!Directory.Exists(backupRoot))
            {
                return new BackupHealthResponse(GetMissingBackupStatus(), null, "ยังไม่พบโฟลเดอร์ backup", BackupDirectory: backupRoot);
            }

            var files = Directory
                .EnumerateFiles(backupRoot, "*", SearchOption.AllDirectories)
                .Select(path => new FileInfo(path))
                .Where(file => !file.Name.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .ToList();

            var latest = files.FirstOrDefault();
            if (latest is null)
            {
                return new BackupHealthResponse(GetMissingBackupStatus(), null, "ยังไม่พบไฟล์ backup", BackupDirectory: backupRoot);
            }

            var lastBackup = latest.LastWriteTimeUtc;
            var age = DateTime.UtcNow - lastBackup;
            var status = age.TotalHours <= 24
                ? "Healthy"
                : age.TotalHours <= 72
                    ? "Warning"
                    : "Unhealthy";

            var restoreTest = files
                .Where(file => file.Name.Contains("restore", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .FirstOrDefault()?.LastWriteTimeUtc;

            return new BackupHealthResponse(
                status,
                lastBackup,
                status == "Healthy" ? null : "backup ล่าสุดเกินช่วงเวลาที่แนะนำ",
                restoreTest,
                backupRoot,
                latest.Length,
                latest.Name);
        }
        catch (Exception)
        {
            return new BackupHealthResponse("Warning", null, "ไม่สามารถตรวจสอบ backup ได้", BackupDirectory: backupRoot);
        }
    }

    private async Task<QueueHealthResponse> CheckQueueAsync(CancellationToken cancellationToken)
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
            var status = failed > 0 || pending > 100 ? "Warning" : "Healthy";
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

    private string? ResolveStoragePath()
    {
        return configuration["Storage:RootPath"] ??
            configuration["STORAGE_ROOT_PATH"] ??
            configuration["STORAGE_PATH"];
    }

    private string ResolveDatabaseProviderName()
    {
        try
        {
            return db.Database.ProviderName ?? "Unknown";
        }
        catch (Exception)
        {
            return "Unknown";
        }
    }

    private string? ResolveGitCommit()
    {
        return configuration["Git:Commit"] ??
            configuration["GIT_COMMIT"] ??
            configuration["APP_COMMIT"];
    }

    private string GetMissingBackupStatus()
    {
        return environment.IsProduction() ? "Unhealthy" : "Warning";
    }

    private static string CalculateOverallStatus(IEnumerable<string> statuses)
    {
        var normalized = statuses.Select(NormalizeStatus).ToList();
        if (normalized.Any(status => status == "Unhealthy")) return "Unhealthy";
        if (normalized.Any(status => status == "Warning" || status == "Unknown")) return "Warning";
        return "Healthy";
    }

    private static string NormalizeStatus(string? status)
    {
        return string.Equals(status, "Disabled", StringComparison.OrdinalIgnoreCase)
            ? "Warning"
            : string.IsNullOrWhiteSpace(status) ? "Unknown" : status;
    }

    private static string? SanitizeMessage(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var sanitized = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return sanitized.Length > 300 ? sanitized[..300] : sanitized;
    }

    private static double BytesToGb(long bytes)
    {
        return Math.Round(bytes / 1024d / 1024d / 1024d, 2);
    }

    private static double? GetTotalMemoryMb()
    {
        if (File.Exists("/proc/meminfo"))
        {
            var totalLine = File.ReadLines("/proc/meminfo").FirstOrDefault(line => line.StartsWith("MemTotal:", StringComparison.OrdinalIgnoreCase));
            if (TryReadMeminfoKb(totalLine, out var totalKb))
            {
                return Math.Round(totalKb / 1024d, 2);
            }
        }

        var info = GC.GetGCMemoryInfo();
        return info.TotalAvailableMemoryBytes > 0
            ? Math.Round(info.TotalAvailableMemoryBytes / 1024d / 1024d, 2)
            : null;
    }

    private static double? GetAvailableMemoryMb()
    {
        if (!File.Exists("/proc/meminfo"))
        {
            return null;
        }

        var availableLine = File.ReadLines("/proc/meminfo").FirstOrDefault(line => line.StartsWith("MemAvailable:", StringComparison.OrdinalIgnoreCase));
        return TryReadMeminfoKb(availableLine, out var availableKb)
            ? Math.Round(availableKb / 1024d, 2)
            : null;
    }

    private static bool TryReadMeminfoKb(string? line, out long value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 && long.TryParse(parts[1], out value);
    }

    private static string? ReadLoadAverage()
    {
        try
        {
            if (!File.Exists("/proc/loadavg"))
            {
                return null;
            }

            var parts = File.ReadAllText("/proc/loadavg")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(3);
            return string.Join(", ", parts);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
