namespace Hop.Api.Interfaces;

public interface IFileScanningService
{
    Task<FileScanResult> ScanAsync(IFormFile file, CancellationToken cancellationToken = default);
}

public record FileScanResult(bool IsClean, string Provider, string Message);
