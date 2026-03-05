namespace PMS.Application.Models.MonthlyReport;

/// <summary>
/// 月报列表项 DTO
/// </summary>
public class MonthlyReportItemDto
{
    public long Id { get; set; }
    public string HospitalName { get; set; } = string.Empty;
    public string ReportMonth { get; set; } = string.Empty;
    public string SubmittedBy { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Attachments { get; set; } = [];
    public string Status { get; set; } = "draft";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 创建/更新月报请求 DTO
/// </summary>
public class MonthlyReportUpsertDto
{
    public string HospitalName { get; set; } = string.Empty;
    public string ReportMonth { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Attachments { get; set; } = [];
    public string? Status { get; set; }
}

/// <summary>
/// 月报查询参数
/// </summary>
public class MonthlyReportQuery
{
    public string? HospitalName { get; set; }
    public string? ReportMonth { get; set; }
    public string? SubmittedBy { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
}
