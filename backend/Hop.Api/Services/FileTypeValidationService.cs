using Hop.Api.DTOs;
using Hop.Api.Interfaces;

namespace Hop.Api.Services;

public sealed class FileTypeValidationService : IFileTypeValidationService
{
    private static readonly Dictionary<string, byte[][]> Signatures = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = [Bytes("%PDF-")],
        [".jpg"] = [[0xFF, 0xD8, 0xFF]],
        [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
        [".png"] = [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]],
        [".webp"] = [Bytes("RIFF")],
        [".docx"] = [[0x50, 0x4B, 0x03, 0x04], [0x50, 0x4B, 0x05, 0x06], [0x50, 0x4B, 0x07, 0x08]],
        [".xlsx"] = [[0x50, 0x4B, 0x03, 0x04], [0x50, 0x4B, 0x05, 0x06], [0x50, 0x4B, 0x07, 0x08]],
        [".pptx"] = [[0x50, 0x4B, 0x03, 0x04], [0x50, 0x4B, 0x05, 0x06], [0x50, 0x4B, 0x07, 0x08]],
        [".doc"] = [[0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]],
        [".xls"] = [[0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]],
        [".ppt"] = [[0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]],
        [".zip"] = [[0x50, 0x4B, 0x03, 0x04], [0x50, 0x4B, 0x05, 0x06], [0x50, 0x4B, 0x07, 0x08]]
    };

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    public async Task<FileValidationResult> ValidateAsync(
        IFormFile file,
        IReadOnlySet<string> allowedExtensions,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension))
        {
            return new FileValidationResult(false, "File type is not allowed.");
        }

        if (!Signatures.TryGetValue(extension, out var signatures))
        {
            return new FileValidationResult(false, "File type is not supported.");
        }

        var header = new byte[16];
        await using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
        if (bytesRead == 0)
        {
            return new FileValidationResult(false, "File is empty.");
        }

        var hasValidSignature = signatures.Any(signature => StartsWith(header, bytesRead, signature));
        if (!hasValidSignature)
        {
            return new FileValidationResult(false, "File content does not match the declared file type.");
        }

        if (extension.Equals(".webp", StringComparison.OrdinalIgnoreCase) &&
            (bytesRead < 12 || !StartsWith(header.AsSpan(8), Bytes("WEBP"))))
        {
            return new FileValidationResult(false, "File content does not match the declared file type.");
        }

        if (ImageExtensions.Contains(extension) && !IsImageContentType(file.ContentType))
        {
            return new FileValidationResult(false, "Image content type is not allowed.");
        }

        return new FileValidationResult(true);
    }

    private static bool StartsWith(byte[] header, int bytesRead, byte[] signature)
    {
        return bytesRead >= signature.Length && header.AsSpan(0, signature.Length).SequenceEqual(signature);
    }

    private static bool StartsWith(ReadOnlySpan<byte> header, byte[] signature)
    {
        return header.Length >= signature.Length && header[..signature.Length].SequenceEqual(signature);
    }

    private static byte[] Bytes(string value)
    {
        return System.Text.Encoding.ASCII.GetBytes(value);
    }

    private static bool IsImageContentType(string? contentType)
    {
        return contentType is not null &&
            (contentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase) ||
             contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase) ||
             contentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase));
    }
}
