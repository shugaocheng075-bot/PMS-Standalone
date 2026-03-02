namespace PMS.Application.Models.Handover;

public class HandoverSummaryDto
{
    public int PendingCount { get; set; }
    public int EmailSentCount { get; set; }
    public int InProgressCount { get; set; }
    public int CompletedCount { get; set; }
    public int Total { get; set; }
}
