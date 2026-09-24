using Hop.Api.Interfaces;

namespace Hop.Api.Services;

public sealed class MeetingRoomPhotoStorage(IConfiguration configuration, IFileScanningService scanner, IFileTypeValidationService fileTypes)
{
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    public async Task<(string Path, string ContentType)> SaveAsync(Guid roomId, IFormFile file, CancellationToken ct)
    {
        if (file.Length <= 0 || file.Length > 5 * 1024L * 1024L) throw new ArgumentException("รูปห้องต้องมีขนาดไม่เกิน 5 MB");
        var validation = await fileTypes.ValidateAsync(file, Allowed, ct);
        if (!validation.IsValid) throw new ArgumentException(validation.ErrorMessage ?? "ชนิดรูปไม่ถูกต้อง");
        var scan = await scanner.ScanAsync(file, ct);
        if (!scan.IsClean) throw new ArgumentException(scan.Message);
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var contentType = extension switch { ".jpg" or ".jpeg" => "image/jpeg", ".png" => "image/png", _ => "image/webp" };
        var relative = Path.Combine("meeting-rooms", "photos", roomId.ToString("N"), Guid.NewGuid().ToString("N") + extension);
        var target = Resolve(relative);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        await using (var stream = File.Create(target)) await file.CopyToAsync(stream, ct);
        return (relative, contentType);
    }

    public FileInfo Get(string relative) => new(Resolve(relative));

    public void Delete(string? relative)
    {
        if (relative is null) return;
        var path = Resolve(relative);
        if (File.Exists(path)) File.Delete(path);
    }

    private string Resolve(string relative)
    {
        var root = configuration["Storage:RootPath"] ?? configuration["STORAGE_ROOT_PATH"] ?? throw new InvalidOperationException("Storage root is required.");
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(fullRoot, relative));
        if (!path.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Unsafe storage path.");
        return path;
    }
}
