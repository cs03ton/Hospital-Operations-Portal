using Hop.Api.DTOs;

namespace Hop.Api.Interfaces;

public sealed record DocumentationAccessContext(
    IReadOnlySet<string> Roles,
    IReadOnlySet<string> Permissions
);

public interface IDocumentationService
{
    Task<IReadOnlyList<DocumentationSummaryResponse>> GetDocumentsAsync(
        DocumentationAccessContext access,
        CancellationToken cancellationToken = default);

    Task<DocumentationDetailResponse?> GetDocumentAsync(
        string slug,
        DocumentationAccessContext access,
        CancellationToken cancellationToken = default);

    Task<DocumentationDetailResponse?> UpdateDocumentAsync(
        string slug,
        string contentMarkdown,
        DocumentationAccessContext access,
        CancellationToken cancellationToken = default);

    Task<byte[]?> GeneratePdfAsync(
        string slug,
        DocumentationAccessContext access,
        CancellationToken cancellationToken = default);
}
