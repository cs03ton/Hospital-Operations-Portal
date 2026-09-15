namespace Hop.Api.Authorization;

public static class MeetingRoomPermissions
{
    public const string ViewCalendar = "MeetingRoom.Calendar.View";
    public const string ViewOwn = "MeetingRoom.Booking.ViewOwn";
    public const string Create = "MeetingRoom.Booking.Create";
    public const string ManageBookings = "MeetingRoom.Booking.Manage";
    public const string ManageRooms = "MeetingRoom.Room.Manage";
    public static readonly string[] Basic = [ViewCalendar, ViewOwn, Create];
    public static readonly string[] All = [ViewCalendar, ViewOwn, Create, ManageBookings, ManageRooms];
}
