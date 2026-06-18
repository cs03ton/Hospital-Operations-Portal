using System.Net.Sockets;
using Hop.Api.Interfaces;

namespace Hop.Api.Services;

public sealed class ClamAvFileScanningService(IConfiguration configuration, ILogger<ClamAvFileScanningService> logger) : IFileScanningService
{
    public async Task<FileScanResult> ScanAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        var enabled = configuration.GetValue("FileScan:Enabled", configuration.GetValue("FILE_SCAN_ENABLED", false));
        if (!enabled)
        {
            return new FileScanResult(true, "ClamAV", "File scanning is disabled.");
        }

        var host = configuration["ClamAv:Host"] ?? configuration["CLAMAV_HOST"] ?? "localhost";
        var port = configuration.GetValue("ClamAv:Port", configuration.GetValue("CLAMAV_PORT", 3310));
        var failClosed = configuration.GetValue("FileScan:FailClosed", configuration.GetValue("FILE_SCAN_FAIL_CLOSED", true));
        var timeoutMs = Math.Max(1000, configuration.GetValue("ClamAv:TimeoutMs", configuration.GetValue("CLAMAV_TIMEOUT_MS", 5000)));

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(timeoutMs));

            using var client = new TcpClient();
            await client.ConnectAsync(host, port, timeoutCts.Token);
            await using var networkStream = client.GetStream();
            await networkStream.WriteAsync("zINSTREAM\0"u8.ToArray(), timeoutCts.Token);

            await using (var input = file.OpenReadStream())
            {
                var buffer = new byte[8192];
                int read;
                while ((read = await input.ReadAsync(buffer, timeoutCts.Token)) > 0)
                {
                    var length = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(read));
                    await networkStream.WriteAsync(length, timeoutCts.Token);
                    await networkStream.WriteAsync(buffer.AsMemory(0, read), timeoutCts.Token);
                }
            }

            await networkStream.WriteAsync(new byte[] { 0, 0, 0, 0 }, timeoutCts.Token);

            using var responseReader = new StreamReader(networkStream);
            var response = await responseReader.ReadLineAsync(timeoutCts.Token) ?? string.Empty;
            logger.LogInformation("ClamAV scan response for {FileName}: {Response}", file.FileName, response);

            if (response.Contains(" OK", StringComparison.OrdinalIgnoreCase))
            {
                return new FileScanResult(true, "ClamAV", response);
            }

            if (response.Contains(" FOUND", StringComparison.OrdinalIgnoreCase))
            {
                return new FileScanResult(false, "ClamAV", response);
            }

            return new FileScanResult(!failClosed, "ClamAV", $"Unexpected ClamAV response: {response}");
        }
        catch (Exception ex) when (ex is SocketException or IOException or TimeoutException or OperationCanceledException)
        {
            logger.LogWarning(ex, "ClamAV scan unavailable for {FileName}. FailClosed={FailClosed}", file.FileName, failClosed);
            return new FileScanResult(!failClosed, "ClamAV", failClosed
                ? "ClamAV is unavailable and fail-closed mode is enabled."
                : "ClamAV is unavailable; fail-open mode allowed the file.");
        }
    }
}
