using Hop.Api.Interfaces;
using Hop.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Hop.Api.Tests;

public class DocumentationServiceTests
{
    [Fact]
    public async Task Staff_SeesOnlyAllowedDocuments()
    {
        using var temp = new TempDocumentationRoot();
        var service = temp.CreateService();
        var access = new DocumentationAccessContext(
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Staff" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Documentation.View" });

        var docs = await service.GetDocumentsAsync(access);

        Assert.Contains(docs, item => item.Slug == "staff-guide");
        Assert.Contains(docs, item => item.Slug == "faq");
        Assert.DoesNotContain(docs, item => item.Slug == "admin-guide");
        Assert.DoesNotContain(docs, item => item.Slug == "release-notes");
    }

    [Fact]
    public async Task AdminPermission_SeesAllDocuments()
    {
        using var temp = new TempDocumentationRoot();
        var service = temp.CreateService();
        var access = new DocumentationAccessContext(
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Documentation.AdminView" });

        var docs = await service.GetDocumentsAsync(access);

        Assert.Contains(docs, item => item.Slug == "staff-guide");
        Assert.Contains(docs, item => item.Slug == "head-guide");
        Assert.Contains(docs, item => item.Slug == "director-guide");
        Assert.Contains(docs, item => item.Slug == "admin-guide");
        Assert.Contains(docs, item => item.Slug == "release-notes");
    }

    [Fact]
    public async Task InvalidSlug_ReturnsNull()
    {
        using var temp = new TempDocumentationRoot();
        var service = temp.CreateService();
        var access = new DocumentationAccessContext(
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "SuperAdmin" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        var doc = await service.GetDocumentAsync("../../appsettings", access);

        Assert.Null(doc);
    }

    [Fact]
    public async Task Detail_RedactsSensitiveAssignments()
    {
        using var temp = new TempDocumentationRoot();
        File.AppendAllText(Path.Combine(temp.Root, "staff.md"), "\nAccessToken=real-secret-token");
        var service = temp.CreateService();
        var access = new DocumentationAccessContext(
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Staff" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Documentation.View" });

        var doc = await service.GetDocumentAsync("staff-guide", access);

        Assert.NotNull(doc);
        Assert.Contains("AccessToken= [REDACTED]", doc!.ContentMarkdown);
        Assert.DoesNotContain("real-secret-token", doc.ContentMarkdown);
    }

    [Fact]
    public async Task UpdateDocument_RejectsSensitiveAssignments()
    {
        using var temp = new TempDocumentationRoot();
        var service = temp.CreateService();
        var access = new DocumentationAccessContext(
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "SuperAdmin" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Documentation.Manage" });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateDocumentAsync("staff-guide", "# Test\n\nChannelSecret: real-secret", access));
    }

    [Fact]
    public async Task GeneratePdf_ReturnsPdfBytesForAllowedDocument()
    {
        using var temp = new TempDocumentationRoot();
        var service = temp.CreateService();
        var access = new DocumentationAccessContext(
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Staff" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Documentation.View" });

        var bytes = await service.GeneratePdfAsync("staff-guide", access);

        Assert.NotNull(bytes);
        Assert.True(bytes!.Length > 100);
        Assert.Equal((byte)'%', bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
    }

    private sealed class TempDocumentationRoot : IDisposable
    {
        public TempDocumentationRoot()
        {
            Root = Path.Combine(Path.GetTempPath(), $"hop-docs-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Root);
            foreach (var file in new[] { "staff.md", "head.md", "director.md", "admin.md", "faq.md", "release-notes.md" })
            {
                File.WriteAllText(Path.Combine(Root, file), $"# {file}\n\nเนื้อหาทดสอบ");
            }
        }

        public string Root { get; }

        public DocumentationService CreateService()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Documentation:RootPath"] = Root
                })
                .Build();

            return new DocumentationService(config, new TestEnvironment(Root), NullLogger<DocumentationService>.Instance);
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }

    private sealed class TestEnvironment(string contentRootPath) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Hop.Api.Tests";
        public string WebRootPath { get; set; } = contentRootPath;
        public IFileProvider WebRootFileProvider { get; set; } = new PhysicalFileProvider(contentRootPath);
        public string ContentRootPath { get; set; } = contentRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(contentRootPath);
    }
}
