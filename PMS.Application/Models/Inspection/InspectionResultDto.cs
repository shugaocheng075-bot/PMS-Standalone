namespace PMS.Application.Models.Inspection;

/// <summary>
/// 从 SystemAuditTool 推送过来的巡检结果。
/// </summary>
public class InspectionResultDto
{
    public long Id { get; set; }

    /// <summary>医院名称（关联键）</summary>
    public string HospitalName { get; set; } = string.Empty;

    /// <summary>产品名称（关联键）</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>巡检时间</summary>
    public DateTime InspectedAt { get; set; }

    /// <summary>巡检工程师</summary>
    public string Inspector { get; set; } = string.Empty;

    /// <summary>巡检是否成功</summary>
    public bool Success { get; set; }

    /// <summary>失败时的错误信息</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>巡检耗时（秒）</summary>
    public double DurationSeconds { get; set; }

    // ─── 风险统计 ───

    /// <summary>风险项总数</summary>
    public int RiskCount { get; set; }

    /// <summary>警告数</summary>
    public int WarningCount { get; set; }

    /// <summary>严重数</summary>
    public int CriticalCount { get; set; }

    /// <summary>整体健康评级：Good / Warning / Critical</summary>
    public string HealthLevel { get; set; } = "Good";

    /// <summary>综合评分 0-100</summary>
    public int OverallScore { get; set; } = 100;

    // ─── 数据库摘要 ───

    /// <summary>数据库版本</summary>
    public string? DatabaseVersion { get; set; }

    /// <summary>存储使用率 %</summary>
    public double? StorageUsedPercent { get; set; }

    /// <summary>表空间使用率 %</summary>
    public double? TablespaceUsedPercent { get; set; }

    /// <summary>备份状态描述</summary>
    public string? BackupStatus { get; set; }

    /// <summary>容量预测：预计多少天空间耗尽</summary>
    public int? DaysToFull { get; set; }

    // ─── 重点风险 ───

    /// <summary>TOP 风险条目</summary>
    public List<InspectionRiskItemDto> TopRisks { get; set; } = [];

    /// <summary>审核状态：pending/approved/rejected</summary>
    public string ReviewStatus { get; set; } = "pending";

    /// <summary>审核人</summary>
    public string? ReviewedBy { get; set; }

    /// <summary>审核时间</summary>
    public DateTime? ReviewedAt { get; set; }
}

/// <summary>
/// 单条风险项。
/// </summary>
public class InspectionRiskItemDto
{
    /// <summary>级别：Normal / Warning / Critical</summary>
    public string Level { get; set; } = "Normal";

    /// <summary>分类：Performance / Resource / Lock / Backup / Security / Service / Availability</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>标题</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>描述</summary>
    public string? Description { get; set; }

    /// <summary>当前值</summary>
    public string? CurrentValue { get; set; }

    /// <summary>阈值</summary>
    public string? ThresholdValue { get; set; }
}
