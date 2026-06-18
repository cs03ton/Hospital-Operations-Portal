using Hop.Api.Interfaces;

namespace Hop.Api.Services;

public sealed class PlaceholderFileScanningService(IConfiguration configuration) : IFileScanningService
{
    public Task<FileScanResult> ScanAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        var enabled = configuration.GetValue("FileScan:Enabled", configuration.GetValue("FILE_SCAN_ENABLED", false));
        var provider = configuration["FileScan:Provider"] ?? configuration["FILE_SCAN_PROVIDER"] ?? "Placeholder";
        if (!enabled)
        {
            return Task.FromResult(new FileScanResult(true, provider, "File scanning is disabled."));
        }

        if (!string.Equals(provider, "Placeholder", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new FileScanResult(false, provider, $"File scan provider '{provider}' is not implemented."));
        }

        return Task.FromResult(new FileScanResult(true, provider, "Placeholder scan passed. Replace with ClamAV implementation in production."));
    }
}
