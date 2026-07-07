namespace Hop.Api.DTOs;

public record DeleteReferenceSummary(string Label, int Count);

public record DeleteResultResponse(
    string Action,
    string Message,
    IReadOnlyList<DeleteReferenceSummary> References
);
