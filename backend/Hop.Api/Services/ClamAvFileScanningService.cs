using System.Net;
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

        var connectionType = FirstConfigured("ClamAv:ConnectionType", "ClamAV:ConnectionType", "CLAMAV_CONNECTION_TYPE") ?? "Tcp";
        var socketPath = FirstConfigured("ClamAv:SocketPath", "ClamAV:SocketPath", "CLAMAV_SOCKET_PATH") ?? "/var/run/clamav/clamd.ctl";
        var host = FirstConfigured("ClamAv:Host", "ClamAV:Host", "CLAMAV_HOST") ?? "localhost";
        var port = configuration.GetValue("ClamAv:Port", configuration.GetValue("CLAMAV_PORT", 3310));
        var failClosed = configuration.GetValue("FileScan:FailClosed", configuration.GetValue("FILE_SCAN_FAIL_CLOSED", true));
        var timeoutMs = Math.Max(1000, configuration.GetValue("ClamAv:TimeoutMs", configuration.GetValue("CLAMAV_TIMEOUT_MS", 5000)));

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(timeoutMs));

            await using var clamAvStream = await OpenClamAvStreamAsync(connectionType, host, port, socketPath, timeoutCts.Token);
            await clamAvStream.WriteAsync("zINSTREAM\0"u8.ToArray(), timeoutCts.Token);

            await using (var input = file.OpenReadStream())
            {
                var buffer = new byte[8192];
                int read;
                while ((read = await input.ReadAsync(buffer, timeoutCts.Token)) > 0)
                {
                    var length = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(read));
                    await clamAvStream.WriteAsync(length, timeoutCts.Token);
                    await clamAvStream.WriteAsync(buffer.AsMemory(0, read), timeoutCts.Token);
                }
            }

            await clamAvStream.WriteAsync(new byte[] { 0, 0, 0, 0 }, timeoutCts.Token);

            using var responseReader = new StreamReader(clamAvStream);
            var response = await responseReader.ReadLineAsync(timeoutCts.Token) ?? string.Empty;
            logger.LogInformation("ClamAV scan response for {FileName} via {ConnectionType}: {Response}", file.FileName, connectionType, response);

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
            logger.LogWarning(ex, "ClamAV scan unavailable for {FileName}. ConnectionType={ConnectionType}, FailClosed={FailClosed}", file.FileName, connectionType, failClosed);
            return new FileScanResult(!failClosed, "ClamAV", failClosed
                ? "ClamAV is unavailable and fail-closed mode is enabled."
                : "ClamAV is unavailable; fail-open mode allowed the file.");
        }
    }

    private async Task<Stream> OpenClamAvStreamAsync(
        string connectionType,
        string host,
        int port,
        string socketPath,
        CancellationToken cancellationToken)
    {
        if (string.Equals(connectionType, "UnixSocket", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(connectionType, "Unix", StringComparison.OrdinalIgnoreCase))
        {
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            try
            {
                await socket.ConnectAsync(new UnixDomainSocketEndPoint(socketPath), cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        var tcpSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        try
        {
            await tcpSocket.ConnectAsync(host, port, cancellationToken);
            return new NetworkStream(tcpSocket, ownsSocket: true);
        }
        catch
        {
            tcpSocket.Dispose();
            throw;
        }
    }

    private string? FirstConfigured(params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = configuration[key];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
