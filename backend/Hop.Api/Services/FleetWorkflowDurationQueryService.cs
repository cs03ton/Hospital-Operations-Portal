using System.Data;
using Hop.Api.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Hop.Api.Services;

public sealed record FleetDurationStatistic(decimal? AverageMinutes, int SampleCount, decimal? MinimumMinutes, decimal? MaximumMinutes, decimal? P90Minutes);

public sealed class FleetWorkflowDurationQueryService(AppDbContext db)
{
    private const string Sql = """
WITH selected AS (
 SELECT r.id,r.created_at,r.submitted_at,
   max(h.created_at) FILTER (WHERE h.to_status='PENDING_DISPATCH') pending_dispatch,
   max(h.created_at) FILTER (WHERE h.to_status='PENDING_ADMIN_REVIEW') pending_admin,
   max(h.created_at) FILTER (WHERE h.to_status='PENDING_DIRECTOR') pending_director,
   max(h.created_at) FILTER (WHERE h.to_status IN ('APPROVED','PENDING_DRIVER_ACK')) director_done,
   max(h.created_at) FILTER (WHERE h.to_status='READY') ready_at,
   max(h.created_at) FILTER (WHERE h.to_status='IN_PROGRESS') in_progress_at,
   max(h.created_at) FILTER (WHERE h.to_status IN ('COMPLETED','ABORTED')) terminal_at
 FROM fleet_requests r LEFT JOIN fleet_request_status_histories h ON h.fleet_request_id=r.id
 WHERE r.created_at>=@start AND r.created_at<@end GROUP BY r.id
), values AS (
 SELECT id,'draftToSubmit' metric,extract(epoch FROM (submitted_at-created_at))/60.0 minutes FROM selected WHERE submitted_at>=created_at
 UNION ALL SELECT id,'dispatch',extract(epoch FROM (pending_admin-pending_dispatch))/60.0 FROM selected WHERE pending_admin>=pending_dispatch
 UNION ALL SELECT id,'adminReview',extract(epoch FROM (pending_director-pending_admin))/60.0 FROM selected WHERE pending_director>=pending_admin
 UNION ALL SELECT id,'directorApproval',extract(epoch FROM (director_done-pending_director))/60.0 FROM selected WHERE director_done>=pending_director
 UNION ALL SELECT id,'driverAcknowledgement',extract(epoch FROM (ready_at-director_done))/60.0 FROM selected WHERE ready_at>=director_done
 UNION ALL SELECT id,'waitingBeforeTrip',extract(epoch FROM (in_progress_at-ready_at))/60.0 FROM selected WHERE in_progress_at>=ready_at
 UNION ALL SELECT id,'tripDuration',extract(epoch FROM (terminal_at-in_progress_at))/60.0 FROM selected WHERE terminal_at>=in_progress_at
 UNION ALL SELECT id,'totalLeadTime',extract(epoch FROM (terminal_at-created_at))/60.0 FROM selected WHERE terminal_at>=created_at
 UNION ALL SELECT c.id,'cancellationResolution',extract(epoch FROM (coalesce(c.completed_at,c.reviewed_at)-c.created_at))/60.0 FROM fleet_cancellation_requests c WHERE c.created_at>=@start AND c.created_at<@end AND coalesce(c.completed_at,c.reviewed_at)>=c.created_at
 UNION ALL SELECT a.id,'assignmentReplacement',extract(epoch FROM (a.created_at-old.created_at))/60.0 FROM fleet_assignments a JOIN fleet_assignments old ON old.id=a.replaced_assignment_id WHERE a.created_at>=@start AND a.created_at<@end AND a.created_at>=old.created_at
)
SELECT metric,avg(minutes),count(*)::int,min(minutes),max(minutes),percentile_cont(0.9) within group(order by minutes) FROM values GROUP BY metric
""";
    public async Task<IReadOnlyDictionary<string,FleetDurationStatistic>> Query(DateTime start, DateTime end, CancellationToken ct)
    {
        var connection=(NpgsqlConnection)db.Database.GetDbConnection(); if(connection.State!=ConnectionState.Open) await connection.OpenAsync(ct);
        await using var command=new NpgsqlCommand(Sql,connection); command.Parameters.AddWithValue("start",start); command.Parameters.AddWithValue("end",end);
        var result=new Dictionary<string,FleetDurationStatistic>(); await using var reader=await command.ExecuteReaderAsync(ct);
        while(await reader.ReadAsync(ct)) result[reader.GetString(0)]=new(reader.IsDBNull(1)?null:Convert.ToDecimal(reader.GetValue(1)),reader.GetInt32(2),reader.IsDBNull(3)?null:Convert.ToDecimal(reader.GetValue(3)),reader.IsDBNull(4)?null:Convert.ToDecimal(reader.GetValue(4)),reader.IsDBNull(5)?null:Convert.ToDecimal(reader.GetValue(5)));
        return result;
    }
}
