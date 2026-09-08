using System.Data;
using Hop.Api.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace Hop.Api.Services;

public sealed record FleetVehicleUtilization(
    Guid VehicleId, string Vehicle, decimal ReportingWindowHours, decimal AssignedHours,
    decimal TripHours, decimal UnavailableHours, decimal MaintenanceHours, decimal BlockedHours,
    decimal AvailableHours, decimal? UtilizationPercentage, int TotalTrips, decimal TotalDistance,
    int ReplacementCount);

public static class FleetIntervalAggregation
{
    public static decimal UnionHours(IEnumerable<(DateTime Start, DateTime End)> intervals, DateTime windowStart, DateTime windowEnd)
    {
        var ordered = intervals.Select(x => (Start: x.Start < windowStart ? windowStart : x.Start, End: x.End > windowEnd ? windowEnd : x.End)).Where(x => x.End > x.Start).OrderBy(x => x.Start).ToList();
        if (ordered.Count == 0) return 0; var total = TimeSpan.Zero; var current = ordered[0];
        foreach (var next in ordered.Skip(1)) { if (next.Start <= current.End) current.End = next.End > current.End ? next.End : current.End; else { total += current.End - current.Start; current = next; } }
        total += current.End - current.Start; return (decimal)total.TotalHours;
    }
}

public sealed class FleetUtilizationQueryService(AppDbContext db)
{
    private const string Sql = """
WITH params AS (
  SELECT @start::timestamptz AS start_at, @end::timestamptz AS end_at, @snapshot::timestamptz AS snapshot_at
), intervals AS (
  SELECT a.vehicle_id, 'ASSIGNED' kind,
         tstzrange(greatest(r.departure_at,p.start_at), least(r.expected_return_at,p.end_at),'[)') period
  FROM fleet_assignments a JOIN fleet_requests r ON r.id=a.fleet_request_id CROSS JOIN params p
  WHERE r.departure_at < p.end_at AND r.expected_return_at > p.start_at
  UNION ALL
  SELECT a.vehicle_id, 'TRIP', tstzrange(greatest(t.actual_start_at,p.start_at), least(coalesce(t.actual_end_at,t.aborted_at,p.snapshot_at),p.end_at),'[)')
  FROM fleet_trip_records t JOIN fleet_assignments a ON a.id=t.assignment_id CROSS JOIN params p
  WHERE t.actual_start_at IS NOT NULL AND t.actual_start_at < p.end_at AND coalesce(t.actual_end_at,t.aborted_at,p.snapshot_at) > p.start_at
  UNION ALL
  SELECT u.vehicle_id, 'UNAVAILABLE', tstzrange(greatest(u.start_at,p.start_at), least(u.end_at,p.end_at),'[)')
  FROM fleet_vehicle_unavailability u CROSS JOIN params p WHERE u.start_at < p.end_at AND u.end_at > p.start_at
  UNION ALL
  SELECT s.vehicle_id, 'MAINTENANCE', tstzrange(greatest(coalesce(rec.started_at,s.updated_at,s.created_at),p.start_at), least(coalesce(rec.completed_at,p.snapshot_at),p.end_at),'[)')
  FROM fleet_vehicle_maintenance_schedules s LEFT JOIN fleet_vehicle_maintenance_records rec ON rec.maintenance_schedule_id=s.id AND NOT rec.is_cancelled CROSS JOIN params p
  WHERE s.status='IN_PROGRESS' AND coalesce(rec.started_at,s.updated_at,s.created_at) < p.end_at AND coalesce(rec.completed_at,p.snapshot_at) > p.start_at
), valid AS (SELECT * FROM intervals WHERE NOT isempty(period)),
expanded AS (
  SELECT vehicle_id,kind,unnest(range_agg(period)) period FROM valid GROUP BY vehicle_id,kind
), durations AS (
  SELECT vehicle_id,kind,sum(extract(epoch FROM upper(period)-lower(period))/3600.0) hours FROM expanded GROUP BY vehicle_id,kind
), blocked_source AS (
  SELECT vehicle_id,period FROM valid WHERE kind IN ('UNAVAILABLE','MAINTENANCE')
), blocked_expanded AS (
  SELECT vehicle_id,unnest(range_agg(period)) period FROM blocked_source GROUP BY vehicle_id
), blocked AS (
  SELECT vehicle_id,sum(extract(epoch FROM upper(period)-lower(period))/3600.0) hours FROM blocked_expanded GROUP BY vehicle_id
), stats AS (
  SELECT a.vehicle_id,count(DISTINCT t.id)::int total_trips,
         coalesce(sum(greatest(coalesce(t.end_mileage,t.start_mileage,0)-coalesce(t.start_mileage,0),0)),0) total_distance,
         count(*) FILTER (WHERE a.replaced_assignment_id IS NOT NULL)::int replacement_count
  FROM fleet_assignments a JOIN fleet_requests r ON r.id=a.fleet_request_id CROSS JOIN params p
  LEFT JOIN fleet_trip_records t ON t.assignment_id=a.id
  WHERE r.created_at>=p.start_at AND r.created_at<p.end_at GROUP BY a.vehicle_id
)
SELECT v.id,v.vehicle_code || ' ' || v.registration_number,
       extract(epoch FROM (p.end_at-p.start_at))/3600.0 reporting_hours,
       coalesce(da.hours,0),coalesce(dt.hours,0),coalesce(du.hours,0),coalesce(dm.hours,0),coalesce(b.hours,0),
       greatest(extract(epoch FROM (p.end_at-p.start_at))/3600.0-coalesce(b.hours,0),0) available_hours,
       CASE WHEN extract(epoch FROM (p.end_at-p.start_at))/3600.0-coalesce(b.hours,0)<=0 THEN NULL
            ELSE coalesce(dt.hours,0)*100.0/greatest(extract(epoch FROM (p.end_at-p.start_at))/3600.0-coalesce(b.hours,0),0.000001) END utilization,
       coalesce(s.total_trips,0),coalesce(s.total_distance,0),coalesce(s.replacement_count,0)
FROM fleet_vehicles v CROSS JOIN params p
LEFT JOIN durations da ON da.vehicle_id=v.id AND da.kind='ASSIGNED'
LEFT JOIN durations dt ON dt.vehicle_id=v.id AND dt.kind='TRIP'
LEFT JOIN durations du ON du.vehicle_id=v.id AND du.kind='UNAVAILABLE'
LEFT JOIN durations dm ON dm.vehicle_id=v.id AND dm.kind='MAINTENANCE'
LEFT JOIN blocked b ON b.vehicle_id=v.id LEFT JOIN stats s ON s.vehicle_id=v.id
WHERE (@vehicle_id::uuid IS NULL OR v.id=@vehicle_id::uuid) ORDER BY v.vehicle_code
""";

    public async Task<IReadOnlyList<FleetVehicleUtilization>> Query(DateTime start, DateTime end, Guid? vehicleId, DateTime snapshot, CancellationToken ct)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand(Sql, connection);
        command.Parameters.AddWithValue("start", start); command.Parameters.AddWithValue("end", end); command.Parameters.AddWithValue("snapshot", snapshot);
        command.Parameters.Add(new NpgsqlParameter("vehicle_id", NpgsqlDbType.Uuid) { Value = vehicleId is null ? DBNull.Value : vehicleId.Value });
        var rows = new List<FleetVehicleUtilization>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) rows.Add(new(reader.GetGuid(0), reader.GetString(1), D(reader,2), D(reader,3), D(reader,4), D(reader,5), D(reader,6), D(reader,7), D(reader,8), reader.IsDBNull(9)?null:D(reader,9), reader.GetInt32(10), D(reader,11), reader.GetInt32(12)));
        return rows;
    }
    private static decimal D(NpgsqlDataReader reader, int index) => Convert.ToDecimal(reader.GetValue(index));
}
