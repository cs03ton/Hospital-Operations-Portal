using System.ComponentModel.DataAnnotations;
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

[ApiController, Route("api/repairs"), Authorize, RepairActiveUser]
[RequireAnyPermission(RepairPermissions.ViewOwn, RepairPermissions.Create, RepairPermissions.WorkIT,
    RepairPermissions.WorkGeneral, RepairPermissions.ViewAll, RepairPermissions.Manage)]
public sealed class RepairsController(AppDbContext db, IDomainEventPublisher events) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private Task<RepairAccess> Access(CancellationToken ct) => RepairWorkflow.Access(db, Actor, ct);
    private static object OkData(object data) => ApiResponse<object>.Ok(data);
    private IQueryable<RepairRequest> Visible(RepairAccess a, string? scope)
    {
        var q = db.Set<RepairRequest>().AsNoTracking();
        if (scope == "mine") return q.Where(x => x.RequesterId == a.UserId && a.Permissions.Contains(RepairPermissions.ViewOwn));
        var all = a.Permissions.Contains(RepairPermissions.ViewAll);
        var it = a.Work("IT"); var general = a.Work("GENERAL");
        if (scope == "team") return q.Where(x => (it && x.TeamCode == "IT") || (general && x.TeamCode == "GENERAL") || all);
        return q.Where(x => all || (it && x.TeamCode == "IT") || (general && x.TeamCode == "GENERAL") ||
            (x.RequesterId == a.UserId && a.Permissions.Contains(RepairPermissions.ViewOwn)));
    }

    [HttpGet("options")]
    public async Task<object> Options(CancellationToken ct) => OkData(new
    {
        teams = await db.Set<RepairTeam>().AsNoTracking().OrderBy(x => x.Code).ToListAsync(ct),
        categories = await db.Set<RepairCategory>().AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(ct)
    });

    [HttpGet]
    public async Task<object> List([FromQuery] string scope = "mine", [FromQuery] string? status = null,
        [FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var q = Visible(await Access(ct), scope);
        if (!string.IsNullOrEmpty(status)) q = q.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim(); q = q.Where(x => x.Title.Contains(term) || x.Location.Contains(term)); }
        page = Math.Clamp(page, 1, 1_000_000); pageSize = Math.Clamp(pageSize, 1, 100);
        return OkData(new { total = await q.CountAsync(ct), page, pageSize,
            items = await q.OrderByDescending(x => x.UpdatedAt).ThenByDescending(x => x.Number).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct) });
    }

    [HttpGet("summary")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<object> Summary(CancellationToken ct)
    {
        var a = await Access(ct);
        return OkData(new {
            generatedAtUtc = DateTime.UtcNow,
            counts = await Visible(a, null).GroupBy(x => x.Status).Select(g => new { status = g.Key, count = g.Count() }).ToListAsync(ct),
            teamPending = await Visible(a, "team").CountAsync(x => RepairWorkflow.Active.Contains(x.Status), ct)
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken ct)
    {
        var a = await Access(ct);
        var r = await Visible(a, null).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return NotFound();
        var events = await db.Set<RepairEvent>().AsNoTracking().Where(x => x.RequestId == id).OrderBy(x => x.CreatedAt).ToListAsync(ct);
        var eventIds = events.Select(x => x.Id).ToArray();
        var contributors = await db.Set<RepairContributor>().AsNoTracking().Where(x => eventIds.Contains(x.EventId)).ToListAsync(ct);
        var ids = events.Select(x => x.ActorId).Concat(events.Where(x => x.SolverId != null).Select(x => x.SolverId!.Value))
            .Concat(contributors.Select(x => x.UserId)).Append(r.RequesterId).Distinct().ToArray();
        var actions = new[] { "start", "wait", "resume", "return", "priority", "note", "solve", "accept", "reject-solution", "reopen", "resubmit", "cancel" };
        return Ok(OkData(new { request = r, events, contributors,
            departmentName = await db.Departments.Where(x => x.Id == r.DepartmentId).Select(x => x.Name).SingleOrDefaultAsync(ct),
            categoryName = await db.Set<RepairCategory>().Where(x => x.Id == r.CategoryId).Select(x => x.Name).SingleAsync(ct),
            people = await db.Users.AsNoTracking().Where(x => ids.Contains(x.Id)).Select(x => new { x.Id, x.FullName }).ToListAsync(ct),
            rounds = await db.Set<RepairRound>().AsNoTracking().Where(x => x.RequestId == id).OrderBy(x => x.Number).ToListAsync(ct),
            waiting = await db.Set<RepairWaitingPeriod>().AsNoTracking().Where(x => x.RequestId == id).ToListAsync(ct),
            images = await db.Set<RepairImage>().AsNoTracking().Where(x => x.RequestId == id).Select(x => new { x.Id, x.EventId, x.CreatedAt }).ToListAsync(ct),
            actions = actions.Where(x => RepairWorkflow.Next(r, x, a) != null),
            canUpload = (a.Work(r.TeamCode) || a.UserId == r.RequesterId) && r.Status is not ("Closed" or "Cancelled")
        }));
    }

    [HttpGet("{id:guid}/solvers")]
    public async Task<IActionResult> Solvers(Guid id, CancellationToken ct)
    {
        var r = await db.Set<RepairRequest>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (r is null || !(await Access(ct)).Work(r.TeamCode)) return NotFound();
        var permission = r.TeamCode == "IT" ? RepairPermissions.WorkIT : RepairPermissions.WorkGeneral;
        return Ok(OkData(await db.Users.AsNoTracking().Where(x => x.IsActive && db.UserRoles.Any(ur => ur.UserId == x.Id &&
            ur.Role != null && ur.Role.IsActive && ur.Role.RolePermissions.Any(rp => rp.Permission != null && rp.Permission.IsActive && rp.Permission.Code == permission)))
            .OrderBy(x => x.FullName).Select(x => new { x.Id, x.FullName }).ToListAsync(ct)));
    }

    [HttpPost, RequirePermission(RepairPermissions.Create)]
    public async Task<IActionResult> Create(RepairInput input, CancellationToken ct)
    {
        var category = await db.Set<RepairCategory>().SingleOrDefaultAsync(x => x.Id == input.CategoryId && x.IsActive, ct);
        if (category is null) return BadRequest(ApiResponse<object>.Fail("กรุณาเลือกประเภทที่เปิดใช้งาน"));
        var user = await db.Users.AsNoTracking().SingleAsync(x => x.Id == Actor, ct);
        if (string.IsNullOrWhiteSpace(user.PhoneNumber))
            return BadRequest(ApiResponse<object>.Fail("กรุณาเพิ่มเบอร์โทรในข้อมูลส่วนตัวก่อนแจ้งซ่อม"));
        var r = new RepairRequest { RequesterId = Actor, DepartmentId = user.DepartmentId, CategoryId = category.Id, TeamCode = category.TeamCode };
        Apply(r, input);
        r.Contact = RequesterContact(user);
        db.Add(r);
        db.Add(new RepairRound { RequestId = r.Id, Number = 1 });
        var e = Event(r, "submit", "", $"ส่งแจ้งซ่อม · {category.Name} · ทีม {r.TeamCode}");
        await PublishRepair(e, r, ct);
        await db.SaveChangesAsync(ct);
        return Ok(OkData(r));
    }

    [HttpPost("{id:guid}/actions/{operation}")]
    public async Task<IActionResult> Change(Guid id, string operation, RepairAction input, CancellationToken ct)
    {
        var a = await Access(ct);
        var r = await db.Set<RepairRequest>().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (r is null || !a.View(r)) return NotFound();
        if (r.ConcurrencyToken != input.ConcurrencyToken) return Conflict(ApiResponse<object>.Fail("ข้อมูลเปลี่ยนแล้ว กรุณาโหลดใหม่"));
        var next = RepairWorkflow.Next(r, operation, a);
        if (next is null) return StatusCode(403, ApiResponse<object>.Fail("ไม่สามารถดำเนินการในสถานะนี้"));
        if (string.IsNullOrWhiteSpace(input.Note) && operation is not ("start" or "resume"))
            return BadRequest(ApiResponse<object>.Fail("กรุณาระบุรายละเอียดหรือเหตุผล"));
        if (operation == "priority" && input.Priority is not ("Normal" or "Urgent" or "Emergency"))
            return BadRequest(ApiResponse<object>.Fail("ความเร่งด่วนไม่ถูกต้อง"));
        if (operation == "resubmit")
        {
            if (input.Request is null) return BadRequest(ApiResponse<object>.Fail("กรุณาระบุข้อมูลแจ้งซ่อม"));
            var category = await db.Set<RepairCategory>().SingleOrDefaultAsync(x => x.Id == input.Request.CategoryId && x.IsActive, ct);
            if (category is null) return BadRequest(ApiResponse<object>.Fail("ประเภทไม่พร้อมใช้งาน"));
            var requester = await db.Users.AsNoTracking().SingleAsync(x => x.Id == r.RequesterId, ct);
            if (string.IsNullOrWhiteSpace(requester.PhoneNumber))
                return BadRequest(ApiResponse<object>.Fail("กรุณาเพิ่มเบอร์โทรในข้อมูลส่วนตัวก่อนส่งแจ้งซ่อมอีกครั้ง"));
            Apply(r, input.Request); r.Contact = RequesterContact(requester);
            r.CategoryId = category.Id; r.TeamCode = category.TeamCode; r.Priority = null;
        }
        var solverIds = (input.ContributorIds ?? []).Append(input.SolverId ?? Guid.Empty).Distinct().ToArray();
        if (operation == "solve")
        {
            if (solverIds.Length > 20 || solverIds.Contains(Guid.Empty)) return BadRequest(ApiResponse<object>.Fail("กรุณาระบุผู้แก้ไขหลัก"));
            var permission = r.TeamCode == "IT" ? RepairPermissions.WorkIT : RepairPermissions.WorkGeneral;
            var valid = await db.Users.CountAsync(x => solverIds.Contains(x.Id) && x.IsActive && db.UserRoles.Any(ur => ur.UserId == x.Id &&
                ur.Role != null && ur.Role.IsActive && ur.Role.RolePermissions.Any(rp => rp.Permission != null && rp.Permission.IsActive && rp.Permission.Code == permission)), ct);
            if (valid != solverIds.Length) return BadRequest(ApiResponse<object>.Fail("ผู้แก้ไขต้องเป็นสมาชิกที่เปิดใช้งานของทีม"));
        }
        var now = DateTime.UtcNow;
        var from = r.Status;
        if (from == "WaitingParts" && next != from)
        {
            var waiting = await db.Set<RepairWaitingPeriod>().Where(x => x.RequestId == id && x.EndedAt == null).ToListAsync(ct);
            foreach (var item in waiting) item.EndedAt = now;
        }
        if (operation == "wait") db.Add(new RepairWaitingPeriod { RequestId = id, Round = r.CurrentRound, StartedAt = now });
        if (operation == "start") r.HasStarted = true;
        if (operation == "reopen") { r.CurrentRound++; db.Add(new RepairRound { RequestId = id, Number = r.CurrentRound, StartedAt = now }); }
        if (operation == "priority") r.Priority = input.Priority;
        r.Status = next; r.UpdatedAt = now; r.ConcurrencyToken = Guid.NewGuid();
        var e = Event(r, operation, from, input.Note?.Trim() ?? "");
        if (operation == "solve")
        {
            e.SolverId = input.SolverId;
            foreach (var userId in solverIds.Where(x => x != input.SolverId)) db.Add(new RepairContributor { EventId = e.Id, UserId = userId });
        }
        if (operation is "accept" or "cancel")
        {
            var round = await db.Set<RepairRound>().SingleAsync(x => x.RequestId == id && x.Number == r.CurrentRound, ct);
            round.ClosedAt = now;
            if (operation == "accept") { round.AcceptedById = Actor; round.AcceptanceNote = input.Note; }
        }
        if (operation is "start" or "resume" or "reject-solution" or "solve" or "accept" or "resubmit" or "reopen")
        {
            await PublishRepair(e, r, ct);
        }
        if (operation is "return" or "solve" or "cancel")
            db.Notifications.Add(new Notification { Id = Guid.NewGuid(), UserId = r.RequesterId, Category = "Repair",
                Title = "อัปเดตงานแจ้งซ่อม", Message = $"งาน REP-{r.Number:D6} มีสถานะใหม่ กรุณาตรวจสอบรายละเอียด",
                ActionUrl = $"/repairs/{r.Id}", ReferenceEntity = "RepairRequest", ReferenceId = r.Id.ToString() });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Conflict(ApiResponse<object>.Fail("มีผู้ดำเนินการก่อนแล้ว กรุณาโหลดใหม่")); }
        return Ok(OkData(r));
    }

    private static void Apply(RepairRequest r, RepairInput x) { r.Title = x.Title.Trim(); r.Description = x.Description.Trim(); r.Location = x.Location.Trim(); }
    private static string RequesterContact(User user) => $"{user.FullName.Trim()} · {user.PhoneNumber!.Trim()}";
    private RepairEvent Event(RepairRequest r, string action, string from, string note)
    {
        var e = new RepairEvent { RequestId = r.Id, Round = r.CurrentRound, ActorId = Actor, Action = action, FromStatus = from, ToStatus = r.Status, Note = note, Priority = r.Priority };
        db.Add(e); return e;
    }
    private Task PublishRepair(RepairEvent e, RepairRequest r, CancellationToken ct)
    {
        var eventType = e.Action switch
        {
            "start" => "Repair.Started", "resume" or "reject-solution" => "Repair.Resumed",
            "solve" => "Repair.Solved", "accept" => "Repair.Closed", "reopen" => "Repair.Reopened",
            "resubmit" => "Repair.Resubmitted", _ => "Repair.Submitted"
        };
        return events.PublishAsync(new DomainEventEnvelope(eventType, "REPAIR", nameof(RepairRequest), r.Id, Actor,
            HttpContext.TraceIdentifier, new { r.Number, r.TeamCode, r.Status, e.Action, e.SolverId }, []), ct);
    }
}

public sealed record RepairInput(
    Guid CategoryId,
    [Required, MaxLength(200)] string Title,
    [Required, MaxLength(8000)] string Description,
    [Required, MaxLength(500)] string Location,
    [Required, MaxLength(300)] string Contact);
public sealed record RepairAction(Guid ConcurrencyToken, [MaxLength(8000)] string? Note,
    string? Priority = null, Guid? SolverId = null, Guid[]? ContributorIds = null, RepairInput? Request = null);
