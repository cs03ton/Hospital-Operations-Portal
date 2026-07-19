using Hop.Api.DTOs;

namespace Hop.Api.Interfaces;

public interface IDiagnosticsService
{
    Task<DiagnosticsSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<DiagnosticTestResultResponse> RunTestAsync(string diagnosticType, Guid? userId, string referenceId, CancellationToken cancellationToken = default);
    Task<DiagnosticsLogResponse> GetLogsAsync(DiagnosticsLogQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecentErrorResponse>> GetRecentErrorsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DiagnosticRunResponse>> GetHistoryAsync(CancellationToken cancellationToken = default);
    Task<SupportBundleResponse> CreateSupportBundleAsync(SupportBundleRequest request, Guid? userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupportBundleHistoryResponse>> GetSupportBundlesAsync(CancellationToken cancellationToken = default);
    Task<(string FilePath, string FileName, string ContentType)?> GetSupportBundleFileAsync(Guid id, Guid? userId, CancellationToken cancellationToken = default);
}
