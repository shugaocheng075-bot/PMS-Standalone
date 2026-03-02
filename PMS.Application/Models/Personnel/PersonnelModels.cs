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
}
