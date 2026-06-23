using Hop.Api.Data;
using Hop.Api.Interfaces;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed class LeaveRequestNumberService(AppDbContext db) : ILeaveRequestNumberService
{
    public async Task<string> GenerateAsync(DateTime createdAtUtc, CancellationToken cancellationToken = default)
    {
        var month = createdAtUtc.ToString("yyyyMM", CultureInfo.InvariantCulture);
        var prefix = $"LV-{month}-";

        var latestNumber = await db.LeaveRequests
            .Where(item => item.RequestNumber != null && item.RequestNumber.StartsWith(prefix))
            .OrderByDescending(item => item.RequestNumber)
            .Select(item => item.RequestNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var nextSequence = 1;
        if (!string.IsNullOrWhiteSpace(latestNumber) &&
            latestNumber.Length > prefix.Length &&
            int.TryParse(latestNumber[prefix.Length..], out var currentSequence))
        {
            nextSequence = currentSequence + 1;
        }

        return $"{prefix}{nextSequence:000}";
    }
}
