using Hop.Api.Interfaces;
using Hop.Api.Models;

namespace Hop.Api.Services;

public sealed class LeaveAttachmentStorageService(IConfiguration configuration) : ILeaveAttachmentStorageService
{
    public async Task<LeaveAttachment> SaveAsync(Guid leaveRequestId, Guid uploadedByUserId, IFormFile file)
    {
        if (file.Length == 0)
        {
            throw new InvalidOperationException("File is required.");
        }

        var rootPath = configuration["Storage:RootPath"] ?? configuration["STORAGE_ROOT_PATH"];
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new InvalidOperationException("STORAGE_ROOT_PATH is required.");
        }

        var maxSizeMb = configuration.GetValue("LeaveAttachments:MaxFileSizeMb", 5);
        var maxBytes = maxSizeMb * 1024L * 1024L;
        if (file.Length > maxBytes)
        {
            throw new InvalidOperationException($"File exceeds the configured size limit of {maxSizeMb} MB.");
        }

        var allowedExtensions = (configuration["LeaveAttachments:AllowedExtensions"] ?? ".pdf,.jpg,.jpeg,.png")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(extension => extension.StartsWith('.') ? extension.ToLowerInvariant() : $".{extension.ToLowerInvariant()}")
            .ToHashSet();

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("File type is not allowed.");
        }

        var now = DateTime.UtcNow;
        var relativeDirectory = Path.Combine(
            "leave-attachments",
            now.Year.ToString("0000"),
            now.Month.ToString("00"),
            leaveRequestId.ToString());
        var absoluteDirectory = Path.Combine(rootPath, relativeDirectory);
        Directory.CreateDirectory(absoluteDirectory);

        var safeFileName = $"{Guid.NewGuid():N}{extension}";
        var absolutePath = Path.Combine(absoluteDirectory, safeFileName);
        await using (var stream = File.Create(absolutePath))
        {
            await file.CopyToAsync(stream);
        }

        return new LeaveAttachment
        {
            LeaveRequestId = leaveRequestId,
            FileName = file.FileName,
            FilePath = Path.Combine(relativeDirectory, safeFileName).Replace('\\', '/'),
            ContentType = file.ContentType,
            FileSizeBytes = file.Length,
            UploadedByUserId = uploadedByUserId
        };
    }

    public Task DeleteAsync(LeaveAttachment attachment)
    {
        var fileInfo = GetFileInfo(attachment);
        if (fileInfo.Exists)
        {
            fileInfo.Delete();
        }

        return Task.CompletedTask;
    }

    public FileInfo GetFileInfo(LeaveAttachment attachment)
    {
        var rootPath = configuration["Storage:RootPath"] ?? configuration["STORAGE_ROOT_PATH"];
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new InvalidOperationException("STORAGE_ROOT_PATH is required.");
        }

        var absolutePath = Path.Combine(rootPath, attachment.FilePath.Replace('/', Path.DirectorySeparatorChar));
        return new FileInfo(absolutePath);
    }
}
