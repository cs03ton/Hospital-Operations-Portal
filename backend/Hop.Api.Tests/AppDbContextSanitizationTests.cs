using Hop.Api.Data;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hop.Api.Tests;

public class AppDbContextSanitizationTests
{
    [Fact]
    public async Task SaveChangesAsync_RemovesNullCharactersFromAddedStringProperties()
    {
        await using var db = CreateDbContext();
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Information\0 Technology",
            Description = "Support\0 department"
        };

        db.Departments.Add(department);
        await db.SaveChangesAsync();

        Assert.Equal("Information Technology", department.Name);
        Assert.Equal("Support department", department.Description);
    }

    [Fact]
    public async Task SaveChangesAsync_RemovesNullCharactersFromModifiedStringProperties()
    {
        await using var db = CreateDbContext();
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Information Technology",
            Description = "Support department"
        };
        db.Departments.Add(department);
        await db.SaveChangesAsync();

        department.Description = "Updated\0 description";
        await db.SaveChangesAsync();

        Assert.Equal("Updated description", department.Description);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
