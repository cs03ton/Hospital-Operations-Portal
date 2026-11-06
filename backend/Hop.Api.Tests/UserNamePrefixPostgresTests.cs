using Hop.Api.Data;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

namespace Hop.Api.Tests;

public sealed class NamePrefixPostgresFactAttribute : FactAttribute
{
    public NamePrefixPostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HOP_PREFIX_TEST_CONNECTION_STRING")))
            Skip = "Set HOP_PREFIX_TEST_CONNECTION_STRING to an isolated local PostgreSQL connection.";
    }
}

public sealed class UserNamePrefixPostgresTests : IAsyncLifetime
{
    private const string Migration = "20261106030602_AddUserNamePrefix";
    private string? adminConnection;
    private string? connection;
    private readonly string database = "hop_prefix_test_" + Guid.NewGuid().ToString("N");

    public async Task InitializeAsync()
    {
        adminConnection = Environment.GetEnvironmentVariable("HOP_PREFIX_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(adminConnection)) return;
        var builder = new NpgsqlConnectionStringBuilder(adminConnection);
        if (builder.Host is not ("127.0.0.1" or "localhost" or "::1"))
            throw new InvalidOperationException("Name prefix tests require local PostgreSQL.");
        await Execute(adminConnection, $"CREATE DATABASE {database}");
        builder.Database = database;
        connection = builder.ConnectionString;
        await Execute(connection, """
            CREATE TABLE public.users (id integer PRIMARY KEY, fullname text NOT NULL);
            INSERT INTO public.users VALUES (1, 'ชื่อเดิม นามสกุลเดิม');
            CREATE TABLE public."__EFMigrationsHistory" (
                "MigrationId" varchar(150) PRIMARY KEY, "ProductVersion" varchar(32) NOT NULL);
            """);
    }

    public async Task DisposeAsync()
    {
        if (connection is null) return;
        NpgsqlConnection.ClearAllPools();
        await Execute(adminConnection!, $"DROP DATABASE {database} WITH (FORCE)");
    }

    [NamePrefixPostgresFact]
    public async Task DBeaverScriptIsRepeatableAndPreservesExistingNames()
    {
        var sql = await File.ReadAllTextAsync(Path.Combine(Repository(), "docs", "sql", "user-name-prefix.sql"));
        await Execute(connection!, sql);
        await Execute(connection!, sql);
        await using var db = Context();
        Assert.Contains(Migration, await db.Database.GetAppliedMigrationsAsync());
        await using var conn = new NpgsqlConnection(connection);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT fullname, name_prefix FROM users WHERE id = 1", conn);
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            Assert.True(await reader.ReadAsync());
            Assert.Equal("ชื่อเดิม นามสกุลเดิม", reader.GetString(0));
            Assert.True(reader.IsDBNull(1));
        }
        await Execute(connection!, "UPDATE users SET name_prefix = 'นางสาว' WHERE id = 1");
        await Execute(connection!, sql);
        Assert.Equal("นางสาว", await new NpgsqlCommand("SELECT name_prefix FROM users WHERE id = 1", conn).ExecuteScalarAsync());
        Assert.Equal(1L, await new NpgsqlCommand($"SELECT count(*) FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = '{Migration}'", conn).ExecuteScalarAsync());
    }

    [NamePrefixPostgresFact]
    public async Task WrongExistingColumnIsRejectedWithoutRecordingMigration()
    {
        var sql = await File.ReadAllTextAsync(Path.Combine(Repository(), "docs", "sql", "user-name-prefix.sql"));
        foreach (var shape in new[] { "text", "varchar(50)", "varchar(100) NOT NULL DEFAULT ''", "varchar(100) DEFAULT 'นาย'" })
        {
            await Execute(connection!, $"ALTER TABLE users ADD COLUMN name_prefix {shape}");
            await Assert.ThrowsAsync<PostgresException>(() => Execute(connection!, sql));
            await using var db = Context();
            Assert.DoesNotContain(Migration, await db.Database.GetAppliedMigrationsAsync());
            await Execute(connection!, "ALTER TABLE users DROP COLUMN name_prefix");
        }
    }

    [NamePrefixPostgresFact]
    public async Task EfMigrationAndDBeaverScriptAgreeAndLeaveNoPendingMigration()
    {
        await using var db = Context();
        foreach (var previous in db.Database.GetMigrations().Where(x => x != Migration))
            await Execute(connection!, $"INSERT INTO \"__EFMigrationsHistory\" VALUES ('{previous}', '9.0.15')");
        var migrator = db.GetService<IMigrator>();
        await Execute(connection!, migrator.GenerateScript("20261022083439_AddAnnouncementLineGroups", Migration));
        await Execute(connection!, await File.ReadAllTextAsync(Path.Combine(Repository(), "docs", "sql", "user-name-prefix.sql")));
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.False(migrator.HasPendingModelChanges());
        await Execute(connection!, "UPDATE users SET name_prefix = 'นพ.'");
        await Execute(connection!, migrator.GenerateScript(Migration, "20261022083439_AddAnnouncementLineGroups"));
        Assert.DoesNotContain(Migration, await db.Database.GetAppliedMigrationsAsync());
    }

    private AppDbContext Context() => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options);

    [NamePrefixPostgresFact]
    public async Task CorrespondenceQueriesTranslateOnPostgresAndReturnPrefixedPeople()
    {
        await Execute(connection!, "DROP TABLE public.users; DROP TABLE public.\"__EFMigrationsHistory\";");
        await using var db = Context();
        await db.Database.EnsureCreatedAsync();
        var person = new User { Username = "head", FullName = "ทดสอบ ชื่อ", NamePrefix = "นางสาว", Position = "หัวหน้า", Department = new Department { Name = "ทดสอบ" },
            UserRoles = [new() { Role = new Role { Name = "DepartmentHead" } }] };
        db.Users.Add(person); await db.SaveChangesAsync();
        var service = new CorrespondenceMemoService(db, new ConfigurationBuilder().Build());
        var options = await service.OptionsAsync(person.Id, default);
        Assert.Equal("นางสาวทดสอบ ชื่อ", options.Proposer.Name);
        Assert.Equal("นางสาวทดสอบ ชื่อ", Assert.Single(options.Heads).Name);
        Assert.Equal("นางสาวทดสอบ ชื่อ", Assert.Single(options.Approvers!).Name);
        Assert.Equal("นางสาวทดสอบ ชื่อ", Assert.Single(await service.DepartmentPeopleAsync(person.Id, person.DepartmentId!.Value, default)).Name);
    }

    private static async Task Execute(string connectionString, string sql)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        await new NpgsqlCommand(sql, conn).ExecuteNonQueryAsync();
    }

    private static string Repository()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository not found.");
    }
}
