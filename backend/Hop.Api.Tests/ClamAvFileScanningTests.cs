using System.Net;
using System.Net.Sockets;
using Hop.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Hop.Api.Tests;

public class ClamAvFileScanningTests
{
    [Fact]
    public async Task ScanAsync_ReturnsCleanWhenClamAvRespondsOk()
    {
        using var server = new FakeClamAvServer("stream: OK\0");
        await server.StartAsync();
        var service = new ClamAvFileScanningService(CreateConfiguration(server.Port, failClosed: true), NullLogger<ClamAvFileScanningService>.Instance);

        var result = await service.ScanAsync(CreateFormFile("sample.pdf"));

        Assert.True(result.IsClean);
        Assert.Equal("ClamAV", result.Provider);
    }

    [Fact]
    public async Task ScanAsync_ReturnsRejectedWhenClamAvFindsVirus()
    {
        using var server = new FakeClamAvServer("stream: Eicar-Test-Signature FOUND\0");
        await server.StartAsync();
        var service = new ClamAvFileScanningService(CreateConfiguration(server.Port, failClosed: true), NullLogger<ClamAvFileScanningService>.Instance);

        var result = await service.ScanAsync(CreateFormFile("sample.pdf"));

        Assert.False(result.IsClean);
        Assert.Contains("FOUND", result.Message);
    }

    [Fact]
    public async Task ScanAsync_FailOpenAllowsFileWhenClamAvUnavailable()
    {
        var service = new ClamAvFileScanningService(CreateConfiguration(9, failClosed: false), NullLogger<ClamAvFileScanningService>.Instance);

        var result = await service.ScanAsync(CreateFormFile("sample.pdf"));

        Assert.True(result.IsClean);
        Assert.Contains("fail-open", result.Message);
    }

    private static IConfiguration CreateConfiguration(int port, bool failClosed)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FileScan:Enabled"] = "true",
                ["FileScan:Provider"] = "ClamAV",
                ["FileScan:FailClosed"] = failClosed.ToString(),
                ["ClamAv:Host"] = "127.0.0.1",
                ["ClamAv:Port"] = port.ToString(),
                ["ClamAv:TimeoutMs"] = "1000"
            })
            .Build();
    }

    private static IFormFile CreateFormFile(string fileName)
    {
        var bytes = "hello"u8.ToArray();
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };
    }

    private sealed class FakeClamAvServer(string response) : IDisposable
    {
        private readonly TcpListener listener = new(IPAddress.Loopback, 0);
        private Task? serverTask;

        public int Port { get; private set; }

        public Task StartAsync()
        {
            listener.Start();
            Port = ((IPEndPoint)listener.LocalEndpoint).Port;
            serverTask = Task.Run(HandleClientAsync);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            listener.Stop();
            serverTask?.Wait(TimeSpan.FromSeconds(1));
        }

        private async Task HandleClientAsync()
        {
            using var client = await listener.AcceptTcpClientAsync();
            await using var stream = client.GetStream();
            var command = new byte[10];
            await stream.ReadExactlyAsync(command);

            while (true)
            {
                var lengthBuffer = new byte[4];
                await stream.ReadExactlyAsync(lengthBuffer);
                var length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lengthBuffer));
                if (length == 0)
                {
                    break;
                }

                var data = new byte[length];
                await stream.ReadExactlyAsync(data);
            }

            await stream.WriteAsync(System.Text.Encoding.ASCII.GetBytes(response));
        }
    }
}
