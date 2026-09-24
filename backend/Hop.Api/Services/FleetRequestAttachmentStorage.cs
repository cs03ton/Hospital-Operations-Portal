using Hop.Api.Interfaces;
using Hop.Api.Models;
using System.IO.Compression;

namespace Hop.Api.Services;

public sealed class FleetRequestAttachmentStorage(IConfiguration configuration, IFileScanningService scanner, IFileTypeValidationService fileTypes)
{
    public const long MaximumBytes = 5 * 1024L * 1024L;
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".jpg", ".png", ".doc", ".docx" };

    public async Task<FleetRequestAttachment> SaveAsync(Guid requestId, Guid actor, IFormFile file, CancellationToken ct)
    {
        if (file.Length <= 0 || file.Length > MaximumBytes) throw new ArgumentException("ไฟล์ต้องมีขนาดไม่เกิน 5 MB");
        var validation = await fileTypes.ValidateAsync(file, Allowed, ct);
        if (!validation.IsValid) throw new ArgumentException(validation.ErrorMessage ?? "ชนิดไฟล์ไม่ถูกต้อง");
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension == ".docx")
        {
            try
            {
                await using var input = file.OpenReadStream();
                using var archive = new ZipArchive(input, ZipArchiveMode.Read, leaveOpen: false);
                if (archive.GetEntry("[Content_Types].xml") is null || archive.GetEntry("word/document.xml") is null)
                    throw new ArgumentException("ไฟล์ DOCX ไม่ถูกต้อง");
            }
            catch (InvalidDataException) { throw new ArgumentException("ไฟล์ DOCX ไม่ถูกต้อง"); }
        }
        var scan = await scanner.ScanAsync(file, ct);
        if (!scan.IsClean) throw new ArgumentException(scan.Message ?? "ไฟล์ไม่ผ่านการตรวจความปลอดภัย");
        var contentType = extension switch
        {
            ".pdf" => "application/pdf", ".jpg" => "image/jpeg", ".png" => "image/png",
            ".doc" => "application/msword", _ => "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };
        var relative = Path.Combine("fleet", "request-documents", requestId.ToString("N"), Guid.NewGuid().ToString("N") + extension);
        var path = Resolve(relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        try { await using var output = File.Create(path); await file.CopyToAsync(output, ct); }
        catch { if (File.Exists(path)) File.Delete(path); throw; }
        return new FleetRequestAttachment
        {
            FleetRequestId = requestId, UploadedByUserId = actor,
            OriginalFileName = Path.GetFileName(file.FileName)[..Math.Min(260, Path.GetFileName(file.FileName).Length)],
            StoredPath = relative.Replace('\\', '/'), ContentType = contentType, FileSize = file.Length
        };
    }

    public FileInfo Get(FleetRequestAttachment item) => new(Resolve(item.StoredPath));

    public void DeleteStoredFile(FleetRequestAttachment item)
    {
        var path = Resolve(item.StoredPath);
        if (File.Exists(path)) File.Delete(path);
    }

    private string Resolve(string relative)
    {
        var root = configuration["Storage:RootPath"] ?? configuration["STORAGE_ROOT_PATH"] ?? throw new InvalidOperationException("Storage root is required.");
        var rootFull = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var full = Path.GetFullPath(Path.Combine(rootFull, relative));
        if (!full.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Unsafe storage path.");
        return full;
    }
}
