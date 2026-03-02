namespace PMS.Application.Models.AnnualReport;

public class AnnualReportSummaryDto
{
    public int NotStartedCount { get; set; }
    public int WritingCount { get; set; }
    public int SubmittedCount { get; set; }
    public int CompletedCount { get; set; }
    public int ThisYearCount { get; set; }
    public int Total { get; set; }
}
