using Hop.Api.DTOs;

namespace Hop.Api.Interfaces;

public interface IHealthCenterService
{
    Task<AdminHealthResponse> GetHealthAsync(CancellationToken cancellationToken = default);
    Task<HealthComponentResponse> CheckDatabaseAsync(CancellationToken cancellationToken = default);
    StorageHealthResponse CheckStorage();
    Task<LineHealthResponse> CheckLineAsync(CancellationToken cancellationToken = default);
    DiskHealthResponse CheckDisk();
    MemoryHealthResponse CheckMemory();
    CpuHealthResponse CheckCpu();
    BackupHealthResponse CheckBackup();
}
