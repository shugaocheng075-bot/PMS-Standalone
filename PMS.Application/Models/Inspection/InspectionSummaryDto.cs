namespace PMS.Application.Models.Inspection;

public class InspectionSummaryDto
{
    public int PlannedCount { get; set; }
    public int InProgressCount { get; set; }
    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }
    public int ThisMonthCount { get; set; }
    public int Total { get; set; }
}
