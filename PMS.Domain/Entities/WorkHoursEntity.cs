namespace PMS.Domain.Entities;

public class WorkHoursEntity
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public string PersonnelName { get; set; } = string.Empty;
    public string HospitalName { get; set; } = string.Empty;
    public string WorkDate { get; set; } = string.Empty; // yyyy-MM-dd
    public decimal Hours { get; set; }
    public string WorkType { get; set; } = string.Empty; // 驻场/远程/出差
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
