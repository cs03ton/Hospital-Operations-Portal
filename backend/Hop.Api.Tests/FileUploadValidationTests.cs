using Hop.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Hop.Api.Tests;

public class FileUploadValidationTests
{
    [Fact]
    public async Task SaveAsync_AllowsPdfWithinLimit()
    {
        var root = Path.Combine(Path.GetTempPath(), $"hop-upload-{Guid.NewGuid():N}");
        var service = new LeaveAttachmentStorageService(CreateConfiguration(root, 1), new FileTypeValidationService());
        var file = CreateFormFile("sample.pdf", "application/pdf", "%PDF-1.7\nvalid test content");

        var attachment = await service.SaveAsync(Guid.NewGuid(), Guid.NewGuid(), file);

        Assert.EndsWith(".pdf", attachment.FilePath);
        Assert.True(File.Exists(Path.Combine(root, attachment.FilePath.Replace('/', Path.DirectorySeparatorChar))));
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public async Task SaveAsync_RejectsDisallowedExtension()
    {
        var root = Path.Combine(Path.GetTempPath(), $"hop-upload-{Guid.NewGuid():N}");
        var service = new LeaveAttachmentStorageService(CreateConfiguration(root, 1), new FileTypeValidationService());
        var file = CreateFormFile("malware.exe", "application/octet-stream", "MZ");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveAsync(Guid.NewGuid(), Guid.NewGuid(), file));

        Assert.Contains("not allowed", ex.Message);
    }

    [Fact]
    public async Task SaveAsync_RejectsFileOverConfiguredLimit()
    {
        var root = Path.Combine(Path.GetTempPath(), $"hop-upload-{Guid.NewGuid():N}");
        var service = new LeaveAttachmentStorageService(CreateConfiguration(root, 1), new FileTypeValidationService());
        var file = CreateFormFile("large.pdf", "application/pdf", 2 * 1024 * 1024);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveAsync(Guid.NewGuid(), Guid.NewGuid(), file));

        Assert.Contains("size limit", ex.Message);
    }

    [Fact]
    public async Task SaveAsync_RejectsSpoofedPdfContent()
    {
        var root = Path.Combine(Path.GetTempPath(), $"hop-upload-{Guid.NewGuid():N}");
        var service = new LeaveAttachmentStorageService(CreateConfiguration(root, 1), new FileTypeValidationService());
        var file = CreateFormFile("sample.pdf", "application/pdf", "not a pdf");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveAsync(Guid.NewGuid(), Guid.NewGuid(), file));

        Assert.Contains("content", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static IConfiguration CreateConfiguration(string root, int maxMb)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:RootPath"] = root,
                ["LeaveAttachments:MaxFileSizeMb"] = maxMb.ToString(),
                ["LeaveAttachments:AllowedExtensions"] = ".pdf,.jpg,.jpeg,.png"
            })
            .Build();
    }

    private static IFormFile CreateFormFile(string fileName, string contentType, int size)
    {
        var bytes = Enumerable.Repeat((byte)'A', size).ToArray();
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static IFormFile CreateFormFile(string fileName, string contentType, string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }
}
