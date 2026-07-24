using Hop.Api.DTOs;

namespace Hop.Api.Interfaces;

public interface IFileTypeValidationService
{
    Task<FileValidationResult> ValidateAsync(IFormFile file, IReadOnlySet<string> allowedExtensions, CancellationToken cancellationToken = default);
}
