using Hop.Api.DTOs;
using Hop.Api.Models;

namespace Hop.Api.Interfaces;

public interface IBackupCenterService
{
    Task<BackupOverviewResponse> GetOverviewAsync(CancellationToken cancellationToken = default);
    Task<PagedResponse<BackupRunResponse>> GetBackupsAsync(BackupQuery query, CancellationToken cancellationToken = default);
    Task<BackupRunDetailResponse?> GetBackupAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RestorePreviewResponse?> PreviewRestoreAsync(Guid id, Guid? userId, CancellationToken cancellationToken = default);
    Task<RestoreRunResponse?> RestoreAsync(Guid id, RestoreRequest request, Guid? userId, CancellationToken cancellationToken = default);
    Task<PagedResponse<RestoreRunResponse>> GetRestoreRunsAsync(BackupQuery query, CancellationToken cancellationToken = default);
    Task<RetentionPreviewResponse> PreviewRetentionAsync(Guid? userId, CancellationToken cancellationToken = default);
    Task<ApplyRetentionResponse> ApplyRetentionAsync(ApplyRetentionRequest request, Guid? userId, CancellationToken cancellationToken = default);
    Task<BackupVerificationResponse?> VerifyBackupAsync(Guid id, Guid? userId, CancellationToken cancellationToken = default);
}
