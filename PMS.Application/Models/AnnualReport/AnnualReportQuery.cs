namespace PMS.Application.Models.AnnualReport;

public class AnnualReportQuery
{
    public string? Status { get; set; }
    public int? ReportYear { get; set; }
    /// <summary>到期月份筛选，格式 YYYY-MM</summary>
    public string? DueMonth { get; set; }
    public string? GroupName { get; set; }
    public string? ServicePerson { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
}
