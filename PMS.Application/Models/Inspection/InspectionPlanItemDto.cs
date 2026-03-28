namespace PMS.Application.Models.Inspection;

public class InspectionPlanItemDto
{
    public long Id { get; set; }
    public string HospitalName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string HospitalLevel { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string Inspector { get; set; } = string.Empty;
    public DateTime PlanDate { get; set; }
    public DateTime? ActualDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string InspectionType { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
}

public class InspectionPlanUpsertDto
{
    public string? HospitalName { get; set; }
    public string? ProductName { get; set; }
    public string? Province { get; set; }
    public string? HospitalLevel { get; set; }
    public string? GroupName { get; set; }
    public string? Inspector { get; set; }
    public DateTime? PlanDate { get; set; }
    public DateTime? ActualDate { get; set; }
    public string? Status { get; set; }
    public string? InspectionType { get; set; }
    public string? Priority { get; set; }
    public string? Remarks { get; set; }
}
