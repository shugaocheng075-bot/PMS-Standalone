namespace PMS.Application.Models.Operations;

public class OperationsTaskQuery
{
    public string? Source { get; set; }
    public string? Level { get; set; }
    public string? Owner { get; set; }
    public string? HospitalName { get; set; }
    public string? Keyword { get; set; }
    public string ScopeType { get; set; } = "own";
    public List<string> AccessibleHospitalNames { get; set; } = [];
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
}

public class OperationsTaskItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string HospitalName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string? DueAt { get; set; }
    public int OverdueDays { get; set; }
    public string RelatedPath { get; set; } = string.Empty;
    public Dictionary<string, string> RelatedQuery { get; set; } = [];
}

public class OperationsTaskSummaryDto
{
    public int Total { get; set; }
    public int Severe { get; set; }
    public int Warning { get; set; }
    public int Reminder { get; set; }
    public int Overdue { get; set; }
}
