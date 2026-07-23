using Hop.Api.Configuration;
using Hop.Api.Interfaces;
using Hop.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SkiaSharp;
using Xunit;

namespace Hop.Api.Tests;

public class AnnouncementMediaStorageServiceTests : IDisposable
{
    private readonly string tempRoot = Path.Combine(Path.GetTempPath(), $"hop-announcement-media-{Guid.NewGuid():N}");

    [Fact]
    public async Task SaveImageAsync_CreatesVariantsAndMetadata()
    {
        var service = CreateService(new FileScanResult(true, "Test", "Clean"));
        var file = CreateImageFormFile("cover.jpg", "image/jpeg");

        var image = await service.SaveImageAsync(Guid.NewGuid(), Guid.NewGuid(), file, true, 1);

        Assert.True(image.IsCover);
        Assert.Equal("cover.jpg", image.OriginalFileName);
        Assert.Equal("image/jpeg", image.MimeType);
        Assert.NotNull(image.ThumbnailPath);
        Assert.True(File.Exists(Path.Combine(tempRoot, image.RelativePath.Replace('/', Path.DirectorySeparatorChar))));
        Assert.True(File.Exists(Path.Combine(tempRoot, image.ThumbnailPath!.Replace('/', Path.DirectorySeparatorChar))));
    }

    [Fact]
    public async Task SaveImageAsync_RejectsFakeJpg()
    {
        var service = CreateService(new FileScanResult(true, "Test", "Clean"));
        var file = CreateFormFile("fake.jpg", "image/jpeg", "not an image"u8.ToArray());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveImageAsync(Guid.NewGuid(), Guid.NewGuid(), file, false, 1));

        Assert.Equal("ชนิดไฟล์ไม่ตรงกับข้อมูลภายในไฟล์", ex.Message);
    }

    [Fact]
    public async Task SaveAttachmentAsync_RejectsDisallowedExtension()
    {
        var service = CreateService(new FileScanResult(true, "Test", "Clean"));
        var file = CreateFormFile("script.exe", "application/octet-stream", "MZ"u8.ToArray());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveAttachmentAsync(Guid.NewGuid(), Guid.NewGuid(), file));

        Assert.Equal("ชนิดไฟล์ไม่อยู่ในรายการที่อนุญาต", ex.Message);
    }

    [Fact]
    public async Task SaveImageAsync_RejectsVirusScanFailure()
    {
        var service = CreateService(new FileScanResult(false, "Test", "Infected"));
        var file = CreateImageFormFile("cover.png", "image/png", SKEncodedImageFormat.Png);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveImageAsync(Guid.NewGuid(), Guid.NewGuid(), file, true, 1));

        Assert.Equal("ไฟล์ไม่ผ่านการตรวจสอบความปลอดภัย", ex.Message);
        Assert.False(Directory.Exists(Path.Combine(tempRoot, "announcements")));
    }

    private AnnouncementMediaStorageService CreateService(FileScanResult scanResult)
    {
        Directory.CreateDirectory(tempRoot);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:RootPath"] = tempRoot
            })
            .Build();

        return new AnnouncementMediaStorageService(
            configuration,
            Options.Create(new AnnouncementStorageOptions()),
            new StubFileScanningService(scanResult));
    }

    private static IFormFile CreateImageFormFile(string fileName, string contentType, SKEncodedImageFormat format = SKEncodedImageFormat.Jpeg)
    {
        using var bitmap = new SKBitmap(640, 360);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.DarkGreen);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 85);
        return CreateFormFile(fileName, contentType, data.ToArray());
    }

    private static IFormFile CreateFormFile(string fileName, string contentType, byte[] bytes)
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, stream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    public void Dispose()
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    private sealed class StubFileScanningService(FileScanResult result) : IFileScanningService
    {
        public Task<FileScanResult> ScanAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(result);
        }
    }
}
