using Hop.Api.Data;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public interface IFleetTripParticipantService
{
    Task<IReadOnlyList<FleetTripParticipant>> FinalizeEmployeesAsync(Guid tripId, Guid actorUserId, IReadOnlyCollection<Guid> employeeUserIds, CancellationToken ct = default);
}

public sealed class FleetTripParticipantService(AppDbContext db) : IFleetTripParticipantService
{
    public async Task<IReadOnlyList<FleetTripParticipant>> FinalizeEmployeesAsync(Guid tripId, Guid actorUserId, IReadOnlyCollection<Guid> employeeUserIds, CancellationToken ct = default)
    {
        var trip = await db.FleetTripRecords
            .Include(x => x.FleetRequest)
            .Include(x => x.Participants)
            .SingleOrDefaultAsync(x => x.Id == tripId, ct)
            ?? throw new KeyNotFoundException("Trip not found.");

        if (trip.DriverUserId != actorUserId)
            throw new UnauthorizedAccessException("Only the assigned driver can confirm actual participants.");

        if (trip.ActualEndAt is not null || trip.FleetRequest?.Status != FleetRequestStatuses.InProgress)
            throw new InvalidOperationException("Actual participants can only be confirmed while the trip is in progress.");

        var ids = employeeUserIds.Distinct().ToArray();
        var activeIds = await db.Users.AsNoTracking()
            .Where(x => ids.Contains(x.Id) && x.IsActive)
            .Select(x => x.Id)
            .ToListAsync(ct);
        if (activeIds.Count != ids.Length)
            throw new ArgumentException("Participant list contains an inactive or unknown employee.");
        if (ids.Contains(trip.DriverUserId))
            throw new ArgumentException("The assigned driver cannot be an actual passenger.");

        db.FleetTripParticipants.RemoveRange(trip.Participants);
        var now = DateTime.UtcNow;
        var participants = ids.Select(userId => new FleetTripParticipant
        {
            TripId = trip.Id,
            UserId = userId,
            IsRequester = userId == trip.FleetRequest!.RequesterUserId,
            ParticipantType = FleetPassengerTypes.Employee,
            IsActualParticipant = true,
            CreatedAt = now,
            CreatedByUserId = actorUserId
        }).ToList();
        db.FleetTripParticipants.AddRange(participants);
        await db.SaveChangesAsync(ct);
        return participants;
    }
}
