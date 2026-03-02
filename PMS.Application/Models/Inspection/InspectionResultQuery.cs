namespace PMS.Application.Models.Inspection;

/// <summary>
/// 巡检结果查询参数。
/// </summary>
public class InspectionResultQuery
{
    public string? HospitalName { get; set; }
    public string? ProductName { get; set; }
    public string? Inspector { get; set; }

    /// <summary>健康等级筛选: Good / Warning / Critical</summary>
    public string? HealthLevel { get; set; }

    /// <summary>开始日期</summary>
    public DateTime? From { get; set; }

    /// <summary>截止日期</summary>
    public DateTime? To { get; set; }

    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
}
