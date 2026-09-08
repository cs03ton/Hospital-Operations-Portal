using Hop.Api.Configuration;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.Extensions.Options;

namespace Hop.Api.Services;

public sealed class FleetMaintenanceAttachmentStorage(IConfiguration config, IFileScanningService scanner, IFileTypeValidationService fileTypes, IOptions<FleetOperationsOptions> options)
{
    private static readonly HashSet<string> Allowed = [".pdf", ".jpg", ".jpeg", ".png"];
    public async Task<FleetMaintenanceAttachment> Save(Guid recordId, Guid actor, IFormFile file, CancellationToken ct)
    {
        if (file.Length <= 0 || file.Length > options.Value.Maintenance.MaximumAttachmentSizeMb * 1024L * 1024L) throw new ArgumentException("Invalid attachment size.");
        var extension = Path.GetExtension(Path.GetFileName(file.FileName)).ToLowerInvariant(); if (!Allowed.Contains(extension)) throw new ArgumentException("Only PDF, JPG, JPEG and PNG are allowed.");
        var validation = await fileTypes.ValidateAsync(file, Allowed); if (!validation.IsValid) throw new ArgumentException(validation.ErrorMessage ?? "File content is invalid.");
        var scan = await scanner.ScanAsync(file, ct); if (!scan.IsClean) throw new ArgumentException("Attachment failed malware scanning.");
        var root = config["Storage:RootPath"] ?? config["STORAGE_ROOT_PATH"] ?? throw new InvalidOperationException("Storage root is required."); var now = DateTime.UtcNow; var relative = Path.Combine("fleet", "maintenance", now.ToString("yyyy"), now.ToString("MM"), recordId.ToString()); var directory = Path.GetFullPath(Path.Combine(root, relative)); var rootFull = Path.GetFullPath(root); if (!directory.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Unsafe storage path."); Directory.CreateDirectory(directory); var stored = Guid.NewGuid().ToString("N") + extension; await using var output = File.Create(Path.Combine(directory, stored)); await file.CopyToAsync(output, ct);
        return new() { MaintenanceRecordId = recordId, OriginalFileName = Path.GetFileName(file.FileName), StoredFileName = stored, ContentType = file.ContentType, FilePath = Path.Combine(relative, stored).Replace('\\', '/'), FileSize = file.Length, CreatedByUserId = actor };
    }
    public FileInfo Get(FleetMaintenanceAttachment attachment)
    {
        var root = config["Storage:RootPath"] ?? config["STORAGE_ROOT_PATH"] ?? throw new InvalidOperationException("Storage root is required.");
        var rootFull = Path.GetFullPath(root); var path = Path.GetFullPath(Path.Combine(rootFull, attachment.FilePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Unsafe storage path.");
        return new(path);
    }
}
