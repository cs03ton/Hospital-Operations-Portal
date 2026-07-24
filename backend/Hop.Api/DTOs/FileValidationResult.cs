namespace Hop.Api.DTOs;

public sealed record FileValidationResult(bool IsValid, string? ErrorMessage = null);
