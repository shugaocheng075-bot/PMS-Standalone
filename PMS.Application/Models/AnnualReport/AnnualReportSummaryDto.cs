namespace PMS.Application.Models.AnnualReport;

public class AnnualReportSummaryDto
{
    public int NotStartedCount { get; set; }
    public int WritingCount { get; set; }
    public int SubmittedCount { get; set; }
    public int CompletedCount { get; set; }
    public int ThisYearCount { get; set; }
    /// <summary>本月到期需写年报的数量</summary>
    public int DueThisMonthCount { get; set; }
    /// <summary>逾期待处理：到期月份 ≤ 当前月 且 状态 ≠ 已完成</summary>
    public int OverdueCount { get; set; }
    public int Total { get; set; }
}
