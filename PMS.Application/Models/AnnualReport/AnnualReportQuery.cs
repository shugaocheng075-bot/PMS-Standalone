namespace PMS.Application.Models.AnnualReport;

public class AnnualReportQuery
{
    public string? HospitalName { get; set; }
    public string? ProductName { get; set; }
    public string? Status { get; set; }
    public int? ReportYear { get; set; }
    /// <summary>到期月份筛选，格式 YYYY-MM</summary>
    public string? DueMonth { get; set; }
    public string? GroupName { get; set; }
    public string? ServicePerson { get; set; }
    public string? Priority { get; set; }
    public string? Reviewer { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
}
