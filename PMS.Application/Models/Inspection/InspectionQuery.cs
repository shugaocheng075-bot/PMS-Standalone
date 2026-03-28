namespace PMS.Application.Models.Inspection;

public class InspectionQuery
{
    public string? HospitalName { get; set; }
    public string? Status { get; set; }
    public string? Province { get; set; }
    public string? ProductName { get; set; }
    public string? GroupName { get; set; }
    public string? Inspector { get; set; }
    public string? InspectionType { get; set; }
    public string? Priority { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
}
