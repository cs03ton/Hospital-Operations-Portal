using System.Diagnostics;
using System.IO.Compression;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Hop.Api.Configuration;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed class DiagnosticsService(
    AppDbContext db,
    IHealthCenterService healthCenterService,
    IBackupCenterService backupCenterService,
    ILineMessagingService lineMessagingService,
    LineConfigurationResolver lineConfiguration,
    IDiagnosticsRedactionService redaction,
    IConfiguration configuration,
    IWebHostEnvironment environment,
    IHttpContextAccessor httpContextAccessor,
    ILogger<DiagnosticsService> logger) : IDiagnosticsService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private static readonly HashSet<string> SupportedTests = new(StringComparer.OrdinalIgnoreCase)
    {
        "database",
        "storage",
        "upload",
        "pdf",
        "line-text",
        "line-flex",
        "backup",
        "notification-worker"
    };

    public async Task<DiagnosticsSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var health = await healthCenterService.GetHealthAsync(cancellationToken);
        var services = new Dictionary<string, DiagnosticServiceStatusResponse>(StringComparer.OrdinalIgnoreCase)
        {
            ["api"] = FromComponent("api", "API", health.Api),
            ["database"] = FromComponent("database", "Database", health.Database),
            ["storage"] = new("storage", "Storage", health.Storage.Status, health.Storage.Message, null, new Dictionary<string, string?> { ["writable"] = health.Storage.Writable.ToString(), ["path"] = SanitizePath(health.Storage.Path) }),
            ["upload"] = await CheckFolderSummaryAsync("upload", "Upload", GetStorageChildPath("uploads"), cancellationToken),
            ["pdf"] = await CheckFolderSummaryAsync("pdf", "PDF", GetStorageChildPath("generated-pdf"), cancellationToken),
            ["line"] = new("line", "LINE", health.Line.Status, health.Line.Message, null, new Dictionary<string, string?> { ["enabled"] = health.Line.Enabled.ToString(), ["hasAccessToken"] = health.Line.HasAccessToken.ToString(), ["hasChannelSecret"] = health.Line.HasChannelSecret.ToString() }),
            ["notificationWorker"] = new("notificationWorker", "Notification Worker", health.Queue.Status, health.Queue.Message, null, new Dictionary<string, string?> { ["failedLineDeliveries"] = health.Queue.FailedLineDeliveries.ToString(), ["pendingRetries"] = health.Queue.PendingRetries.ToString() }),
            ["backup"] = new("backup", "Backup", health.Backup.Status, health.Backup.Message, null, new Dictionary<string, string?> { ["latestFile"] = health.Backup.LatestBackupFile, ["directory"] = SanitizePath(health.Backup.BackupDirectory) }),
            ["disk"] = new("disk", "Disk", health.Disk.Status, health.Disk.Message, null, new Dictionary<string, string?> { ["usedPercent"] = health.Disk.UsedPercent?.ToString("0.##") }),
            ["cpu"] = new("cpu", "CPU", health.Cpu.Status, health.Cpu.Message, null, new Dictionary<string, string?> { ["processors"] = health.Cpu.ProcessorCount.ToString(), ["loadAverage"] = health.Cpu.LoadAverage }),
            ["ram"] = new("ram", "RAM", health.Memory.Status, health.Memory.Message, null, new Dictionary<string, string?> { ["usedPercent"] = health.Memory.UsedPercent?.ToString("0.##") }),
            ["nginx"] = CheckKnownService("nginx", "Nginx"),
            ["postgresql"] = CheckKnownService("postgresql", "PostgreSQL"),
            ["hopApiService"] = CheckKnownService("hopApiService", "hop-api systemd service")
        };

        var recentErrors = await GetRecentErrorsAsync(cancellationToken);
        return new DiagnosticsSummaryResponse(
            DateTimeOffset.UtcNow,
            health.Environment,
            health.Version,
            health.GitCommit,
            services,
            recentErrors,
            GetLastDeploy(),
            await GetLastMigrationAsync(cancellationToken));
    }

    public async Task<DiagnosticTestResultResponse> RunTestAsync(string diagnosticType, Guid? userId, string referenceId, CancellationToken cancellationToken = default)
    {
        if (!SupportedTests.Contains(diagnosticType))
        {
            throw new ArgumentOutOfRangeException(nameof(diagnosticType), "Unsupported diagnostic type.");
        }

        var stopwatch = Stopwatch.StartNew();
        var run = new DiagnosticRun
        {
            Id = Guid.NewGuid(),
            DiagnosticType = diagnosticType,
            Status = DiagnosticStatuses.Running,
            StartedAt = DateTime.UtcNow,
            ReferenceId = referenceId,
            CreatedByUserId = userId
        };

        db.DiagnosticRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var result = diagnosticType.ToLowerInvariant() switch
            {
                "database" => await TestDatabaseAsync(cancellationToken),
                "storage" => TestWritablePath(GetStorageRootPath(), "Storage"),
                "upload" => TestWritablePath(GetStorageChildPath("uploads"), "Upload"),
                "pdf" => TestPdf(),
                "line-text" => await TestLineTextAsync(cancellationToken),
                "line-flex" => await TestLineFlexAsync(cancellationToken),
                "backup" => await TestBackupAsync(cancellationToken),
                "notification-worker" => await TestNotificationWorkerAsync(cancellationToken),
                _ => new DiagnosticTestResultResponse(run.Id, diagnosticType, DiagnosticStatuses.Failed, "ไม่รองรับ diagnostic type นี้", referenceId, stopwatch.ElapsedMilliseconds)
            };

            stopwatch.Stop();
            run.Status = result.Status;
            run.CompletedAt = DateTime.UtcNow;
            run.DurationMs = stopwatch.ElapsedMilliseconds;
            run.ResultSummary = redaction.Redact(result.Message);
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Diagnostic test completed Type={DiagnosticType} Status={Status} ReferenceId={ReferenceId} DurationMs={DurationMs}", diagnosticType, run.Status, referenceId, run.DurationMs);
            return result with { RunId = run.Id, DurationMs = stopwatch.ElapsedMilliseconds };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            run.Status = DiagnosticStatuses.Failed;
            run.CompletedAt = DateTime.UtcNow;
            run.DurationMs = stopwatch.ElapsedMilliseconds;
            run.ErrorMessage = redaction.Redact(ex.Message);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogError(ex, "Diagnostic test failed Type={DiagnosticType} ReferenceId={ReferenceId}", diagnosticType, referenceId);
            return new DiagnosticTestResultResponse(run.Id, diagnosticType, DiagnosticStatuses.Failed, "ทดสอบไม่สำเร็จ กรุณาตรวจ backend log ด้วย referenceId", referenceId, stopwatch.ElapsedMilliseconds);
        }
    }

    public async Task<DiagnosticsLogResponse> GetLogsAsync(DiagnosticsLogQuery query, CancellationToken cancellationToken = default)
    {
        var source = string.IsNullOrWhiteSpace(query.Source) ? "hop-api" : query.Source.Trim();
        var paths = GetLogPathMap();
        if (!paths.TryGetValue(source, out var path) || !IsAllowedLogPath(path))
        {
            return new DiagnosticsLogResponse(source, query.Page, query.PageSize, 0, 0, []);
        }

        if (!File.Exists(path))
        {
            return new DiagnosticsLogResponse(source, query.Page, query.PageSize, 0, 0, []);
        }

        var lines = await File.ReadAllLinesAsync(path, cancellationToken);
        var filtered = lines
            .Reverse()
            .Where(line => string.IsNullOrWhiteSpace(query.Search) || line.Contains(query.Search, StringComparison.OrdinalIgnoreCase))
            .Where(line => string.IsNullOrWhiteSpace(query.Severity) || line.Contains(query.Severity, StringComparison.OrdinalIgnoreCase))
            .Take(1000)
            .Select(line => new DiagnosticsLogLineResponse(null, DetectSeverity(line), redaction.Redact(line)))
            .ToList();

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 20, 500);
        var total = filtered.Count;
        var items = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new DiagnosticsLogResponse(source, page, pageSize, total, (int)Math.Ceiling(total / (double)pageSize), items);
    }

    public async Task<IReadOnlyList<RecentErrorResponse>> GetRecentErrorsAsync(CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddDays(-7);
        var errors = await db.AuditLogs
            .AsNoTracking()
            .Where(item => item.CreatedAt >= since && (item.Result == "Failed" || item.Action.Contains("Failed") || item.Action.Contains("PermissionDenied")))
            .OrderByDescending(item => item.CreatedAt)
            .Take(30)
            .Select(item => new RecentErrorResponse(
                item.CreatedAt,
                item.EntityName,
                item.Detail ?? item.Action,
                item.EntityId,
                item.UserId == null ? null : $"{item.UserId.ToString()!.Substring(0, 8)}...",
                null,
                item.Result))
            .ToListAsync(cancellationToken);

        return errors.Select(item => item with { Message = redaction.Redact(item.Message) }).ToList();
    }

    public async Task<IReadOnlyList<DiagnosticRunResponse>> GetHistoryAsync(CancellationToken cancellationToken = default)
    {
        return await db.DiagnosticRuns
            .AsNoTracking()
            .Include(item => item.CreatedByUser)
            .OrderByDescending(item => item.StartedAt)
            .Take(100)
            .Select(item => new DiagnosticRunResponse(
                item.Id,
                item.DiagnosticType,
                item.Status,
                item.StartedAt,
                item.CompletedAt,
                item.DurationMs,
                item.ResultSummary,
                item.ReferenceId,
                item.ErrorMessage,
                item.CreatedByUser == null ? null : item.CreatedByUser.Username))
            .ToListAsync(cancellationToken);
    }

    public async Task<SupportBundleResponse> CreateSupportBundleAsync(SupportBundleRequest request, Guid? userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length < 5)
        {
            throw new InvalidOperationException("กรุณาระบุเหตุผลในการสร้าง Support Bundle");
        }

        var bundleRoot = configuration["Diagnostics:BundlePath"] ?? Path.Combine(GetStorageRootPath(), "diagnostics", "support-bundles");
        Directory.CreateDirectory(bundleRoot);
        var createdAt = DateTime.UtcNow;
        var fileName = $"diagnostics-{createdAt:yyyyMMdd-HHmmss}.zip";
        var filePath = Path.Combine(bundleRoot, fileName);

        using (var archive = ZipFile.Open(filePath, ZipArchiveMode.Create))
        {
            await AddJsonAsync(archive, "summary.json", await GetSummaryAsync(cancellationToken), cancellationToken);
            await AddJsonAsync(archive, "environment-sanitized.json", BuildSanitizedEnvironment(), cancellationToken);
            if (request.IncludeHealth)
            {
                await AddJsonAsync(archive, "health.json", await healthCenterService.GetHealthAsync(cancellationToken), cancellationToken);
            }

            if (request.IncludeBackupSummary)
            {
                await AddJsonAsync(archive, "backup-summary.json", await backupCenterService.GetOverviewAsync(cancellationToken), cancellationToken);
            }

            if (request.IncludeLineSummary)
            {
                await AddJsonAsync(archive, "line-summary.json", BuildLineSummary(), cancellationToken);
            }

            if (request.IncludeMigrationInfo)
            {
                await AddJsonAsync(archive, "migrations.json", await BuildMigrationInfoAsync(cancellationToken), cancellationToken);
            }

            if (request.IncludeDeployInfo)
            {
                await AddJsonAsync(archive, "deployment.json", GetLastDeploy(), cancellationToken);
            }

            await AddJsonAsync(archive, "recent-errors.json", await GetRecentErrorsAsync(cancellationToken), cancellationToken);
            await AddTextAsync(archive, "README.txt", BuildBundleReadme(request), cancellationToken);
            await AddLogsAsync(archive, request, cancellationToken);
        }

        var checksum = await ComputeSha256Async(filePath, cancellationToken);
        var bundle = new SupportBundle
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            FilePath = filePath,
            FileSizeBytes = new FileInfo(filePath).Length,
            Checksum = checksum,
            ExpiresAt = createdAt.AddHours(24),
            Reason = request.Reason.Trim(),
            CreatedByUserId = userId,
            CreatedAt = createdAt
        };

        db.SupportBundles.Add(bundle);
        await db.SaveChangesAsync(cancellationToken);

        return ToBundleResponse(bundle);
    }

    public async Task<IReadOnlyList<SupportBundleHistoryResponse>> GetSupportBundlesAsync(CancellationToken cancellationToken = default)
    {
        return await db.SupportBundles
            .AsNoTracking()
            .Include(item => item.CreatedByUser)
            .OrderByDescending(item => item.CreatedAt)
            .Take(100)
            .Select(item => new SupportBundleHistoryResponse(
                item.Id,
                item.FileName,
                item.FileSizeBytes,
                item.Checksum,
                item.ExpiresAt,
                item.Reason,
                item.Status,
                item.CreatedByUser == null ? null : item.CreatedByUser.Username,
                item.CreatedAt,
                item.DownloadedAt,
                item.DeletedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<(string FilePath, string FileName, string ContentType)?> GetSupportBundleFileAsync(Guid id, Guid? userId, CancellationToken cancellationToken = default)
    {
        var bundle = await db.SupportBundles.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (bundle is null || bundle.DeletedAt is not null || bundle.ExpiresAt <= DateTime.UtcNow || !File.Exists(bundle.FilePath))
        {
            if (bundle is not null && bundle.Status != SupportBundleStatuses.Expired && bundle.ExpiresAt <= DateTime.UtcNow)
            {
                bundle.Status = SupportBundleStatuses.Expired;
                await db.SaveChangesAsync(cancellationToken);
            }
            return null;
        }

        bundle.DownloadedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return (bundle.FilePath, bundle.FileName, "application/zip");
    }

    private async Task<DiagnosticTestResultResponse> TestDatabaseAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        await db.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
        stopwatch.Stop();
        return new DiagnosticTestResultResponse(Guid.Empty, "database", DiagnosticStatuses.Healthy, $"ฐานข้อมูลตอบสนองใน {stopwatch.ElapsedMilliseconds} ms", CurrentReferenceId(), stopwatch.ElapsedMilliseconds);
    }

    private DiagnosticTestResultResponse TestWritablePath(string path, string label)
    {
        Directory.CreateDirectory(path);
        var stopwatch = Stopwatch.StartNew();
        var file = Path.Combine(path, $".diagnostic-{Guid.NewGuid():N}.tmp");
        File.WriteAllText(file, "HOP diagnostic temp file");
        _ = File.ReadAllText(file);
        File.Delete(file);
        stopwatch.Stop();
        return new DiagnosticTestResultResponse(Guid.Empty, label.ToLowerInvariant(), DiagnosticStatuses.Healthy, $"{label} เขียน/อ่าน/ลบ temp file ได้", CurrentReferenceId(), stopwatch.ElapsedMilliseconds);
    }

    private DiagnosticTestResultResponse TestPdf()
    {
        var path = GetStorageChildPath("generated-pdf");
        var result = TestWritablePath(path, "PDF");
        var templatePath = configuration["Pdf:TemplatePath"] ?? Path.Combine(environment.ContentRootPath, "storage", "templates", "leave");
        var fontFamily = configuration["Pdf:FontFamily"] ?? configuration["DocumentTemplate:FontFamily"] ?? "TH SarabunPSK";
        var message = $"{result.Message}; template={Directory.Exists(templatePath)}; font={redaction.Redact(fontFamily)}";
        return result with { DiagnosticType = "pdf", Message = message };
    }

    private async Task<DiagnosticTestResultResponse> TestLineTextAsync(CancellationToken cancellationToken)
    {
        var userId = lineConfiguration.TestUserId;
        var result = await lineMessagingService.SendTestMessageAsync(userId ?? string.Empty, "ทดสอบ Diagnostics Center จาก HOP", "Diagnostics.LineText", cancellationToken);
        return new DiagnosticTestResultResponse(Guid.Empty, "line-text", result.Success ? DiagnosticStatuses.Healthy : DiagnosticStatuses.Warning, redaction.Redact(result.Message), CurrentReferenceId(), result.ResponseTimeMs ?? 0);
    }

    private async Task<DiagnosticTestResultResponse> TestLineFlexAsync(CancellationToken cancellationToken)
    {
        var payload = """
        {
          "type": "flex",
          "altText": "HOP Diagnostics Flex Test",
          "contents": {
            "type": "bubble",
            "body": {
              "type": "box",
              "layout": "vertical",
              "contents": [
                { "type": "text", "text": "HOP Diagnostics", "weight": "bold", "size": "lg" },
                { "type": "text", "text": "ทดสอบ Flex Message", "size": "sm", "color": "#6B7280" }
              ]
            }
          }
        }
        """;
        var result = await lineMessagingService.SendRawPayloadToLineUserAsync(lineConfiguration.TestUserId ?? string.Empty, payload, "Diagnostics.LineFlex", null, cancellationToken);
        return new DiagnosticTestResultResponse(Guid.Empty, "line-flex", result.Success ? DiagnosticStatuses.Healthy : DiagnosticStatuses.Warning, redaction.Redact(result.Message), CurrentReferenceId(), result.ResponseTimeMs ?? 0);
    }

    private async Task<DiagnosticTestResultResponse> TestBackupAsync(CancellationToken cancellationToken)
    {
        var backup = await backupCenterService.GetOverviewAsync(cancellationToken);
        var status = backup.LastSuccessfulBackup is null ? DiagnosticStatuses.Warning : DiagnosticStatuses.Healthy;
        var message = backup.LastSuccessfulBackup is null ? "ยังไม่พบ backup สำเร็จล่าสุด" : $"พบ backup ล่าสุด {backup.LastSuccessfulBackup.FileName}";
        return new DiagnosticTestResultResponse(Guid.Empty, "backup", status, redaction.Redact(message), CurrentReferenceId(), 0);
    }

    private async Task<DiagnosticTestResultResponse> TestNotificationWorkerAsync(CancellationToken cancellationToken)
    {
        var failed = await db.LineDeliveryLogs.CountAsync(item => item.Status == "Failed", cancellationToken);
        var retry = await db.LineDeliveryLogs.CountAsync(item => item.NextRetryAt != null && item.Status != "Success", cancellationToken);
        var status = failed > 0 ? DiagnosticStatuses.Warning : DiagnosticStatuses.Healthy;
        return new DiagnosticTestResultResponse(Guid.Empty, "notification-worker", status, $"LINE failed {failed}, retry {retry}", CurrentReferenceId(), 0);
    }

    private async Task<DiagnosticInfoResponse> GetLastMigrationAsync(CancellationToken cancellationToken)
    {
        var migrations = await db.Database.GetAppliedMigrationsAsync(cancellationToken);
        var latest = migrations.LastOrDefault();
        return string.IsNullOrWhiteSpace(latest)
            ? new DiagnosticInfoResponse("Unknown", "ไม่พบ migration ที่ apply แล้ว")
            : new DiagnosticInfoResponse("Healthy", latest, Reference: latest);
    }

    private async Task<object> BuildMigrationInfoAsync(CancellationToken cancellationToken)
    {
        var applied = await db.Database.GetAppliedMigrationsAsync(cancellationToken);
        var pending = await db.Database.GetPendingMigrationsAsync(cancellationToken);
        return new { applied = applied.ToList(), pending = pending.ToList(), latest = applied.LastOrDefault() };
    }

    private DiagnosticServiceStatusResponse FromComponent(string key, string label, HealthComponentResponse component)
    {
        return new DiagnosticServiceStatusResponse(key, label, component.Status, component.Message, component.LatencyMs, new Dictionary<string, string?> { ["provider"] = component.Provider });
    }

    private async Task<DiagnosticServiceStatusResponse> CheckFolderSummaryAsync(string key, string label, string path, CancellationToken cancellationToken)
    {
        await Task.Yield();
        if (string.IsNullOrWhiteSpace(path))
        {
            return new(key, label, "Unknown", "ยังไม่ได้ตั้งค่า path");
        }

        if (!Directory.Exists(path))
        {
            return new(key, label, "Warning", "ยังไม่พบ directory", Details: new Dictionary<string, string?> { ["path"] = SanitizePath(path) });
        }

        return new(key, label, "Healthy", "พบ directory", Details: new Dictionary<string, string?> { ["path"] = SanitizePath(path) });
    }

    private DiagnosticServiceStatusResponse CheckKnownService(string key, string label)
    {
        if (!OperatingSystem.IsLinux())
        {
            return new(key, label, "Unknown", "ตรวจ systemd service ได้เฉพาะ Linux server");
        }

        return new(key, label, "Unknown", "Phase นี้ไม่ execute shell command จาก Diagnostics Center");
    }

    private DiagnosticInfoResponse GetLastDeploy()
    {
        var marker = configuration["Diagnostics:DeployMarkerPath"] ?? "/opt/hop/releases/current";
        if (File.Exists(marker))
        {
            var info = new FileInfo(marker);
            return new DiagnosticInfoResponse("Healthy", "พบ deploy marker", info.LastWriteTimeUtc, SanitizePath(marker));
        }

        return new DiagnosticInfoResponse("Unknown", "ยังไม่ได้ตั้งค่า deploy marker");
    }

    private object BuildSanitizedEnvironment()
    {
        return new
        {
            environment = environment.EnvironmentName,
            contentRoot = SanitizePath(environment.ContentRootPath),
            publicAppUrl = redaction.Redact(configuration["PUBLIC_APP_URL"] ?? configuration["PublicAppUrl"]),
            storageRootConfigured = !string.IsNullOrWhiteSpace(configuration["Storage:RootPath"]),
            lineEnabled = lineConfiguration.Enabled,
            lineHasAccessToken = lineConfiguration.HasAccessToken,
            lineHasChannelSecret = lineConfiguration.HasChannelSecret
        };
    }

    private object BuildLineSummary()
    {
        return new
        {
            enabled = lineConfiguration.Enabled,
            hasAccessToken = lineConfiguration.HasAccessToken,
            hasChannelSecret = lineConfiguration.HasChannelSecret,
            testUserConfigured = !string.IsNullOrWhiteSpace(lineConfiguration.TestUserId),
            testUserId = redaction.Redact(lineConfiguration.TestUserId)
        };
    }

    private async Task AddLogsAsync(ZipArchive archive, SupportBundleRequest request, CancellationToken cancellationToken)
    {
        var paths = GetLogPathMap();
        var selected = new List<string>();
        if (request.IncludeAppLogs) selected.Add("hop-api");
        if (request.IncludeNginxLogs) selected.Add("nginx-error");
        if (request.IncludePostgresLogs) selected.Add("postgresql");
        selected.Add("backup");
        selected.Add("deploy");

        foreach (var source in selected.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!paths.TryGetValue(source, out var path) || !File.Exists(path) || !IsAllowedLogPath(path))
            {
                continue;
            }

            var lines = await File.ReadAllLinesAsync(path, cancellationToken);
            var sanitized = string.Join(Environment.NewLine, lines.Reverse().Take(500).Reverse().Select(line => redaction.Redact(line)));
            await AddTextAsync(archive, $"logs/{source}.log", sanitized, cancellationToken);
        }
    }

    private async Task AddJsonAsync(ZipArchive archive, string entryName, object value, CancellationToken cancellationToken)
    {
        await AddTextAsync(archive, entryName, redaction.Redact(JsonSerializer.Serialize(value, JsonOptions)), cancellationToken);
    }

    private static async Task AddTextAsync(ZipArchive archive, string entryName, string content, CancellationToken cancellationToken)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);
        await using var stream = entry.Open();
        await using var writer = new StreamWriter(stream, Encoding.UTF8);
        await writer.WriteAsync(content.AsMemory(), cancellationToken);
    }

    private string BuildBundleReadme(SupportBundleRequest request)
    {
        return $"""
        HOP Diagnostics Support Bundle
        CreatedAt: {DateTime.UtcNow:O}
        Reason: {redaction.Redact(request.Reason)}

        This bundle is sanitized by HOP Diagnostics Center.
        It must not contain .env files, JWT keys, LINE tokens/secrets, DB dumps, uploaded documents, or user password hashes.
        """;
    }

    private IReadOnlyDictionary<string, string> GetLogPathMap()
    {
        var root = configuration["Diagnostics:LogRoot"] ?? "/opt/hop/logs";
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["hop-api"] = configuration["Diagnostics:Logs:HopApi"] ?? Path.Combine(root, "hop-api.log"),
            ["nginx-error"] = configuration["Diagnostics:Logs:NginxError"] ?? "/var/log/nginx/error.log",
            ["postgresql"] = configuration["Diagnostics:Logs:Postgresql"] ?? "/var/log/postgresql/postgresql.log",
            ["backup"] = configuration["Diagnostics:Logs:Backup"] ?? "/var/log/hop/backup.log",
            ["deploy"] = configuration["Diagnostics:Logs:Deploy"] ?? Path.Combine(root, "deploy.log")
        };
    }

    private bool IsAllowedLogPath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var allowedRoots = (configuration.GetSection("Diagnostics:AllowedLogRoots").Get<string[]>() ?? ["/opt/hop/logs", "/var/log/hop", "/var/log/nginx", "/var/log/postgresql"])
            .Select(Path.GetFullPath);
        return allowedRoots.Any(root => fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase));
    }

    private string GetStorageRootPath()
    {
        return configuration["Storage:RootPath"] ?? configuration["STORAGE_ROOT_PATH"] ?? Path.Combine(environment.ContentRootPath, "storage");
    }

    private string GetStorageChildPath(string child)
    {
        return Path.Combine(GetStorageRootPath(), child);
    }

    private string SanitizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;
        var normalized = path.Replace('\\', '/');
        if (normalized.StartsWith("/opt/hop", StringComparison.OrdinalIgnoreCase)) return normalized;
        if (normalized.StartsWith("/var/log", StringComparison.OrdinalIgnoreCase)) return normalized;
        return Path.GetFileName(normalized);
    }

    private static string DetectSeverity(string line)
    {
        if (line.Contains("error", StringComparison.OrdinalIgnoreCase) || line.Contains("fail", StringComparison.OrdinalIgnoreCase)) return "Error";
        if (line.Contains("warn", StringComparison.OrdinalIgnoreCase)) return "Warning";
        return "Info";
    }

    private static async Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private SupportBundleResponse ToBundleResponse(SupportBundle bundle)
    {
        return new SupportBundleResponse(bundle.Id, bundle.FileName, bundle.FileSizeBytes, bundle.Checksum, bundle.ExpiresAt, bundle.Status, bundle.CreatedAt, $"/api/admin/diagnostics/support-bundle/{bundle.Id}/download");
    }

    private string CurrentReferenceId()
    {
        return httpContextAccessor.HttpContext?.TraceIdentifier ?? Activity.Current?.Id ?? Guid.NewGuid().ToString("N");
    }
}
