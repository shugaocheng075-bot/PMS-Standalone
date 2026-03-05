namespace PMS.Application.Models.Contract;

public class ContractAlertQuery
{
    public string? AlertLevel { get; set; }
    public string? ContractType { get; set; }
    public string? ContractValidityStatus { get; set; }
    public string? Province { get; set; }
    public string? GroupName { get; set; }
    public string? SalesName { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
}
