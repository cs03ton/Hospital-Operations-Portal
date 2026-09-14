namespace Hop.Api.Models;

public class RepairTeam
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string ResponsibleDepartment { get; set; } = "";
}

public class RepairCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string TeamCode { get; set; } = "IT";
    public bool IsActive { get; set; } = true;
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
}

public class RepairRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long Number { get; set; }
    public Guid RequesterId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid CategoryId { get; set; }
    public string TeamCode { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Location { get; set; } = "";
    public string Contact { get; set; } = "";
    public string Status { get; set; } = "Submitted";
    public string? Priority { get; set; }
    public int CurrentRound { get; set; } = 1;
    public bool HasStarted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
}

public class RepairRound
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequestId { get; set; }
    public int Number { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    public Guid? AcceptedById { get; set; }
    public string? AcceptanceNote { get; set; }
}

// Append-only events preserve every repair submission, including rejected solutions.
public class RepairEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequestId { get; set; }
    public int Round { get; set; }
    public Guid ActorId { get; set; }
    public string Action { get; set; } = "";
    public string FromStatus { get; set; } = "";
    public string ToStatus { get; set; } = "";
    public string Note { get; set; } = "";
    public Guid? SolverId { get; set; }
    public string? Priority { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class RepairContributor
{
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
}

public class RepairWaitingPeriod
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequestId { get; set; }
    public int Round { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
}

public class RepairImage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequestId { get; set; }
    public Guid EventId { get; set; }
    public Guid UploadedById { get; set; }
    public string StoredPath { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class RepairDispatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public Guid RequestId { get; set; }
    public string TeamCode { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public int Attempts { get; set; }
    public DateTime AvailableAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public string? ErrorCode { get; set; }
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
}
