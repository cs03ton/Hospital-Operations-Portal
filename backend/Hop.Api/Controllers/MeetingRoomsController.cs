using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;
using Hop.Api.Authorization;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Controllers;

[ApiController, Authorize, Route("api/meeting-rooms")]
public sealed class MeetingRoomsController(AppDbContext db, MeetingRoomAttachmentStorage storage) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("rooms"), RequirePermission(MeetingRoomPermissions.ViewCalendar)]
    public async Task<object> Rooms([FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        var manage = await Has(MeetingRoomPermissions.ManageRooms, ct);
        var query = db.MeetingRooms.AsNoTracking();
        if (!includeInactive || !manage) query = query.Where(x => x.IsActive);
        return ApiResponse<object>.Ok(await query.OrderBy(x => x.Name).ToListAsync(ct));
    }

    [HttpGet("calendar"), RequirePermission(MeetingRoomPermissions.ViewCalendar), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Calendar([FromQuery] DateOnly start, [FromQuery] DateOnly end, [FromQuery] Guid? roomId, [FromQuery] Guid? departmentId, CancellationToken ct)
    {
        if (start == default || end == default || end < start || end.DayNumber - start.DayNumber > 366) return BadRequest(ApiResponse<object>.Fail("ช่วงวันที่ไม่ถูกต้องหรือเกิน 366 วัน"));
        var from = BangkokUtc(start, TimeOnly.MinValue); var to = BangkokUtc(end.AddDays(1), TimeOnly.MinValue);
        var query = db.MeetingRoomBookings.AsNoTracking().Where(x => x.StartAt < to && x.EndAt > from);
        if (roomId.HasValue) query = query.Where(x => x.RoomId == roomId); if (departmentId.HasValue) query = query.Where(x => x.DepartmentId == departmentId);
        var rows = await Project(query.OrderBy(x => x.StartAt).ThenBy(x => x.Number)).ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(rows));
    }

    [HttpGet("bookings"), RequireAnyPermission(MeetingRoomPermissions.ViewOwn, MeetingRoomPermissions.ManageBookings)]
    public async Task<object> Bookings([FromQuery] string scope = "mine", [FromQuery] string? search = null, [FromQuery] string? status = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var manage = await Has(MeetingRoomPermissions.ManageBookings, ct);
        var query = db.MeetingRoomBookings.AsNoTracking();
        if (!manage || !string.Equals(scope, "all", StringComparison.OrdinalIgnoreCase)) query = query.Where(x => x.BookerId == Actor);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim(); query = query.Where(x => x.Subject.Contains(term) || x.Purpose.Contains(term)); }
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100); var total = await query.CountAsync(ct);
        var items = await Project(query
            .OrderByDescending(x => x.StartAt)
            .ThenByDescending(x => x.Number)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)).ToListAsync(ct);
        return ApiResponse<object>.Ok(new { items, total, page, pageSize });
    }

    [HttpGet("bookings/{id:guid}"), RequirePermission(MeetingRoomPermissions.ViewCalendar)]
    public async Task<IActionResult> Detail(Guid id, CancellationToken ct)
    {
        var row = await Project(db.MeetingRoomBookings.AsNoTracking().Where(x => x.Id == id)).SingleOrDefaultAsync(ct);
        if (row is null) return NotFound();
        var history = await db.MeetingRoomBookingHistories.AsNoTracking().Where(x => x.BookingId == id).Join(db.Users, x => x.ActorId, x => x.Id, (x, u) => new { x.Id, x.Action, x.FromStatus, x.ToStatus, x.Detail, x.CreatedAt, ActorName = u.FullName }).OrderBy(x => x.CreatedAt).ToListAsync(ct);
        var attachments = await db.MeetingRoomAttachments.AsNoTracking().Where(x => x.BookingId == id).Select(x => new { x.Id, x.OriginalFileName, x.ContentType, x.FileSize, x.CreatedAt }).ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { booking = row, history, attachments }));
    }

    [HttpPost("bookings"), RequirePermission(MeetingRoomPermissions.Create)]
    public async Task<IActionResult> Create(MeetingBookingInput input, CancellationToken ct)
    {
        var actor = await db.Users.AsNoTracking().SingleAsync(x => x.Id == Actor && x.IsActive, ct);
        var validation = Validate(input); if (validation is not null) return BadRequest(ApiResponse<object>.Fail(validation));
        var times = ResolveTimes(input); if (times.End <= DateTime.UtcNow) return BadRequest(ApiResponse<object>.Fail("ไม่สามารถจองช่วงเวลาที่สิ้นสุดแล้ว"));
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        await LockRoom(input.RoomId, ct);
        var room = await db.MeetingRooms.SingleOrDefaultAsync(x => x.Id == input.RoomId && x.IsActive, ct);
        if (room is null) return BadRequest(ApiResponse<object>.Fail("ห้องประชุมไม่พร้อมใช้งาน"));
        if (input.AttendeeCount > room.Capacity) return BadRequest(ApiResponse<object>.Fail($"ห้องรองรับได้สูงสุด {room.Capacity} คน"));
        if (await Overlaps(input.RoomId, times.Start, times.End, null, ct)) return Conflict(ApiResponse<object>.Fail("ห้องประชุมถูกจองในช่วงเวลานี้แล้ว"));
        var row = new MeetingRoomBooking { RoomId = input.RoomId, BookerId = Actor, DepartmentId = actor.DepartmentId };
        Apply(row, input, times); db.Add(row); AddHistory(row, "Created", "", "Confirmed", null);
        await db.SaveChangesAsync(ct);
        await NotifyManagers(row, actor.FullName, ct); Audit("MeetingRoom.BookingCreated", row.Id, null); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return Ok(ApiResponse<object>.Ok(row));
    }

    [HttpPut("bookings/{id:guid}"), RequirePermission(MeetingRoomPermissions.ManageBookings)]
    public async Task<IActionResult> Update(Guid id, MeetingBookingUpdateInput input, CancellationToken ct)
    {
        var validation = Validate(input); if (validation is not null) return BadRequest(ApiResponse<object>.Fail(validation)); var times = ResolveTimes(input);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct); await LockRoom(input.RoomId, ct);
        var row = await db.MeetingRoomBookings.SingleOrDefaultAsync(x => x.Id == id, ct); if (row is null) return NotFound();
        if (row.ConcurrencyToken != input.ConcurrencyToken) return Conflict(ApiResponse<object>.Fail("ข้อมูลเปลี่ยนแล้ว กรุณาโหลดใหม่"));
        if (row.Status == "Cancelled") return Conflict(ApiResponse<object>.Fail("รายการถูกยกเลิกแล้ว"));
        var room = await db.MeetingRooms.SingleOrDefaultAsync(x => x.Id == input.RoomId && x.IsActive, ct);
        if (room is null || input.AttendeeCount > room.Capacity) return BadRequest(ApiResponse<object>.Fail(room is null ? "ห้องประชุมไม่พร้อมใช้งาน" : $"ห้องรองรับได้สูงสุด {room.Capacity} คน"));
        if (await Overlaps(input.RoomId, times.Start, times.End, id, ct)) return Conflict(ApiResponse<object>.Fail("ห้องประชุมถูกจองในช่วงเวลานี้แล้ว"));
        Apply(row, input, times); row.ConcurrencyToken = Guid.NewGuid(); row.UpdatedAt = DateTime.UtcNow; AddHistory(row, "Updated", row.Status, row.Status, null);
        NotifyBooker(row, "รายการจองห้องประชุมถูกแก้ไข", $"รายการ MR-{row.Number:D6} ถูกปรับข้อมูลโดยผู้ดูแล"); Audit("MeetingRoom.BookingUpdated", id, null);
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return Ok(ApiResponse<object>.Ok(row));
    }

    [HttpPost("bookings/{id:guid}/cancel"), RequirePermission(MeetingRoomPermissions.ManageBookings)]
    public async Task<IActionResult> Cancel(Guid id, MeetingCancelInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Reason)) return BadRequest(ApiResponse<object>.Fail("กรุณาระบุเหตุผลยกเลิก"));
        var row = await db.MeetingRoomBookings.SingleOrDefaultAsync(x => x.Id == id, ct); if (row is null) return NotFound();
        if (row.ConcurrencyToken != input.ConcurrencyToken) return Conflict(ApiResponse<object>.Fail("ข้อมูลเปลี่ยนแล้ว กรุณาโหลดใหม่"));
        if (row.Status == "Cancelled") return Conflict(ApiResponse<object>.Fail("รายการถูกยกเลิกแล้ว"));
        row.Status = "Cancelled"; row.CancellationReason = input.Reason.Trim(); row.CancelledAt = row.UpdatedAt = DateTime.UtcNow; row.CancelledById = Actor; row.ConcurrencyToken = Guid.NewGuid();
        AddHistory(row, "Cancelled", "Confirmed", "Cancelled", row.CancellationReason); NotifyBooker(row, "รายการจองห้องประชุมถูกยกเลิก", $"รายการ MR-{row.Number:D6} ถูกยกเลิก: {row.CancellationReason}"); Audit("MeetingRoom.BookingCancelled", id, row.CancellationReason);
        await db.SaveChangesAsync(ct); return Ok(ApiResponse<object>.Ok(row));
    }

    [HttpPost("rooms"), RequirePermission(MeetingRoomPermissions.ManageRooms)]
    public async Task<IActionResult> SaveRoom(MeetingRoomInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Code) || string.IsNullOrWhiteSpace(input.Name) || input.Capacity < 1) return BadRequest(ApiResponse<object>.Fail("กรุณากรอกข้อมูลห้องและความจุให้ครบ"));
        var row = input.Id.HasValue ? await db.MeetingRooms.FindAsync([input.Id.Value], ct) : new MeetingRoom(); if (row is null) return NotFound();
        if (input.Id.HasValue && row.ConcurrencyToken != input.ConcurrencyToken) return Conflict(ApiResponse<object>.Fail("ข้อมูลห้องเปลี่ยนแล้ว กรุณาโหลดใหม่"));
        if (await db.MeetingRooms.AnyAsync(x => x.Code == input.Code.Trim() && x.Id != row.Id, ct)) return Conflict(ApiResponse<object>.Fail("รหัสห้องซ้ำ"));
        if (!input.Id.HasValue) db.Add(row); row.Code = input.Code.Trim(); row.Name = input.Name.Trim(); row.Location = input.Location.Trim(); row.Capacity = input.Capacity; row.IsActive = input.IsActive; row.UpdatedAt = DateTime.UtcNow; row.ConcurrencyToken = Guid.NewGuid();
        Audit("MeetingRoom.RoomSaved", row.Id, null); await db.SaveChangesAsync(ct); return Ok(ApiResponse<object>.Ok(row));
    }

    [HttpPost("bookings/{id:guid}/attachments"), RequirePermission(MeetingRoomPermissions.Create)]
    public async Task<IActionResult> Upload(Guid id, [FromForm] List<IFormFile> files, CancellationToken ct)
    {
        var booking = await db.MeetingRoomBookings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct); if (booking is null) return NotFound();
        if (booking.BookerId != Actor && !await Has(MeetingRoomPermissions.ManageBookings, ct)) return Forbid();
        var existing = await db.MeetingRoomAttachments.CountAsync(x => x.BookingId == id, ct); if (files.Count == 0 || existing + files.Count > 5) return BadRequest(ApiResponse<object>.Fail("แนบเอกสารได้สูงสุด 5 ไฟล์"));
        try { foreach (var file in files) db.Add(await storage.SaveAsync(id, Actor, file, ct)); await db.SaveChangesAsync(ct); }
        catch (ArgumentException ex) { return BadRequest(ApiResponse<object>.Fail(ex.Message)); }
        return Ok(ApiResponse<object>.Ok(new { count = files.Count }));
    }

    [HttpGet("attachments/{id:guid}"), RequirePermission(MeetingRoomPermissions.ViewCalendar)]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var row = await db.MeetingRoomAttachments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct); if (row is null) return NotFound(); var file = storage.Get(row); if (!file.Exists) return NotFound();
        return PhysicalFile(file.FullName, row.ContentType, row.OriginalFileName, enableRangeProcessing: true);
    }

    private IQueryable<MeetingBookingDto> Project(IQueryable<MeetingRoomBooking> query) => query.Select(x => new MeetingBookingDto(x.Id, x.Number, x.RoomId, db.MeetingRooms.Where(r => r.Id == x.RoomId).Select(r => r.Name).First(), x.BookerId, db.Users.Where(u => u.Id == x.BookerId).Select(u => u.FullName).First(), x.DepartmentId, db.Departments.Where(d => d.Id == x.DepartmentId).Select(d => d.Name).FirstOrDefault(), x.Subject, x.Purpose, x.StartAt, x.EndAt, x.AttendeeCount, x.MeetingLink, x.AdditionalRequest, x.Status, x.CancellationReason, x.ConcurrencyToken, x.CreatedAt, x.UpdatedAt));
    private async Task<bool> Has(string permission, CancellationToken ct) => await db.UserRoles.Where(x => x.UserId == Actor && x.Role != null && x.Role.IsActive).SelectMany(x => x.Role!.RolePermissions).AnyAsync(x => x.Permission != null && x.Permission.IsActive && x.Permission.Code == permission, ct);
    private async Task LockRoom(Guid roomId, CancellationToken ct) => await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({roomId.ToString()}, 0))", ct);
    private Task<bool> Overlaps(Guid roomId, DateTime start, DateTime end, Guid? except, CancellationToken ct) => db.MeetingRoomBookings.AnyAsync(x => x.RoomId == roomId && x.Status == "Confirmed" && x.Id != except && x.StartAt < end && x.EndAt > start, ct);
    private static string? Validate(MeetingBookingInput input) { if (input.RoomId == Guid.Empty || string.IsNullOrWhiteSpace(input.Subject) || string.IsNullOrWhiteSpace(input.Purpose) || input.AttendeeCount < 1) return "กรุณากรอกข้อมูลการประชุมให้ครบ"; if (input.EndTime <= input.StartTime) return "เวลาสิ้นสุดต้องอยู่หลังเวลาเริ่ม"; if (!string.IsNullOrWhiteSpace(input.MeetingLink) && (!Uri.TryCreate(input.MeetingLink, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || uri.UserInfo != "")) return "Link ประชุมต้องเป็น HTTPS ที่ถูกต้อง"; return null; }
    private static (DateTime Start, DateTime End) ResolveTimes(MeetingBookingInput input) => (BangkokUtc(input.Date, input.StartTime), BangkokUtc(input.Date, input.EndTime));
    private static DateTime BangkokUtc(DateOnly date, TimeOnly time) { TimeZoneInfo zone; try { zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok"); } catch { zone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); } return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(date.ToDateTime(time), DateTimeKind.Unspecified), zone); }
    private static void Apply(MeetingRoomBooking row, MeetingBookingInput input, (DateTime Start, DateTime End) times) { row.RoomId = input.RoomId; row.Subject = input.Subject.Trim(); row.Purpose = input.Purpose.Trim(); row.StartAt = times.Start; row.EndAt = times.End; row.AttendeeCount = input.AttendeeCount; row.MeetingLink = string.IsNullOrWhiteSpace(input.MeetingLink) ? null : input.MeetingLink.Trim(); row.AdditionalRequest = string.IsNullOrWhiteSpace(input.AdditionalRequest) ? null : input.AdditionalRequest.Trim(); }
    private void AddHistory(MeetingRoomBooking row, string action, string from, string to, string? detail) => db.Add(new MeetingRoomBookingHistory { BookingId = row.Id, ActorId = Actor, Action = action, FromStatus = from, ToStatus = to, Detail = detail });
    private async Task NotifyManagers(MeetingRoomBooking row, string booker, CancellationToken ct) { var ids = await db.UserRoles.Where(x => x.Role != null && x.Role.IsActive && x.Role.RolePermissions.Any(rp => rp.Permission != null && rp.Permission.IsActive && rp.Permission.Code == MeetingRoomPermissions.ManageBookings)).Select(x => x.UserId).Distinct().ToListAsync(ct); foreach (var id in ids.Where(x => x != Actor)) db.Notifications.Add(Notification(id, row, "มีการจองห้องประชุมใหม่", $"{booker} จองห้องประชุมรายการ MR-{row.Number:D6}")); }
    private void NotifyBooker(MeetingRoomBooking row, string title, string message) { if (row.BookerId != Actor) db.Notifications.Add(Notification(row.BookerId, row, title, message)); }
    private static Notification Notification(Guid userId, MeetingRoomBooking row, string title, string message) => new() { Id = Guid.NewGuid(), UserId = userId, Category = "MeetingRoom", Title = title, Message = message, ActionUrl = $"/meeting-rooms/bookings/{row.Id}", ReferenceEntity = "MeetingRoomBooking", ReferenceId = row.Id.ToString() };
    private void Audit(string action, Guid id, string? reason) => db.AuditLogs.Add(new AuditLog { UserId = Actor, EffectiveActorUserId = Actor, Action = action, EntityName = "MeetingRoomBooking", EntityId = id.ToString(), Reason = reason, CorrelationId = HttpContext.TraceIdentifier });
}

public record MeetingBookingInput(Guid RoomId, DateOnly Date, TimeOnly StartTime, TimeOnly EndTime, [Required, MaxLength(300)] string Subject, [Required, MaxLength(4000)] string Purpose, [Range(1, 10000)] int AttendeeCount, [MaxLength(1000)] string? MeetingLink, [MaxLength(2000)] string? AdditionalRequest);
public record MeetingBookingUpdateInput(Guid RoomId, DateOnly Date, TimeOnly StartTime, TimeOnly EndTime, string Subject, string Purpose, int AttendeeCount, string? MeetingLink, string? AdditionalRequest, Guid ConcurrencyToken) : MeetingBookingInput(RoomId, Date, StartTime, EndTime, Subject, Purpose, AttendeeCount, MeetingLink, AdditionalRequest);
public record MeetingCancelInput(Guid ConcurrencyToken, [Required, MaxLength(2000)] string Reason);
public record MeetingRoomInput(Guid? Id, [Required] string Code, [Required] string Name, string Location, int Capacity, bool IsActive, Guid? ConcurrencyToken);
public record MeetingBookingDto(Guid Id, long Number, Guid RoomId, string RoomName, Guid BookerId, string BookerName, Guid? DepartmentId, string? DepartmentName, string Subject, string Purpose, DateTime StartAt, DateTime EndAt, int AttendeeCount, string? MeetingLink, string? AdditionalRequest, string Status, string? CancellationReason, Guid ConcurrencyToken, DateTime CreatedAt, DateTime UpdatedAt);
