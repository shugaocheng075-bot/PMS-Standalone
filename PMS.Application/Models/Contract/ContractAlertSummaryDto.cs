namespace PMS.Application.Models.Contract;

public class ContractAlertSummaryDto
{
    public int ReminderCount { get; set; }
    public int WarningCount { get; set; }
    public int CriticalCount { get; set; }
    public int Total { get; set; }
}
