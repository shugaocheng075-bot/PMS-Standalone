namespace PMS.Application.Models.Personnel;

public class PersonnelSummaryDto
{
    public int Total { get; set; }
    public int ServiceCount { get; set; }
    public int ImplementationCount { get; set; }
    public int OnsiteCount { get; set; }
}

public class PersonnelItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string RoleType { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsOnsite { get; set; }
    public int ProjectCount { get; set; }
    public int OverdueCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, string> SourceColumns { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class PersonnelUpsertDto
{
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string RoleType { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsOnsite { get; set; }
    public int ProjectCount { get; set; }
    public int OverdueCount { get; set; }
    public Dictionary<string, string>? SourceColumns { get; set; }
}

public class PersonnelExternalSyncResultDto
{
    public bool Success { get; set; }
    public bool Skipped { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int ParsedCount { get; set; }
    public int AddedCount { get; set; }
    public int UpdatedCount { get; set; }
    public DateTime AttemptedAt { get; set; }
    public DateTime? LastSuccessAt { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
}
