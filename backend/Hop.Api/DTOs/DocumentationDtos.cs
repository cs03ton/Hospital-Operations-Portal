namespace Hop.Api.DTOs;

public record DocumentationSummaryResponse(
    string Slug,
    string Title,
    string Description,
    string Category,
    IReadOnlyList<string> Roles,
    DateTime UpdatedAt
);

public record DocumentationDetailResponse(
    string Slug,
    string Title,
    string Description,
    string Category,
    IReadOnlyList<string> Roles,
    string ContentMarkdown,
    DateTime UpdatedAt
);

public record UpdateDocumentationRequest(string ContentMarkdown);
