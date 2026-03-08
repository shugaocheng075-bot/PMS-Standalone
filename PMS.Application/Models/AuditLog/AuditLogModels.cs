namespace PMS.Application.Models.AuditLog;

public class AuditLogItemDto
{
    public long Id { get; set; }
    public string Operator { get; set; } = string.Empty;
    public long OperatorId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AuditLogQuery
{
    public string? Action { get; set; }
    public string? Module { get; set; }
    public string? Operator { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
}

public class AuditLogSummaryDto
{
    public int Total { get; set; }
    public int TodayCount { get; set; }
    public Dictionary<string, int> ActionCounts { get; set; } = new();
}
