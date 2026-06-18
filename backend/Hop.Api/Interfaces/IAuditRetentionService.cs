namespace Hop.Api.Interfaces;

public interface IAuditRetentionService
{
    Task<int> RunAsync(CancellationToken cancellationToken = default);
}
