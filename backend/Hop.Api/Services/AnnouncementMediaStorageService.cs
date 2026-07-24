using Hop.Api.Configuration;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace Hop.Api.Services;

public sealed class AnnouncementMediaStorageService(
    IConfiguration configuration,
    IOptions<AnnouncementStorageOptions> options,
    IFileScanningService fileScanningService,
    IFileTypeValidationService fileTypeValidationService) : IAnnouncementMediaStorageService
{
    private static readonly Dictionary<string, string> ImageMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp"
    };

    private static readonly Dictionary<string, string[]> AttachmentMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = ["application/pdf"],
        [".doc"] = ["application/msword", "application/octet-stream"],
        [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/zip", "application/octet-stream"],
        [".xls"] = ["application/vnd.ms-excel", "application/octet-stream"],
        [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/zip", "application/octet-stream"],
        [".ppt"] = ["application/vnd.ms-powerpoint", "application/octet-stream"],
        [".pptx"] = ["application/vnd.openxmlformats-officedocument.presentationml.presentation", "application/zip", "application/octet-stream"],
        [".zip"] = ["application/zip", "application/x-zip-compressed", "application/octet-stream"]
    };

    private readonly AnnouncementStorageOptions storageOptions = options.Value;

    public async Task<AnnouncementImage> SaveImageAsync(
        Guid announcementId,
        Guid uploadedByUserId,
        IFormFile file,
        bool isCover,
        int displayOrder,
        CancellationToken cancellationToken = default)
    {
        await ValidateCommonAsync(file, storageOptions.MaxImageSizeBytes, cancellationToken);
        var extension = ValidateExtension(file.FileName, storageOptions.AllowedImageExtensions);
        var mimeType = await ValidateImageSignatureAsync(file, extension, cancellationToken);
        ValidateBrowserContentType(file, mimeType);
        await ScanAsync(file, cancellationToken);

        var now = DateTime.UtcNow;
        var storedBaseName = Guid.NewGuid().ToString("N");
        var relativeDirectory = BuildRelativeDirectory("images", announcementId, now);
        var absoluteDirectory = ResolveStoragePath(relativeDirectory);
        Directory.CreateDirectory(absoluteDirectory);

        var originalRelativePath = CombineRelative(relativeDirectory, $"{storedBaseName}_original{extension}");
        var largeRelativePath = CombineRelative(relativeDirectory, $"{storedBaseName}_large{extension}");
        var mediumRelativePath = CombineRelative(relativeDirectory, $"{storedBaseName}_medium{extension}");
        var thumbnailRelativePath = CombineRelative(relativeDirectory, $"{storedBaseName}_thumbnail{extension}");

        var writtenPaths = new List<string>();
        try
        {
            var originalBytes = await ReadAllBytesAsync(file, cancellationToken);
            using (var bitmap = SKBitmap.Decode(originalBytes))
            {
                if (bitmap is null)
                {
                    throw new InvalidOperationException("ไม่สามารถอ่านข้อมูลรูปภาพได้");
                }

                ValidateDimensions(bitmap.Width, bitmap.Height);

                await SaveImageVariantAsync(bitmap, ResolveStoragePath(originalRelativePath), extension, null, null, cancellationToken);
                writtenPaths.Add(originalRelativePath);
                await SaveImageVariantAsync(bitmap, ResolveStoragePath(largeRelativePath), extension, storageOptions.LargeMaxSize, storageOptions.LargeMaxSize, cancellationToken);
                writtenPaths.Add(largeRelativePath);
                await SaveImageVariantAsync(bitmap, ResolveStoragePath(mediumRelativePath), extension, storageOptions.MediumMaxSize, storageOptions.MediumMaxSize, cancellationToken);
                writtenPaths.Add(mediumRelativePath);
                await SaveImageVariantAsync(bitmap, ResolveStoragePath(thumbnailRelativePath), extension, storageOptions.ThumbnailMaxWidth, storageOptions.ThumbnailMaxHeight, cancellationToken, crop: true);
                writtenPaths.Add(thumbnailRelativePath);

                return new AnnouncementImage
                {
                    AnnouncementId = announcementId,
                    OriginalFileName = Path.GetFileName(file.FileName),
                    StoredFileName = $"{storedBaseName}{extension}",
                    RelativePath = originalRelativePath,
                    LargePath = largeRelativePath,
                    MediumPath = mediumRelativePath,
                    ThumbnailPath = thumbnailRelativePath,
                    MimeType = mimeType,
                    FileSize = file.Length,
                    Width = bitmap.Width,
                    Height = bitmap.Height,
                    DisplayOrder = displayOrder,
                    IsCover = isCover,
                    CreatedByUserId = uploadedByUserId,
                    CreatedAt = now
                };
            }
        }
        catch
        {
            CleanupRelativePaths(writtenPaths);
            throw;
        }
    }

    public async Task<AnnouncementFile> SaveAttachmentAsync(
        Guid announcementId,
        Guid uploadedByUserId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        await ValidateCommonAsync(file, storageOptions.MaxAttachmentSizeBytes, cancellationToken);
        var extension = ValidateExtension(file.FileName, storageOptions.AllowedAttachmentExtensions);
        ValidateAttachmentContentType(file, extension);
        var allowedExtensions = storageOptions.AllowedAttachmentExtensions
            .Select(item => item.StartsWith('.') ? item.ToLowerInvariant() : $".{item.ToLowerInvariant()}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var validation = await fileTypeValidationService.ValidateAsync(file, allowedExtensions, cancellationToken);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException("ชนิดไฟล์ไม่ตรงกับข้อมูลภายในไฟล์");
        }

        await ScanAsync(file, cancellationToken);

        var now = DateTime.UtcNow;
        var relativeDirectory = BuildRelativeDirectory("attachments", announcementId, now);
        var absoluteDirectory = ResolveStoragePath(relativeDirectory);
        Directory.CreateDirectory(absoluteDirectory);

        var storedName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = CombineRelative(relativeDirectory, storedName);
        var absolutePath = ResolveStoragePath(relativePath);

        try
        {
            await using var output = new FileStream(absolutePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            await file.CopyToAsync(output, cancellationToken);
        }
        catch
        {
            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }

            throw;
        }

        return new AnnouncementFile
        {
            AnnouncementId = announcementId,
            FileName = storedName,
            OriginalFileName = Path.GetFileName(file.FileName),
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            FilePath = relativePath,
            FileSize = file.Length,
            FileRole = "Attachment",
            CreatedAt = now,
            CreatedByUserId = uploadedByUserId
        };
    }

    public Task<FileInfo> OpenImageAsync(AnnouncementImage image, string variant, CancellationToken cancellationToken = default)
    {
        var relativePath = variant.ToLowerInvariant() switch
        {
            "thumbnail" => image.ThumbnailPath,
            "medium" => image.MediumPath,
            "large" => image.LargePath,
            "original" => image.RelativePath,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new FileNotFoundException("Image variant is not available.");
        }

        return Task.FromResult(new FileInfo(ResolveStoragePath(relativePath)));
    }

    public Task<FileInfo> OpenAttachmentAsync(AnnouncementFile file, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new FileInfo(ResolveStoragePath(file.FilePath)));
    }

    public Task DeleteImageAsync(AnnouncementImage image, CancellationToken cancellationToken = default)
    {
        CleanupRelativePaths(new[]
        {
            image.RelativePath,
            image.LargePath,
            image.MediumPath,
            image.ThumbnailPath
        });
        return Task.CompletedTask;
    }

    public Task DeleteAttachmentAsync(AnnouncementFile file, CancellationToken cancellationToken = default)
    {
        var absolutePath = ResolveStoragePath(file.FilePath);
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }

        return Task.CompletedTask;
    }

    public Task GenerateImageVariantsAsync(AnnouncementImage image, CancellationToken cancellationToken = default)
    {
        // Variants are generated during SaveImageAsync for local storage.
        return Task.CompletedTask;
    }

    private async Task ValidateCommonAsync(IFormFile file, long maxBytes, CancellationToken cancellationToken)
    {
        if (file.Length <= 0)
        {
            throw new InvalidOperationException("กรุณาเลือกไฟล์");
        }

        if (file.Length > maxBytes)
        {
            throw new InvalidOperationException("ไฟล์มีขนาดใหญ่เกินกำหนด");
        }

        await Task.CompletedTask;
    }

    private async Task ScanAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var result = await fileScanningService.ScanAsync(file, cancellationToken);
        if (!result.IsClean)
        {
            throw new InvalidOperationException("ไฟล์ไม่ผ่านการตรวจสอบความปลอดภัย");
        }
    }

    private string ValidateExtension(string fileName, string[] allowedExtensions)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var allowList = allowedExtensions
            .Select(item => item.StartsWith('.') ? item.ToLowerInvariant() : $".{item.ToLowerInvariant()}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(extension) || !allowList.Contains(extension))
        {
            throw new InvalidOperationException("ชนิดไฟล์ไม่อยู่ในรายการที่อนุญาต");
        }

        return extension;
    }

    private async Task<string> ValidateImageSignatureAsync(IFormFile file, string extension, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var header = new byte[Math.Min(16, file.Length)];
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
        var span = header.AsSpan(0, read);

        var isJpeg = span.Length >= 3 && span[0] == 0xFF && span[1] == 0xD8 && span[2] == 0xFF;
        var isPng = span.Length >= 8 &&
            span[0] == 0x89 && span[1] == 0x50 && span[2] == 0x4E && span[3] == 0x47 &&
            span[4] == 0x0D && span[5] == 0x0A && span[6] == 0x1A && span[7] == 0x0A;
        var isWebp = span.Length >= 12 &&
            span[0] == 0x52 && span[1] == 0x49 && span[2] == 0x46 && span[3] == 0x46 &&
            span[8] == 0x57 && span[9] == 0x45 && span[10] == 0x42 && span[11] == 0x50;

        var matches = extension switch
        {
            ".jpg" or ".jpeg" => isJpeg,
            ".png" => isPng,
            ".webp" => isWebp,
            _ => false
        };

        if (!matches)
        {
            throw new InvalidOperationException("ชนิดไฟล์ไม่ตรงกับข้อมูลภายในไฟล์");
        }

        return ImageMimeTypes[extension];
    }

    private void ValidateBrowserContentType(IFormFile file, string expectedMimeType)
    {
        if (string.IsNullOrWhiteSpace(file.ContentType))
        {
            return;
        }

        if (!string.Equals(file.ContentType, expectedMimeType, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("MIME type ของไฟล์ไม่ถูกต้อง");
        }
    }

    private void ValidateAttachmentContentType(IFormFile file, string extension)
    {
        if (string.IsNullOrWhiteSpace(file.ContentType))
        {
            return;
        }

        if (!AttachmentMimeTypes.TryGetValue(extension, out var allowed) ||
            !allowed.Any(item => string.Equals(item, file.ContentType, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("MIME type ของไฟล์ไม่ถูกต้อง");
        }
    }

    private void ValidateDimensions(int width, int height)
    {
        if (width <= 0 || height <= 0 ||
            width > storageOptions.MaximumWidth ||
            height > storageOptions.MaximumHeight ||
            (long)width * height > storageOptions.MaximumPixelCount)
        {
            throw new InvalidOperationException("ขนาดภาพไม่เหมาะสมหรือใหญ่เกินกำหนด");
        }
    }

    private async Task<byte[]> ReadAllBytesAsync(IFormFile file, CancellationToken cancellationToken)
    {
        await using var input = file.OpenReadStream();
        using var memory = new MemoryStream();
        await input.CopyToAsync(memory, cancellationToken);
        return memory.ToArray();
    }

    private async Task SaveImageVariantAsync(
        SKBitmap image,
        string absolutePath,
        string extension,
        int? maxWidth,
        int? maxHeight,
        CancellationToken cancellationToken,
        bool crop = false)
    {
        var targetWidth = image.Width;
        var targetHeight = image.Height;
        SKBitmap source = image;
        SKBitmap? cropped = null;

        if (maxWidth is not null && maxHeight is not null)
        {
            if (crop)
            {
                var sourceRatio = image.Width / (float)image.Height;
                var targetRatio = maxWidth.Value / (float)maxHeight.Value;
                var cropWidth = image.Width;
                var cropHeight = image.Height;
                if (sourceRatio > targetRatio)
                {
                    cropWidth = Math.Max(1, (int)(image.Height * targetRatio));
                }
                else
                {
                    cropHeight = Math.Max(1, (int)(image.Width / targetRatio));
                }

                var left = Math.Max(0, (image.Width - cropWidth) / 2);
                var top = Math.Max(0, (image.Height - cropHeight) / 2);
                cropped = new SKBitmap(cropWidth, cropHeight);
                using var canvas = new SKCanvas(cropped);
                canvas.DrawBitmap(image, new SKRect(left, top, left + cropWidth, top + cropHeight), new SKRect(0, 0, cropWidth, cropHeight));
                source = cropped;
                targetWidth = maxWidth.Value;
                targetHeight = maxHeight.Value;
            }
            else
            {
                var ratio = Math.Min(maxWidth.Value / (float)image.Width, maxHeight.Value / (float)image.Height);
                ratio = Math.Min(1f, ratio);
                targetWidth = Math.Max(1, (int)Math.Round(image.Width * ratio));
                targetHeight = Math.Max(1, (int)Math.Round(image.Height * ratio));
            }
        }

        using var resized = source.Resize(new SKImageInfo(targetWidth, targetHeight), SKSamplingOptions.Default)
            ?? throw new InvalidOperationException("ไม่สามารถสร้างรูปภาพย่อได้");
        using var imageData = SKImage.FromBitmap(resized);
        using var encoded = imageData.Encode(GetEncodedFormat(extension), 82)
            ?? throw new InvalidOperationException("ไม่สามารถบันทึกรูปภาพได้");

        await using var output = new FileStream(absolutePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        encoded.SaveTo(output);
        await output.FlushAsync(cancellationToken);
        cropped?.Dispose();
    }

    private static SKEncodedImageFormat GetEncodedFormat(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => SKEncodedImageFormat.Jpeg,
            ".png" => SKEncodedImageFormat.Png,
            ".webp" => SKEncodedImageFormat.Webp,
            _ => SKEncodedImageFormat.Jpeg
        };
    }

    private string BuildRelativeDirectory(string kind, Guid announcementId, DateTime now)
    {
        return CombineRelative(
            "announcements",
            now.Year.ToString("0000"),
            now.Month.ToString("00"),
            announcementId.ToString(),
            kind);
    }

    private static string CombineRelative(params string[] segments)
    {
        return string.Join('/', segments.Select(segment => segment.Trim('/', '\\')));
    }

    private string ResolveStoragePath(string relativePath)
    {
        var rootPath = configuration["Storage:RootPath"] ?? configuration["STORAGE_ROOT_PATH"];
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new InvalidOperationException("Storage__RootPath is required.");
        }

        var rootFullPath = Path.GetFullPath(rootPath);
        var absolutePath = Path.GetFullPath(Path.Combine(rootFullPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!absolutePath.StartsWith(rootFullPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid storage path.");
        }

        return absolutePath;
    }

    private void CleanupRelativePaths(IEnumerable<string?> relativePaths)
    {
        foreach (var relativePath in relativePaths.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            var absolutePath = ResolveStoragePath(relativePath!);
            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }
        }
    }
}
