namespace PMS.Domain.Entities;

public class ProjectEntity
{
    public long Id { get; set; }
    public string HospitalName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string HospitalLevel { get; set; } = string.Empty;
    public string ContractStatus { get; set; } = string.Empty;
    public decimal MaintenanceAmount { get; set; }
    public int OverdueDays { get; set; }
}
