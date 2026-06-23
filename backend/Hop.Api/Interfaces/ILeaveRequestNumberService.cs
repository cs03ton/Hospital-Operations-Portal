namespace Hop.Api.Interfaces;

public interface ILeaveRequestNumberService
{
    Task<string> GenerateAsync(DateTime createdAtUtc, CancellationToken cancellationToken = default);
}
