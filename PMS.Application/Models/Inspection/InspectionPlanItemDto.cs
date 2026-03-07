namespace PMS.Application.Models.Inspection;

public class InspectionPlanItemDto
{
    public long Id { get; set; }
    public string HospitalName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string Inspector { get; set; } = string.Empty;
    public DateTime PlanDate { get; set; }
    public DateTime? ActualDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string InspectionType { get; set; } = string.Empty;
}

public class InspectionPlanUpsertDto
{
    public string GroupName { get; set; } = string.Empty;
    public string Inspector { get; set; } = string.Empty;
    public DateTime PlanDate { get; set; }
    public DateTime? ActualDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string InspectionType { get; set; } = string.Empty;
}
