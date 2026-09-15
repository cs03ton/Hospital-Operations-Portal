using Hop.Api.Interfaces;
using Hop.Api.Models;

namespace Hop.Api.Services;

public sealed class MeetingRoomAttachmentStorage(IConfiguration configuration, IFileScanningService scanner, IFileTypeValidationService fileTypes)
{
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx" };

    public async Task<MeetingRoomAttachment> SaveAsync(Guid bookingId, Guid actor, IFormFile file, CancellationToken ct)
    {
        if (file.Length <= 0 || file.Length > 10 * 1024L * 1024L) throw new ArgumentException("ไฟล์ต้องมีขนาดไม่เกิน 10 MB");
        var validation = await fileTypes.ValidateAsync(file, Allowed, ct);
        if (!validation.IsValid) throw new ArgumentException(validation.ErrorMessage ?? "ชนิดไฟล์ไม่ถูกต้อง");
        var scan = await scanner.ScanAsync(file, ct);
        if (!scan.IsClean) throw new ArgumentException(scan.Message);
        var root = configuration["Storage:RootPath"] ?? configuration["STORAGE_ROOT_PATH"] ?? throw new InvalidOperationException("Storage root is required.");
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var relative = Path.Combine("meeting-rooms", DateTime.UtcNow.ToString("yyyy"), bookingId.ToString("N"));
        var rootFull = Path.GetFullPath(root);
        var directory = Path.GetFullPath(Path.Combine(rootFull, relative));
        if (!directory.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Unsafe storage path.");
        Directory.CreateDirectory(directory);
        var storedName = Guid.NewGuid().ToString("N") + extension;
        await using (var output = File.Create(Path.Combine(directory, storedName))) await file.CopyToAsync(output, ct);
        return new MeetingRoomAttachment { BookingId = bookingId, UploadedById = actor, OriginalFileName = Path.GetFileName(file.FileName), StoredPath = Path.Combine(relative, storedName), ContentType = file.ContentType, FileSize = file.Length };
    }

    public FileInfo Get(MeetingRoomAttachment row)
    {
        var root = configuration["Storage:RootPath"] ?? configuration["STORAGE_ROOT_PATH"] ?? throw new InvalidOperationException("Storage root is required.");
        var rootFull = Path.GetFullPath(root);
        var path = Path.GetFullPath(Path.Combine(rootFull, row.StoredPath));
        if (!path.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Unsafe storage path.");
        return new FileInfo(path);
    }
}
