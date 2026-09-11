using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace Hop.Api.Tests;

public sealed class BackupPostgresFactAttribute : FactAttribute
{
    public BackupPostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HOP_BACKUP_TEST_CONNECTION_STRING")))
            Skip = "Set HOP_BACKUP_TEST_CONNECTION_STRING to a local PostgreSQL admin connection.";
    }
}

public sealed class BackupSyncPostgresTests : IAsyncLifetime
{
    private readonly string databaseName = "hop_backup_test_" + Guid.NewGuid().ToString("N");
    private readonly string root = Path.Combine(Path.GetTempPath(), "hop-backup-test-" + Guid.NewGuid().ToString("N"));
    private string? connection;
    private string? adminConnection;

    public async Task InitializeAsync()
    {
        adminConnection = Environment.GetEnvironmentVariable("HOP_BACKUP_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(adminConnection)) return;
        var builder = new NpgsqlConnectionStringBuilder(adminConnection);
        if (builder.Host is not ("localhost" or "127.0.0.1" or "::1"))
            throw new InvalidOperationException("Backup integration tests require local PostgreSQL.");
        await using var admin = new NpgsqlConnection(adminConnection);
        await admin.OpenAsync();
        await new NpgsqlCommand($"CREATE DATABASE {databaseName}", admin).ExecuteNonQueryAsync();
        builder.Database = databaseName;
        connection = builder.ConnectionString;
        await using var db = Database();
        await db.Database.EnsureCreatedAsync();
        Directory.CreateDirectory(Path.Combine(root, "postgres"));
        Directory.CreateDirectory(Path.Combine(root, "storage"));
        // Quotes exercise SQL parameterization; reverse creation order exercises sorting.
        await File.WriteAllTextAsync(Path.Combine(root, "postgres", "hop_db_z'quote.backup"), "z");
        await File.WriteAllTextAsync(Path.Combine(root, "postgres", "hop_db_a.backup"), "a");
    }

    public async Task DisposeAsync()
    {
        if (connection is not null)
        {
            NpgsqlConnection.ClearAllPools();
            await using var admin = new NpgsqlConnection(adminConnection);
            await admin.OpenAsync();
            await new NpgsqlCommand($"DROP DATABASE {databaseName} WITH (FORCE)", admin).ExecuteNonQueryAsync();
        }
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }

    private AppDbContext Database() => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options);

    private BackupCenterService Service(AppDbContext db, ILogger<BackupCenterService>? logger = null) =>
        new(db, new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Backup:RootPath"] = Path.Combine(root, "postgres", "..")
        }).Build(), null!, null!, logger ?? NullLogger<BackupCenterService>.Instance);

    [BackupPostgresFact]
    public async Task ConcurrentListCalls_AreIdempotent_AndPreserveExistingMetadata()
    {
        await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
        {
            await using var db = Database();
            var response = await new AdminBackupsController(Service(db)).GetBackups(new BackupQuery(), default);
            Assert.NotNull(response.Value);
        }));
        await using var check = Database();
        Assert.Equal(2, await check.BackupRuns.CountAsync());
        var existing = await check.BackupRuns.OrderBy(x => x.FilePath).FirstAsync();
        existing.Status = "Verified";
        existing.Checksum = "keep-checksum";
        await check.SaveChangesAsync();
        var before = await check.BackupRuns.AsNoTracking().OrderBy(x => x.FilePath).ToListAsync();
        await Service(check).SyncFileSystemBackupsAsync(default);
        var after = await check.BackupRuns.AsNoTracking().OrderBy(x => x.FilePath).ToListAsync();
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(before), System.Text.Json.JsonSerializer.Serialize(after));
    }

    // A sequence survives rollback, letting PostgreSQL fail exactly the first N attempts.
    private async Task InstallFailure(string code, int failures)
    {
        Assert.Contains(code, new[] { "40P01", "P0001" });
        await using var db = Database();
        // Test-only DDL: code is allowlisted above and failures is a typed integer.
#pragma warning disable EF1002
        await db.Database.ExecuteSqlRawAsync($"""
            CREATE SEQUENCE sync_attempts;
            CREATE FUNCTION fail_sync() RETURNS trigger LANGUAGE plpgsql AS $f$
            BEGIN
              IF NEW.file_name LIKE 'hop_db_z%' THEN
                IF nextval('sync_attempts') <= {failures} THEN
                  RAISE EXCEPTION 'synthetic sync failure' USING ERRCODE = '{code}';
                END IF;
              END IF;
              RETURN NEW;
            END $f$;
            CREATE TRIGGER fail_sync BEFORE INSERT ON backup_runs
              FOR EACH ROW EXECUTE FUNCTION fail_sync();
            """);
#pragma warning restore EF1002
    }

    [BackupPostgresFact]
    public async Task Deadlock_RetriesInFreshTransactions()
    {
        await InstallFailure("40P01", 2);
        await using var db = Database();
        await Service(db).SyncFileSystemBackupsAsync(default);
        Assert.Equal(2, await db.BackupRuns.CountAsync());
        Assert.Equal(3L, await db.Database.SqlQueryRaw<long>("SELECT last_value AS \"Value\" FROM sync_attempts").SingleAsync());
    }

    [BackupPostgresFact]
    public async Task Deadlock_StopsAfterThreeAttempts_AndRollsBack()
    {
        await InstallFailure("40P01", 100);
        await using var db = Database();
        var ex = await Assert.ThrowsAsync<PostgresException>(() => Service(db).SyncFileSystemBackupsAsync(default));
        Assert.Equal("40P01", ex.SqlState);
        Assert.Equal(0, await db.BackupRuns.CountAsync());
        Assert.Equal(3L, await db.Database.SqlQueryRaw<long>("SELECT last_value AS \"Value\" FROM sync_attempts").SingleAsync());
    }

    [BackupPostgresFact]
    public async Task OtherErrors_DoNotRetry_AndRollBackEarlierInserts()
    {
        await InstallFailure("P0001", 100);
        await using var db = Database();
        await Assert.ThrowsAsync<PostgresException>(() => Service(db).SyncFileSystemBackupsAsync(default));
        Assert.Equal(0, await db.BackupRuns.CountAsync());
        Assert.Equal(1L, await db.Database.SqlQueryRaw<long>("SELECT last_value AS \"Value\" FROM sync_attempts").SingleAsync());
    }

    [BackupPostgresFact]
    public async Task CancellationDuringRetryDelay_StopsAndRollsBack()
    {
        await InstallFailure("40P01", 100);
        using var cancellation = new CancellationTokenSource();
        await using var db = Database();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Service(db, new CancelOnWarning(cancellation)).SyncFileSystemBackupsAsync(cancellation.Token));
        Assert.Equal(0, await db.BackupRuns.CountAsync());
        Assert.Equal(1L, await db.Database.SqlQueryRaw<long>("SELECT last_value AS \"Value\" FROM sync_attempts").SingleAsync());
    }

    [BackupPostgresFact]
    public async Task PreCancelledRequest_DoesNotInsert()
    {
        await using var db = Database();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Service(db).SyncFileSystemBackupsAsync(new CancellationToken(true)));
        Assert.Equal(0, await db.BackupRuns.CountAsync());
    }

    private sealed class CancelOnWarning(CancellationTokenSource source) : ILogger<BackupCenterService>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (level == LogLevel.Warning) source.Cancel();
        }
    }
}
