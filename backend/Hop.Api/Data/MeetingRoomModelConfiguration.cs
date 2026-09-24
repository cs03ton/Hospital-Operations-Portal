using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Data;

public static class MeetingRoomModelConfiguration
{
    public static void ConfigureMeetingRooms(this ModelBuilder model)
    {
        var room = model.Entity<MeetingRoom>();
        room.ToTable("meeting_rooms"); room.HasKey(x => x.Id); room.HasIndex(x => x.Code).IsUnique();
        room.Property(x => x.Code).HasMaxLength(50); room.Property(x => x.Name).HasMaxLength(200); room.Property(x => x.Location).HasMaxLength(500);
        room.Property(x => x.PhotoPath).HasMaxLength(1000); room.Property(x => x.PhotoContentType).HasMaxLength(100);
        room.Property(x => x.ConcurrencyToken).IsConcurrencyToken();

        var booking = model.Entity<MeetingRoomBooking>();
        booking.ToTable("meeting_room_bookings"); booking.HasKey(x => x.Id);
        booking.Property(x => x.Number).UseIdentityByDefaultColumn(); booking.HasIndex(x => x.Number).IsUnique();
        booking.HasIndex(x => new { x.RoomId, x.StartAt, x.EndAt }); booking.HasIndex(x => new { x.BookerId, x.StartAt });
        booking.Property(x => x.Subject).HasMaxLength(300); booking.Property(x => x.Purpose).HasMaxLength(4000);
        booking.Property(x => x.MeetingLink).HasMaxLength(1000); booking.Property(x => x.AdditionalRequest).HasMaxLength(2000);
        booking.Property(x => x.Status).HasMaxLength(30); booking.Property(x => x.CancellationReason).HasMaxLength(2000);
        booking.Property(x => x.ConcurrencyToken).IsConcurrencyToken();
        booking.HasOne<MeetingRoom>().WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);
        booking.HasOne<User>().WithMany().HasForeignKey(x => x.BookerId).OnDelete(DeleteBehavior.Restrict);
        booking.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        booking.HasOne<User>().WithMany().HasForeignKey(x => x.CancelledById).OnDelete(DeleteBehavior.Restrict);

        var attendee = model.Entity<MeetingRoomBookingAttendee>();
        attendee.ToTable("meeting_room_booking_attendees"); attendee.HasKey(x => new { x.BookingId, x.UserId });
        attendee.HasIndex(x => x.UserId);
        attendee.HasOne<MeetingRoomBooking>().WithMany().HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Cascade);
        attendee.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);

        var history = model.Entity<MeetingRoomBookingHistory>();
        history.ToTable("meeting_room_booking_histories"); history.HasKey(x => x.Id);
        history.Property(x => x.Action).HasMaxLength(50); history.Property(x => x.FromStatus).HasMaxLength(30);
        history.Property(x => x.ToStatus).HasMaxLength(30); history.Property(x => x.Detail).HasMaxLength(4000);
        history.HasIndex(x => new { x.BookingId, x.CreatedAt });
        history.HasOne<MeetingRoomBooking>().WithMany().HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Restrict);
        history.HasOne<User>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);

        var attachment = model.Entity<MeetingRoomAttachment>();
        attachment.ToTable("meeting_room_attachments"); attachment.HasKey(x => x.Id);
        attachment.Property(x => x.OriginalFileName).HasMaxLength(500); attachment.Property(x => x.StoredPath).HasMaxLength(1000);
        attachment.Property(x => x.ContentType).HasMaxLength(200); attachment.HasIndex(x => new { x.BookingId, x.CreatedAt });
        attachment.HasOne<MeetingRoomBooking>().WithMany().HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Restrict);
        attachment.HasOne<User>().WithMany().HasForeignKey(x => x.UploadedById).OnDelete(DeleteBehavior.Restrict);

        foreach (var type in new[] { typeof(MeetingRoom), typeof(MeetingRoomBooking), typeof(MeetingRoomBookingAttendee), typeof(MeetingRoomBookingHistory), typeof(MeetingRoomAttachment) })
            foreach (var property in model.Entity(type).Metadata.GetProperties())
                property.SetColumnName(string.Concat(property.Name.Select((c, i) => char.IsUpper(c) && i > 0 ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString())));
    }
}
