using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Data;

public static class RepairModelConfiguration
{
    public static void ConfigureRepairs(this ModelBuilder model)
    {
        model.Entity<RepairTeam>().HasKey(x => x.Code);
        model.Entity<RepairCategory>().HasOne<RepairTeam>().WithMany().HasForeignKey(x => x.TeamCode).OnDelete(DeleteBehavior.Restrict);
        model.Entity<RepairCategory>().Property(x => x.ConcurrencyToken).IsConcurrencyToken();
        model.Entity<RepairCategory>().HasIndex(x => x.Name).IsUnique();
        var request = model.Entity<RepairRequest>();
        request.Property(x => x.Number).UseIdentityByDefaultColumn();
        request.HasIndex(x => x.Number).IsUnique();
        request.HasIndex(x => new { x.TeamCode, x.Status, x.CreatedAt });
        request.HasIndex(x => new { x.RequesterId, x.CreatedAt });
        request.Property(x => x.ConcurrencyToken).IsConcurrencyToken();
        request.HasOne<User>().WithMany().HasForeignKey(x => x.RequesterId).OnDelete(DeleteBehavior.Restrict);
        request.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        request.HasOne<RepairCategory>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        request.HasOne<RepairTeam>().WithMany().HasForeignKey(x => x.TeamCode).OnDelete(DeleteBehavior.Restrict);
        model.Entity<RepairRound>().HasIndex(x => new { x.RequestId, x.Number }).IsUnique();
        model.Entity<RepairRound>().HasOne<RepairRequest>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<RepairRound>().HasOne<User>().WithMany().HasForeignKey(x => x.AcceptedById).OnDelete(DeleteBehavior.Restrict);
        model.Entity<RepairEvent>().HasOne<RepairRequest>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<RepairEvent>().HasOne<User>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<RepairEvent>().HasOne<User>().WithMany().HasForeignKey(x => x.SolverId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<RepairEvent>().HasIndex(x => new { x.RequestId, x.CreatedAt });
        model.Entity<RepairContributor>().HasKey(x => new { x.EventId, x.UserId });
        model.Entity<RepairContributor>().HasOne<RepairEvent>().WithMany().HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<RepairContributor>().HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<RepairWaitingPeriod>().HasOne<RepairRequest>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<RepairImage>().HasOne<RepairEvent>().WithMany().HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<RepairImage>().HasOne<RepairRequest>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<RepairImage>().HasOne<User>().WithMany().HasForeignKey(x => x.UploadedById).OnDelete(DeleteBehavior.Restrict);
        model.Entity<RepairDispatch>().HasOne<RepairEvent>().WithMany().HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<RepairDispatch>().HasOne<RepairRequest>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<RepairDispatch>().HasOne<RepairTeam>().WithMany().HasForeignKey(x => x.TeamCode).OnDelete(DeleteBehavior.Restrict);
        model.Entity<RepairDispatch>().HasIndex(x => new { x.EventId, x.TeamCode }).IsUnique();
        model.Entity<RepairDispatch>().HasIndex(x => new { x.Status, x.AvailableAt });
        model.Entity<RepairDispatch>().Property(x => x.ConcurrencyToken).IsConcurrencyToken();
        var types = new (Type Type, string Table)[]
        {
            (typeof(RepairTeam), "repair_teams"), (typeof(RepairCategory), "repair_categories"),
            (typeof(RepairRequest), "repair_requests"), (typeof(RepairRound), "repair_rounds"),
            (typeof(RepairEvent), "repair_events"), (typeof(RepairContributor), "repair_contributors"),
            (typeof(RepairWaitingPeriod), "repair_waiting_periods"), (typeof(RepairImage), "repair_images"),
            (typeof(RepairDispatch), "repair_dispatches")
        };
        foreach (var (type, table) in types)
        {
            var entity = model.Entity(type);
            entity.ToTable(table);
            foreach (var property in entity.Metadata.GetProperties())
            {
                var snake = string.Concat(property.Name.Select((c, i) => char.IsUpper(c) && i > 0 ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
                property.SetColumnName(snake);
                if (property.ClrType == typeof(string)) property.SetMaxLength(property.Name is "Note" or "Description" or "AcceptanceNote" ? 8000 : 1000);
            }
        }
    }
}
