namespace PMS.Application.Models.Contract;

public class ContractAlertItemDto
{
    public long ProjectId { get; set; }
    public string ContractType { get; set; } = string.Empty;
    public string HospitalName { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string SalesName { get; set; } = string.Empty;
    public string ContractStatus { get; set; } = string.Empty;
    public string ContractValidityStatus { get; set; } = string.Empty;
    public decimal MaintenanceAmount { get; set; }
    public int OverdueDays { get; set; }
    public string AlertLevel { get; set; } = string.Empty;
}
