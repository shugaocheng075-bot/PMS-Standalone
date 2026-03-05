namespace PMS.Domain.Entities;

/// <summary>
/// 月报实体
/// </summary>
public class MonthlyReportEntity
{
    public long Id { get; set; }

    /// <summary>所属医院</summary>
    public string HospitalName { get; set; } = string.Empty;

    /// <summary>报告月份 yyyy-MM</summary>
    public string ReportMonth { get; set; } = string.Empty;

    /// <summary>提交人</summary>
    public string SubmittedBy { get; set; } = string.Empty;

    /// <summary>月报标题</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>月报内容/摘要</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>附件路径列表（JSON 序列化存储）</summary>
    public List<string> Attachments { get; set; } = [];

    /// <summary>状态：draft / submitted / approved</summary>
    public string Status { get; set; } = "draft";

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
