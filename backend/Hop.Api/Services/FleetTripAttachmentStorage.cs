using Hop.Api.Configuration;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.Extensions.Options;

namespace Hop.Api.Services;

public sealed class FleetTripAttachmentStorage(IConfiguration config,IFileScanningService scanner,IFileTypeValidationService fileTypes,IOptions<FleetOperationsOptions> options)
{
    private static readonly HashSet<string> Allowed=[".pdf",".jpg",".jpeg",".png"];
    public async Task<FleetTripAttachment> Save(Guid tripId,Guid requestId,Guid actor,string idempotencyKey,IFormFile file,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(idempotencyKey)||idempotencyKey.Length>200)throw new ArgumentException("A valid Idempotency-Key is required.");
        if(file.Length<=0||file.Length>options.Value.Maintenance.MaximumAttachmentSizeMb*1024L*1024L)throw new ArgumentException("Invalid attachment size.");
        var extension=Path.GetExtension(Path.GetFileName(file.FileName)).ToLowerInvariant();if(!Allowed.Contains(extension))throw new ArgumentException("Only PDF, JPG, JPEG and PNG are allowed.");
        var validation=await fileTypes.ValidateAsync(file,Allowed);if(!validation.IsValid)throw new ArgumentException(validation.ErrorMessage??"File content is invalid.");var scan=await scanner.ScanAsync(file,ct);if(!scan.IsClean)throw new ArgumentException("Attachment failed malware scanning.");
        var root=config["Storage:RootPath"]??config["STORAGE_ROOT_PATH"]??throw new InvalidOperationException("Storage root is required.");var now=DateTime.UtcNow;var relative=Path.Combine("fleet","trips",now.ToString("yyyy"),now.ToString("MM"),requestId.ToString());var directory=Path.GetFullPath(Path.Combine(root,relative));var rootFull=Path.GetFullPath(root);if(!directory.StartsWith(rootFull,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Unsafe storage path.");Directory.CreateDirectory(directory);var stored=Guid.NewGuid().ToString("N")+extension;await using var output=File.Create(Path.Combine(directory,stored));await file.CopyToAsync(output,ct);
        return new(){TripId=tripId,OriginalFileName=Path.GetFileName(file.FileName),StoredFileName=stored,ContentType=file.ContentType,FilePath=Path.Combine(relative,stored).Replace('\\','/'),FileSize=file.Length,IdempotencyKey=idempotencyKey,CreatedByUserId=actor};
    }
}
