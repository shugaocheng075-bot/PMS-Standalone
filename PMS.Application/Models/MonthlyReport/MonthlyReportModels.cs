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
    public string GroupName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Attachments { get; set; } = [];
    public string Status { get; set; } = "draft";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // ─── 结构化数据段 ───
    public int TeamTotal { get; set; }
    public string TeamOnsiteJson { get; set; } = "[]";
    public string TeamSummaryJson { get; set; } = "{}";
    public string ProjectOverviewJson { get; set; } = "{}";
    public string PerCapitaMetricsJson { get; set; } = "{}";
    public string HandoverItemsJson { get; set; } = "[]";
    public decimal WeeklyReportRate { get; set; }
    public decimal MonthlyReportRate { get; set; }
    public string MajorDemandAcceptanceJson { get; set; } = "[]";
    public string InspectionRecordsJson { get; set; } = "[]";
    public string AnnualServiceReportsJson { get; set; } = "[]";
    public string IncidentsJson { get; set; } = "[]";
    public string NextMonthInspectionPlanJson { get; set; } = "[]";
    public string NextMonthAnnualReportPlanJson { get; set; } = "[]";
    public string NextMonthOtherPlanJson { get; set; } = "[]";
}

/// <summary>
/// 创建/更新月报请求 DTO
/// </summary>
public class MonthlyReportUpsertDto
{
    public string HospitalName { get; set; } = string.Empty;
    public string ReportMonth { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Attachments { get; set; } = [];
    public string? Status { get; set; }

    // 结构化段
    public int? TeamTotal { get; set; }
    public string? TeamOnsiteJson { get; set; }
    public string? TeamSummaryJson { get; set; }
    public string? ProjectOverviewJson { get; set; }
    public string? PerCapitaMetricsJson { get; set; }
    public string? HandoverItemsJson { get; set; }
    public decimal? WeeklyReportRate { get; set; }
    public decimal? MonthlyReportRate { get; set; }
    public string? MajorDemandAcceptanceJson { get; set; }
    public string? InspectionRecordsJson { get; set; }
    public string? AnnualServiceReportsJson { get; set; }
    public string? IncidentsJson { get; set; }
    public string? NextMonthInspectionPlanJson { get; set; }
    public string? NextMonthAnnualReportPlanJson { get; set; }
    public string? NextMonthOtherPlanJson { get; set; }
}

/// <summary>
/// 月报查询参数
/// </summary>
public class MonthlyReportQuery
{
    public string? HospitalName { get; set; }
    public string? ReportMonth { get; set; }
    public string? SubmittedBy { get; set; }
    public string? GroupName { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
}

/// <summary>
/// 月报自动生成请求 — 指定月份，系统从各模块聚合数据
/// </summary>
public class MonthlyReportGenerateRequest
{
    public string ReportMonth { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string? SupervisorName { get; set; }
    public string SubmittedBy { get; set; } = string.Empty;
    public MonthlyReportTeamDataSourceInput? TeamDataSource { get; set; }
}

public class MonthlyReportTeamDataSourceInput
{
    public int? AuthorizedHeadcount { get; set; }
    public int? CentralStandardServiceCount { get; set; }
    public int? CentralOnsiteCount { get; set; }
    public int? NorthwestStandardServiceCount { get; set; }
    public int? NorthwestOnsiteCount { get; set; }
    public int? SickLeaveCount { get; set; }
    public int? PersonalLeaveCount { get; set; }
    public int? OtherSpecialCount { get; set; }
    public string? OtherSpecialRemark { get; set; }
    public int? ExcludedCustomerCount { get; set; }
    public int? ExcludedProjectCount { get; set; }
}

public class MonthlyReportSourcePreviewDto
{
    public string ReportMonth { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string SupervisorName { get; set; } = string.Empty;
    public MonthlyReportSourceTeamSummaryDto TeamSummary { get; set; } = new();
    public MonthlyReportSourceProjectSummaryDto ProjectSummary { get; set; } = new();
    public MonthlyReportSourcePerCapitaDto PerCapitaMetrics { get; set; } = new();
    public List<MonthlyReportSourcePersonnelItemDto> PersonnelItems { get; set; } = [];
    public List<MonthlyReportSourceOnsiteDeductionDto> OnsiteDeductionItems { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

public class MonthlyReportSourceTeamSummaryDto
{
    public int AuthorizedHeadcount { get; set; }
    public int TotalHeadcount { get; set; }
    public int OnsiteCount { get; set; }
    public int RemoteCount { get; set; }
    public int CentralStandardServiceCount { get; set; }
    public int CentralOnsiteCount { get; set; }
    public int NorthwestStandardServiceCount { get; set; }
    public int NorthwestOnsiteCount { get; set; }
    public int UnmatchedPersonnelCount { get; set; }
    public int SickLeaveCount { get; set; }
    public int PersonalLeaveCount { get; set; }
    public int OtherSpecialCount { get; set; }
    public string OtherSpecialRemark { get; set; } = string.Empty;
}

public class MonthlyReportSourceProjectSummaryDto
{
    public int TotalCustomerCount { get; set; }
    public int TotalProductCount { get; set; }
    public int CentralCustomerCount { get; set; }
    public int CentralProductCount { get; set; }
    public int NorthwestCustomerCount { get; set; }
    public int NorthwestProductCount { get; set; }
    public int OnsiteDeductedCustomerCount { get; set; }
    public int OnsiteDeductedProductCount { get; set; }
}

public class MonthlyReportSourcePerCapitaDto
{
    public MonthlyReportSourceMetricBlockDto AllPersonnelAverage { get; set; } = new();
    public MonthlyReportSourceMetricBlockDto ExcludeOnsiteAverage { get; set; } = new();
}

public class MonthlyReportSourceMetricBlockDto
{
    public int Headcount { get; set; }
    public int CustomerCount { get; set; }
    public int ProductCount { get; set; }
    public double CustomerPerPerson { get; set; }
    public double ProductPerPerson { get; set; }
}

public class MonthlyReportSourcePersonnelItemDto
{
    public int PersonnelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string RoleType { get; set; } = string.Empty;
    public string SystemRole { get; set; } = string.Empty;
    public string ServiceMode { get; set; } = string.Empty;
    public bool IsOnsite { get; set; }
    public string SupervisorName { get; set; } = string.Empty;
    public int ResponsibilityHospitalCount { get; set; }
    public int ResponsibilityProductCount { get; set; }
    public List<string> HospitalNames { get; set; } = [];
    public List<string> ProductNames { get; set; } = [];
    public string MatchingStatus { get; set; } = string.Empty;
}

public class MonthlyReportSourceOnsiteDeductionDto
{
    public int PersonnelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DeductedHospitalCount { get; set; }
    public int DeductedProductCount { get; set; }
    public List<string> HospitalNames { get; set; } = [];
    public List<string> ProductNames { get; set; } = [];
}
